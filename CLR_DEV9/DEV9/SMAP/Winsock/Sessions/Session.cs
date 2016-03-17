using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Net;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    abstract class Session : IDisposable
    {
        public byte[] SourceIP;
        public byte[] DestIP;
        protected IPAddress adapterIP;

        public Session(IPAddress parAdapterIP)
        {
            adapterIP = parAdapterIP;
        }

        public abstract IPPayload Recv();
        public abstract bool Send(IPPayload payload);
        public abstract void Reset();
        public abstract bool isOpen();
        public abstract void Dispose();

    }
}
