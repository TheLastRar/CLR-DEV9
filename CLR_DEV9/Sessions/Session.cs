using CLR_DEV9.PacketReader;
using System;

namespace CLR_DEV9.Sessions
{
    abstract class Session : IDisposable
    {
        public byte[] SourceIP;
        public byte[] DestIP;

        public abstract IPPayload recv();
        public abstract bool send(IPPayload payload);
        public abstract bool isOpen();
        public abstract void Dispose();

        //public abstract void Dispose();
    }
}
