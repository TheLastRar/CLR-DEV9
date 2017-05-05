using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS
{

    class DNSQuestionEntry
    {
        int nameDNSLength;
        public string Name;
        public UInt16 Type;
        public UInt16 Class;

        public virtual byte Length { get { return (byte)(nameDNSLength + 4); } }

        private void ReadDNSString(byte[] buffer, ref int offset, out string value)
        {
            value = "";
            while (buffer[offset] != 0)
            {
                int len = buffer[offset];
                string o;

                if (len >= 192)
                {
                    byte[] addrB;
                    DataLib.ReadByteArray(buffer, ref offset, 2, out addrB);

                    addrB[0] &= unchecked((byte)~0xC0);
                    UInt16 addr;
                    int tmp = 0;
                    NetLib.ReadUInt16(addrB, ref tmp, out addr);
                    tmp = addr;
                    ReadDNSString(buffer, ref tmp, out o);

                    value += o + ".";
                    offset -= 1;
                    break;
                }
                else
                {
                    offset += 1;
                    NetLib.ReadCString(buffer, ref offset, len, out o);
                }
                offset -= 1;
                value += o + ".";
            }
            value = value.Substring(0, value.Length - 1);
            offset += 1;
        }
        private void WriteDNSString(ref byte[] buffer, ref int offset, string value)
        {
            //if (buffer[offset] == 11)
            //{
            //    value = "PtrNamesNotImplemented";
            //}
            string[] spl = value.Split('.');

            foreach (string s in spl)
            {
                if (s.Length == 0) { continue; }
                NetLib.WriteByte08(ref buffer, ref offset, (byte)s.Length);
                NetLib.WriteCString(ref buffer, ref offset, s);
                offset -= 1;
            }
            offset += 1;
        }

        public DNSQuestionEntry(byte[] buffer, int offset)
        {
            int s = offset;
            ReadDNSString(buffer, ref offset, out Name);
            nameDNSLength = offset - s;
            NetLib.ReadUInt16(buffer, ref offset, out Type);
            NetLib.ReadUInt16(buffer, ref offset, out Class);
        }
        public virtual byte[] GetBytes()
        {
            nameDNSLength = Name.Length + 2;
            byte[] ret = new byte[Length];
            int counter = 0;
            WriteDNSString(ref ret, ref counter, Name);
            NetLib.WriteUInt16(ref ret, ref counter, Type);
            NetLib.WriteUInt16(ref ret, ref counter, Class);
            return ret;
        }
    }
    class DNSResponseEntry : DNSQuestionEntry
    {
        public UInt32 TTL;
        //UInt16 DataLength;
        public byte[] Data;

        public override byte Length { get { return (byte)(base.Length + 4 + 2 + Data.Length); } }

        public DNSResponseEntry(byte[] buffer, int offset) : base(buffer, offset)
        {
            offset += base.Length;
            UInt16 dataLen;
            NetLib.ReadUInt32(buffer, ref offset, out TTL);
            NetLib.ReadUInt16(buffer, ref offset, out dataLen);
            NetLib.ReadByteArray(buffer, ref offset, dataLen, out Data);
        }
        public override byte[] GetBytes()
        {
            byte[] ret = base.GetBytes();
            int counter = base.Length;
            NetLib.WriteUInt32(ref ret, ref counter, TTL);
            NetLib.WriteUInt16(ref ret, ref counter, (UInt16)Data.Length);
            NetLib.WriteByteArray(ref ret, ref counter, Data);
            return ret;
        }
    }
}
