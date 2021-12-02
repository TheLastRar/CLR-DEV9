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
        readonly bool isBroadcast = false;
        bool isMulticast = false;
        readonly bool isFixedPort = false;
        //EndBroadcast

        Stopwatch deathClock = new Stopwatch();
        const double MAX_IDLE = 120; //See RFC 4787 Section 4.3

        public UDPSession(ConnectionKey parKey, IPAddress parAdapterIP)
            : base(parKey, parAdapterIP)
        {
            lock (deathClock)
            {
                deathClock.Start();
            }
        }

        public UDPSession(ConnectionKey parKey, IPAddress parAdapterIP, bool parIsBroadcast, bool parIsMulticast, UdpClient parClient)
            : base(parKey, parAdapterIP)
        {
            isFixedPort = true;

            client = parClient;
            srcPort = parKey.PS2Port;
            destPort = parKey.SRVPort;
            isBroadcast = parIsBroadcast;
            isMulticast = parIsMulticast;

            lock (deathClock)
            {
                deathClock.Start();
            }

            open = true;
        }
        //bool thing = false;
        public override IPPayload Recv()
        {
            if (!open)
            {
                return null;
            }
            if (isFixedPort)
            {
                lock (deathClock)
                {
                    if (deathClock.Elapsed.TotalSeconds > MAX_IDLE)
                    {
                        Dispose();
                        Log_Info("UDPFixed Max Idle Reached");
                        RaiseEventConnectionClosed();
                    }
                }
                return null;
            }

            bool hasData;

            //client may be disposed when we try to check
            //available data on a rejected connection
            try
            {
                hasData = client.Available != 0;
            } 
            catch (ObjectDisposedException) 
            {
                hasData = false; 
            }

            if (hasData)
            {
                IPEndPoint remoteIPEndPoint;
                //this isn't a filter
                remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
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

                UDP iRet = new UDP(recived)
                {
                    DestinationPort = srcPort,
                    SourcePort = destPort
                };

                //if (isMulticast)
                //{
                //    //Log_Error(remoteIPEndPoint.ToString());
                //    DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4
                //    iRet.SourcePort = (UInt16)remoteIPEndPoint.Port;
                //}
                lock (deathClock)
                {
                    deathClock.Restart();
                }

                if (destPort == 53)
                {
                    Log_Info("DNS Packet Sent From " + remoteIPEndPoint.Address);
                    Log_Info("Contents");
                    PacketReader.DNS.DNS pDNS = new PacketReader.DNS.DNS(recived);
                }

                return iRet;
            }
            lock (deathClock)
            {
                if (deathClock.Elapsed.TotalSeconds > MAX_IDLE)
                {
                    Dispose();
                    Log_Info("UDP Max Idle Reached");
                    RaiseEventConnectionClosed();
                }
            }
            return null;
        }

        public bool WillRecive(byte[] parDestIP)
        {
            if (!open)
            {
                Log_Error("WillRecive() called on closed port");
                return false;
            }
            if (isBroadcast ||
                isMulticast ||
                Utils.memcmp(parDestIP, 0, DestIP, 0, 4))
            {
                lock (deathClock)
                {
                    deathClock.Restart();
                }
                return true;
            }
            Log_Error("WillRecive() Connection not broadcast and from unexpected IP");
            //Log IPs
            string str = "";
            for (int y = 0; y < DestIP.Length; y++)
            {
                str += DestIP[y] + ":";
            }
            Log_Error("Connecton Expected: " + str.TrimEnd(':'));
            str = "";
            for (int y = 0; y < parDestIP.Length; y++)
            {
                str += parDestIP[y] + ":";
            }
            Log_Error("Connecton Got: " + str.TrimEnd(':'));
            return false;
        }

        bool hasRetryed = false;
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
                //create client
                destPort = udp.DestinationPort;
                srcPort = udp.SourcePort;

                //Multicast address start with 0b1110
                if ((DestIP[0] & 0xF0) == 0xE0)
                {
                    isMulticast = true;
                    Log_Error("Multicast from non fixed port");
                }

                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(adapterIP, 0));

                ////needs testing
                //if (isMulticast)
                //{
                //    Log_Info("Is Multicast");
                //    //client.JoinMulticastGroup(address);
                //}
                //else
                //{
                IPAddress address = new IPAddress(DestIP);
                client.Connect(address, destPort);
                //}
                if (srcPort != 0)
                {
                    open = true;
                }
            }

            if (destPort == 53)
            {
                Log_Info("DNS Packet Sent To " + new IPAddress(DestIP));
                Log_Info("Contents");
                PacketReader.DNS.DNS pDNS = new PacketReader.DNS.DNS(udp.GetPayload());
            }

            if (isBroadcast)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(IPAddress.Broadcast, destPort));
            }
            else if (isMulticast | isFixedPort)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length, new IPEndPoint(new IPAddress(DestIP), destPort));
            }
            else if (isFixedPort)
            {
                client.Send(udp.GetPayload(), udp.GetPayload().Length);
            }
            else
            {
                //As fair as I know,
                //this won't throw a 10061 Connection refused or 10054 Connection reset
                //under any probable event on windows (As much as I've tried)
                //Yet it can throw 10061 on linux
                try
                {
                    client.Send(udp.GetPayload(), udp.GetPayload().Length);
                }
                catch (SocketException err)
                {
                    if (!hasRetryed)
                    {
                        Log_Error("UDP Send Error: " + err.Message);
                        Log_Error("Error Code: " + err.ErrorCode);
                        Log_Error("Hiding further errors from this connection");
                        hasRetryed = true;
                    }
                    client.Close();
                    //recreate UDP client
                    IPAddress address = new IPAddress(DestIP);
                    client = new UdpClient();
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    client.Client.Bind(new IPEndPoint(adapterIP, 0));
                    client.Connect(address, destPort);
                    //And retry sending
                    client.Send(udp.GetPayload(), udp.GetPayload().Length);
                }
            }

            if (srcPort == 0)
            {
                RaiseEventConnectionClosed();
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
