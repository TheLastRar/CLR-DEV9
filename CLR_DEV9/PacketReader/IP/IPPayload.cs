using System;

namespace CLR_DEV9.PacketReader
{
    abstract class IPPayload
    {
        abstract public byte[] GetPayload();
        abstract public void CalculateCheckSum(byte[] srcIP, byte[] dstIP);
        abstract public bool VerifyCheckSum(byte[] srcIP, byte[] dstIP);
        abstract public byte[] GetBytes();
        abstract public byte Protocol
        {
            get;
        }
        abstract public UInt16 Length
        {
            get;
            protected set;
        }
    }
}
