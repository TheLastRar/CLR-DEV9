using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    //This has to be paried with a UDP session of the correct port
    //Which means we can only do it if the PS2 sends to toe group before
    //expecting data
    class IGMPSession : Session
    {
        //UdpClient client;
        IPAddress groupAddress;

        public override IPPayload Recv()
        {
            return null;
        }

        public IGMPSession(ConnectionKey parKey, IPAddress parAdapterIP)
            : base(parKey, parAdapterIP)
        {
            //Stub implemtation
        }

        public override bool Send(IPPayload payload)
        {
            IGMP igmpPkt = (IGMP)payload;
            groupAddress = new IPAddress(igmpPkt.GroupAddress);

            switch (igmpPkt.Type)
            {
                case 0x12: //Ver 1 Join
                case 0x16: //Ver 2 Join
                case 0x22: //Ver 3 Join
                    Log_Info("TODO IGMP Multicast Join Packet");
                    break;
                case 0x17: //Leave group
                    RaiseEventConnectionClosed();
                    return true;
                case 0x11: //Group query
                    break;
                default:
                    Log_Error("Unkown IGMP packet");
                    break;
            }

            //client = new UdpClient(new IPEndPoint(adapterIP, 0));
            //client.JoinMulticastGroup(groupAddress);
            return true;
            //throw new NotImplementedException();
        }

        public override void Reset()
        {
            Dispose();
            RaiseEventConnectionClosed();
        }

        public override void Dispose()
        {
            //client.DropMulticastGroup(groupAddress);
            //client.Close();
        }
        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.IGMPSession, str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.IGMPSession, str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.IGMPSession, str);
        }
    }
}
