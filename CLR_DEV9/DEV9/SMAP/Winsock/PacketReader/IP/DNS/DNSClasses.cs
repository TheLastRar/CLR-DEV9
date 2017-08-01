using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS
{
    class DNSQuestionEntry
    {
        int nameDNSLength;
        byte[] nameBytes;
        string nameStr;
        public string Name
        {
            get
            {
                return nameStr;
            }
        }
        UInt16 _type;
        public UInt16 Type { get { return _type; } }
        UInt16 _class;
        public UInt16 Class { get { return _class; } }

        public virtual byte Length { get { return (byte)(nameDNSLength + 4); } }

        private void ReadDNSString(byte[] buffer, ref int offset, out string value)
        {
            int startOffset = offset;
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
        private void WriteDNSString(byte[] buffer, ref int offset, string value)
        {
            //if (buffer[offset] == 11)
            //{
            //    value = "PtrNamesNotImplemented";
            //}
            string[] spl = value.Split('.');

            foreach (string s in spl)
            {
                if (s.Length == 0) { continue; }
                NetLib.WriteByte08(buffer, ref offset, (byte)s.Length);
                NetLib.WriteCString(buffer, ref offset, s);
                offset -= 1;
            }
            offset += 1;
        }

        public DNSQuestionEntry(string name, UInt16 Qtype, UInt16 Qclass)
        {
            nameStr = name;
            nameDNSLength = nameStr.Length + 2;
            nameBytes = new byte[nameDNSLength];
            int counter = 0;
            WriteDNSString(nameBytes, ref counter, nameStr);
            _type = Qtype;
            _class = Qclass;
        }

        public DNSQuestionEntry(byte[] buffer, int offset)
        {
            int s = offset;
            ReadDNSString(buffer, ref offset, out nameStr);
            nameDNSLength = offset - s;
            NetLib.ReadByteArray(buffer, ref s, nameDNSLength, out nameBytes);
            NetLib.ReadUInt16(buffer, ref offset, out _type);
            NetLib.ReadUInt16(buffer, ref offset, out _class);
        }
        public virtual byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            WriteDNSString(ret, ref counter, Name);
            NetLib.WriteByteArray(ret, ref counter, nameBytes);
            NetLib.WriteUInt16(ret, ref counter, Type);
            NetLib.WriteUInt16(ret, ref counter, Class);
            return ret;
        }
    }
    class DNSResponseEntry : DNSQuestionEntry
    {
        UInt32 ttl;
        public uint TTL { get { return ttl; } }
        //UInt16 DataLength;
        byte[] data;
        public byte[] Data { get { return data; } }

        public override byte Length { get { return (byte)(base.Length + 4 + 2 + data.Length); } }

        public DNSResponseEntry(byte[] buffer, int offset) : base(buffer, offset)
        {
            offset += base.Length;
            UInt16 dataLen;
            NetLib.ReadUInt32(buffer, ref offset, out ttl);
            NetLib.ReadUInt16(buffer, ref offset, out dataLen);
            NetLib.ReadByteArray(buffer, ref offset, dataLen, out data);
        }
        public override byte[] GetBytes()
        {
            byte[] ret = base.GetBytes();
            int counter = base.Length;
            NetLib.WriteUInt32(ret, ref counter, ttl);
            NetLib.WriteUInt16(ret, ref counter, (UInt16)data.Length);
            NetLib.WriteByteArray(ret, ref counter, data);
            return ret;
        }
    }
}
