using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPSession : Session
    {
        List<UDP> recvbuff = new List<UDP>();

        UdpClient client;

        UInt16 srcPort = 0;
        UInt16 destPort = 0;
        //Broadcast
        byte[] broadcastAddr;
        bool isBroadcast = false;
        byte[] broadcastResponseData = null;
        byte[] broadcastResponseIP = null;
        //EndBroadcast

        Stopwatch deathClock = new Stopwatch();
        const double MAX_IDLE = 72;
        public UDPSession(IPAddress parAdapterIP, byte[] parBroadcastIP)
            : base(parAdapterIP)
        {
            broadcastAddr = parBroadcastIP;
            deathClock.Start();
        }
        public override IPPayload Recv()
        {
            if (recvbuff.Count != 0)
            {
                UDP ret = recvbuff[0];
                recvbuff.RemoveAt(0);
                deathClock.Restart();
                return ret;
            }
            if (srcPort == 0)
            {
                return null;
            }

            {
                if (client.Available != 0)
                {
                    IPEndPoint remoteIPEndPoint;
                    if (isBroadcast)
                    {
                        remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    }
                    else
                    {
                        remoteIPEndPoint = new IPEndPoint(new IPAddress(DestIP), destPort);
                    }
                    byte[] recived = client.Receive(ref remoteIPEndPoint);
                    //Error.WriteLine("UDP Got Data");
                    if (isBroadcast)
                    {
                        DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4
                    }
                    UDP iRet = new UDP(recived);
                    iRet.DestinationPort = srcPort;
                    iRet.SourcePort = destPort;
                    deathClock.Restart();
                    return iRet;
                }
            }
            if (deathClock.Elapsed.TotalSeconds > MAX_IDLE)
            {
                client.Close();
                open = false;
            }
            return null;
        }
        public override bool Send(IPPayload payload)
        {
            deathClock.Restart();
            UDP udp = (UDP)payload;

            if (destPort != 0)
            {
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

                if (Utils.memcmp(DestIP, 0, broadcastAddr, 0, 4))
                {
                    isBroadcast = true;
                }

                if (isBroadcast)
                {
                    Log_Info("Is Broadcast");

                    client = new UdpClient(new IPEndPoint(adapterIP, srcPort)); //Assuming broadcast wants a return message
                    client.EnableBroadcast = true;

                    //client.Close();
                    //client = new UdpClient(SrcPort);
                    //client.BeginReceive(ReceiveFromBroadcast, new object());
                    open = true;
                }
                else
                {
                    IPAddress address = new IPAddress(DestIP);
                    if (srcPort == destPort)
                    {
                        client = new UdpClient(new IPEndPoint(adapterIP, srcPort)); //Needed for Crash TTR (and probable other games) LAN
                    }
                    else
                    {
                        client = new UdpClient(new IPEndPoint(adapterIP, 0));
                    }

                    client.Connect(address, destPort); //address to send on
                    if (srcPort != 0)
                    {
                        //Error.WriteLine("UDP expects Data");
                        open = true;
                    }
                }
            }

            if (isBroadcast)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(IPAddress.Broadcast, destPort));
            }
            else
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length);
            }

            //Error.WriteLine("UDP Sent");
            return true;
        }

        private void ReceiveFromBroadcast(IAsyncResult ar)
        {
            Log_Verb("Got Data");
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, destPort);
            byte[] bytes = client.EndReceive(ar, ref ip);
            broadcastResponseData = bytes;
            broadcastResponseIP = ip.Address.GetAddressBytes();
        }
        public override void Reset()
        {
            Dispose();
        }

        bool open = false;
        public override bool isOpen()
        {
            return open;
        }
        public override void Dispose()
        {
            open = false;
            client.Close();
        }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Winsock, "UDPSession", str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Winsock, "UDPSession", str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Winsock, "UDPSession", str);
        }
    }
}
