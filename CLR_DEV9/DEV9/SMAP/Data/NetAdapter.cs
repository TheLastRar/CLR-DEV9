using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Data
{
    abstract class NetAdapter : IDisposable
    {
        //Shared
        protected byte[] broadcast_mac = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        //Hope this dosn't clash (Also used as DHCP server in intercept mode)
        protected byte[] virtural_gateway_mac = { 0x76, 0x6D, 0xF4, 0x63, 0x30, 0x31 };

        protected byte[] ps2_mac;

        protected DEV9_State dev9 = null;

        public NetAdapter(DEV9_State pardev9)
        {
            dev9 = pardev9;
            //Read MAC from eeprom to get ps2_mac
            ps2_mac = new byte[6];
            byte[] eepromBytes = new byte[6];
            for (int i = 0; i < 3; i++)
            {
                byte[] tmp = BitConverter.GetBytes(dev9.eeprom[i]);
                Utils.memcpy(ref eepromBytes, i * 2, tmp, 0, 2);
            }
            Utils.memcpy(ref ps2_mac, 0, eepromBytes, 0, 6);
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
        public abstract bool Send(NetPacket pkt);	//sends the packet and deletes it when done
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

            if ((Utils.memcmp(pkt.buffer, 0, ps2_mac, 0, 6) == false) & (Utils.memcmp(pkt.buffer, 0, broadcast_mac, 0, 6) == false))
            {
                //ignore strange packets
                Log_Error("Dropping Strange Packet");
                return false;
            }

            if (Utils.memcmp(pkt.buffer, 6, ps2_mac, 0, 6) == true)
            {
                //avoid pcap looping packets
                Log_Error("Dropping Looping Packet");
                return false;
            }
            pkt.size = read_size;
            return true;
        }

        //static protected List<string[]> getadapters()
        //{

        //    NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
        //    List<string[]> names = new List<string[]>();

        //    foreach (NetworkInterface adapter in Interfaces)
        //    {
        //        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        //        {
        //            continue;
        //        }
        //        if (adapter.OperationalStatus == OperationalStatus.Up)
        //        {
        //            UnicastIPAddressInformationCollection IPInfo = adapter.GetIPProperties().UnicastAddresses;
        //            IPInterfaceProperties properties = adapter.GetIPProperties();

        //            foreach (UnicastIPAddressInformation IPAddressInfo in IPInfo)
        //            {
        //                if (IPAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
        //                {
        //                    //return adapter
        //                    names.Add(new string[] { adapter.Name, adapter.Description, adapter.Id });
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    return names;
        //}

        protected static NetworkInterface GetAdapterFromGuid(string parGUID)
        {
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in Interfaces)
            {
                //Use WMI instead?
                //if (Utils.memcmp(adapter.GetPhysicalAddress().GetAddressBytes(), 0, new byte[] { 0, 0, 0, 0, 0, 0 }, 0, 6))
                //{
                //    System.Windows.Forms.MessageBox.Show("Found Bridge");
                //}
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
