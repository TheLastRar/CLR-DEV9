using System;
using System.Net;

namespace CLR_DEV9.PacketReader
{
    class ICMP : IPPayload
    {
        public byte Type;
        public byte Code;
        protected UInt16 Checksum;
        public override byte Protocol
        {
            get { return 0x01; }
        }

        public byte[] HeaderData = new byte[4];
        public byte[] Data = new byte[0];

        public override ushort Length
        {
            get
            {
                return (UInt16)(4 + HeaderData.Length + Data.Length);
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }
        public override byte[] GetPayload()
        {
            throw new NotImplementedException();
        }
        public ICMP(byte[] data)
        {
            Data = data;
        }

        public ICMP(EthernetFrame Ef, int offset, int Length) //Length = IP payload len
        {
            NetLib.ReadByte08(Ef.RawPacket.buffer, ref offset, out Type);
            //Console.Error.WriteLine("Type = " + Type);
            NetLib.ReadByte08(Ef.RawPacket.buffer, ref offset, out Code);
            //Console.Error.WriteLine("Code = " + Code);
            NetLib.ReadUInt16(Ef.RawPacket.buffer, ref offset, out Checksum);
            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref offset,4, out HeaderData);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref offset, Length - 8, out HeaderData);
        }
        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = ((Length));
            if ((pHeaderLen & 1) != 0)
            {
                //Console.Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            Checksum = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            Checksum = IPPacket.InternetChecksum(headerSegment);
        }
        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = ((Length));
            if ((pHeaderLen & 1) != 0)
            {
                //Console.Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }
            
            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            UInt16 CsumCal = IPPacket.InternetChecksum(headerSegment);
            //Console.Error.WriteLine("IMCP Checksum Good = " + (CsumCal == 0));
            return (CsumCal == 0);
        }
        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ref ret, ref counter, Type);
            NetLib.WriteByte08(ref ret, ref counter, Code);
            NetLib.WriteUInt16(ref ret, ref counter, Checksum);
            NetLib.WriteByteArray(ref ret, ref counter, HeaderData);
            NetLib.WriteByteArray(ref ret, ref counter, Data);
            return ret;
        }
    }
}
