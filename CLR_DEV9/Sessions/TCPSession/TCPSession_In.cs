using CLR_DEV9.PacketReader;
using System;
using System.Net.Sockets;

namespace CLR_DEV9.Sessions
{
    partial class TCPSession
    {
        public override IPPayload recv()
        {
            TCP ret = PopRecvBuff();
            if (ret != null)
            {
                return ret;
            }

            int avaData = 0;

            lock (clientSentry)
            {
                if (client == null) { return null; }

                if (client.Connected == false) { return null; }

                avaData = client.Available;
            }

            if (avaData != 0 && MyNumberACKed.WaitOne(0))
            {
                if (avaData > (MaxSegmentSize - 16))
                {
                    Console.Error.WriteLine("Got a lot of data");
                    avaData = MaxSegmentSize - 16;
                }

                byte[] recived = new byte[avaData];
                //Console.Error.WriteLine("Received " + avaData);
                netStream.Read(recived, 0, avaData);

                TCP iRet = CreateBasePacket(recived);
                IncrementMyNumber((uint)avaData);

                iRet.ACK = true;
                iRet.PSH = true;

                MyNumberACKed.Reset();
                return iRet;
            }

            lock (clientSentry)
            {
                if (client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0 && state == TCPState.Connected)
                {
                    Console.Error.WriteLine("Detected Closed By Remote Connection");
                    PerformCloseByRemote();
                    client.Close();
                }
            }

            return null;
        }

        private void PerformCloseByRemote()
        {
            client.Close();
            netStream = null;
            Console.Error.WriteLine("Remote has closed connection");

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            PushRecvBuff(ret);
            state = TCPState.ConnectionClosedByRemote;
        }
    }
}
