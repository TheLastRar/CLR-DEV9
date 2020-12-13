using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDPServerSession : Session
    {
        UdpClient listener;

        UInt16 port;

        public UDPServerSession(ConnectionKey parKey, IPAddress parAdapterIP, UdpClient parClient)
            : base(parKey, parAdapterIP)
        {
            port = Key.PS2Port;
            listener = parClient;
        }

        public override void Dispose() { }

        public override IPPayload Recv()
        {
            return null;
        }

        public override void Reset() { }

        public override bool Send(IPPayload payload)
        {
            throw new NotImplementedException();
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
