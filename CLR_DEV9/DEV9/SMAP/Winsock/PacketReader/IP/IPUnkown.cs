using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    class IPUnkown : IPPayload
    {
        public override UInt16 Length
        {
            get
            {
                return (UInt16)data.Length;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }
        byte[] data;
        public override byte Protocol
        {
            get { return (byte)IPType.UDP; }
        }
        public override byte[] GetPayload()
        {
            return data;
        }
        public IPUnkown(byte[] parData)
        {
            data = parData;
        }
        public IPUnkown(byte[] buffer, int offset, int parLength)
        {
            //Bits 0+
            NetLib.ReadByteArray(buffer, ref offset, parLength, out data);
            //AllDone
        }
        public override void CalculateCheckSum(byte[] srcIP, byte[] dstIP) { }
        public override bool VerifyCheckSum(byte[] srcIP, byte[] dstIP) { return true; }
        public override byte[] GetBytes()
        {
            return data;
        }
    }
}
