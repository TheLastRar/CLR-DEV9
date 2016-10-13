using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    //This has to be paried with a UDP session of the correct port
    //Which means we can only do it if the PS2 sends to toe group before
    //expecting data
    class IGMPSession : Session
    {
        UdpClient client;

        public override IPPayload Recv()
        {
            return null;
        }

        public IGMPSession(ConnectionKey parKey, IPAddress parAdapterIP)
            : base(parKey, parAdapterIP)
        {
        }

        public override bool Send(IPPayload payload)
        {
            IGMP igmpPkt = (IGMP)payload;
            client = new UdpClient(new IPEndPoint(adapterIP, 0));
            client.JoinMulticastGroup(new IPAddress(igmpPkt.GroupAddress));
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
            client.Close();
        }
    }
}
