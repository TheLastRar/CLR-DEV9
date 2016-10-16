using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
//using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPSession : Session
    {
        //List<UDP> recvbuff = new List<UDP>();

        UdpClient client;

        UInt16 srcPort = 0;
        UInt16 destPort = 0;
        //Broadcast
        byte[] broadcastAddr;
        byte[] multicastAddr;
        bool isBroadcast = false;
        bool isMulticast = false;
        //byte[] broadcastResponseData = null;
        //byte[] broadcastResponseIP = null;
        //EndBroadcast

        Stopwatch deathClock = new Stopwatch();
        const double MAX_IDLE = 72;
        public UDPSession(ConnectionKey parKey, IPAddress parAdapterIP, byte[] parBroadcastIP)
            : base(parKey, parAdapterIP)
        {
            broadcastAddr = parBroadcastIP;
            deathClock.Start();
        }
        //bool thing = false;
        public override IPPayload Recv()
        {
            //if (recvbuff.Count != 0)
            //{
            //    UDP ret = recvbuff[0];
            //    recvbuff.RemoveAt(0);
            //    deathClock.Restart();
            //    return ret;
            //}
            if (srcPort == 0)
            {
                return null;
            }

            {
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
                    byte[] recived = client.Receive(ref remoteIPEndPoint);
                    //Error.WriteLine("UDP Got Data");

                    string ret;
                    System.Text.Encoding targetEncoding = System.Text.Encoding.ASCII;
                    ret = targetEncoding.GetString(recived, 0, recived.Length);
                    //if (thing)
                    //{
                    //    //Log_Error("Fudging packet");
                    //    //recived = targetEncoding.GetBytes(ret.Replace("WANCommonInterfaceConfig", "InternetGatewayDevice"));
                    //    //ret = targetEncoding.GetString(recived, 0, recived.Length);
                    //}
                    if (ret.StartsWith("HTTP/1.1 200 OK"))
                    {
                        Log_Error("Fudging packet");
                        //ret = ret.TrimEnd();
                        //ret = ret + "\r\nBOOTID.UPNP.ORG: 56\r\n\r\n";
                        //ret = ret.Replace("\r\n", "\n");
                        //ret = ret.Replace("UPnP/1.0","UPnP/1.1");
                        //recived = targetEncoding.GetBytes(ret);
                        ////thing = true;
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

                    deathClock.Restart();

                    //if (iRet.DestinationPort == 1900 | (DestIP[0] == 192 & DestIP[1] == 168))
                    //{
                        Log_Error("Recv");
                        Log_Error(ret);
                    //}

                    return iRet;
                }
            }
            if (deathClock.Elapsed.TotalSeconds > MAX_IDLE)
            {
                client.Close();
                RaiseEventConnectionClosed();
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

                //Multicast address start with 0b1110
                if (Utils.memcmp(DestIP, 0, broadcastAddr, 0, 4))
                {
                    isBroadcast = true;
                }
                if ((DestIP[0] & 0xF0) == 0xE0)
                {
                    isMulticast = true;
                }

                if (isBroadcast)
                {
                    Log_Info("Is Broadcast");
                    client = new UdpClient(new IPEndPoint(adapterIP, srcPort)); //Assuming broadcast wants a return message
                    client.EnableBroadcast = true;

                    //client.Close();
                    //client = new UdpClient(SrcPort);
                    //client.BeginReceive(ReceiveFromBroadcast, new object());
                    //open = true;
                }
                else if (isMulticast)
                {
                    Log_Info("Is Multicast");
                    multicastAddr = DestIP;
                    client = new UdpClient(new IPEndPoint(adapterIP, 0));
                    IPAddress address = new IPAddress(multicastAddr);
                    client.JoinMulticastGroup(address);
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
                        //open = true;
                    }
                }
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
                client.Send(udp.GetPayload(), udp.GetPayload().Length);
            }

            //if (udp.DestinationPort == 1900 | (DestIP[0] == 192 & DestIP[1] == 168))
            //{
                Log_Error("Send");
                string ret;
            //NetLib.ReadCString(udp.GetPayload(), ref off, int.MaxValue, out ret);
            System.Text.Encoding targetEncoding = System.Text.Encoding.ASCII;
            ret = targetEncoding.GetString(udp.GetPayload(), 0, udp.GetPayload().Length);
            Log_Error(ret);
            //}

            //Error.WriteLine("UDP Sent");
            return true;
        }

        //private void ReceiveFromBroadcast(IAsyncResult ar)
        //{
        //    Log_Verb("Got Data");
        //    IPEndPoint ip = new IPEndPoint(IPAddress.Any, destPort);
        //    byte[] bytes = client.EndReceive(ar, ref ip);
        //    broadcastResponseData = bytes;
        //    broadcastResponseIP = ip.Address.GetAddressBytes();
        //}
        public override void Reset()
        {
            client.Close();
            RaiseEventConnectionClosed();
        }

        //bool open = false;
        //public override bool isOpen()
        //{
        //    return open;
        //}
        public override void Dispose()
        {
            //open = false;
            client.Close();
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
