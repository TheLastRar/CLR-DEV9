using System;

namespace CLR_DEV9.PacketReader
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
