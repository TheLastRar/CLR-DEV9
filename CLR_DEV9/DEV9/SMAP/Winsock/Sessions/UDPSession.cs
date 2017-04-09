using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPSession : Session
    {
        volatile bool open = false;

        UdpClient client;

        UInt16 srcPort = 0;
        UInt16 destPort = 0;
        //Broadcast
        //byte[] broadcastAddr;
        byte[] multicastAddr;
        bool isBroadcast = false;
        bool isMulticast = false;
        bool isFixedPort = false;
        //EndBroadcast

        Stopwatch deathClock = new Stopwatch();
        const double MAX_IDLE = 72;
        public UDPSession(ConnectionKey parKey, IPAddress parAdapterIP, byte[] parBroadcastIP)
            : base(parKey, parAdapterIP)
        {
            //broadcastAddr = parBroadcastIP;
            lock (deathClock)
            {
                deathClock.Start();
            }
        }

        public UDPSession(ConnectionKey parKey, IPAddress parAdapterIP, bool parIsBroadcast, UdpClient parClient)
            : base(parKey, parAdapterIP)
        {
            //broadcastAddr = parBroadcastIP;
            client = parClient;

            srcPort = parKey.PS2Port;
            destPort = parKey.SRVPort;
            isFixedPort = true;
            isBroadcast = parIsBroadcast;
        }
        //bool thing = false;
        public override IPPayload Recv()
        {
            if (!open)
            {
                return null;
            }
            if (srcPort == 0)
            {
                return null;
            }

            if (client.Available != 0)
            {
                IPEndPoint remoteIPEndPoint;
                if (isBroadcast | isMulticast)
                {
                    remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                }
                else
                {
                    remoteIPEndPoint = new IPEndPoint(new IPAddress(DestIP), destPort);
                }
                byte[] recived = null;
                try
                {
                    recived = client.Receive(ref remoteIPEndPoint);
                    Log_Verb("Got Data");
                }
                catch (SocketException err)
                {
                    Log_Error("UDP Recv Error: " + err.Message);
                    Log_Error("Error Code: " + err.ErrorCode);
                    RaiseEventConnectionClosed();
                    return null;
                }

                UDP iRet = new UDP(recived);
                iRet.DestinationPort = srcPort;
                iRet.SourcePort = destPort;

                if (isBroadcast | isMulticast)
                {
                    Log_Error(remoteIPEndPoint.ToString());
                    DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4
                    iRet.SourcePort = (UInt16)remoteIPEndPoint.Port;
                }
                lock (deathClock)
                {
                    deathClock.Restart();
                }

                return iRet;
            }
            lock (deathClock)
            {
                if (deathClock.Elapsed.TotalSeconds > MAX_IDLE)
                {
                    client.Close();
                    RaiseEventConnectionClosed();
                }
            }
            return null;
        }
        public override bool Send(IPPayload payload)
        {
            lock (deathClock)
            {
                deathClock.Restart();
            }
            UDP udp = (UDP)payload;

            if (destPort != 0)
            {
                //client already created
                if (!(udp.DestinationPort == destPort && udp.SourcePort == srcPort))
                {
                    Log_Error("UDP packet invalid for current session (Duplicate key?)");
                    return false;
                }
            }
            else
            {
                destPort = udp.DestinationPort;
                srcPort = udp.SourcePort;

                //Multicast address start with 0b1110
                if ((DestIP[0] & 0xF0) == 0xE0)
                {
                    isMulticast = true;
                }

                else if (isMulticast)
                {
                    Log_Info("Is Multicast");
                    multicastAddr = DestIP;
                    client = new UdpClient(new IPEndPoint(adapterIP, 0));
                    //IPAddress address = new IPAddress(multicastAddr);
                    //client.JoinMulticastGroup(address);
                }
                else
                {
                    IPAddress address = new IPAddress(DestIP);
                    if (srcPort == destPort)
                    {
                        //client = new UdpClient(new IPEndPoint(adapterIP, srcPort)); //Needed for Crash TTR (and probable other games) LAN
                        throw new Exception("UDP Session Must Be Created with UDPFixedPort");
                    }
                    else
                    {
                        client = new UdpClient(new IPEndPoint(adapterIP, 0));
                    }

                    if (srcPort != 0)
                    {
                        //Error.WriteLine("UDP expects Data");
                        //open = true;
                    }
                }
                open = true;
            }

            if (isBroadcast)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(IPAddress.Broadcast, destPort));
            }
            else if (isMulticast)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(new IPAddress(multicastAddr), destPort));
            }
            else 
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(new IPAddress(DestIP), destPort));
            }

            return true;
        }

        public override void Reset()
        {
            open = false;
            if (!isFixedPort)
            {
                client.Close();
            }
            RaiseEventConnectionClosed();
        }

        public override void Dispose()
        {
            open = false;
            if (!isFixedPort)
            {
                client.Close();
            }
        }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.UDPSession, str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.UDPSession, str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.UDPSession, str);
        }
    }
}
