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
    struct ConnectionKey
    {
        public byte IP0;
        public byte IP1;
        public byte IP2;
        public byte IP3;
        public byte Protocol;
        public ushort PS2Port;
        public ushort SRVPort;

        public override bool Equals(object obj)
        {
            return obj is ConnectionKey && this == (ConnectionKey)obj;
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + IP0.GetHashCode();
                hash = hash * 23 + IP1.GetHashCode();
                hash = hash * 23 + IP2.GetHashCode();
                hash = hash * 23 + IP3.GetHashCode();
                hash = hash * 23 + Protocol.GetHashCode();
                hash = hash * 23 + PS2Port.GetHashCode();
                hash = hash * 23 + SRVPort.GetHashCode();
                return hash;
            }
        }
        public static bool operator ==(ConnectionKey x, ConnectionKey y)
        {
            return x.IP0 == y.IP0 &&
                x.IP1 == y.IP1 &&
                x.IP2 == y.IP2 &&
                x.IP3 == y.IP3 &&
                x.Protocol == y.Protocol &&
                x.PS2Port == y.PS2Port &&
                x.SRVPort == y.SRVPort;
        }
        public static bool operator !=(ConnectionKey x, ConnectionKey y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return (IP0 + "." + IP1 + "." + IP2 + "." + IP3 +
                    "-" + Protocol + "-" + PS2Port + ":" + SRVPort);
        }
    }

    sealed class Winsock : NetAdapter
    {
        List<NetPacket> vRecBuffer = new List<NetPacket>(); //Non IP packets
        UDP_DHCPsession dhcpServer;
        IPAddress adapterIP = IPAddress.Any;
        //List<Session> Connections = new List<Session>();
        object sentry = new object();

        Dictionary<ConnectionKey, Session> connections = new Dictionary<ConnectionKey, Session>();
        ConcurrentQueue<Session> deadConnections = new ConcurrentQueue<Session>();

        static public List<string[]> GetAdapters()
        {
            //Add Auto
            List<string[]> names = new List<string[]>();
            names.Add(new string[] { "Auto", "Autoselected adapter", "Auto" });

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

            if (!DEV9Header.config.SocketConnectionSettings.AutoDNS1)
            {
                dns1 = IPAddress.Parse(DEV9Header.config.SocketConnectionSettings.DNS1).GetAddressBytes();
            }
            if (!DEV9Header.config.SocketConnectionSettings.AutoDNS2)
            {
                dns2 = IPAddress.Parse(DEV9Header.config.SocketConnectionSettings.DNS2).GetAddressBytes();
            }

            ConnectionKey dhcpKey = new ConnectionKey();
            dhcpKey.Protocol = (byte)IPType.UDP;
            dhcpKey.SRVPort = 67;

            dhcpServer = new UDP_DHCPsession(dhcpKey, adapter, dns1, dns2);
            dhcpServer.ConnectionClosedEvent += HandleConnectionClosed;

            dhcpServer.SourceIP = new byte[] { 255, 255, 255, 255 };
            dhcpServer.DestIP = DefaultDHCPConfig.DHCP_IP;

            connections.Add(dhcpKey, dhcpServer);
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
            //return false;
            Log_Verb("Reciving NetPacket");
            bool result = false;

            if (vRecBuffer.Count == 0)
            {
                //List<ConnectionKey> DeadConnections = new List<ConnectionKey>();
                lock (sentry)
                {
                    Session[] sessions = connections.Values.ToArray();
                    foreach (Session session in sessions) //ToDo: better multi-connection stuff?
                    {
                        IPPayload pl;
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

                    Session s;
                    while (deadConnections.TryDequeue(out s))
                    {
                        connections.Remove(s.Key);
                        s.Dispose();
                        //Log_Error("Closed Dead Connection");
                    }
                }
            }
            else
            {
                pkt = vRecBuffer[0];
                vRecBuffer.RemoveAt(0);
                result = true;
            }

            if (result)
            {
                return true;
            }
            else
                return false;
        }
        //sends the packet and deletes it when done (if successful).rv :true success
        public override bool Send(NetPacket pkt)
        {
            Log_Verb("Sending NetPacket");
            bool result = false;

            EthernetFrame ef = new EthernetFrame(pkt);

            switch (ef.Protocol)
            {
                case (int)EtherFrameType.NULL:
                    //Adapter Reset

                    lock (sentry)
                    {
                        Log_Verb("Reset " + connections.Count + " Connections");
                        Session[] sessions = connections.Values.ToArray();
                        foreach (Session session in sessions)
                        {
                            //connections[key].Reset();
                            session.Reset();
                        }
                    }
                    break;
                case (int)EtherFrameType.IPv4:
                    result = SendIP((IPPacket)ef.Payload);
                    break;
                #region "ARP"
                case (int)EtherFrameType.ARP:
                    Log_Verb("ARP (Ignoring)");
                    ARPPacket arpPkt = ((ARPPacket)ef.Payload);

                    ////Detect ARP Packet Types
                    //if (Utils.memcmp(arppkt.SenderProtocolAddress, 0, new byte[] { 0, 0, 0, 0 }, 0, 4))
                    //{
                    //    WriteLine("ARP Probe"); //(Who has my IP?)
                    //    break;
                    //}
                    //if (Utils.memcmp(arppkt.SenderProtocolAddress, 0, arppkt.TargetProtocolAddress, 0, 4))
                    //{
                    //    if (Utils.memcmp(arppkt.TargetHardwareAddress, 0, new byte[] { 0, 0, 0, 0, 0, 0 }, 0, 6) & arppkt.OP == 1)
                    //    {
                    //        WriteLine("ARP Announcement Type 1");
                    //        break;
                    //    }
                    //    if (Utils.memcmp(arppkt.SenderHardwareAddress, 0, arppkt.TargetHardwareAddress, 0, 6) & arppkt.OP == 2)
                    //    {
                    //        WriteLine("ARP Announcement Type 2");
                    //        break;
                    //    }
                    //}

                    if (arpPkt.OP == 1) //ARP request
                    {
                        byte[] gateway;
                        lock (sentry)
                        {
                            gateway = dhcpServer.Gateway;
                        }
                        if (Utils.memcmp(arpPkt.TargetProtocolAddress, 0, gateway, 0, 4))
                        //it's trying to resolve the virtual gateway's mac addr
                        {
                            Log_Verb("ARP Attempt to Resolve Gateway Mac");
                            arpPkt.TargetHardwareAddress = arpPkt.SenderHardwareAddress;
                            arpPkt.SenderHardwareAddress = virturalDHCPMAC;
                            arpPkt.TargetProtocolAddress = arpPkt.SenderProtocolAddress;
                            arpPkt.SenderProtocolAddress = gateway;
                            arpPkt.OP = 2;

                            EthernetFrame retARP = new EthernetFrame(arpPkt);
                            retARP.DestinationMAC = ps2MAC;
                            retARP.SourceMAC = virturalDHCPMAC;
                            retARP.Protocol = (UInt16)EtherFrameType.ARP;
                            vRecBuffer.Add(retARP.CreatePacket());
                            break;
                        }
                    }
                    //Error.WriteLine("Unhandled ARP packet");

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
                    return SendIMCP(Key, ipPkt);
                case (byte)IPType.TCP:
                    return SendTCP(Key, ipPkt);
                case (byte)IPType.UDP:
                    return SendUDP(Key, ipPkt);
                default:
                    Log_Error("Unkown Protocol");
                    //throw new NotImplementedException();
                    return false;
            }
        }

        public bool SendIMCP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("ICMP");
            lock (sentry)
            {
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
                    connections.Add(Key, s);
                    return s.Send(ipPkt.Payload);
                }
            }
        }
        public bool SendTCP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("TCP");
            TCP tcp = (TCP)ipPkt.Payload;

            Key.PS2Port = tcp.SourcePort; Key.SRVPort = tcp.DestinationPort;

            lock (sentry)
            {
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
                    connections.Add(Key, s);
                    return s.Send(ipPkt.Payload);
                }
            }
        }
        public bool SendUDP(ConnectionKey Key, IPPacket ipPkt)
        {
            Log_Verb("UDP");
            UDP udp = (UDP)ipPkt.Payload;

            Key.PS2Port = udp.SourcePort; Key.SRVPort = udp.DestinationPort;

            lock (sentry)
            {
                if (udp.DestinationPort == 67)
                { //DHCP
                    return dhcpServer.Send(ipPkt.Payload);
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
                    UDPSession s = new UDPSession(Key, adapterIP, dhcpServer.Broadcast);
                    s.ConnectionClosedEvent += HandleConnectionClosed;
                    s.DestIP = ipPkt.DestinationIP;
                    s.SourceIP = dhcpServer.PS2IP;
                    connections.Add(Key, s);
                    return s.Send(ipPkt.Payload);
                }
            }
        }
        //Must lock in calling function
        public int SendFromConnection(ConnectionKey Key, IPPacket ipPkt)
        {
            if (connections.ContainsKey(Key))
            {
                //if (connections[Key].isOpen() == false)
                //{
                //    Log_Error("Attempt to send on Closed Connection");
                //    throw new Exception("Attempt to send on Closed Connection");
                //}
                //Error.WriteLine("Found Open Connection");
                return connections[Key].Send(ipPkt.Payload) ? 1 : 0;
            }
            else
                return -1;
        }

        public void HandleConnectionClosed(object sender, EventArgs e)
        {
            Session s = (Session)sender;
            s.ConnectionClosedEvent -= HandleConnectionClosed;
            deadConnections.Enqueue(s);
            //lock (sentry)
            //{
            //    connections.Remove(s.Key);
            //    s.Dispose();
            //    Log_Error("Closed Dead Connection");
            //}
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
                lock (sentry)
                {
                    Log_Verb("Closing " + connections.Count + " Connections");
                    foreach (ConnectionKey key in connections.Keys) //ToDo better multi-connection stuff
                    {
                        connections[key].Dispose();
                    }
                    vRecBuffer.Clear();
                    connections.Clear();
                    Session s;
                    while (deadConnections.TryDequeue(out s)) { }
                    //Connections.Add("DHCP", DCHP_server);
                    dhcpServer.Dispose();
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
