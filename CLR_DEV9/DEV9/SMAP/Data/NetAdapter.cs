using System;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Data
{
    abstract class NetAdapter : IDisposable
    {
        //Shared
        protected byte[] broadcastMAC = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        //Hope this dosn't clash (Also used as DHCP server in intercept mode)
        protected byte[] virturalGatewayMAC = { 0x76, 0x6D, 0xF4, 0x63, 0x30, 0x31 };

        protected byte[] ps2MAC;

        protected DEV9_State dev9 = null;

        public NetAdapter(DEV9_State parDev9)
        {
            dev9 = parDev9;
            //Read MAC from eeprom to get ps2_mac
            ps2MAC = new byte[6];
            byte[] eepromBytes = new byte[6];
            for (int i = 0; i < 3; i++)
            {
                byte[] tmp = BitConverter.GetBytes(dev9.eeprom[i]);
                Utils.memcpy(ref eepromBytes, i * 2, tmp, 0, 2);
            }
            Utils.memcpy(ref ps2MAC, 0, eepromBytes, 0, 6);
        }

        //public abstract List<string[]> getadapters(); //TODO
        public virtual bool Blocks()
        {
            return false;
        }
        public virtual bool IsInitialised()
        {
            return false;
        }
        public abstract bool Recv(ref NetPacket pkt); //gets a packet
        public abstract bool Send(NetPacket pkt);   //sends the packet and deletes it when done
        public abstract void Close(); //Prepare to shutdown thread
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public virtual void Dispose(bool disposing) { }
        //Shared functions

        protected bool Verify(NetPacket pkt, int read_size)
        {
            //TODO? Boost with pointers instead of converting?

            if ((Utils.memcmp(pkt.buffer, 0, ps2MAC, 0, 6) == false) & (Utils.memcmp(pkt.buffer, 0, broadcastMAC, 0, 6) == false))
            {
                //ignore strange packets
                Log_Verb("Dropping Strange Packet");
                return false;
            }

            if (Utils.memcmp(pkt.buffer, 6, ps2MAC, 0, 6) == true)
            {
                //avoid pcap looping packets
                Log_Error("Dropping Looping Packet");
                return false;
            }
            pkt.size = read_size;
            return true;
        }

        protected static NetworkInterface GetAdapterFromGuid(string parGUID)
        {
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in Interfaces)
            {
                if (adapter.Id == parGUID)
                {
                    return adapter;
                }
            }
            return null;
        }

        protected virtual void Log_Error(string str) { }
        protected virtual void Log_Info(string str) { }
        protected virtual void Log_Verb(string str) { }
    }
}
