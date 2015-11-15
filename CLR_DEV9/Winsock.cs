using CLR_DEV9.PacketReader;
using CLR_DEV9.Sessions;
using System;
using System.Collections.Generic;

namespace CLR_DEV9
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

        public override bool Equals(Object obj)
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

    class Winsock : netHeader.NetAdapter
    {
        List<netHeader.NetPacket> vRecBuffer = new List<netHeader.NetPacket>(); //Non IP packets
        UDP_DHCPsession DCHP_server = new UDP_DHCPsession();
        //List<Session> Connections = new List<Session>();
        Object sentry = new Object();
        Dictionary<ConnectionKey, Session> Connections = new Dictionary<ConnectionKey, Session>();
        public Winsock()
        {
            //Add allways on connections
            DCHP_server.SourceIP = new byte[] { 255, 255, 255, 255 };
            DCHP_server.DestIP = UDP_DHCPsession.DHCP_IP;

            ConnectionKey DHCP_Key = new ConnectionKey();
            DHCP_Key.Protocol = (byte)IPType.UDP;
            DHCP_Key.SRVPort = 67;
            Connections.Add(DHCP_Key, DCHP_server);
        }

        public override bool blocks()
        {
            return false;	//we use blocking io
        }

        byte[] gateway_mac = { 0x76, 0x6D, 0xF4, 0x63, 0x30, 0x31 };
        byte[] ps2_mac;
        byte[] broadcast_adddrrrr = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        //gets a packet.rv :true success
        public override bool recv(ref netHeader.NetPacket pkt)
        {
            //return false;
            bool result = false;

            if (ps2_mac == null)
            {
                ps2_mac = new byte[6];
                byte[] eeprombytes = new byte[6];
                for (int i = 0; i < 3; i++)
                {
                    byte[] tmp = BitConverter.GetBytes(DEV9Header.dev9.eeprom[i]);
                    Utils.memcpy(ref eeprombytes, i * 2, tmp, 0, 2);
                }
                Utils.memcpy(ref ps2_mac, 0, eeprombytes, 0, 6);
            }

            if (vRecBuffer.Count == 0)
            {
                List<ConnectionKey> DeadConnections = new List<ConnectionKey>();
                lock (sentry)
                {
                    foreach (ConnectionKey key in Connections.Keys) //ToDo: better multi-connection stuff?
                    {
                        IPPayload PL;
                        PL = Connections[key].recv();
                        if (!(PL == null))
                        {
                            IPPacket ippkt = new IPPacket(PL);
                            ippkt.DestinationIP = Connections[key].SourceIP;
                            ippkt.SourceIP = Connections[key].DestIP;
                            EthernetFrame eF = new EthernetFrame(ippkt);
                            eF.SourceMAC = gateway_mac;
                            eF.DestinationMAC = ps2_mac;
                            eF.Protocol = (UInt16)EtherFrameType.IPv4;
                            pkt = eF.CreatePacket();
                            result = true;
                            break;
                        }
                        if (Connections[key].isOpen() == false)
                        {
                            //Console.Error.WriteLine("Removing Closed Connection : " + key);
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
                //TODO? Boost with pointers instead of converting?
                byte[] eeprombytes = new byte[6];
                for (int i = 0; i < 3; i++)
                {
                    byte[] tmp = BitConverter.GetBytes(DEV9Header.dev9.eeprom[i]);
                    Utils.memcpy(ref eeprombytes, i * 2, tmp, 0, 2);
                }
                //original memcmp returns 0 on perfect match
                //the if statment check if !=0
                if ((Utils.memcmp(pkt.buffer, 0, eeprombytes, 0, 6) == false) & (Utils.memcmp(pkt.buffer, 0, broadcast_adddrrrr, 0, 6) == false))
                {
                    //ignore strange packets
                    Console.Error.WriteLine("Dropping Strange Packet");
                    return false;
                }
                return true;
            }
            else
                return false;
        }
        //sends the packet and deletes it when done (if successful).rv :true success
        public override bool send(netHeader.NetPacket pkt)
        {
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
                    Console.Error.WriteLine("ARP (Ignoring)");
                    ARPPacket arppkt = ((ARPPacket)ef.Payload);

                    ////Detect ARP Packet Types
                    //if (Utils.memcmp(arppkt.SenderProtocolAddress, 0, new byte[] { 0, 0, 0, 0 }, 0, 4))
                    //{
                    //    Console.WriteLine("ARP Probe"); //(Who has my IP?)
                    //    break;
                    //}
                    //if (Utils.memcmp(arppkt.SenderProtocolAddress, 0, arppkt.TargetProtocolAddress, 0, 4))
                    //{
                    //    if (Utils.memcmp(arppkt.TargetHardwareAddress, 0, new byte[] { 0, 0, 0, 0, 0, 0 }, 0, 6) & arppkt.OP == 1)
                    //    {
                    //        Console.WriteLine("ARP Announcement Type 1");
                    //        break;
                    //    }
                    //    if (Utils.memcmp(arppkt.SenderHardwareAddress, 0, arppkt.TargetHardwareAddress, 0, 6) & arppkt.OP == 2)
                    //    {
                    //        Console.WriteLine("ARP Announcement Type 2");
                    //        break;
                    //    }
                    //}

                    ////if (arppkt.OP == 1) //ARP request
                    ////{
                    ////    //This didn't work for whatever reason.
                    ////    if (Utils.memcmp(arppkt.TargetProtocolAddress,0,UDP_DHCPsession.GATEWAY_IP,0,4))
                    ////    //it's trying to resolve the virtual gateway's mac addr
                    ////    {
                    ////        Console.Error.WriteLine("ARP Attempt to Resolve Gateway Mac");
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
                    //Console.Error.WriteLine("Unhandled ARP packet");

                    result = true;
                    break;
                #endregion
                case (int)0x0081:
                    Console.Error.WriteLine("VLAN-tagged frame (IEEE 802.1Q)");
                    throw new NotImplementedException();
                //break;
                default:
                    Console.Error.WriteLine("Unkown EtherframeType " + ef.Protocol.ToString("X4"));
                    break;
            }

            return result;
        }

        public bool sendIP(IPPacket ippkt)
        {
            //TODO Optimise Checksum for implace checksumming
            if (ippkt.VerifyCheckSum() == false)
            {
                Console.Error.WriteLine("IP packet with bad CSUM");
                return false;
            }
            if (ippkt.Payload.VerifyCheckSum(ippkt.SourceIP, ippkt.DestinationIP) == false)
            {
                Console.Error.WriteLine("IP packet with bad Payload CSUM");
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
                    Console.Error.WriteLine("Unkown Protocol");
                    //throw new NotImplementedException();
                    return false;
            }
        }

        public bool SendIMCP(ConnectionKey Key, IPPacket ippkt)
        {
            Console.Error.WriteLine("ICMP");
            lock (sentry)
            {
                int res = SendFromConnection(Key, ippkt);
                if (res == 1)
                    return true;
                else if (res == 0)
                    return false;
                else
                {
                    //Console.Error.WriteLine("Creating New Connection with key " + Key);
                    ICMPSession s = new ICMPSession();
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = UDP_DHCPsession.PS2_IP;
                    Connections.Add(Key, s);
                    return s.send(ippkt.Payload);
                }
            }
        }
        public bool SendTCP(ConnectionKey Key, IPPacket ippkt)
        {
            //Console.Error.WriteLine("TCP");
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
                    //Console.Error.WriteLine("Creating New Connection with key " + Key);
                    //Console.Error.WriteLine("Creating New TCP Connection with Dest Port " + tcp.DestinationPort);
                    TCPSession s = new TCPSession();
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = UDP_DHCPsession.PS2_IP;
                    Connections.Add(Key, s);
                    return s.send(ippkt.Payload);
                }
            }
        }
        public bool SendUDP(ConnectionKey Key, IPPacket ippkt)
        {
            //Console.Error.WriteLine("UDP");
            UDP udp = (UDP)ippkt.Payload;

            Key.PS2Port = udp.SourcePort; Key.SRVPort = udp.DestinationPort;

            if (udp.DestinationPort == 67)
            { //DHCP
                return DCHP_server.send(ippkt.Payload);
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
                    //Console.Error.WriteLine("Creating New Connection with key " + Key);
                    Console.Error.WriteLine("Creating New UDP Connection with Dest Port " + udp.DestinationPort);
                    UDPSession s = new UDPSession();
                    s.DestIP = ippkt.DestinationIP;
                    s.SourceIP = UDP_DHCPsession.PS2_IP;
                    Connections.Add(Key, s);
                    return s.send(ippkt.Payload);
                }
            }
        }
        //Must lock in calling function
        public int SendFromConnection(ConnectionKey Key, IPPacket ippkt)
        {
            if (Connections.ContainsKey(Key))
            {
                if (Connections[Key].isOpen() == false)
                    throw new Exception("Attempt to send on Closed Connection");
                //Console.Error.WriteLine("Found Open Connection");
                return Connections[Key].send(ippkt.Payload) ? 1 : 0;
            }
            else
                return -1;
        }

        public override void Dispose()
        {
            //TODO close all open connections
            lock (sentry)
            {
                Console.WriteLine("Closing " + Connections.Count + " Connections");
                foreach (ConnectionKey key in Connections.Keys) //ToDo better multi-connection stuff
                {
                    Connections[key].Dispose(); //replace with dispose?
                }
                vRecBuffer.Clear();
                Connections.Clear();
                //Connections.Add("DHCP", DCHP_server);
                DCHP_server.Dispose();
            }
        }
    }
}
