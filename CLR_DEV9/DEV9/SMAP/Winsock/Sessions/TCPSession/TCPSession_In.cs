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
            //Don't read data untill PS2 ACKs
            //connection
            if (state == TCPState.SentSYN_ACK)
            {
                return null;
            }
            //When TCP connection is closed by the server
            //the server is the last to send a packet
            //so the event must be raised here
            if (state == TCPState.CloseCompletedFlushBuffer)
            {
                state = TCPState.CloseCompleted;
                RaiseEventConnectionClosed();
                return null;
            }

            if (!(state == TCPState.Connected |
                state == TCPState.Closing_ClosedByPS2))
            {
                return null;
            }

            int maxSize;
            if (sendTimeStamps)
            {
                maxSize = Math.Min(maxSegmentSize, windowSize);
            }
            else
            {
                maxSize = Math.Min(maxSegmentSize - 16, windowSize);
            }

            if (maxSize != 0 &&
                myNumberACKed.WaitOne(0))
            {
                byte[] buffer = null;
                SocketError err;
                int recived = -1;

                try
                {
                    if (client.Available > maxSize)
                    {
                        Log_Info("Got a lot of data");
                    }

                    buffer = new byte[maxSize];
                    recived = client.Receive(buffer, 0, maxSize, SocketFlags.None, out err);
                }
                catch (ObjectDisposedException) { err = SocketError.Shutdown; }
                if (err == SocketError.WouldBlock)
                {
                    return null;
                }
                else if(err == SocketError.Shutdown)
                {
                    //In theory, this should only occur when the PS2 has RST the connection
                    //and the call to TCPSession.Recv() occurs at just the right time.
                    Log_Info("Recv() on shutdown socket");
                    return null;
                }
                else if (err != SocketError.Success)
                {
                    //throw new SocketException((int)err);
                    Log_Error("TCP Recv Error: " + (new SocketException((int)err)).Message);
                    Log_Error("Error Code: " + (int)err);
                    CloseByRemoteRST();
                    return null;
                }
                if (recived == 0)
                {
                    //Server Closed Socket
                    client.Shutdown(SocketShutdown.Receive);

                    switch (state)
                    {
                        case TCPState.Connected:
                            CloseByRemoteStage1();
                            break;
                        case TCPState.Closing_ClosedByPS2:
                            CloseByPS2Stage3();
                            break;
                        default:
                            throw new Exception("Remote Close In Invalid State");
                    }
                    return null;
                }

                Log_Verb("[SRV]Sending " + recived + " bytes");

                byte[] recivedData = new byte[recived];
                Array.Copy(buffer, recivedData, recived);

                TCP iRet = CreateBasePacket(recivedData);
                IncrementMyNumber((uint)recived);

                iRet.ACK = true;
                iRet.PSH = true;

                myNumberACKed.Reset();
                Log_Verb("myNumberACKed Reset");
                return iRet;
            }

            return null;
        }

        private void CloseByPS2Stage3()
        {
            Log_Info("Remote has closed connection after PS2");

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            myNumberACKed.Reset();
            Log_Verb("myNumberACKed Reset");

            PushRecvBuff(ret);
            state = TCPState.Closing_ClosedByPS2ThenRemote_WaitingForAck;
        }

        private void CloseByRemoteStage1()
        {
            Log_Info("Remote has closed connection");

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            myNumberACKed.Reset();
            Log_Verb("myNumberACKed Reset");

            PushRecvBuff(ret);
            state = TCPState.Closing_ClosedByRemote;
        }
    }
}
