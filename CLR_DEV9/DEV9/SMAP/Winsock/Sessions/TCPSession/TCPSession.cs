using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        List<TCP> _recvbuff = new List<TCP>();
        private void PushRecvBuff(TCP tcp)
        {
            lock (_recvbuff)
            {
                _recvbuff.Add(tcp);
            }
        }
        private TCP PopRecvBuff()
        {
            lock (_recvbuff)
            {
                if (_recvbuff.Count != 0)
                {
                    TCP tcp = _recvbuff[0];
                    _recvbuff.RemoveAt(0);
                    return tcp;
                }
                else
                    return null;
            }
        }
        #endregion
        //TCP LastDataPacket = null; //Only 1 outstanding data packet from the remote source can exist at a time
        Object clientSentry = new Object();
        TcpClient client;
        NetworkStream netStream = null;
        TCPState state = TCPState.None;

        UInt16 SrcPort = 0; //PS2 Port
        UInt16 DestPort = 0; //Remote Port

        //UInt16 WindowSize; //assume zero scale
        UInt16 MaxSegmentSize = 1460;//Accesed By Both In and Out Threads, but set only on Connect Thread

        UInt32 LastRecivedTimeStamp; //Accesed By Both In and Out Threads
        Stopwatch TimeStamp = new Stopwatch(); //Accesed By Both In and Out Threads
        bool SendTimeStamps = false; //Accesed By Out Thread Only

        UInt32 ExpectedSequenceNumber; //Accesed By Out Thread Only
        List<UInt32> ReceivedPS2SequenceNumbers = new List<UInt32>(); //Accesed By Out Thread Only

        #region MySequenceNumber
        Object MyNumberSentry = new Object();
        UInt32 _MySequenceNumber = 1;
        UInt32 _OldMyNumber = 1; //Is set on one thread, and read on another
        private void IncrementMyNumber(UInt32 amount)
        {
            lock (MyNumberSentry)
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
            lock (MyNumberSentry)
            {
                return _MySequenceNumber;
            }
        }
        private void GetAllMyNumbers(out UInt32 Current, out UInt32 Old)
        {
            lock (MyNumberSentry)
            {
                Current = _MySequenceNumber;
                Old = _OldMyNumber;
            }
        }
        ManualResetEvent MyNumberACKed = new ManualResetEvent(true);
        #endregion

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
            MyNumberACKed.Dispose();
        }

        //CheckNumbers

        //PerformCloseByPS2
        //PerformCloseByRemote

        private TCP CreateBasePacket(byte[] data = null)
        {
            if (data == null) { data = new byte[0]; }
            TCP ret = new TCP(data);

            //and now to setup THE ENTIRE THING
            ret.SourcePort = DestPort;
            ret.DestinationPort = SrcPort;

            ret.SequenceNumber = GetMyNumber();

            ret.AcknowledgementNumber = ExpectedSequenceNumber;

            //ret.WindowSize = 16 * 1024;
            ret.WindowSize = (UInt16)(2 * MaxSegmentSize); //default 2920B (2.85MB)

            if (SendTimeStamps)
            {
                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopNOP());
                ret.Options.Add(new TCPopTS((UInt32)TimeStamp.Elapsed.TotalSeconds, LastRecivedTimeStamp));
            }
            return ret;
        }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.TCP, "TCPSession", str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.TCP, "TCPSession", str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.TCP, "TCPSession", str);
        }
    }
}
