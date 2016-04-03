using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

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

        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern string pcap_lib_version();
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern IntPtr pcap_open_live(string device, int snaplen, int promisc, int to_ms, StringBuilder errbuf);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_datalink(IntPtr p);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern IntPtr pcap_datalink_val_to_name(int dlt);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_setnonblock(IntPtr p, int nonblock, StringBuilder errbuf);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_next_ex(IntPtr p, out IntPtr ptr_pkt_header, out IntPtr ptr_pkt_data);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_sendpacket(IntPtr p, byte[] buf, int size);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_close(IntPtr p);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern int pcap_findalldevs(ref IntPtr alldevsp, StringBuilder errbuf);
        [DllImport(PCAP_LIB_NAME, CharSet = CharSet.Ansi)]
        static extern void pcap_freealldevs(IntPtr alldevsp);
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

        [StructLayout(LayoutKind.Sequential)]
        struct pcap_if
        {
            private IntPtr next;
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            [MarshalAs(UnmanagedType.LPStr)]
            public string description;
            private IntPtr addresses; //I don't need this
            UInt32 flags;

            //public pcap_addr GetAddresses()
            //{
            //    if (!HasAddresses())
            //    {
            //        throw new NullReferenceException("PCAP Address is null");
            //    }
            //    return (pcap_addr)Marshal.PtrToStructure(addresses, typeof(pcap_addr));
            //}
            //public bool HasAddresses()
            //{
            //    return (addresses != IntPtr.Zero);
            //}

            public pcap_if GetNext()
            {
                if (isNext())
                {
                    return (pcap_if)Marshal.PtrToStructure(next, typeof(pcap_if));
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            public bool isNext()
            {
                return (next != IntPtr.Zero);
            }

            public pcap_if_enumerator GetEnumerator()
            {
                return new pcap_if_enumerator(this);
            }

            internal class pcap_if_enumerator : IEnumerator<pcap_if>
            {
                private pcap_if first;
                private pcap_if current;
                private int curIndex = -1;

                public pcap_if_enumerator(pcap_if d_first)
                {
                    first = d_first;
                }

                public pcap_if Current
                {
                    get { return current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    if (curIndex == -1)
                    {
                        current = first;
                        curIndex = 0;
                        return true;
                    }
                    else
                    {
                        if (current.isNext())
                        {
                            current = current.GetNext();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                public void Reset() { curIndex = -1; }
            }
        }

        //[StructLayout(LayoutKind.Sequential)]
        //struct pcap_addr
        //{
        //    private IntPtr next;
        //    private IntPtr addr;
        //    private IntPtr netmask;
        //    private IntPtr broadaddr;
        //    private IntPtr dstaddr;

        //    public sockaddr GetAddr()
        //    {
        //        if (!HasAddr())
        //        {
        //            throw new NullReferenceException("PCAP Address.Addr is null");
        //        }
        //        return (sockaddr)Marshal.PtrToStructure(addr, typeof(sockaddr));
        //    }
        //    private bool HasAddr()
        //    {
        //        return (addr != IntPtr.Zero);
        //    }

        //    public pcap_addr GetNext()
        //    {
        //        if (isNext())
        //        {
        //            return (pcap_addr)Marshal.PtrToStructure(next, typeof(pcap_addr));
        //        }
        //        else
        //        {
        //            throw new NullReferenceException();
        //        }
        //    }
        //    public bool isNext()
        //    {
        //        return (next != IntPtr.Zero);
        //    }
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //struct sockaddr
        //{
        //    public UInt16 sa_family;

        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        //    public byte[] sa_data;
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //struct sockaddr_in
        //{
        //    public UInt16 sin_family;
        //    public UInt16 sin_port;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        //    public byte[] sin_addr;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        //    public byte[] sin_zero;

        //    static public explicit operator sockaddr_in(sockaddr sa)
        //    {
        //        sockaddr_in sin = new sockaddr_in();
        //        sin.sin_family = sa.sa_family;
        //        sin.sin_port = BitConverter.ToUInt16(sa.sa_data, 0);
        //        sin.sin_addr = new byte[4];
        //        Array.Copy(sa.sa_data, 2, sin.sin_addr, 0, 4);
        //        //Rest is zeros
        //        return sin;
        //    }
        //}

        static bool PcapAvailable()
        {
            IntPtr hmod = LoadLibrary(PCAP_LIB_NAME);
            if (hmod == IntPtr.Zero)
            {
                return false;
            }
            FreeLibrary(hmod);
            return true;
        }

        static List<string[]> PcapListAdapters()
        {
            List<string[]> devices = new List<string[]>();

            IntPtr rawPcapAdapter = IntPtr.Zero;
            StringBuilder errbuf = new StringBuilder(PCAP_ERRBUF_SIZE);
            if (pcap_findalldevs(ref rawPcapAdapter, errbuf) == -1)
            {
                return null;
            }
            try
            {
                pcap_if d_0 = (pcap_if)Marshal.PtrToStructure(rawPcapAdapter, typeof(pcap_if));
                foreach (pcap_if d in d_0)
                {
                    devices.Add(new string[] { d.description, d.name });
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                pcap_freealldevs(rawPcapAdapter);
            }
            return devices;
        }
        private static string GetFriendlyName(string guid)
        {
            //find adapter in WMI and compare servicename
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

            ObjectQuery query = new ObjectQuery("SELECT NetConnectionID, GUID FROM Win32_NetworkAdapter Where GUID = '" + guid + "'");
            using (ManagementObjectSearcher netSearcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection netQueryCollection = netSearcher.Get())
                {
                    using (ManagementObjectCollection.ManagementObjectEnumerator netMOEn = netQueryCollection.GetEnumerator())
                    {
                        if (netMOEn.MoveNext())
                        {
                            ManagementObject netMO = (ManagementObject)netMOEn.Current;

                            return (string)netMO["NetConnectionID"];
                        }
                    }
                }
            }
            return null;
        }

        bool PcapInitIO(string adapter)
        {
            StringBuilder errbuf = new StringBuilder(PCAP_ERRBUF_SIZE);

            int dlt;
            string dlt_name;
            //Set PS2 MAC Based on Adapter MAC
            if ((adHandle = pcap_open_live(adapter,
                                            65536,
                                            switched ? 1 : 0,
                                            1,
                                            errbuf)) == IntPtr.Zero)
            {
                Log_Error(errbuf.ToString());
                Log_Error("Unable to open the adapter. " + adapter + "is not supported by WinPcap");
                return false;
            }

            dlt = pcap_datalink(adHandle);
            dlt_name = Marshal.PtrToStringAnsi(pcap_datalink_val_to_name(dlt));

            Log_Info("Device uses DLT " + dlt + ": " + dlt_name);
            switch (dlt)
            {
                case 1: //DLT_EN10MB
                    break;
                default:
                    Log_Error("Unsupported DataLink Type " + dlt + ": " + dlt_name);
                    return false;
            }

            if (pcap_setnonblock(adHandle, 1, errbuf) == -1)
            {
                Log_Error("WARNING: Error setting non-blocking mode. Default mode will be used");
            }

            //Don't bother with pcap logs yet

            pcapRunning = true;

            return true;
        }

        int PcapRecvIO(byte[] data, int max_len)
        {
            int res;
            pcap_pkthdr header;
            IntPtr headerPtr;
            IntPtr pkt_dataPtr;

            if (!pcapRunning)
            {
                return -1;
            }

            if ((res = pcap_next_ex(adHandle, out headerPtr, out pkt_dataPtr)) > 0)
            {
                header = (pcap_pkthdr)Marshal.PtrToStructure(headerPtr, typeof(pcap_pkthdr));
                Marshal.Copy(pkt_dataPtr, data, 0, Math.Min((int)header.len, max_len));
                return (int)header.len;
            }
            else
            {
                return -1;
            }
        }

        bool PcapSendIO(byte[] data, int len)
        {
            if (!pcapRunning)
            {
                return false;
            }
            Log_Verb(" * pcap io: Sending " + len + " byte packet");

            if (pcap_sendpacket(adHandle, data, len) == 0)
            {
                return true;
            }
            else {
                return false;
            }
        }

        void PcapCloseIO()
        {
            //Close logs (not ported)
            if (adHandle != IntPtr.Zero)
            {
                pcap_close(adHandle);
                adHandle = IntPtr.Zero;
            }
            pcapRunning = false;
        }
    }
}
