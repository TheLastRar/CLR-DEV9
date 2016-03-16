using CLRDEV9.DEV9.SMAP.Data;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using CLRDEV9.DEV9.SMAP.Winsock.Sessions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    class Winsock : NetAdapter
    {
        List<NetPacket> vRecBuffer = new List<NetPacket>(); //Non IP packets
        UDP_DHCPsession DHCP_server = new UDP_DHCPsession(null,null);
        //List<Session> Connections = new List<Session>();
        Object sentry = new Object();
        Dictionary<ConnectionKey, Session> Connections = new Dictionary<ConnectionKey, Session>();

        static public List<string[]> GetAdapters()
        {
            //Add Auto
            List<string[]> names = new List<string[]>();
            names.Add(new string[] { "Auto", "Autoselected adapter", "Auto" });

            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in Interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    UnicastIPAddressInformationCollection IPInfo = adapter.GetIPProperties().UnicastAddresses;
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    foreach (UnicastIPAddressInformation IPAddressInfo in IPInfo)
                    {
                        if (IPAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
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

        public Winsock(DEV9_State pardev9, string parDevice) 
            : base(pardev9)
        {
            if (parDevice != "Auto")
            {
                throw new NotImplementedException();
            }

            //Add allways on connections
            DHCP_server.SourceIP = new byte[] { 255, 255, 255, 255 };
            DHCP_server.DestIP = DefaultDHCPConfig.DHCP_IP;

            ConnectionKey DHCP_Key = new ConnectionKey();
            DHCP_Key.Protocol = (byte)IPType.UDP;
            DHCP_Key.SRVPort = 67;
            Connections.Add(DHCP_Key, DHCP_server);
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
                List<ConnectionKey> DeadConnections = new List<ConnectionKey>();
                lock (sentry)
                {
                    foreach (ConnectionKey key in Connections.Keys) //ToDo: better multi-connection stuff?
                    {
                        IPPayload PL;
                        PL = Connections[key].Recv();
                        if (!(PL == null))
                        {
                            IPPacket ippkt = new IPPacket(PL);
                            ippkt.DestinationIP = Connections[key].SourceIP;
                            ippkt.SourceIP = Connections[key].DestIP;
                            EthernetFrame eF = new EthernetFrame(ippkt);
                            eF.SourceMAC = virtural_gateway_mac;
                            eF.DestinationMAC = ps2_mac;
                            eF.Protocol = (UInt16)EtherFrameType.IPv4;
                            pkt = eF.CreatePacket();
                            result = true;
                            break;
                        }
                        if (Connections[key].isOpen() == false)
                        {
                            //Error.WriteLine("Removing Closed Connection : " + key);
                            DeadConnections.Add(key);
                        }
                    }
                    foreach (ConnectionKey key in DeadConnections)
                    {
                        Connections.Remove(key);
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

            PacketReader.EthernetFrame ef = new PacketReader.EthernetFrame(pkt);

            switch (ef.Protocol)
            {
                case (int)EtherFrameType.NULL:
                    //Adapter Reset
                    //TODO close all open connections
                    break;
                case (int)EtherFrameType.IPv4:
                    result = sendIP((IPPacket)ef.Payload);
                    break;
                #region "ARP"
                case (int)EtherFrameType.ARP:
                    Log_Verb("ARP (Ignoring)");
                    ARPPacket arppkt = ((ARPPacket)ef.Payload);

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

                    ////if (arppkt.OP == 1) //ARP request
                    ////{
                    ////    //This didn't work for whatever reason.
                    ////    if (Utils.memcmp(arppkt.TargetProtocolAddress,0,UDP_DHCPsession.GATEWAY_IP,0,4))
                    ////    //it's trying to resolve the virtual gateway's mac addr
                    ////    {
                    ////        Error.WriteLine("ARP Attempt to Resolve Gateway Mac");
                    ////        arppkt.TargetHardwareAddress = arppkt.SenderHardwareAddress;
                    ////        arppkt.SenderHardwareAddress = gateway_mac;
                    ////        arppkt.TargetProtocolAddress = arppkt.SenderProtocolAddress;
                    ////        arppkt.SenderProtocolAddress = UDP_DHCPsession.GATEWAY_IP;
                    ////        arppkt.OP = 2;

                    ////        EthernetFrame retARP = new EthernetFrame(arppkt);
                    ////        retARP.DestinationMAC = ps2_mac;
                    ////        retARP.SourceMAC = gateway_mac;
                    ////        retARP.Protocol = (Int16)EtherFrameType.ARP;
                    ////        vRecBuffer.Add(retARP.CreatePacket());
                    ////        break;
                    ////    }
                    ////}
                    //Error.WriteLine("Unhandled ARP packet");

                    result = true;
                    break;
                #endregion
                case (int)0x0081:
                    Log_Error("VLAN-tagged frame (IEEE 802.1Q)");
                    throw new NotImplementedException();
                //break;
                default:
                    Log_Error("Unkown EtherframeType " + ef.Protocol.ToString("X4"));
                    break;
            }

            return result;
        }

        public bool sendIP(IPPacket ippkt)
        {
            //TODO Optimise Checksum for implace checksumming
            if (ippkt.VerifyCheckSum() == false)
            {
                Log_Error("IP packet with bad CSUM");
                return false;
            }
            if (ippkt.Payload.VerifyCheckSum(ippkt.SourceIP, ippkt.DestinationIP) == false)
            {
                Log_Error("IP packet with bad Payload CSUM");
                return false;
            }

            ConnectionKey Key = new ConnectionKey();
            Key.IP0 = ippkt.DestinationIP[0]; Key.IP1 = ippkt.DestinationIP[1]; Key.IP2 = ippkt.DestinationIP[2]; Key.IP3 = ippkt.DestinationIP[3];
            Key.Protocol = ippkt.Protocol;

            switch (ippkt.Protocol) //(Prase Payload)
            {
                case (byte)IPType.ICMP:
                    return SendIMCP(Key, ippkt);
                case (byte)IPType.TCP:
                    return SendTCP(Key, ippkt);
                case (byte)IPType.UDP:
                    return SendUDP(Key, ippkt);
                default:
                    Log_Error("Unkown Protocol");
                    //throw new NotImplementedException();
                    return false;
            }
        }

        public bool SendIMCP(ConnectionKey Key, IPPacket ippkt)
        {
            Log_Verb("ICMP");
            lock (sentry)
            {
                int res = SendFromConnection(Key, ippkt);
                if (res == 1)
                    return true;
                else if (res == 0)
                    return false;
                else
                {
                    Log_Verb("Creating New Connection with key " + Key);
                    ICMPSession s = new ICMPSession(Connections);
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = DHCP_server.PS2IP;
                    Connections.Add(Key, s);
                    return s.Send(ippkt.Payload);
                }
            }
        }
        public bool SendTCP(ConnectionKey Key, IPPacket ippkt)
        {
            Log_Verb("TCP");
            TCP tcp = (TCP)ippkt.Payload;

            Key.PS2Port = tcp.SourcePort; Key.SRVPort = tcp.DestinationPort;

            lock (sentry)
            {
                int res = SendFromConnection(Key, ippkt);
                if (res == 1)
                    return true;
                else if (res == 0)
                    return false;
                else
                {
                    Log_Verb("Creating New Connection with key " + Key);
                    Log_Info("Creating New TCP Connection with Dest Port " + tcp.DestinationPort);
                    TCPSession s = new TCPSession();
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = DHCP_server.PS2IP;
                    Connections.Add(Key, s);
                    return s.Send(ippkt.Payload);
                }
            }
        }
        public bool SendUDP(ConnectionKey Key, IPPacket ippkt)
        {
            Log_Verb("UDP");
            UDP udp = (UDP)ippkt.Payload;

            Key.PS2Port = udp.SourcePort; Key.SRVPort = udp.DestinationPort;

            if (udp.DestinationPort == 67)
            { //DHCP
                return DHCP_server.Send(ippkt.Payload);
            }
            lock (sentry)
            {
                int res = SendFromConnection(Key, ippkt);
                if (res == 1)
                    return true;
                else if (res == 0)
                    return false;
                else
                {

                    Log_Verb("Creating New Connection with key " + Key);
                    Log_Info("Creating New UDP Connection with Dest Port " + udp.DestinationPort);
                    UDPSession s = new UDPSession(DHCP_server.Broadcast);
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = DHCP_server.PS2IP;
                    Connections.Add(Key, s);
                    return s.Send(ippkt.Payload);
                }
            }
        }
        //Must lock in calling function
        public int SendFromConnection(ConnectionKey Key, IPPacket ippkt)
        {
            if (Connections.ContainsKey(Key))
            {
                if (Connections[Key].isOpen() == false)
                {
                    Log_Error("Attempt to send on Closed Connection");
                    throw new Exception("Attempt to send on Closed Connection");
                }
                //Error.WriteLine("Found Open Connection");
                return Connections[Key].Send(ippkt.Payload) ? 1 : 0;
            }
            else
                return -1;
        }

        public override void Dispose(bool disposing)
        {
            //TODO close all open connections
            if (disposing)
            {
                lock (sentry)
                {
                    Log_Verb("Closing " + Connections.Count + " Connections");
                    foreach (ConnectionKey key in Connections.Keys) //ToDo better multi-connection stuff
                    {
                        Connections[key].Dispose(); //replace with dispose?
                    }
                    vRecBuffer.Clear();
                    Connections.Clear();
                    //Connections.Add("DHCP", DCHP_server);
                    DHCP_server.Dispose();
                }
            }
        }

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Winsock, "Winsock", str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Winsock, "Winsock", str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Winsock, "Winsock", str);
        }
    }
}
