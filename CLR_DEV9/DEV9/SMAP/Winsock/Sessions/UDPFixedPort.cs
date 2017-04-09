using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPFixedPort : Session
    {
        UdpClient client;

        UInt16 port;

        object connectionCountSentry = new object();
        ushort activeConnections = 0;

        public UDPFixedPort(ConnectionKey parKey, IPAddress parAdapterIP, UInt16 parPort)
            : base(parKey, parAdapterIP)
        {
            port = parPort;
            client = new UdpClient(new IPEndPoint(adapterIP, port));
            client.EnableBroadcast = true;
        }

        public override IPPayload Recv() { return null; }
        public override bool Send(IPPayload payload) { throw new NotImplementedException(); }

        public override void Reset() { }

        public override void Dispose()
        {
            client.Close();
        }

        public UDPSession NewClientSession(ConnectionKey parNewKey, bool parIsBrodcast)
        {
            UDPSession udp = new UDPSession(parNewKey, adapterIP, parIsBrodcast, client);
            udp.ConnectionClosedEvent += HandleChildConnectionClosed;

            return udp;
        }
        public UDPServerSession NewListenSession(ConnectionKey parNewKey)
        {
            UDPServerSession udp = new UDPServerSession(parNewKey, adapterIP, client);
            udp.ConnectionClosedEvent += HandleChildConnectionClosed;

            return udp;
        }

        private void HandleChildConnectionClosed(object sender, EventArgs e)
        {
            Session s = (Session)sender;
            s.ConnectionClosedEvent -= HandleChildConnectionClosed;
            lock (connectionCountSentry)
            {
                activeConnections -= 1;
                if (activeConnections == 0)
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
