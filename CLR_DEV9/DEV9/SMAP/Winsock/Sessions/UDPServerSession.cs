using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
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
            if (listener.Available != 0)
            {
                IPEndPoint remoteIPEndPoint;
                remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);

                byte[] recived = null;
                try
                {
                    recived = listener.Receive(ref remoteIPEndPoint);
                    Log_Verb("Listener Got Data");
                }
                catch (SocketException err)
                {
                    Log_Error("UDP Recv Error: " + err.Message);
                    Log_Error("Error Code: " + err.ErrorCode);
                    RaiseEventConnectionClosed();
                    return null;
                }

                UDP iRet = new UDP(recived);

                //Get sender IP and port
                iRet.DestinationPort = port;
                iRet.SourcePort = (UInt16)remoteIPEndPoint.Port;
                DestIP = remoteIPEndPoint.Address.GetAddressBytes(); //assumes ipv4

                //Return data
                return iRet;
            }
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
