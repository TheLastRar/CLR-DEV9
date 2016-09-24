using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
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
                    //Log_Error("EndConnect");
                    client.EndConnect(res);
                }
            }
            catch (SocketException err)
            {
                Log_Error("TCP Connection Error: " + err.Message);
                Log_Error("ErrorCode: " + err.ErrorCode);
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

                //open = true;
                //Log_Error("SendSYNACK packet");
                state = TCPState.SentSYN_ACK;
                TCP ret = new TCP(new byte[] { });
                //Return the fact we connected
                ret.SourcePort = tcp.DestinationPort;
                ret.DestinationPort = tcp.SourcePort;

                ret.SequenceNumber = GetMyNumber();
                IncrementMyNumber(1);

                ret.AcknowledgementNumber = expectedSeqNumber;

                ret.SYN = true;
                ret.ACK = true;
                ret.WindowSize = (UInt16)(2 * maxSegmentSize);
                ret.Options.Add(new TCPopMSS(maxSegmentSize));

                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopWS(0));

                if (sendTimeStamps)
                {
                    ret.Options.Add(new TCPopNOP());
                    ret.Options.Add(new TCPopNOP());
                    ret.Options.Add(new TCPopTS((UInt32)timeStamp.Elapsed.Seconds, lastRecivedTimeStamp));
                }
                //Log_Error("Pushed onto buffer");
                PushRecvBuff(ret);
                //Log_Error(_recvBuff.Count.ToString());
            }
            else
            {
                //failed to connect
                //open = false;
                state = TCPState.CloseCompleted;
                //Recv buffer should be empty
                RaiseEventConnectionClosed();
            }
        }

        public override bool Send(IPPayload payload)
        {
            TCP tcp = (TCP)payload;
            if (destPort != 0)
            {
                if (!(tcp.DestinationPort == destPort && tcp.SourcePort == srcPort))
                {
                    Log_Error("TCP packet invalid for current session (Duplicate key?)");
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
                        state = TCPState.CloseCompleted;
                        //open = false;
                        //PS2 sent RST, clearly not expecting
                        //more data
                        RaiseEventConnectionClosed();
                        return true;
                    }
                }
            }

            switch (state)
            {
                case TCPState.None:
                    return SendConnect(tcp);
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
                        CloseByPS2Stage1();
                        return true;
                    }
                    return SendData(tcp);
                case TCPState.Closing_ClosedByPS2AndRemote_WaitingForAck:
                    return CloseByPS2Stage2(tcp);
                case TCPState.Closing_ClosedByRemote_WaitingForAck:
                    return CloseByRemoteStage2(tcp, false);
                case TCPState.Closing_ClosedByRemoteAcknowledged:
                    return CloseByRemoteStage2(tcp, true);
                case TCPState.CloseCompleted:
                    throw new Exception("Attempt to send data on closed TCP connection");
                default:
                    throw new Exception("Invalid State");
            }
        }

        //PS2 sent SYN
        private bool SendConnect(TCP tcp)
        {
            //Expect SYN Packet
            destPort = tcp.DestinationPort;
            srcPort = tcp.SourcePort;

            if (tcp.SYN == false)
            {
                CloseByRemoteRST();
                Log_Error("Attempt To Send Data On Non Connected Connection");
                return true;
            }
            expectedSeqNumber = tcp.SequenceNumber + 1;
            //Fill out last received numbers
            receivedPS2SeqNumbers.Clear();
            for (int i = 0; i < receivedPS2SeqNumberCount; i++)
            {
                receivedPS2SeqNumbers.Add(tcp.SequenceNumber);
            }
            ResetMyNumbers();

            for (int i = 0; i < tcp.Options.Count; i++)
            {
                switch (tcp.Options[i].Code)
                {
                    case 0: //End
                    case 1: //Nop
                        continue;
                    case 2: //MSS
                        maxSegmentSize = ((TCPopMSS)(tcp.Options[i])).MaxSegmentSize;
                        break;
                    case 3: //WindowSize
                        windowSize = ((TCPopWS)(tcp.Options[i])).WindowScale;
                        break;
                    case 8: //TimeStamp
                        lastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        sendTimeStamps = true;
                        timeStamp.Start();
                        break;
                    default:
                        Log_Error("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception("Got Unknown Option " + tcp.Options[i].Code);
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
            client = new TcpClient(new IPEndPoint(adapterIP, 0));
            IPAddress address = new IPAddress(DestIP);
            client.BeginConnect(address, destPort, new AsyncCallback(AsyncConnectComplete), tcp);
            state = TCPState.SendingSYN_ACK;
            //open = true;
            return true;
        }

        //PS2 responding to our SYN-ACK (by sending ACK)
        private bool SendConnected(TCP tcp)
        {
            //Log_Error("SendConnected");
            if (tcp.SYN == true)
            {
                Log_Error("Attempt to Connect to an operning Port");
                throw new Exception("Attempt to Connect to an operning Port");
            }
            NumCheckResult Result = CheckNumbers(tcp);
            if (Result == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }

            for (int i = 0; i < tcp.Options.Count; i++)
            {
                switch (tcp.Options[i].Code)
                {
                    case 0: //End
                    case 1: //Nop
                        continue;
                    case 8: //Timestamp
                        lastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        break;
                    default:
                        Log_Error("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception("Got Unknown Option " + tcp.Options[i].Code);
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
                Log_Error("Attempt to Connect to an open Port");
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
                        lastRecivedTimeStamp = ((TCPopTS)(tcp.Options[i])).SenderTimeStamp;
                        break;
                    default:
                        Log_Error("Got Unknown Option " + tcp.Options[i].Code);
                        throw new Exception("Got Unknown Option " + tcp.Options[i].Code);
                        //break;
                }
            }

            windowSize = tcp.WindowSize << windowScale;

            NumCheckResult Result = CheckNumbers(tcp);
            uint delta = GetDelta(expectedSeqNumber, tcp.SequenceNumber);
            if (Result == NumCheckResult.GotOldData)
            {
                Log_Verb("[PS2] New Data Offset: " + delta + " bytes");
                Log_Verb("[PS2] New Data Length: " + ((uint)tcp.GetPayload().Length - delta) + " bytes");
            }
            if (Result == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }
            if (tcp.GetPayload().Length != 0)
            {
                if (tcp.GetPayload().Length - delta > 0)
                {
                    Log_Verb("[PS2] Sending: " + tcp.GetPayload().Length + " bytes");
                    receivedPS2SeqNumbers.RemoveAt(0);
                    receivedPS2SeqNumbers.Add(expectedSeqNumber);
                    //Send the Data
                    try
                    {
                        netStream.Write(tcp.GetPayload(), (int)delta, tcp.GetPayload().Length - (int)delta);
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show("Got IO Error: " + e.ToString());
                        //Connection Lost
                        //Send Shutdown by RST (Untested)
                        CloseByRemoteRST();
                        //open = false;
                        return true;
                    }
                    unchecked
                    {
                        expectedSeqNumber += ((uint)tcp.GetPayload().Length - delta);
                    }
                    //Done send
                }
                //ACK data
                Log_Verb("[SRV] ACK Data: " + expectedSeqNumber);
                TCP ret = CreateBasePacket();
                ret.ACK = true;

                PushRecvBuff(ret);
            }
            return true;
        }

        private NumCheckResult CheckNumbers(TCP tcp)
        {
            UInt32 seqNum;
            List<UInt32> oldSeqNums;

            GetAllMyNumbers(out seqNum, out oldSeqNums);

            Log_Verb("CHECK_NUMBERS");
            Log_Verb("[SRV]CurrSeqNumber = " + seqNum + " [PS2]Ack Number = " + tcp.AcknowledgementNumber);
            Log_Verb("[SRV]CurrAckNumber = " + expectedSeqNumber + " [PS2]Seq Number = " + tcp.SequenceNumber);
            Log_Verb("[PS2]Data Length = " + tcp.GetPayload().Length);

            if (tcp.AcknowledgementNumber != seqNum)
            {
                Log_Verb("[PS2]Sent Outdated Acknowledgement Number, Got " + tcp.AcknowledgementNumber + " Expected " + seqNum);
                if (!oldSeqNums.Contains(tcp.AcknowledgementNumber))
                {
                    Log_Error("Unexpected Acknowledgement Number did not Match Old Numbers, Got " + tcp.AcknowledgementNumber + " Expected " + seqNum);
                    throw new Exception("Unexpected Acknowledgement Number did not Match Old Numbers, Got " + tcp.AcknowledgementNumber + " Expected " + seqNum);
                }
            }
            else
            {
                Log_Verb("[PS2]CurrSeqNumber Acknowleged By PS2");
                myNumberACKed.Set();
            }

            if (tcp.SequenceNumber != expectedSeqNumber)
            {
                if (tcp.GetPayload().Length == 0)
                {
                    Log_Verb("[PS2]Sent Unexpected Sequence Number From ACK Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSeqNumber);
                }
                else
                {
                    if (receivedPS2SeqNumbers.Contains(tcp.SequenceNumber))
                    {
                        Log_Error("[PS2]Sent an Old Seq Number on an Data packet, Got " + tcp.SequenceNumber + " Expected " + expectedSeqNumber);
                        return NumCheckResult.GotOldData;
                    }
                    else
                    {
                        Log_Error("[PS2]Sent Unexpected Sequence Number From Data Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSeqNumber);
                        throw new Exception("Unexpected Sequence Number From Data Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSeqNumber);
                    }
                }
            }

            return NumCheckResult.OK;
        }

        private uint GetDelta(uint parExpectedSeq, uint parGotSeq)
        {
            uint delta = parExpectedSeq - parGotSeq;
            if (delta > 0.5 * uint.MaxValue)
            {
                delta = uint.MaxValue - parExpectedSeq + parGotSeq;
                Log_Error("[PS2] SequenceNumber Overflow Detected");
                Log_Error("[PS2] New Data Offset: " + delta + " bytes");
            }
            return delta;
        }

        //On Close by PS2
        //PS2 Sends FIN+ACK
        //CloseByPS2Stage1 sends FIN+ACK, state set to Closing_ClosedByPS2AndRemote
        //(Close in 3 steps)
        //PS2 Sends ACK
        //Connection Closing Finished
        private void CloseByPS2Stage1()
        {
            lock (clientSentry)
            {
                client.Close();
            }
            netStream = null;
            Log_Info("PS2 has closed connection");
            //Connection Close Part 2, Send ACK to PS2
            receivedPS2SeqNumbers.RemoveAt(0);
            receivedPS2SeqNumbers.Add(expectedSeqNumber);
            unchecked
            {
                expectedSeqNumber += 1;
            }

            TCP ret = CreateBasePacket();
            IncrementMyNumber(1);

            ret.ACK = true;
            ret.FIN = true;

            PushRecvBuff(ret);
            state = TCPState.Closing_ClosedByPS2AndRemote_WaitingForAck;
        }
        //PS2 responding to server response to PS2 Closing connection
        private bool CloseByPS2Stage2(TCP tcp)
        {
            //Close Part 4, Recive ACK from PS2
            Log_Info("Compleated Close By PS2");
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            if (ResultFIN == NumCheckResult.GotOldData) { return false; }
            if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }
            state = TCPState.CloseCompleted;
            //recv buffer should be empty
            RaiseEventConnectionClosed();
            //open = false;
            return true;
        }

        //On Close By Server
        //CloseByRemoteStage1 sends FIN+ACK, state set to Closing_ClosedByRemote
        //Either
        //PS2 Sends ACK, state set to ConnectionClosedByRemoteAcknowledged
        //PS2 Then sends FIN
        //Connection Closed
        //
        //or
        //PS2 Sends ACK+FIN
        //Connection Closed
        private bool CloseByRemoteStage2(TCP tcp, bool HasACKedFIN)
        {
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            receivedPS2SeqNumbers.RemoveAt(0);
            receivedPS2SeqNumbers.Add(expectedSeqNumber);

            //Expect FIN + ACK
            if (tcp.FIN & (HasACKedFIN | tcp.ACK))
            {
                Log_Info("Compleated Close By Remote");

                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }

                unchecked
                {
                    expectedSeqNumber += 1;
                }
                TCP ret = CreateBasePacket();

                ret.ACK = true;

                PushRecvBuff(ret);
                state = TCPState.CloseCompleted;
                //open = false;
                return true;
            }
            else if (tcp.ACK)
            {
                Log_Info("Got ACK from PS2 during server FIN");
                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); ; }
                if (tcp.GetPayload().Length != 0)
                {
                    uint delta = GetDelta(expectedSeqNumber, tcp.SequenceNumber);
                    if (delta == 0)
                    {
                        return true;
                    }
                    Log_Error("Invalid Packet, Packet Has Data");

                    throw new Exception("Invalid Packet");
                }
                if (myNumberACKed.WaitOne(0))
                {
                    Log_Info("ACK was for FIN");
                    state = TCPState.Closing_ClosedByRemoteAcknowledged;
                }
                return true;
            }
            Log_Error("Invalid Packet");
            throw new Exception("Invalid Packet");
            //return false;
        }

        //Error on sending data
        private void CloseByRemoteRST()
        {
            TCP reterr = CreateBasePacket();
            reterr.RST = true;
            PushRecvBuff(reterr);

            state = TCPState.CloseCompleted;
        }
    }
}
