using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader
{
    abstract class EthernetPayload
    {
        //abstract public byte[] GetPayload();
        public abstract UInt16 Length
        {
            get;
            protected set;
        }
        public abstract byte[] GetBytes
        {
            get;
        }
    }
}
