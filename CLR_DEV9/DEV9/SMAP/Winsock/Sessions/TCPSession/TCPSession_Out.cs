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

                open = true;
                state = TCPState.SentSYN_ACK;
                TCP ret = new TCP(new byte[] { });
                //Return the fact we connected
                ret.SourcePort = tcp.DestinationPort;
                ret.DestinationPort = tcp.SourcePort;

                ret.SequenceNumber = GetMyNumber();
                IncrementMyNumber(1);

                ret.AcknowledgementNumber = expectedSequenceNumber;

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
                PushRecvBuff(ret);
            }
            else
            {
                open = false;
                state = TCPState.None;
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
                        state = TCPState.Closed;
                        open = false;
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
        private bool SendConnect(TCP tcp)
        {
            //Expect SYN Packet
            destPort = tcp.DestinationPort;
            srcPort = tcp.SourcePort;

            if (tcp.SYN == false)
            {
                PerformRST();
                Log_Error("Attempt To Send Data On Non Connected Connection");
                return true;
            }
            expectedSequenceNumber = tcp.SequenceNumber + 1;
            //Fill out last received numbers
            receivedPS2SequenceNumbers.Clear();
            receivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            receivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            receivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            receivedPS2SequenceNumbers.Add(tcp.SequenceNumber);
            receivedPS2SequenceNumbers.Add(tcp.SequenceNumber);

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
                    case 3: //WinScale
                        Log_Info("Got WinScale (Not Supported)");
                        // = ((TCPopWS)(tcp.Options[i])).WindowScale;
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
            open = true;
            return true;
        }

        //PS2 responding to our SYN-ACK (by sending ACK)
        private bool SendConnected(TCP tcp)
        {
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
            NumCheckResult Result = CheckNumbers(tcp);
            uint delta = expectedSequenceNumber - tcp.SequenceNumber;
            if (delta > 0.5 * uint.MaxValue)
            {
                delta = uint.MaxValue - expectedSequenceNumber + tcp.SequenceNumber;
                Log_Error("[PS2] SequenceNumber Overflow Detected");
                Log_Error("[PS2] New Data Offset: " + delta + " bytes");
            }
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
                    receivedPS2SequenceNumbers.RemoveAt(0);
                    receivedPS2SequenceNumbers.Add(expectedSequenceNumber);
                    //Send the Data
                    try
                    {
                        netStream.Write(tcp.GetPayload(), (int)delta, tcp.GetPayload().Length - (int)delta);
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show("Got IO Error: " + e.ToString());
                        //Connection Lost
                        //Send Shutdown (Untested)
                        PerformRST();
                        open = false;
                        return true;
                    }
                    unchecked
                    {
                        expectedSequenceNumber += ((uint)tcp.GetPayload().Length - delta);
                    }
                    //Done send
                }
                //ACK data
                Log_Verb("[SRV] ACK Data: " + expectedSequenceNumber);
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
            Log_Info("Compleated Close By PS2");
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            if (ResultFIN == NumCheckResult.GotOldData) { return false; }
            if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }
            state = TCPState.Closed;
            open = false;
            return true;
        }

        private bool SendResponseToClosedServer(TCP tcp, bool HasACKedFIN)
        {
            NumCheckResult ResultFIN = CheckNumbers(tcp);
            receivedPS2SequenceNumbers.RemoveAt(0);
            receivedPS2SequenceNumbers.Add(expectedSequenceNumber);

            //Expect FIN + ACK
            if (tcp.FIN & (HasACKedFIN | tcp.ACK))
            {
                Log_Info("Compleated Close By Remote");

                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); }

                unchecked
                {
                    expectedSequenceNumber += 1;
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
                Log_Info("Got ACK from PS2 during server FIN");
                if (ResultFIN == NumCheckResult.GotOldData) { return false; }
                if (ResultFIN == NumCheckResult.Bad) { Log_Error("Bad TCP Numbers Received"); throw new Exception("Bad TCP Numbers Received"); ; }
                if (tcp.GetPayload().Length != 0)
                {
                    Log_Error("Invalid Packet");
                    throw new Exception("Invalid Packet");
                }
                if (myNumberACKed.WaitOne(0))
                {
                    Log_Info("ACK was for FIN");
                    state = TCPState.ConnectionClosedByRemoteAcknowledged;
                }
                return true;
            }
            Log_Error("Invalid Packet");
            throw new Exception("Invalid Packet");
            //return false;
        }

        private NumCheckResult CheckNumbers(TCP tcp)
        {
            UInt32 seqNum, oldSeqNum;

            GetAllMyNumbers(out seqNum, out oldSeqNum);

            Log_Verb("CHECK_NUMBERS");
            Log_Verb("[SRV]CurrSeqNumber = " + seqNum + " [PS2]Ack Number = " + tcp.AcknowledgementNumber);
            Log_Verb("[SRV]CurrAckNumber = " + expectedSequenceNumber + " [PS2]Seq Number = " + tcp.SequenceNumber);
            Log_Verb("[PS2]Data Length = " + tcp.GetPayload().Length);

            if (tcp.AcknowledgementNumber != seqNum)
            {
                Log_Verb("[PS2]Sent Outdated Acknowledgement Number, Got " + tcp.AcknowledgementNumber + " Expected " + seqNum);
                if (tcp.AcknowledgementNumber != oldSeqNum)
                {
                    Log_Error("Unexpected Acknowledgement Number did not Match Old Number of " + oldSeqNum);
                    throw new Exception("Unexpected Acknowledgement Number did not Match Old Number of " + oldSeqNum);
                }
            }
            else
            {
                Log_Verb("[PS2]CurrSeqNumber Acknowleged By PS2");
                myNumberACKed.Set();
            }

            if (tcp.SequenceNumber != expectedSequenceNumber)
            {
                if (tcp.GetPayload().Length == 0)
                {
                    Log_Verb("[PS2]Sent Unexpected Sequence Number From ACK Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSequenceNumber);
                }
                else
                {
                    if (receivedPS2SequenceNumbers.Contains(tcp.SequenceNumber))
                    {
                        Log_Error("[PS2]Sent an Old Seq Number on an Data packet, Got " + tcp.SequenceNumber + " Expected " + expectedSequenceNumber);
                        return NumCheckResult.GotOldData;
                    }
                    else
                    {
                        Log_Error("[PS2]Sent Unexpected Sequence Number From Data Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSequenceNumber);
                        throw new Exception("Unexpected Sequence Number From Data Packet, Got " + tcp.SequenceNumber + " Expected " + expectedSequenceNumber);
                    }
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
            Log_Info("PS2 has closed connection");
            //Connection Close Part 2, Send ACK to PS2
            receivedPS2SequenceNumbers.RemoveAt(0);
            receivedPS2SequenceNumbers.Add(expectedSequenceNumber);
            unchecked
            {
                expectedSequenceNumber += 1;
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
