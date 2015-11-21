using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
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
