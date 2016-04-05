using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class TCP : IPPayload
    {
        public UInt16 SourcePort;
        public UInt16 DestinationPort;
        public UInt32 SequenceNumber;
        public UInt32 AcknowledgementNumber;
        protected byte dataOffsetAndNS_Flag;
        protected int headerLength //Can have varying Header Len
        //Need to account for this at packet creation
        {
            get
            {
                return (dataOffsetAndNS_Flag >> 4) << 2;
            }
            set
            {
                byte NS = (byte)(dataOffsetAndNS_Flag & 1);
                dataOffsetAndNS_Flag = (byte)((value >> 2) << 4);
                dataOffsetAndNS_Flag |= NS;
            }
        }
        public bool NS
        {
            get { return ((dataOffsetAndNS_Flag & 1) != 0); }
            set
            {
                if (value) { dataOffsetAndNS_Flag |= (1); }
                else { dataOffsetAndNS_Flag &= unchecked((byte)(~(1))); }
            }
        }
        byte flags;
        public UInt16 WindowSize;
        protected UInt16 checksum;
        protected UInt16 urgentPointer;
        public List<TCPOption> Options = new List<TCPOption>();
        byte[] data;
        public override byte Protocol
        {
            get { return (byte)IPType.TCP; }
        }
        public override ushort Length
        {
            get
            {
                ReComputeHeaderLen();
                return (UInt16)(data.Length + headerLength);
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        #region 'Flags'
        public bool CWR
        {
            get { return ((flags & (1 << 7)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool ECE
        {
            get { return ((flags & (1 << 6)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool URG
        {
            get { return ((flags & (1 << 5)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool ACK
        {
            get { return ((flags & (1 << 4)) != 0); }
            set
            {
                if (value) { flags |= (1 << 4); }
                else { flags &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool PSH
        {
            get { return ((flags & (1 << 3)) != 0); }
            set
            {
                if (value) { flags |= (1 << 3); }
                else { flags &= unchecked((byte)(~(1 << 3))); }
            }
        }
        public bool RST
        {
            get { return ((flags & (1 << 2)) != 0); }
            set
            {
                if (value) { flags |= (1 << 2); }
                else { flags &= unchecked((byte)(~(1 << 2))); }
            }
        }
        public bool SYN
        {
            get { return ((flags & (1 << 1)) != 0); }
            set
            {
                if (value) { flags |= (1 << 1); }
                else { flags &= unchecked((byte)(~(1 << 1))); }
            }
        }
        public bool FIN
        {
            get { return ((flags & (1)) != 0); }
            set
            {
                if (value) { flags |= (1); }
                else { flags &= unchecked((byte)(~(1))); }
            }
        }
        #endregion

        private void ReComputeHeaderLen()
        {
            int opOffset = 20;
            for (int i = 0; i < Options.Count; i++)
            {
                opOffset += Options[i].Length;
            }
            opOffset += opOffset % 4; //needs to be a whole number of 32bits
            headerLength = opOffset;
        }

        public override byte[] GetPayload()
        {
            return data;
        }
        public TCP(byte[] payload) //Length = IP payload len
        {
            data = payload;
        }
        public TCP(byte[] buffer, int offset, int parLength) //Length = IP payload len
        {
            int initialOffset = offset;
            //Bits 0-31
            NetLib.ReadUInt16(buffer, ref offset, out SourcePort);
            //Error.WriteLine("src port=" + SourcePort); 
            NetLib.ReadUInt16(buffer, ref offset, out DestinationPort);
            //Error.WriteLine("dts port=" + DestinationPort);

            //Bits 32-63
            NetLib.ReadUInt32(buffer, ref offset, out SequenceNumber);
            //Error.WriteLine("seq num=" + SequenceNumber); //Where in the stream the start of the payload is

            //Bits 64-95
            NetLib.ReadUInt32(buffer, ref offset, out AcknowledgementNumber);
            //Error.WriteLine("ack num=" + AcknowledgmentNumber); //the next expected byte(seq) number

            //Bits 96-127
            NetLib.ReadByte08(buffer, ref offset, out dataOffsetAndNS_Flag);
            //Error.WriteLine("TCP hlen=" + HeaderLength);
            NetLib.ReadByte08(buffer, ref offset, out flags);
            NetLib.ReadUInt16(buffer, ref offset, out WindowSize);
            //Error.WriteLine("win Size=" + WindowSize);

            //Bits 127-159
            NetLib.ReadUInt16(buffer, ref offset, out checksum);
            NetLib.ReadUInt16(buffer, ref offset, out urgentPointer);
            //Error.WriteLine("urg ptr=" + UrgentPointer);

            //Bits 160+
            if (headerLength > 20) //TCP options
            {
                bool opReadFin = false;
                do
                {
                    byte opKind = buffer[offset];
                    byte opLen = buffer[offset + 1];
                    switch (opKind)
                    {
                        case 0:
                            //Error.WriteLine("Got End of Options List @ " + (op_offset-offset-1));
                            opReadFin = true;
                            break;
                        case 1:
                            //Error.WriteLine("Got NOP");
                            Options.Add(new TCPopNOP());
                            offset += 1;
                            continue;
                        case 2:
                            //Error.WriteLine("Got MMS");
                            Options.Add(new TCPopMSS(buffer, offset));
                            break;
                        case 3:
                            Options.Add(new TCPopWS(buffer, offset));
                            break;
                        case 8:
                            //Error.WriteLine("Got Timestamp");
                            Options.Add(new TCPopTS(buffer, offset));
                            break;
                        default:
                            Log_Error("Got TCP Unknown Option " + opKind + "with len" + opLen);
                            break;
                    }
                    offset += opLen;
                    if (offset == initialOffset + headerLength)
                    {
                        //Error.WriteLine("Reached end of Options");
                        opReadFin = true;
                    }
                } while (opReadFin == false);
            }
            offset = initialOffset + headerLength;

            NetLib.ReadByteArray(buffer, ref offset, parLength - headerLength, out data);
            //AllDone
        }

        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP)
        {
            Int16 TCPLength = (Int16)(headerLength + data.Length);
            int pHeaderLen = (12 + TCPLength);
            if ((pHeaderLen & 1) != 0)
            {
                //Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, (UInt16)TCPLength);
            //Pseudo Header added
            //Rest of data is normal Header+data (with zerored checksum feild)
            //Null Checksum
            checksum = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            checksum = IPPacket.InternetChecksum(headerSegment);
        }

        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP)
        {
            UInt16 TCPLength = (UInt16)(Length);
            int pHeaderLen = (12 + TCPLength);
            if ((pHeaderLen & 1) != 0)
            {
                //Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            NetLib.WriteByteArray(ref headerSegment, ref counter, srcIP);
            NetLib.WriteByteArray(ref headerSegment, ref counter, dstIP);
            counter += 1;//[8] = 0
            NetLib.WriteByte08(ref headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(ref headerSegment, ref counter, (UInt16)TCPLength);
            //Pseudo Header added
            //Rest of data is normal neader+data
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            UInt16 CsumCal = IPPacket.InternetChecksum(headerSegment);
            //Error.WriteLine("Checksum Good = " + (CsumCal == 0));
            return (CsumCal == 0);
        }

        public override byte[] GetBytes()
        {
            int len = Length;
            byte[] ret = new byte[len];
            int counter = 0;
            NetLib.WriteUInt16(ref ret, ref counter, SourcePort);
            NetLib.WriteUInt16(ref ret, ref counter, DestinationPort);
            NetLib.WriteUInt32(ref ret, ref counter, SequenceNumber);
            NetLib.WriteUInt32(ref ret, ref counter, AcknowledgementNumber);
            NetLib.WriteByte08(ref ret, ref counter, dataOffsetAndNS_Flag);
            NetLib.WriteByte08(ref ret, ref counter, flags);
            NetLib.WriteUInt16(ref ret, ref counter, WindowSize);
            NetLib.WriteUInt16(ref ret, ref counter, checksum);
            NetLib.WriteUInt16(ref ret, ref counter, urgentPointer);

            //options
            for (int i = 0; i < Options.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Options[i].GetBytes());
            }
            counter = headerLength;
            NetLib.WriteByteArray(ref ret, ref counter, data);
            return ret;
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.TCPPacket, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.TCPPacket, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.TCPPacket, str);
        }
    }
}
