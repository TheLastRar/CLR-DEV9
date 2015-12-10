using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace CLRDEV9.DEV9.SMAP.Tap
{
    partial class TAPAdapter : NetAdapter
    {
        SafeFileHandle htap;
        FileStream htapstream;

        DEV9_State dev9 = null;

        public TAPAdapter(DEV9_State pardev9)
        {
            dev9 = pardev9;

            htap = TAPOpen(DEV9Header.config.Eth.Substring(4, DEV9Header.config.Eth.Length - 4));

            htapstream = new FileStream(htap, FileAccess.ReadWrite, 16 * 1024, true);
        }

        public override bool blocks()
        {
            return true;	//we use blocking io
        }
        public override bool isInitialised()
        {
            if (htap == null)
                return false;
            if (htap.IsInvalid)
                return false;
            return true;
        }
        byte[] broadcast_adddrrrr = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        //gets a packet.rv :true success
        public override bool recv(ref NetPacket pkt)
        {
            int read_size = 0;
            //bool result = false;
            try
            {
                read_size = htapstream.Read(pkt.buffer, 0, pkt.buffer.Length);
                //result = true;
            }
            catch (Exception e)
            {
                Log_Error("Packet Recive Error :" + e.ToString());
                return false;
            }

            //Error.WriteLine(read_size);

            //Result would always be true, don't other checking it.

            //if (result)
            //{
            //original memcmp returns 0 on perfect match
            //the if statment check if !=0
            byte[] eeprombytes = new byte[6];
            for (int i = 0; i < 3; i++)
            {
                byte[] tmp = BitConverter.GetBytes(dev9.eeprom[i]);
                Utils.memcpy(ref eeprombytes, i * 2, tmp, 0, 2);
            }
            if ((Utils.memcmp(pkt.buffer, 0, eeprombytes, 0, 6) == false) & (Utils.memcmp(pkt.buffer, 0, broadcast_adddrrrr, 0, 6) == false))
            {
                //ignore strange packets
                Log_Error("Dropping Strange Packet");
                return false;
            }

            if (Utils.memcmp(pkt.buffer, 6, eeprombytes, 0, 6) == true)
            {
                //avoid pcap looping packets
                Log_Error("Dropping Looping Packet");
                return false;
            }
            pkt.size = read_size;
            return true;
            //}
            //else
            //    return false;
        }
        //sends the packet and deletes it when done (if successful).rv :true success
        public override bool send(NetPacket pkt)
        {
            int writen = 0;

            htapstream.Write(pkt.buffer, 0, pkt.size);
            htapstream.Flush();
            //return type is void, assume full write
            writen = pkt.size;


            if (writen != pkt.size)
            {
                Log_Error("incomplete Send " + Marshal.GetLastWin32Error());
                return false;
            }

            return true;
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Tap, "TAP", str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Tap, "TAP", str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Tap, "TAP", str);
        }

        public override void Dispose()
        {
            TAPSetStatus(htap, false);
            Console.Error.WriteLine("Shutdown Tap");
            htapstream.Close();
            htap.Close();
        }
    };
}
