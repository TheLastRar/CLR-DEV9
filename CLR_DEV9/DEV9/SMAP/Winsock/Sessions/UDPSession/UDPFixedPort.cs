using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPFixedPort : Session
    {
        volatile bool open = true;

        UdpClient client;

        UInt16 port;

        object connectionSentry = new object();
        List<Session> connections = new List<Session>();

        public UDPFixedPort(ConnectionKey parKey, IPAddress parAdapterIP, UInt16 parPort)
            : base(parKey, parAdapterIP)
        {
            port = parPort;
            client = new UdpClient(new IPEndPoint(adapterIP, port));
            client.EnableBroadcast = true;
        }

        public override IPPayload Recv()
        {
            if (!open)
            {
                return null;
            }

            if (client.Available != 0)
            {
                IPEndPoint remoteIPEndPoint;

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

                UDP iRet = new UDP(recived);
                iRet.DestinationPort = port;

                DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4
                iRet.SourcePort = (UInt16)remoteIPEndPoint.Port;

                foreach (Session s in connections)
                {
                    //is forwarded port
                    if (s is UDPServerSession) { return iRet; }
                    UDPSession udpS = (UDPSession)s;
                    //Call into method in UDPSession
                    //to determin if we should recive
                    //or discard packet.
                    //packet is then sent from here
                    if (udpS.WillRecive(DestIP)) { return iRet; }
                }
                Log_Error("Unexpected packet, dropping");
            }
            return null;
        }
        public override bool Send(IPPayload payload) { throw new NotImplementedException(); }

        public override void Reset()
        {
            lock (connectionSentry)
            {
                foreach (Session s in connections)
                {
                    s.Reset();
                }
            }
        }

        public override void Dispose()
        {
            open = false;
            client.Close();
        }

        public UDPSession NewClientSession(ConnectionKey parNewKey, bool parIsBrodcast)
        {
            UDPSession udp = new UDPSession(parNewKey, adapterIP, parIsBrodcast, client);
            udp.ConnectionClosedEvent += HandleChildConnectionClosed;

            lock (connectionSentry)
            {
                connections.Add(udp);
            }

            return udp;
        }
        public UDPServerSession NewListenSession(ConnectionKey parNewKey)
        {
            UDPServerSession udp = new UDPServerSession(parNewKey, adapterIP, client);
            udp.ConnectionClosedEvent += HandleChildConnectionClosed;

            lock (connectionSentry)
            {
                connections.Add(udp);
            }

            return udp;
        }

        private void HandleChildConnectionClosed(object sender, EventArgs e)
        {
            Session s = (Session)sender;
            s.ConnectionClosedEvent -= HandleChildConnectionClosed;
            lock (connectionSentry)
            {
                connections.Remove(s);
                if (connections.Count == 0)
                {
                    RaiseEventConnectionClosed();
                }
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
