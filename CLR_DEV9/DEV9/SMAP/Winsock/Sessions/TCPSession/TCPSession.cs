using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
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
            //Closing_ClosedByPS2,
            Closing_ClosedByPS2AndRemote_WaitingForAck,
            Closing_ClosedByRemote_WaitingForAck,
            Closing_ClosedByRemoteAcknowledged,
            CloseCompleted
        }
        private enum NumCheckResult
        {
            OK,
            GotOldData,
            Bad
        }

        #region ReciveBuffer
        //List<TCP> _recvBuff = new List<TCP>();
        ConcurrentQueue<TCP> _recvBuff = new ConcurrentQueue<TCP>();
        private void PushRecvBuff(TCP tcp)
        {
            //Log_Error("Fake TCP Packet Pushed");
            _recvBuff.Enqueue(tcp);
        }
        private TCP PopRecvBuff()
        {
            TCP tcp;
            if (_recvBuff.TryDequeue(out tcp))
            {
                //if (state == TCPState.SentSYN_ACK)
                //{
                //    Log_Error("Fake TCP Packet Poped");
                //}
                return tcp;
            }
            else
            {
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

        UInt16 maxSegmentSize = 1460; //Accesed By Both In and Out Threads, but set only on Connect Thread
        int windowScale = 0;
        volatile int windowSize = 1460;

        UInt32 lastRecivedTimeStamp; //Accesed By Both In and Out Threads
        Stopwatch timeStamp = new Stopwatch(); //Accesed By Both In and Out Threads
        bool sendTimeStamps = false; //Accesed By Out Thread Only

        const int receivedPS2SeqNumberCount = 5;
        UInt32 expectedSeqNumber; //Accesed By Out Thread Only
        List<UInt32> receivedPS2SeqNumbers = new List<UInt32>(); //Accesed By Out Thread Only

        #region MySequenceNumber
        object myNumberSentry = new object();
        const int oldMyNumCount = 2;
        UInt32 _MySequenceNumber = 1;
        List<UInt32> _OldMyNumbers = new List<UInt32>();
        private void IncrementMyNumber(UInt32 amount)
        {
            lock (myNumberSentry)
            {
                //_OldMyNumber = _MySequenceNumber;
                _OldMyNumbers.Add(_MySequenceNumber);
                _OldMyNumbers.RemoveAt(0);
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
        private void GetAllMyNumbers(out UInt32 Current, out List<UInt32> Old)
        {
            Old = new List<UInt32>();
            lock (myNumberSentry)
            {
                Current = _MySequenceNumber;
                Old.AddRange(_OldMyNumbers);
            }
        }
        private void ResetMyNumbers()
        {
            lock (myNumberSentry)
            {
                _MySequenceNumber = 1;
                _OldMyNumbers.Clear();
                for (int i = 0; i < oldMyNumCount; i++)
                {
                    _OldMyNumbers.Add(1);
                }
            }
        }
        ManualResetEvent myNumberACKed = new ManualResetEvent(true);
        #endregion

        public TCPSession(ConnectionKey parKey, IPAddress parAdapterIP) : base(parKey, parAdapterIP) { }

        //recv

        //Async connect

        //send

        public override void Reset()
        {
            Dispose();
            RaiseEventConnectionClosed();
        }

        //bool open = false;
        //public override bool isOpen()
        //{
        //    return open;
        //}
        public override void Dispose()
        {
            //open = false;
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
            ret.AcknowledgementNumber = expectedSeqNumber;
            Log_Verb("With MyAck: " + ret.AcknowledgementNumber);

            //ret.WindowSize = 16 * 1024;
            ret.WindowSize = (UInt16)(2 * maxSegmentSize); //default 2920B (2.85KB)

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
