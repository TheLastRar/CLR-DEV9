using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS
{

    class DNSQuestionEntry
    {
        int nameDNSLength;

        private string _name;
        public string Name { get { return _name; } }
        public UInt16 _type;
        public UInt16 Type { get { return _type; } }
        public UInt16 _class;
        public UInt16 Class { get { return _class; } }

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

        public DNSQuestionEntry(string name, UInt16 type, UInt16 class_)
        {
            _name = name;
            _type = type;
            _class = class_;
            nameDNSLength = Name.Length + 2;
        }
        //TODO, add a Recompute length call, and call that in DNS.GetBytes()
        public DNSQuestionEntry(byte[] buffer, int offset)
        {
            int s = offset;
            ReadDNSString(buffer, ref offset, out _name);
            nameDNSLength = offset - s;
            NetLib.ReadUInt16(buffer, ref offset, out _type);
            NetLib.ReadUInt16(buffer, ref offset, out _class);
        }
        public virtual byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            WriteDNSString(ref ret, ref counter, _name);
            NetLib.WriteUInt16(ref ret, ref counter, _type);
            NetLib.WriteUInt16(ref ret, ref counter, _class);
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
