using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Net;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    partial class TCPSession
    {
        private void AsyncConnectComplete(IAsyncResult res)
        {
            TCP tcp = (TCP)res.AsyncState;
            try
            {
                lock (clientSentry)
                {
                    client.EndConnect(res);
                }
            }
            catch (System.Net.Sockets.SocketException err)
            {
                Console.Error.WriteLine("TCP Connection Error: " + err.Message);
                Console.Error.WriteLine("ErrorCode: " + err.ErrorCode);
            }
            bool connected = false;
            lock (clientSentry)
            {
                client.NoDelay = true;
                connected = client.Connected;
                if (connected)
                {
                    netStream = client.GetStream();
                }
            }
            if (connected)
            {
                //TODO: Port this to CreateBasePacket?
                //This needs to specify custom TCP Options
                //so may not be doable

                open = true;
                state = TCPState.SentSYN_ACK;
                byte[] emptyByte = new byte[0];
                TCP ret = new TCP(emptyByte);
                //Return the fact we connected
                ret.SourcePort = tcp.DestinationPort;
                ret.DestinationPort = tcp.SourcePort;

                ret.SequenceNumber = GetMyNumber();
                IncrementMyNumber(1);

                ret.AcknowledgementNumber = ExpectedSequenceNumber;

                ret.SYN = true;
                ret.ACK = true;
                ret.WindowSize = (UInt16)(2 * MaxSegmentSize);
                ret.Options.Add(new TCPopMSS(MaxSegmentSize));

                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopWS(0));

                if (SendTimeStamps)
                {
                    ret.Options.Add(new TCPopNOP());
                    ret.Options.Add(new TCPopNOP());
                    ret.Options.Add(new TCPopTS((UInt32)TimeStamp.Elapsed.Seconds, LastRecivedTimeStamp));
                }
                PushRecvBuff(ret);
            }
            else
            {
                open = false;
                state = TCPState.None;
            }
        }

        public override bool send(IPPayload payload)
        {
            TCP tcp = (TCP)payload;
            if (DestPort != 0)
            {
                if (!(tcp.DestinationPort == DestPort && tcp.SourcePort == SrcPort))
                {
                    Console.Error.WriteLine("TCP packet invalid for current session (Duplicate key?)");
                    return false;
                }
            }

            if (tcp.RST == true) //Test this
            {
                lock (clientSentry)
                {
                    if (client.Connected)
                    {
                        client.Close();
                        state = TCPState.Closed;
                        open = false;
                        return true;
                    }
                }
            }

            switch (state)
            {
                case TCPState.None:
                    return sendConnect(tcp);
                case TCPState.SendingSYN_ACK:
                    return true; //Ignore reconnect attempts while we are still attempting connection
                case TCPState.SentSYN_ACK:
                    return SendConnected(tcp);
                case TCPState.Connected:
                    if (tcp.FIN == true) //Connection Close Part 1, receive FIN from PS2
                    {
                        NumCheckResult Result = CheckNumbers(tcp);
                        if (Result == NumCheckResult.GotOldData)
                        {
                            throw new NotImplementedException();
                        }
                        if (Result == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }
                        PerformCloseByPS2();
                        return true;
                    }
                    return SendData(tcp);
                case TCPState.ConnectionClosedByPS2AndRemote:
                    return SendCloseResponseResponse(tcp);
                case TCPState.ConnectionClosedByRemote:
                    return SendResponseToClosedServer(tcp, false);
                case TCPState.ConnectionClosedByRemoteAcknowledged:
                    return SendResponseToClosedServer(tcp, true);
                default:
                    throw new Exception("Invalid State");
            }
        }

        //PS2 sent SYN
        private bool sendConnect(TCP tcp)
        {
            //Expect SYN Packet
            DestPort = tcp.DestinationPort;
            SrcPort = tcp.SourcePort;

            if (tcp.SYN == false)
            {
                PerformRST();
                Console.Error.WriteLine("Connection Not in Connected State");
                return true;
            }
            ExpectedSequenceNumber = tcp.SequenceNumber + 1;
            //Fill out last received numbers
            ReceivedPS2SequenceNumbers.Clear();
            ReceivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            ReceivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            ReceivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            ReceivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            ReceivedPS2SequenceNumbers.Add(tcp.SequenceNumber);

            for (int i = 0; i < tcp.Options.Count; i++)
            {
                switch (tcp.Options[i].Code)
                {
                    case 0: //End
                    case 1: //Nop
                        continue;
                    case 2: //MSS
                        MaxSegmentSize = ((TCPopMSS)(tcp.Options[i])).MaxSegmentSize;
                        break;
                    case 3: //WinScale
                        Console.Error.WriteLine("Got WinScale (Not Supported)");
                        // = ((TCPopWS)(tcp.Options[i])).WindowScale;
                        break;
                    case 8: //TimeStamp
                        LastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        SendTimeStamps = true;
                        TimeStamp.Start();
                        break;
                    default:
                        Console.Error.WriteLine("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception();
                    //break;
                }
            }

            
            lock (clientSentry)
            {
                if (client != null)
                client.Close();
                client = null;
            }
            netStream = null;
            client = new TcpClient();
            IPAddress address = new IPAddress(DestIP);
            client.BeginConnect(address, DestPort, new AsyncCallback(AsyncConnectComplete), tcp);
            state = TCPState.SendingSYN_ACK;
            open = true;
            return true;
        }

        //PS2 responding to our SYN-ACK (by sending ACK)
        private bool SendConnected(TCP tcp)
        {
            if (tcp.SYN == true)
            {
                throw new Exception("Attempt to Connect to an operning Port");
            }
            NumCheckResult Result = CheckNumbers(tcp);
            if (Result == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }

            for (int i = 0; i < tcp.Options.Count; i++)
            {
                switch (tcp.Options[i].Code)
                {
                    case 0: //End
                    case 1: //Nop
                        continue;
                    case 8: //Timestamp
                        LastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        break;
                    default:
                        Console.Error.WriteLine("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception();
                    //break;
                }
            }
            //Next packet will be data
            state = TCPState.Connected;
            return true;
        }

        //PS2 Sending Data
        private bool SendData(TCP tcp)
        {
            if (tcp.SYN == true)
            {
                throw new Exception("Attempt to Connect to an open Port");
            }
            for (int i = 0; i < tcp.Options.Count; i++)
            {
                switch (tcp.Options[i].Code)
                {
                    case 0://End
                    case 1://Nop
                        continue;
                    case 8:
                        //Console.Error.WriteLine("Got TimeStamp");
                        LastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        break;
                    default:
                        Console.Error.WriteLine("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception();
                    //break;
                }
            }
            NumCheckResult Result = CheckNumbers(tcp);
            if (Result == NumCheckResult.GotOldData)
            {
                throw new NotImplementedException();
                //return true;
            }
            if (Result == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }
            if (tcp.GetPayload().Length != 0)
            {
                ReceivedPS2SequenceNumbers.RemoveAt(0);
                ReceivedPS2SequenceNumbers.Add(ExpectedSequenceNumber);
                //Send the Data
                try
                {
                    netStream.Write(tcp.GetPayload(), 0, tcp.GetPayload().Length);
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show("Got IO Error :" + e.ToString());
                    //Connection Lost
                    //Send Shutdown (Untested)
                    PerformRST();
                    open = false;
                    return true;
                }
                unchecked
                {
                    ExpectedSequenceNumber += ((uint)tcp.GetPayload().Length);
                }
                //Done send

                //ACK data
                TCP ret = CreateBasePacket();
                ret.ACK = true;

                PushRecvBuff(ret);
            }
            return true;
        }

        //PS2 responding to server response to PS2 Closing connection
        private bool SendCloseResponseResponse(TCP tcp)
        {
            //Close Part 4, Recive ACK from PS2
            Console.Error.WriteLine("Compleated Close By PS2");
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            if (ResultFIN == NumCheckResult.GotOldData) { return false; }
            if (ResultFIN == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }
            state = TCPState.Closed;
            open = false;
            return true;
        }

        private bool SendResponseToClosedServer(TCP tcp, bool HasACKedFIN)
        {
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            ReceivedPS2SequenceNumbers.RemoveAt(0);
            ReceivedPS2SequenceNumbers.Add(ExpectedSequenceNumber);

            //Expect FIN + ACK
            if (tcp.FIN & (HasACKedFIN | tcp.ACK))
            {
                Console.Error.WriteLine("Compleated Close By Remote");

                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }

                unchecked
                {
                    ExpectedSequenceNumber += 1;
                }
                TCP ret = CreateBasePacket();

                ret.ACK = true;

                PushRecvBuff(ret);
                state = TCPState.ConnectionClosedByPS2AndRemote;
                open = false;
                return true;
            }
            else if (tcp.ACK)
            {
                Console.Error.WriteLine("Got ACK from PS2 during server FIN");
                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { throw new Exception("Bad TCP Number Received"); }
                if (tcp.GetPayload().Length != 0)
                {
                    throw new Exception("Invalid Packet");
                }
                if (MyNumberACKed.WaitOne(0))
                {
                    Console.Error.WriteLine("ACK was for FIN");
                    state = TCPState.ConnectionClosedByRemoteAcknowledged;
                }
                return true;
            }
            throw new Exception("Invalid Packet");
            return false;
        }

        private NumCheckResult CheckNumbers(TCP tcp)
        {
            UInt32 seqNum, oldSeqNum;

            GetAllMyNumbers(out seqNum, out oldSeqNum);

            if (tcp.AcknowledgementNumber != seqNum)
            {
                Console.Error.WriteLine("Outdated Acknowledgement Number, Got " + tcp.AcknowledgementNumber + " Expected " + seqNum);
                if (tcp.AcknowledgementNumber != oldSeqNum)
                {
                    throw new Exception("Unexpected Acknowledgement Number did not Match Old Number of " + oldSeqNum);
                }
            }
            else
            {
                MyNumberACKed.Set();
            }

            if (tcp.SequenceNumber != ExpectedSequenceNumber)
            {
                if (tcp.GetPayload().Length == 0)
                {
                    Console.Error.WriteLine("Unexpected Sequence Number From Act Packet, Got " + tcp.SequenceNumber + " Expected " + ExpectedSequenceNumber);
                }
                else
                {
                    if (ReceivedPS2SequenceNumbers.Contains(tcp.SequenceNumber))
                    {
                        Console.Error.WriteLine("Got an Old Seq Number on an Data packet");
                        return NumCheckResult.GotOldData;
                    }
                    else
                        throw new Exception("Unexpected Sequence Number From Data Packet, Got " + tcp.SequenceNumber + " Expected " + ExpectedSequenceNumber);
                }
            }

            return NumCheckResult.OK;
        }

        private void PerformCloseByPS2()
        {
            lock (clientSentry)
            {
                client.Close();
            }
            netStream = null;
            Console.Error.WriteLine("PS2 has closed connection");
            //Connection Close Part 2, Send ACK to PS2
            ReceivedPS2SequenceNumbers.RemoveAt(0);
            ReceivedPS2SequenceNumbers.Add(ExpectedSequenceNumber);
            unchecked
            {
                ExpectedSequenceNumber += 1;
            }

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            PushRecvBuff(ret);
            state = TCPState.ConnectionClosedByPS2AndRemote;
        }

        private void PerformRST()
        {
            TCP reterr = CreateBasePacket();
            reterr.RST = true;
            PushRecvBuff(reterr);
        }
    }
}
