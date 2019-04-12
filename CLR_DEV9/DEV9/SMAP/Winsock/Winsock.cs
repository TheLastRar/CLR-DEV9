using CLRDEV9.DEV9.SMAP.Data;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using CLRDEV9.DEV9.SMAP.Winsock.Sessions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock
{
    sealed class Winsock : NetAdapter
    {
        ConcurrentQueue<NetPacket> vRecBuffer = new ConcurrentQueue<NetPacket>(); //Non IP packets
        UDP_DHCPSession dhcpServer;
        UDP_DNSSession dnsServer;
        IPAddress adapterIP = IPAddress.Any;
        //List<Session> Connections = new List<Session>();
        //object sentry = new object();
        object sendSentry = new object();
        object recvSentry = new object();

        ConcurrentDictionary<ConnectionKey, Session> connections = new ConcurrentDictionary<ConnectionKey, Session>();

        Dictionary<ushort, UDPFixedPort> fixedUDPPorts = new Dictionary<ushort, UDPFixedPort>();

        static public List<string[]> GetAdapters()
        {
            //Add Auto
            List<string[]> names = new List<string[]> { new string[] { "Auto", "Autoselected adapter", "Auto" }};

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    UnicastIPAddressInformationCollection ipInfo = adapter.GetIPProperties().UnicastAddresses;
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    foreach (UnicastIPAddressInformation ipAddressInfo in ipInfo)
                    {
                        if (ipAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            //return adapter
                            names.Add(new string[] { adapter.Name, adapter.Description, adapter.Id });
                            break;
                        }
                    }
                }
            }
            return names;
        }

        public Winsock(DEV9_State parDev9, string parDevice)
            : base(parDev9)
        {
            //Add allways on connections
            byte[] dns1 = null;
            byte[] dns2 = null;
            Dictionary<string, byte[]> hosts = new Dictionary<string, byte[]>();
            NetworkInterface adapter = null;

            if (parDevice != "Auto")
            {
                adapter = GetAdapterFromGuid(parDevice);
                if (adapter == null)
                {
                    //System.Windows.Forms.MessageBox.Show("Failed to GetAdapter");
                    throw new NullReferenceException("Failed to GetAdapter");
                }
                adapterIP = (from ip in adapter.GetIPProperties().UnicastAddresses
                             where ip.Address.AddressFamily == AddressFamily.InterNetwork
                             select ip.Address).SingleOrDefault();
            }
            else
            {
                adapter = UDP_DHCPSession.AutoAdapter();
            }

            if (adapter == null)
            {
                throw new NullReferenceException("Auto Selection Failed, Check You Connection or Manually Specify Adapter");
            }

            if (!DEV9Header.config.SocketConnectionSettings.AutoDNS1)
            {
                dns1 = IPAddress.Parse(DEV9Header.config.SocketConnectionSettings.DNS1).GetAddressBytes();
            }
            if (!DEV9Header.config.SocketConnectionSettings.AutoDNS2)
            {
                dns2 = IPAddress.Parse(DEV9Header.config.SocketConnectionSettings.DNS2).GetAddressBytes();
            }

            foreach (Config.ConfigHost host in DEV9Header.config.Hosts)
            {
                if (host.Enabled)
                    hosts.Add(host.URL, IPAddress.Parse(host.IP).GetAddressBytes());
            }

            //DHCP emulated server
            ConnectionKey dhcpKey = new ConnectionKey();
            dhcpKey.Protocol = (byte)IPType.UDP;
            dhcpKey.SRVPort = 67;

            dhcpServer = new UDP_DHCPSession(dhcpKey, adapter, dns1, dns2, DEV9Header.config.SocketConnectionSettings.LANMode);
            dhcpServer.ConnectionClosedEvent += HandleConnectionClosed;

            dhcpServer.SourceIP = new byte[] { 255, 255, 255, 255 };
            dhcpServer.DestIP = DefaultDHCPConfig.DHCP_IP;

            if (!connections.TryAdd(dhcpServer.Key, dhcpServer)) { throw new Exception("Connection Add Failed"); }
            //DNS emulated server
            ConnectionKey dnsKey = new ConnectionKey();
            dnsKey.Protocol = (byte)IPType.UDP;
            dnsKey.SRVPort = 53;

            dnsServer = new UDP_DNSSession(dnsKey, hosts);
            dnsServer.ConnectionClosedEvent += HandleConnectionClosed;
            dnsServer.SourceIP = dhcpServer.PS2IP;
            dnsServer.DestIP = DefaultDHCPConfig.DHCP_IP;

            if (!connections.TryAdd(dnsServer.Key, dnsServer)) { throw new Exception("Connection Add Failed"); }
            //

            foreach (Config.ConfigIncomingPort port in
                DEV9Header.config.SocketConnectionSettings.IncomingPorts)
            {
                if (!port.Enabled)
                {
                    continue;
                }

                ConnectionKey Key = new ConnectionKey();
                Key.Protocol = (byte)port.Protocol;
                Key.PS2Port = port.Port;
                Key.SRVPort = port.Port;

                Session s = null;

                if (port.Protocol == IPType.UDP)
                {
                    //avoid duplicates
                    if (fixedUDPPorts.ContainsKey(port.Port))
                    {
                        continue;
                    }

                    ConnectionKey fKey = new ConnectionKey();
                    fKey.Protocol = (byte)IPType.UDP;
                    fKey.PS2Port = port.Port;
                    fKey.SRVPort = 0;

                    UDPFixedPort fPort = new UDPFixedPort(fKey, adapterIP, port.Port);
                    fPort.ConnectionClosedEvent += HandleConnectionClosed;

                    fPort.DestIP = new byte[] { 0, 0, 0, 0 };
                    fPort.SourceIP = dhcpServer.PS2IP;

                    if (!connections.TryAdd(fPort.Key, fPort))
                    {
                        fPort.Dispose();
                        throw new Exception("Connection Add Failed");
                    }

                    fixedUDPPorts.Add(port.Port, fPort);

                    s = fPort.NewListenSession(Key);
                }

                s.ConnectionClosedEvent += HandleConnectionClosed;

                s.SourceIP = dhcpServer.PS2IP;
                s.DestIP = dhcpServer.Broadcast;

                if (!connections.TryAdd(s.Key, s))
                {
                    s.Dispose();
                    throw new Exception("Connection Add Failed");
                }
            }

            SetMAC(null);
            SetMAC(adapter.GetPhysicalAddress().GetAddressBytes());
        }

        public override bool Blocks()
        {
            return false;	//we use blocking io
        }
        public override bool IsInitialised()
        {
            return true;
        }

        //gets a packet.rv :true success
        public override bool Recv(ref NetPacket pkt)
        {
            lock (recvSentry)
            {
                //return false;
                Log_Verb("Reciving NetPacket");
                bool result = false;

                if (!vRecBuffer.TryDequeue(out pkt))
                {
                    pkt = null;
                    ConnectionKey[] keys = connections.Keys.ToArray();
                    foreach (ConnectionKey key in keys)
                    {
                        IPPayload pl;

                        if (!connections.TryGetValue(key, out Session session)) { continue; }
                        pl = session.Recv();
                        if (!(pl == null))
                        {
                            IPPacket ipPkt = new IPPacket(pl);
                            ipPkt.DestinationIP = session.SourceIP;
                            ipPkt.SourceIP = session.DestIP;
                            EthernetFrame ef = new EthernetFrame(ipPkt);
                            ef.SourceMAC = virturalDHCPMAC;
                            ef.DestinationMAC = ps2MAC;
                            ef.Protocol = (UInt16)EtherFrameType.IPv4;
                            pkt = ef.CreatePacket();
                            result = true;
                            break;
                        }
                    }
                }
                else
                {
                    result = true;
                }

                if (result)
                {
                    return true;
                }
                else
                    return false;
            }
        }
        //sends the packet and deletes it when done (if successful).rv :true success
        public override bool Send(NetPacket pkt)
        {
            lock (sendSentry)
            {
                Log_Verb("Sending NetPacket");
                bool result = false;

                EthernetFrame ef = new EthernetFrame(pkt);

                switch (ef.Protocol)
                {
                    case (int)EtherFrameType.NULL:
                        //Adapter Reset

                        //lock (sentry)
                        //{
                        Log_Verb("Reset " + connections.Count + " Connections");
                        ConnectionKey[] keys = connections.Keys.ToArray();
                        foreach (ConnectionKey key in keys)
                        {
                            if (!connections.TryGetValue(key, out Session session)) { continue; }
                            session.Reset();
                        }
                        //}
                        break;
                    case (int)EtherFrameType.IPv4:
                        result = SendIP((IPPacket)ef.Payload);
                        break;
                    #region "ARP"
                    case (int)EtherFrameType.ARP:
                        Log_Verb("ARP");
                        ARPPacket arpPkt = ((ARPPacket)ef.Payload);

                        if (arpPkt.Protocol == (UInt16)EtherFrameType.IPv4)
                        {
                            if (arpPkt.OP == 1) //ARP request
                            {
                                byte[] gateway;
                                //lock (sentry)
                                //{
                                    gateway = dhcpServer.Gateway;
                                //}
                                //if (Utils.memcmp(arpPkt.TargetProtocolAddress, 0, gateway, 0, 4))
                                if (!Utils.memcmp(arpPkt.TargetProtocolAddress, 0, dhcpServer.PS2IP, 0, 4))
                                //it's trying to resolve the virtual gateway's mac addr
                                {
                                    ARPPacket arpRet = new ARPPacket();
                                    arpRet.TargetHardwareAddress = arpPkt.SenderHardwareAddress;
                                    arpRet.SenderHardwareAddress = virturalDHCPMAC;
                                    arpRet.TargetProtocolAddress = arpPkt.SenderProtocolAddress;
                                    arpRet.SenderProtocolAddress = arpPkt.TargetProtocolAddress;
                                    arpRet.OP = 2;
                                    arpRet.Protocol = arpPkt.Protocol;

                                    EthernetFrame retARP = new EthernetFrame(arpRet);
                                    retARP.DestinationMAC = ps2MAC;
                                    retARP.SourceMAC = virturalDHCPMAC;
                                    retARP.Protocol = (UInt16)EtherFrameType.ARP;
                                    vRecBuffer.Enqueue(retARP.CreatePacket());
                                    break;
                                }
                            }
                        }

                        result = true;
                        break;
                    #endregion
                    case 0x0081:
                        Log_Error("VLAN-tagged frame (IEEE 802.1Q)");
                        throw new NotImplementedException();
                    //break;
                    default:
                        Log_Error("Unkown EtherframeType " + ef.Protocol.ToString("X4"));
                        break;
                }

                return result;
            }
        }

        public bool SendIP(IPPacket ipPkt)
        {
            //TODO Optimise Checksum for implace checksumming
            if (ipPkt.VerifyCheckSum() == false)
            {
                Log_Error("IP packet with bad CSUM");
                return false;
            }
            if (ipPkt.Payload.VerifyCheckSum(ipPkt.SourceIP, ipPkt.DestinationIP) == false)
            {
                Log_Error("IP packet with bad Payload CSUM");
                return false;
            }

            ConnectionKey Key = new ConnectionKey();
            Key.IP0 = ipPkt.DestinationIP[0]; Key.IP1 = ipPkt.DestinationIP[1]; Key.IP2 = ipPkt.DestinationIP[2]; Key.IP3 = ipPkt.DestinationIP[3];
            Key.Protocol = ipPkt.Protocol;

            switch (ipPkt.Protocol) //(Prase Payload)
            {
                case (byte)IPType.ICMP:
                    return SendICMP(Key, ipPkt);
                case (byte)IPType.IGMP:
                    return SendIGMP(Key, ipPkt);
                case (byte)IPType.TCP:
                    return SendTCP(Key, ipPkt);
                case (byte)IPType.UDP:
                    return SendUDP(Key, ipPkt);
                default:
                    //Log_Error("Unkown Protocol");
                    throw new NotImplementedException("Unkown IPv4 Protocol " + ipPkt.Protocol.ToString("X2"));
                    //return false;
            }
        }

        public bool SendICMP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("ICMP");
            //lock (sentry)
            //{
            int res = SendFromConnection(Key, ipPkt);
            if (res == 1)
                return true;
            else if (res == 0)
                return false;
            else
            {
                Log_Verb("Creating New Connection with key " + Key);
                ICMPSession s = new ICMPSession(Key, connections);
                s.ConnectionClosedEvent += HandleConnectionClosed;
                s.DestIP = ipPkt.DestinationIP;
                s.SourceIP = dhcpServer.PS2IP;
                if (!connections.TryAdd(Key, s)) { throw new Exception("Connection Add Failed"); }
                return s.Send(ipPkt.Payload);
            }
            //}
        }
        public bool SendIGMP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("IGMP");
            //lock (sentry)
            //{
            int res = SendFromConnection(Key, ipPkt);
            if (res == 1)
                return true;
            else if (res == 0)
                return false;
            else
            {
                Log_Verb("Creating New Connection with key " + Key);
                IGMPSession s = new IGMPSession(Key, adapterIP);
                s.ConnectionClosedEvent += HandleConnectionClosed;
                s.DestIP = ipPkt.DestinationIP;
                s.SourceIP = dhcpServer.PS2IP;
                if (!connections.TryAdd(Key, s)) { throw new Exception("Connection Add Failed"); }
                return s.Send(ipPkt.Payload);
            }
            //}
        }
        public bool SendTCP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("TCP");
            TCP tcp = (TCP)ipPkt.Payload;

            Key.PS2Port = tcp.SourcePort; Key.SRVPort = tcp.DestinationPort;

            if (tcp.DestinationPort == 53 && Utils.memcmp(ipPkt.DestinationIP, 0, DefaultDHCPConfig.DHCP_IP, 0, 4))
            { //DNS
                throw new NotImplementedException("DNS over TCP not supported");
            }

            int res = SendFromConnection(Key, ipPkt);
            if (res == 1)
                return true;
            else if (res == 0)
                return false;
            else
            {
                Log_Verb("Creating New Connection with key " + Key);
                Log_Info("Creating New TCP Connection with Dest Port " + tcp.DestinationPort);
                TCPSession s = new TCPSession(Key, adapterIP);
                s.ConnectionClosedEvent += HandleConnectionClosed;
                s.DestIP = ipPkt.DestinationIP;
                s.SourceIP = dhcpServer.PS2IP;
                if (!connections.TryAdd(Key, s)) { throw new Exception("Connection Add Failed"); }
                return s.Send(ipPkt.Payload);
            }
        }
        public bool SendUDP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("UDP");
            UDP udp = (UDP)ipPkt.Payload;

            Key.PS2Port = udp.SourcePort; Key.SRVPort = udp.DestinationPort;

            if (udp.DestinationPort == 67)
            { //DHCP
                return dhcpServer.Send(ipPkt.Payload);
            }

            if (udp.DestinationPort == 53 && Utils.memcmp(ipPkt.DestinationIP, 0, DefaultDHCPConfig.DHCP_IP, 0, 4))
            { //DNS
                return dnsServer.Send(ipPkt.Payload);
            }

            int res = SendFromConnection(Key, ipPkt);
            if (res == 1)
                return true;
            else if (res == 0)
                return false;
            else
            {
                Log_Verb("Creating New Connection with key " + Key);
                Log_Info("Creating New UDP Connection with Dest Port " + udp.DestinationPort);
                UDPSession s;
                if (udp.SourcePort == udp.DestinationPort || //Used for LAN games that assume the destination port
                    Utils.memcmp(ipPkt.DestinationIP, 0, dhcpServer.Broadcast, 0, 4) ||
                    Utils.memcmp(ipPkt.DestinationIP, 0, dhcpServer.LimitedBroadcast, 0, 4)) //Broadcast packets
                {
                    //Limit of one udpclient per local port
                    //need to reuse the udpclient
                    UDPFixedPort fPort;
                    if (fixedUDPPorts.ContainsKey(udp.SourcePort))
                    {
                        Log_Verb("Using Existing UDPFixedPort");
                        fPort = fixedUDPPorts[udp.SourcePort];
                    }
                    else
                    {
                        ConnectionKey fKey = new ConnectionKey();
                        fKey.Protocol = (byte)IPType.UDP;
                        fKey.PS2Port = udp.SourcePort;
                        fKey.SRVPort = 0;

                        Log_Verb("Creating New UDPFixedPort with key " + fKey);
                        Log_Info("Creating New UDPFixedPort with Port " + udp.SourcePort);

                        fPort = new UDPFixedPort(fKey, adapterIP, udp.SourcePort);
                        fPort.ConnectionClosedEvent += HandleConnectionClosed;

                        fPort.DestIP = new byte[] { 0, 0, 0, 0 };
                        fPort.SourceIP = dhcpServer.PS2IP;

                        if (!connections.TryAdd(fKey, fPort))
                        {
                            fPort.Dispose();
                            throw new Exception("Connection Add Failed");
                        }

                        fixedUDPPorts.Add(udp.SourcePort, fPort);
                    }
                    s = fPort.NewClientSession(Key, Utils.memcmp(ipPkt.DestinationIP, 0, dhcpServer.Broadcast, 0, 4) |
                                                    Utils.memcmp(ipPkt.DestinationIP, 0, dhcpServer.LimitedBroadcast, 0, 4));
                }
                else
                {
                    s = new UDPSession(Key, adapterIP);
                }
                s.ConnectionClosedEvent += HandleConnectionClosed;
                s.DestIP = ipPkt.DestinationIP;
                s.SourceIP = dhcpServer.PS2IP;
                if (!connections.TryAdd(Key, s)) { throw new Exception("Connection Add Failed"); }
                return s.Send(ipPkt.Payload);
            }
        }

        public int SendFromConnection(ConnectionKey Key, IPPacket ipPkt)
        {
            connections.TryGetValue(Key, out Session s);
            if (s != null)
            {
                return s.Send(ipPkt.Payload) ? 1 : 0;
            }
            else
                return -1;
        }

        //Event must only be raised once per connection
        public void HandleConnectionClosed(object sender, EventArgs e)
        {
            Session s = (Session)sender;

            s.ConnectionClosedEvent -= HandleConnectionClosed;
            lock (sendSentry)
            {
                lock (recvSentry)
                {
                    //deadConnections.Enqueue(s);
                    connections.TryRemove(s.Key, out Session dummy);
                    s.Dispose();
                }
            }
            Log_Info("Closed Dead Connection");
        }

        public override void Close()
        {
            //Rx thread still running in close
            //wait untill Rx thread stopped before
            //disposing winsock connections
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(true);
            //TODO close all open connections
            if (disposing)
            {
                //lock (sentry)
                lock (sendSentry)
                    lock(recvSentry)
                        {
                            Log_Verb("Closing " + connections.Count + " Connections");
                            foreach (ConnectionKey key in connections.Keys) //ToDo better multi-connection stuff
                            {
                                connections[key].Dispose();
                            }
                            while (vRecBuffer.TryDequeue(out NetPacket p)) { }
                            connections.Clear();
                            fixedUDPPorts.Clear();

                            dhcpServer.Dispose();
                            dnsServer.Dispose();
                        }
            }
        }

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Winsock, str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Winsock, str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Winsock, str);
        }
    }
}
