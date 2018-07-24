using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader
{
    abstract class EthernetPayload
    {
        abstract public byte[] GetBytes();
        public abstract UInt16 Length
        {
            get;
            protected set;
        }
    }
}
