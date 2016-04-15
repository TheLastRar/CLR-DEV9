using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    partial class TCPSession
    {
        public override IPPayload Recv()
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

            if (avaData != 0 && myNumberACKed.WaitOne(0))
            {
                if (avaData > (maxSegmentSize - 16))
                {
                    Log_Info("Got a lot of data");
                    avaData = maxSegmentSize - 16;
                }

                byte[] recived = new byte[avaData];
                Log_Verb("Received " + avaData);
                netStream.Read(recived, 0, avaData);
                Log_Verb("[SRV]Sending " + avaData + " bytes");
                TCP iRet = CreateBasePacket(recived);
                IncrementMyNumber((uint)avaData);

                iRet.ACK = true;
                iRet.PSH = true;

                myNumberACKed.Reset();
                Log_Verb("myNumberACKed Reset");
                return iRet;
            }

            lock (clientSentry)
            {
                if (client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0 && state == TCPState.Connected)
                {
                    Log_Info("Detected Closed By Remote Connection");
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
            Log_Info("Remote has closed connection");

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            PushRecvBuff(ret);
            state = TCPState.ConnectionClosedByRemote;
        }
    }
}
