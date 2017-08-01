using System;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Data
{
    abstract class NetAdapter : IDisposable
    {
        //Shared
        protected byte[] broadcastMAC = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        //Hope this dosn't clash (used as DHCP server in intercept mode)
        //also used as the virtual gateway mac in winsock
        protected byte[] virturalDHCPMAC = { 0x76, 0x6D, 0xF4, 0x63, 0x30, 0x31 };

        protected byte[] ps2MAC;

        //public byte[] PS2HWAddress { get { return ps2MAC; } }
        //public byte[] DHCPHWAddress { get { return ps2MAC; } }

        protected DEV9_State dev9 = null;

        public NetAdapter(DEV9_State parDev9)
        {
            dev9 = parDev9;
            //SetMAC(null);
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

        //TODO figure out full logic for this
        protected void SetMAC(byte[] parMAC)
        {
            if (parMAC == null)
            {
                //Read MAC from eeprom to get ps2_mac
                ps2MAC = new byte[6];
                for (int i = 0; i < 3; i++)
                {
                    byte[] tmp = BitConverter.GetBytes(dev9.eeprom[i]);
                    Utils.memcpy(ps2MAC, i * 2, tmp, 0, 2);
                }
            }
            else
            {
                ps2MAC = parMAC;
                for (int i = 0; i < 3; i++)
                {
                    dev9.eeprom[i] = (UInt16)BitConverter.ToInt16(parMAC, i * 2);
                }
                dev9.eeprom[3] = (UInt16)((dev9.eeprom[0] + dev9.eeprom[1] + dev9.eeprom[2]) & 0xffff);
            }
        }

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
