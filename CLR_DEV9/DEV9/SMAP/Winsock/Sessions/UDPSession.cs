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

        UInt16 SrcPort = 0;
        UInt16 DestPort = 0;
        //Broadcast
        bool isBroadcast = false;
        byte[] broadcastResponseData = null;
        byte[] broadcastResponseIP = null;
        //EndBroadcast

        Stopwatch DeathClock = new Stopwatch();
        const double MaxIdle = 72;
        public UDPSession()
        {
            DeathClock.Start();
        }
        public override IPPayload recv()
        {
            if (recvbuff.Count != 0)
            {
                UDP ret = recvbuff[0];
                recvbuff.RemoveAt(0);
                DeathClock.Restart();
                return ret;
            }
            if (SrcPort == 0)
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
                        remoteIPEndPoint = new IPEndPoint(new IPAddress(DestIP), DestPort);
                    }
                    byte[] recived = client.Receive(ref remoteIPEndPoint);
                    //Error.WriteLine("UDP Got Data");
                    if (isBroadcast)
                    {
                        DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4
                    }
                    UDP iRet = new UDP(recived);
                    iRet.DestinationPort = SrcPort;
                    iRet.SourcePort = DestPort;
                    DeathClock.Restart();
                    return iRet;
                }
            }
            if (DeathClock.Elapsed.TotalSeconds > MaxIdle)
            {
                client.Close();
                open = false;
            }
            return null;
        }
        public override bool send(IPPayload payload)
        {
            DeathClock.Restart();
            UDP udp = (UDP)payload;

            if (DestPort != 0)
            {
                if (!(udp.DestinationPort == DestPort && udp.SourcePort == SrcPort))
                {
                    Log_Error("UDP packet invalid for current session (Duplicate key?)");
                    return false;
                }
            }
            else
            {
                DestPort = udp.DestinationPort;
                SrcPort = udp.SourcePort;

                if (Utils.memcmp(DestIP, 0, UDP_DHCPsession.BROADCAST, 0, 4))
                {
                    isBroadcast = true;
                }

                if (isBroadcast)
                {
                    Log_Info("Is Broadcast");

                    client = new UdpClient(SrcPort); //Assuming broadcast wants a return message
                    client.EnableBroadcast = true;

                    //client.Close();
                    //client = new UdpClient(SrcPort);
                    //client.BeginReceive(ReceiveFromBroadcast, new object());
                    open = true;
                }
                else
                {
                    IPAddress address = new IPAddress(DestIP);
                    if (SrcPort == DestPort)
                    {
                        client = new UdpClient(SrcPort); //Needed for Crash TTR (and probable other games) LAN
                    }
                    else
                    {
                        client = new UdpClient();
                    }

                    client.Connect(address, DestPort); //address to send on
                    if (SrcPort != 0)
                    {
                        //Error.WriteLine("UDP expects Data");
                        open = true;
                    }
                }
            }

            if (isBroadcast)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(IPAddress.Broadcast, DestPort));
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
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, DestPort);
            byte[] bytes = client.EndReceive(ar, ref ip);
            broadcastResponseData = bytes;
            broadcastResponseIP = ip.Address.GetAddressBytes();
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
