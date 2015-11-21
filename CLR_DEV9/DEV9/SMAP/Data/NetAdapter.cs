using System;

namespace CLRDEV9.DEV9.SMAP.Data
{
    abstract class NetAdapter : IDisposable
    {
        public virtual bool blocks()
        {
            return false;
        }
        public virtual bool isInitialised()
        {
            return false;
        }
        public virtual bool recv(ref NetPacket pkt) //gets a packet
        {
            return false;
        }
        public virtual bool send(NetPacket pkt)	//sends the packet and deletes it when done
        {
            return false;
        }
        public abstract void Dispose();
    }
}
