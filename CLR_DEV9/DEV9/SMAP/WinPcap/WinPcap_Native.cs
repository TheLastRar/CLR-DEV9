using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CLRDEV9.DEV9.SMAP.WinPcap
{
    partial class WinPcapAdapter
    {
        const int PCAP_ERRBUF_SIZE = 256;

        #region 'PInvoke mess'
        const string PCAP_LIB_NAME = "wpcap.dll";
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern string pcap_lib_version();
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern IntPtr pcap_open_live(string device, int snaplen, int promisc, int to_ms, StringBuilder errbuf);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern int pcap_datalink(IntPtr p);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern string pcap_datalink_val_to_name(int dlt);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern int pcap_setnonblock(IntPtr p, int nonblock, StringBuilder errbuf);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern int pcap_next_ex(IntPtr p, out IntPtr ptr_pkt_header, out IntPtr ptr_pkt_data);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern int pcap_sendpacket(IntPtr p, byte[] buf, int size);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Auto)]
        static extern int pcap_close(IntPtr p);
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        struct pcap_pkthdr
        {
            //timeval
            public UInt32 tv_sec;
            public UInt32 tv_usec;
            //end timeval
            public UInt32 caplen;
            public UInt32 len;
        }

        static bool pcap_io_available()
        {
            IntPtr hmod = LoadLibrary(PCAP_LIB_NAME);
            if (hmod == IntPtr.Zero)
            {
                return false;
            }
            FreeLibrary(hmod);
            return true;
        }

        bool pcap_io_init(string adapter)
        {
            StringBuilder errbuf = new StringBuilder(PCAP_ERRBUF_SIZE);

            int dlt;
            string dlt_name;
            //Set PS2 MAC Based on Adapter MAC
            if ((adhandle = pcap_open_live(adapter,
                                            65536,
                                            switched?1:0,
                                            1,
                                            errbuf))==null)
            {
                Log_Error(errbuf.ToString());
                Log_Error("Unable to open the adapter. " + adapter + "is not supported by WinPcap");
                return false;
            }

            dlt = pcap_datalink(adhandle);
            dlt_name = pcap_datalink_val_to_name(dlt);

            Log_Info("Device uses DLT " + dlt + ": " + dlt_name);
            switch(dlt)
            {
                case 1: //DLT_EN10MB
                    break;
                default:
                    Log_Error("Unsupported DataLink Type " + dlt + ": " + dlt_name);
                    return false;
            }

            if (pcap_setnonblock(adhandle, 1, errbuf) == -1)
            {
                Log_Error("WARNING: Error setting non-blocking mode. Default mode will be used");
            }

            //Don't bother with pcap logs yet

            pcap_io_running = true;

            return true;
        }

        int pcap_io_recv(byte[] data, int max_len)
        {
            int res;
            pcap_pkthdr header = new pcap_pkthdr();
            IntPtr headerPtr;
            IntPtr pkt_dataPtr;

            if (!pcap_io_running)
            {
                return -1;
            }

            if ((res = pcap_next_ex(adhandle, out headerPtr, out pkt_dataPtr)) > 0)
            {
                Marshal.PtrToStructure(headerPtr, header);
                Marshal.Copy(pkt_dataPtr, data, 0, Math.Min((int)header.len,max_len));
                return (int)header.len;
            }
            else
            {
                return -1;
            }
        }

        bool pcap_io_send(byte[] data, int len)
        {
            if (!pcap_io_running)
            {
                return false;
            }
            Log_Verb(" * pcap io: Sending " + len + " byte packet");

            if (pcap_sendpacket(adhandle, data, len) == 0)
            {
                return true;
            } else {
                return false;
            }
        }

        void pcap_io_close()
        {
            //Close logs (not ported)
            if (adhandle != IntPtr.Zero)
            {
                pcap_close(adhandle);
                adhandle = IntPtr.Zero;
            }
            pcap_io_running = false;
        }
    }
}
