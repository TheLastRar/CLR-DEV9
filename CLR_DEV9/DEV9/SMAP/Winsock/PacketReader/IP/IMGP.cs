using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class IGMP : IPPayload
    {
        public byte Type;
        private byte maxResponseTime; //todo implement for values >128
        protected UInt16 checksum;
        public byte[] GroupAddress;
        public override byte Protocol
        {
            get { return 0x02; }
        }

        public override ushort Length
        {
            get
            {
                return 8;
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

        public IGMP(){}

        public IGMP(byte[] buffer, int offset, int Length)
        {
            NetLib.ReadByte08(buffer, ref offset, out Type);
            NetLib.ReadByte08(buffer, ref offset, out maxResponseTime);
            NetLib.ReadUInt16(buffer, ref offset, out checksum);
            NetLib.ReadByteArray(buffer, ref offset, 4, out GroupAddress);
            //TODO version 3
        }

        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = ((Length));
            if ((pHeaderLen & 1) != 0)
            {
                //Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;

            checksum = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            checksum = IPPacket.InternetChecksum(headerSegment);
        }
        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP)
        {
            int pHeaderLen = ((Length));
            if ((pHeaderLen & 1) != 0)
            {
                //Error.WriteLine("OddSizedPacket");
                pHeaderLen += 1;
            }

            byte[] headerSegment = new byte[pHeaderLen];
            int counter = 0;
            NetLib.WriteByteArray(ref headerSegment, ref counter, GetBytes());

            UInt16 CsumCal = IPPacket.InternetChecksum(headerSegment);
            //Error.WriteLine("IGMP Checksum Good = " + (CsumCal == 0));
            return (CsumCal == 0);
        }
        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ref ret, ref counter, Type);
            NetLib.WriteByte08(ref ret, ref counter, maxResponseTime);
            NetLib.WriteUInt16(ref ret, ref counter, checksum);
            NetLib.WriteByteArray(ref ret, ref counter, GroupAddress);
            //TODO version 3
            return ret;
        }
    }
}
