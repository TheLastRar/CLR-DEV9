using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    sealed class IPPacket : EthernetPayload //IPv4 Only
    {
        const byte _verHi = 4 << 4; //Assume it is always 4
        int hLen;
        byte typeOfService;
        #region 'DSCP_TOS'
        public byte Class //Equal to Precedence for TOS
        {
            //DSCP vs TOS
            //Default (xxx000)
            //0 = Routine
            //Assured Forwarding (xxx000, xxx010, xxx100, xxx110)
            //1 = Priority
            //2 = Immediate
            //3 = Flash
            //4 = Flash Override
            //Expedited Forwarding (xxx110, xxx100)
            //5 = Critical
            //Not Defined (xxx000)
            //6 = Internetwork Control
            //7 = Network Control
            get { return (byte)((typeOfService >> 5) & 0x7); }
            set { typeOfService = (byte)((typeOfService & ~(0x7 << 5)) | ((value & 0x7) << 5)); }
        }
        public byte DropProbability
        {
            //Low = 0, Mid = 2, High = 3
            //Except for Expedited, where Low = 3
            //3rd bit set to zero
            get { return (byte)((typeOfService >> 3) & 0x3); }
            set { typeOfService = (byte)((typeOfService & ~(0x3 << 3)) | ((value & 0x3) << 3)); }
        }
        bool ECT
        {
            get { return ((typeOfService & (1 << 1)) != 0); }
            set
            {
                if (value) { typeOfService |= (1 << 1); }
                else { typeOfService &= unchecked((byte)(~(1 << 1))); }
            }
        }
        bool CE
        {
            get { return ((typeOfService & (1)) != 0); }
            set
            {
                if (value) { typeOfService |= (1); }
                else { typeOfService &= unchecked((byte)(~(1))); }
            }
        }
        public bool IsECNCapable
        {
            get { return ECT | CE; }
            set
            {
                if (value)
                {
                    ECT = true;
                }
                else
                {
                    ECT = false;
                    CE = false;
                }
            }
        }
        public bool CongestionEvent //Equivalent to a droped packet
        {
            get { return ECT & CE; }
            set { CE = value; }
        }
        //Legacy
        public bool TOSDelay //1 = low
        {
            get { return ((typeOfService & (1 << 4)) != 0); }
            set
            {
                if (value) { typeOfService |= (1 << 4); }
                else { typeOfService &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public bool TOSThroughout //1 = high
        {
            get { return ((typeOfService & (1 << 3)) != 0); }
            set
            {
                if (value) { typeOfService |= (1 << 3); }
                else { typeOfService &= unchecked((byte)(~(1 << 3))); }
            }
        }
        public bool TOSReliability //1 = High (Ignored on DSCP)
        {
            get { return ((typeOfService & (1 << 2)) != 0); }
            set
            {
                if (value) { typeOfService |= (1 << 2); }
                else { typeOfService &= unchecked((byte)(~(1 << 2))); }
            }
        }
        public bool TOSCost //1 = low (Now ECT)
        {
            get { return ((typeOfService & (1 << 1)) != 0); }
            set
            {
                if (value) { typeOfService |= (1 << 1); }
                else { typeOfService &= unchecked((byte)(~(1 << 1))); }
            }
        }
        //TOSMustBeZero (Now CE)
        #endregion
        UInt16 length;
        public override UInt16 Length
        {
            get
            {
                return length;
            }
            protected set
            {
                length = value;
            }
        }
        private UInt16 id; //used during reassembly fragmented packets
        #region "Fragment"
        private byte fragmentFlags1;
        private byte fragmentFlags2;
        //1st bit reserved
        public bool DoNotFragment
        {
            get { return ((fragmentFlags1 & (1 << 6)) != 0); }
            set
            {
                if (value) { fragmentFlags1 |= (1 << 6); }
                else { fragmentFlags1 &= unchecked((byte)(~(1 << 6))); }
            }
        }
        public bool MoreFragments
        {
            get { return ((fragmentFlags1 & (1 << 5)) != 0); }
            set
            {
                if (value) { fragmentFlags1 |= (1 << 5); }
                else { fragmentFlags1 &= unchecked((byte)(~(1 << 5))); }
            }
        }
        public UInt16 FragmentOffset
        {
            get
            {
                int x = 0;
                byte fF1masked = (byte)(fragmentFlags1 & 0x1F);
                NetLib.ReadUInt16(new byte[] { fragmentFlags1, fragmentFlags2 }, ref x, out UInt16 offset);
                return (UInt16)offset;
            }
        }
        #endregion
        private byte ttl = 128;
        public byte Protocol;
        private UInt16 checksum;
        public byte[] SourceIP = new byte[4];
        public byte[] DestinationIP = new byte[4];
        public List<IPOptions> Options = new List<IPOptions>();

        IPPayload _pl;
        public IPPayload Payload
        {
            get
            {
                return _pl;
            }
        }

        private void ReComputeHeaderLen()
        {
            int opOffset = 20;
            for (int i = 0; i < Options.Count; i++)
            {
                opOffset += Options[i].Length;
            }
            opOffset += opOffset % 4; //needs to be a whole number of 32bits
            hLen = opOffset;
        }

        public override byte[] GetBytes
        {
            get
            {
                CalculateCheckSum(); //ReComputeHeaderLen called in CalculateCheckSum
                _pl.CalculateCheckSum(SourceIP, DestinationIP);

                byte[] ret = new byte[Length];
                int counter = 0;
                NetLib.WriteByte08(ret, ref counter, (byte)(_verHi + (hLen >> 2)));
                NetLib.WriteByte08(ret, ref counter, typeOfService);//DSCP/ECN
                NetLib.WriteUInt16(ret, ref counter, length);

                NetLib.WriteUInt16(ret, ref counter, id);
                NetLib.WriteByte08(ret, ref counter, fragmentFlags1);
                NetLib.WriteByte08(ret, ref counter, fragmentFlags2);

                NetLib.WriteByte08(ret, ref counter, ttl);
                NetLib.WriteByte08(ret, ref counter, Protocol);
                NetLib.WriteUInt16(ret, ref counter, checksum); //header csum

                NetLib.WriteByteArray(ret, ref counter, SourceIP);
                NetLib.WriteByteArray(ret, ref counter, DestinationIP); ;

                //options
                for (int i = 0; i < Options.Count; i++)
                {
                    NetLib.WriteByteArray(ret, ref counter, Options[i].GetBytes());
                }
                counter = hLen;

                byte[] plBytes = _pl.GetBytes();
                NetLib.WriteByteArray(ret, ref counter, plBytes);
                return ret;
            }
        }
        //source ip
        //dest ip
        public IPPacket(IPPayload pl)
        {
            _pl = pl;
            hLen = 20;
            Length = (UInt16)(pl.Length + hLen);
            Protocol = _pl.Protocol;
        }

        public IPPacket(ICMP icmpkt)
        {
            if ((icmpkt.Data[0] & 0xF0) == (4 << 4))
            {
                ReadBuffer(icmpkt.Data, 0, icmpkt.Data.Length, true);
            }
            else
            {
                //RE Outbreak creates malformed ICMP packets
                //the data in them is usable, but it's in the
                //wrong place, search for it.
                //This issue occurs on real hardware, so it's
                //not an emulation issue.
                Log_Error("Malformed ICMP Packet");
                int off = 1;
                while ((icmpkt.Data[off] & 0xF0) != (4 << 4))
                {
                    off += 1;
                }
                Log_Error("Payload delayed " + off + " bytes");
                ReadBuffer(icmpkt.Data, off, icmpkt.Data.Length, true);
            }
        }

        public IPPacket(EthernetFrame Ef)
        {
            ReadBuffer(Ef.RawPacket.buffer, Ef.HeaderLength, Ef.RawPacket.size, false);
        }

        private void ReadBuffer(byte[] buffer, int offset, int bufferSize, bool fromICMP)
        {
            int initialOffset = offset;
            int pktOffset = offset;

            //Bits 0-31
            byte v_hl;
            NetLib.ReadByte08(buffer, ref pktOffset, out v_hl);
            hLen = ((v_hl & 0xF) << 2);
            NetLib.ReadByte08(buffer, ref pktOffset, out typeOfService); //TODO, Implement this

            //Not sure PS2 supports this

            //Log_Error("Class :" + Class.ToString());
            ////(DSCP support)
            //Log_Error("DropValue :" + DropProbability.ToString());
            //Log_Error("Supports ECN :" + IsECNCapable.ToString());
            //Log_Error("Congestion :" + CongestionEvent.ToString());
            ////TOS Support
            //Log_Error("LowDelay :" + TOSDelay.ToString());
            //Log_Error("HighThroughput :" + TOSThroughout.ToString());
            //Log_Error("LowCost :" + TOSCost.ToString());

            NetLib.ReadUInt16(buffer, ref pktOffset, out length);
            if (length > bufferSize - offset)
            {
                if (!fromICMP) { Log_Error("Unexpected Length"); }
                length = (UInt16)(bufferSize - offset);
            }

            //Bits 32-63
            NetLib.ReadUInt16(buffer, ref pktOffset, out id); //Send packets with unique IDs
            NetLib.ReadByte08(buffer, ref pktOffset, out fragmentFlags1);
            NetLib.ReadByte08(buffer, ref pktOffset, out fragmentFlags2);
            if (MoreFragments | FragmentOffset != 0)
            {
                Log_Error("FragmentedPacket");
                throw new NotImplementedException("Fragmented Packets are not supported");
            }

            //Bits 64-95
            NetLib.ReadByte08(buffer, ref pktOffset, out ttl);
            NetLib.ReadByte08(buffer, ref pktOffset, out Protocol);
            NetLib.ReadUInt16(buffer, ref pktOffset, out checksum);

            //Bits 96-127
            NetLib.ReadByteArray(buffer, ref pktOffset, 4, out SourceIP);
            //Bits 128-159
            NetLib.ReadByteArray(buffer, ref pktOffset, 4, out DestinationIP);
            //WriteLine("Target IP :" + DestinationIP[0] + "." + DestinationIP[1] + "." + DestinationIP[2] + "." + DestinationIP[3]);

            //Bits 160+
            if (hLen > 20) //IP options (if any)
            {
                bool opReadFin = false;
                do
                {
                    byte opKind = buffer[pktOffset];
                    byte opLen = buffer[pktOffset + 1];
                    switch (opKind)
                    {
                        case 0:
                            opReadFin = true;
                            break;
                        case 1:
                            Options.Add(new IPopNOP());
                            pktOffset += 1;
                            continue;
                        case 148:
                            Options.Add(new IPopRouterAlert(buffer, offset));
                            break;
                        default:
                            Log_Error("Got IP Unknown Option " + opKind + " with len " + opLen);
                            throw new Exception("Got IP Unknown Option " + opKind + " with len " + opLen);
                            //break;
                    }
                    pktOffset += opLen;
                    if (pktOffset == initialOffset + hLen)
                    {
                        opReadFin = true;
                    }
                } while (opReadFin == false);
            }
            pktOffset = initialOffset + hLen;

            switch (Protocol) //(Prase Payload)
            {
                case (byte)IPType.ICMP:
                    _pl = new ICMP(buffer, pktOffset, Length - hLen);
                    //((ICMP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                case (byte)IPType.IGMP:
                    _pl = new IGMP(buffer, pktOffset, Length - hLen);
                    //((ICMP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                case (byte)IPType.TCP:
                    _pl = new TCP(buffer, pktOffset, Length - hLen);
                    //((TCP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                case (byte)IPType.UDP:
                    _pl = new UDP(buffer, pktOffset, Length - hLen);
                    //((UDP)_pl).VerifyCheckSum(SourceIP, DestinationIP);
                    break;
                default:
                    _pl = new IPUnkown(buffer, pktOffset, Length - hLen);
                    Log_Error("Unkown IPv4 Protocol " + Protocol.ToString("X2"));
                    break;
            }
        }
        private void CalculateCheckSum()
        {
            //if (!(i == 5)) //checksum feild is 10-11th byte (5th short), which is skipped
            ReComputeHeaderLen();
            byte[] headerSegment = new byte[hLen];
            int counter = 0;
            NetLib.WriteByte08(headerSegment, ref counter, (byte)(_verHi + (hLen >> 2)));
            NetLib.WriteByte08(headerSegment, ref counter, typeOfService);//DSCP/ECN
            NetLib.WriteUInt16(headerSegment, ref counter, length);

            NetLib.WriteUInt16(headerSegment, ref counter, id);
            NetLib.WriteByte08(headerSegment, ref counter, fragmentFlags1);
            NetLib.WriteByte08(headerSegment, ref counter, fragmentFlags2);

            NetLib.WriteByte08(headerSegment, ref counter, ttl);
            NetLib.WriteByte08(headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(headerSegment, ref counter, 0); //header csum

            NetLib.WriteByteArray(headerSegment, ref counter, SourceIP);
            NetLib.WriteByteArray(headerSegment, ref counter, DestinationIP);

            //options
            for (int i = 0; i < Options.Count; i++)
            {
                NetLib.WriteByteArray(headerSegment, ref counter, Options[i].GetBytes());
            }
            counter = hLen;

            checksum = InternetChecksum(headerSegment);
        }
        public bool VerifyCheckSum()
        {
            ReComputeHeaderLen();
            byte[] headerSegment = new byte[hLen];
            int counter = 0;
            NetLib.WriteByte08(headerSegment, ref counter, (byte)(_verHi + (hLen >> 2)));
            NetLib.WriteByte08(headerSegment, ref counter, typeOfService);//DSCP/ECN
            NetLib.WriteUInt16(headerSegment, ref counter, length);

            NetLib.WriteUInt16(headerSegment, ref counter, id);
            NetLib.WriteByte08(headerSegment, ref counter, fragmentFlags1);
            NetLib.WriteByte08(headerSegment, ref counter, fragmentFlags2);

            NetLib.WriteByte08(headerSegment, ref counter, ttl);
            NetLib.WriteByte08(headerSegment, ref counter, Protocol);
            NetLib.WriteUInt16(headerSegment, ref counter, checksum); //header csum

            NetLib.WriteByteArray(headerSegment, ref counter, SourceIP);
            NetLib.WriteByteArray(headerSegment, ref counter, DestinationIP);

            //options
            for (int i = 0; i < Options.Count; i++)
            {
                NetLib.WriteByteArray(headerSegment, ref counter, Options[i].GetBytes());
            }
            counter = hLen;

            UInt16 CsumCal = InternetChecksum(headerSegment);
            return (CsumCal == 0);
        }
        public static ushort InternetChecksum(byte[] buffer)
        {
            //source http://stackoverflow.com/a/2201090
            //byte[] buffer = value.ToArray();
            int length = buffer.Length;
            int i = 0;
            UInt32 sum = 0;
            UInt32 data = 0;
            while (length > 1)
            {
                data = 0;
                data = (UInt32)(
                ((UInt32)(buffer[i]) << 8) | ((UInt32)(buffer[i + 1]) & 0xFF)
                );

                sum += data;
                if ((sum & 0xFFFF0000) > 0)
                {
                    sum = sum & 0xFFFF;
                    sum += 1;
                }

                i += 2;
                length -= 2;
            }

            if (length > 0)
            {
                sum += (UInt32)(buffer[i] << 8);
                //sum += (UInt32)(buffer[i]);
                if ((sum & 0xFFFF0000) > 0)
                {
                    sum = sum & 0xFFFF;
                    sum += 1;
                }
            }
            sum = ~sum;
            sum = sum & 0xFFFF;
            return (UInt16)sum;
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.IPPacket, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.IPPacket, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.IPPacket, str);
        }
    }
}
