using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    partial class TCPSession : Session
    {
        private enum TCPState
        {
            None,
            SendingSYN_ACK,
            SentSYN_ACK,
            Connected,
            ConnectionClosedByPS2,
            ConnectionClosedByPS2AndRemote,
            ConnectionClosedByRemote,
            ConnectionClosedByRemoteAcknowledged,
            Closed
        }
        private enum NumCheckResult
        {
            OK,
            GotOldData,
            Bad
        }

        #region ReciveBuffer
        List<TCP> _recvBuff = new List<TCP>();
        private void PushRecvBuff(TCP tcp)
        {
            lock (_recvBuff)
            {
                _recvBuff.Add(tcp);
            }
        }
        private TCP PopRecvBuff()
        {
            lock (_recvBuff)
            {
                if (_recvBuff.Count != 0)
                {
                    TCP tcp = _recvBuff[0];
                    _recvBuff.RemoveAt(0);
                    return tcp;
                }
                else
                    return null;
            }
        }
        #endregion
        //TCP LastDataPacket = null; //Only 1 outstanding data packet from the remote source can exist at a time
        object clientSentry = new object();
        TcpClient client;
        NetworkStream netStream = null;
        TCPState state = TCPState.None;

        UInt16 srcPort = 0; //PS2 Port
        UInt16 destPort = 0; //Remote Port

        //UInt16 WindowSize; //assume zero scale
        UInt16 maxSegmentSize = 1460;//Accesed By Both In and Out Threads, but set only on Connect Thread

        UInt32 lastRecivedTimeStamp; //Accesed By Both In and Out Threads
        Stopwatch timeStamp = new Stopwatch(); //Accesed By Both In and Out Threads
        bool sendTimeStamps = false; //Accesed By Out Thread Only

        UInt32 expectedSequenceNumber; //Accesed By Out Thread Only
        List<UInt32> receivedPS2SequenceNumbers = new List<UInt32>(); //Accesed By Out Thread Only

        #region MySequenceNumber
        object myNumberSentry = new object();
        UInt32 _MySequenceNumber = 1;
        UInt32 _OldMyNumber = 1; //Is set on one thread, and read on another
        private void IncrementMyNumber(UInt32 amount)
        {
            lock (myNumberSentry)
            {
                _OldMyNumber = _MySequenceNumber;
                unchecked
                {
                    _MySequenceNumber += amount;
                }
            }
        }
        private UInt32 GetMyNumber()
        {
            lock (myNumberSentry)
            {
                return _MySequenceNumber;
            }
        }
        private void GetAllMyNumbers(out UInt32 Current, out UInt32 Old)
        {
            lock (myNumberSentry)
            {
                Current = _MySequenceNumber;
                Old = _OldMyNumber;
            }
        }
        ManualResetEvent myNumberACKed = new ManualResetEvent(true);
        #endregion

        public TCPSession(IPAddress parAdapterIP) : base(parAdapterIP) { }

        //recv

        //Async connect

        //send

        public override void Reset()
        {
            Dispose();
        }

        bool open = false;
        public override bool isOpen()
        {
            return open;
        }
        public override void Dispose()
        {
            open = false;
            lock (clientSentry)
            {
                client.Close();
            }
            myNumberACKed.Dispose();
        }

        //CheckNumbers

        //PerformCloseByPS2
        //PerformCloseByRemote

        private TCP CreateBasePacket(byte[] data = null)
        {
            Log_Verb("Creating Base Packet");
            if (data == null) { data = new byte[0]; }
            TCP ret = new TCP(data);

            //and now to setup THE ENTIRE THING
            ret.SourcePort = destPort;
            ret.DestinationPort = srcPort;

            ret.SequenceNumber = GetMyNumber();
            Log_Verb("With MySeq: " + ret.SequenceNumber);
            ret.AcknowledgementNumber = expectedSequenceNumber;
            Log_Verb("With MyAck: " + ret.AcknowledgementNumber);

            //ret.WindowSize = 16 * 1024;
            ret.WindowSize = (UInt16)(2 * maxSegmentSize); //default 2920B (2.85MB)

            if (sendTimeStamps)
            {
                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopTS((UInt32)timeStamp.Elapsed.TotalSeconds, lastRecivedTimeStamp));
            }
            return ret;
        }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.TCPSession, str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.TCPSession, str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.TCPSession, str);
        }
    }
}
