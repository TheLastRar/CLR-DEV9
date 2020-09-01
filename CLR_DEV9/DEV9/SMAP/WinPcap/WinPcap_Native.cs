using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.WinPcap
{
    partial class WinPcapAdapter
    {
        const int PCAP_ERRBUF_SIZE = 256;
        const int PCAP_NETMASK_UNKNOWN = unchecked((int)0xffffffff);

        #region 'PInvoke mess'
        //TODO, support libPcap
        //API changes?
        private class NativeMethods
        {
            //Windows
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string lpFileName);
            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);
            //Pcap
            [DllImport("wpcap")]
            public static extern IntPtr pcap_lib_version();
            //
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr pcap_open_live(string device, int snaplen, int promisc, int to_ms, [MarshalAs(UnmanagedType.LPStr)] StringBuilder errbuf);
            [DllImport("wpcap")]
            public static extern int pcap_close(IntPtr p);
            //
            [DllImport("wpcap")]
            public static extern int pcap_next_ex(IntPtr p, out IntPtr ptr_pkt_header, out IntPtr ptr_pkt_data);
            [DllImport("wpcap")]
            public static extern int pcap_sendpacket(IntPtr p, byte[] buf, int size);
            //
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern int pcap_getnonblock(IntPtr p, [MarshalAs(UnmanagedType.LPStr)] StringBuilder errbuf);
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern int pcap_setnonblock(IntPtr p, int nonblock, [MarshalAs(UnmanagedType.LPStr)] StringBuilder errbuf);
            //
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern int pcap_compile(IntPtr p, ref bpf_program ptr_bpf_program, string str, int optimize, int netmask);
            [DllImport("wpcap")]
            public static extern void pcap_freecode(ref bpf_program ptr_bpf_program);
            [DllImport("wpcap")]
            public static extern int pcap_setfilter(IntPtr p, ref bpf_program ptr_bpf_program);
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern string pcap_geterr(IntPtr p);
            //
            [DllImport("wpcap")]
            public static extern int pcap_datalink(IntPtr p);
            [DllImport("wpcap")]
            public static extern IntPtr pcap_datalink_val_to_name(int dlt);
            //
            [DllImport("wpcap", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern int pcap_findalldevs(ref IntPtr alldevsp, [MarshalAs(UnmanagedType.LPStr)] StringBuilder errbuf);
            [DllImport("wpcap", CharSet = CharSet.Ansi)]
            public static extern void pcap_freealldevs(IntPtr alldevsp);
        }
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

        [StructLayout(LayoutKind.Sequential)]
        struct bpf_program
        {
            uint bf_len;
            private IntPtr bf_insns; //I don't need this
        }

        static bool PcapAvailable()
        {
            if (PSE.CLR_PSE_Utils.IsWindows())
            {
                IntPtr hmod = NativeMethods.LoadLibrary("wpcap");
                if (hmod == IntPtr.Zero)
                {
                    return false;
                }
                NativeMethods.FreeLibrary(hmod);
            }
            else
            {
                //On linux, wpcap is remapped to libpcap.so.<version>
                //this is done via a dllmap, so managed code does not
                //know what it is remapped to, so just call a method
                //and see if it errors
                try
                {
                    NativeMethods.pcap_lib_version();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        static List<string[]> PcapListAdapters()
        {
            List<string[]> devices = new List<string[]>();

            IntPtr rawPcapAdapter = IntPtr.Zero;
            StringBuilder errbuf = new StringBuilder(PCAP_ERRBUF_SIZE);
            if (NativeMethods.pcap_findalldevs(ref rawPcapAdapter, errbuf) == -1)
            {
                return null;
            }
            if (rawPcapAdapter == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                pcap_if d_0 = (pcap_if)Marshal.PtrToStructure(rawPcapAdapter, typeof(pcap_if));
                foreach (pcap_if d in d_0)
                {
                    errbuf.Clear();
                    IntPtr handle;
                    if ((handle = NativeMethods.pcap_open_live(d.name,
                                0,
                                0,
                                1000,
                                errbuf)) == IntPtr.Zero)
                    {
                        //PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap,
                        //    errbuf.ToString());
                        //PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap,
                        //    "Unable to open the adapter. " + d.name + " is not supported by WinPcap");
                        continue;
                    }

                    if (PcapIsValid(handle, false))
                    {
                        devices.Add(new string[] { d.description, d.name });
                    }

                    NativeMethods.pcap_close(handle);
                }
            }
            catch
            {
                PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap,
                    "Error Finding devices, halted search");
            }
            finally
            {
                NativeMethods.pcap_freealldevs(rawPcapAdapter);
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

        static bool PcapIsValid(IntPtr handle, bool log)
        {
            int dlt;
            string dlt_name;

            dlt = NativeMethods.pcap_datalink(handle);
            dlt_name = Marshal.PtrToStringAnsi(NativeMethods.pcap_datalink_val_to_name(dlt));
            if (log)
                PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.WinPcap, 
                    "Device uses DLT " + dlt + ": " + dlt_name);
            switch (dlt)
            {
                case 1: //DLT_EN10MB
                    break;
                default:
                    if (log)
                        PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap,
                            "Unsupported DataLink Type " + dlt + ": " + dlt_name);
                    return false;
            }
            return true;
        }

        bool PcapInitIO(string adapter)
        {
            StringBuilder errbuf = new StringBuilder(PCAP_ERRBUF_SIZE);

            //Set PS2 MAC Based on Adapter MAC
            if ((adHandle = NativeMethods.pcap_open_live(adapter,
                                            65536,
                                            switched ? 1 : 0,
                                            1,
                                            errbuf)) == IntPtr.Zero)
            {
                Log_Error(errbuf.ToString());
                Log_Error("Unable to open the adapter. " + adapter + "is not supported by WinPcap");
                return false;
            }

            if (!PcapIsValid(adHandle, true))
            {
                return false;
            }

            if (NativeMethods.pcap_setnonblock(adHandle, 1, errbuf) == -1)
            {
                Log_Error("WARNING: Error setting non-blocking mode. Default mode will be used");
            }

            //Don't bother with pcap logs yet

            if (switched)
            {
                //Setup Filter
                string filter_mac = "{0:x2}:{1:x2}:{2:x2}:{3:x2}:{4:x2}:{5:x2}";
                string filter = "(ether broadcast or ether multicast or ether dst {0}) and not ether src {0}";

                filter_mac = string.Format(filter_mac, ps2MAC[0], ps2MAC[1], ps2MAC[2], ps2MAC[3], ps2MAC[4], ps2MAC[5]);
                filter = string.Format(filter, filter_mac);

                bpf_program program = new bpf_program();
                if (NativeMethods.pcap_compile(adHandle, ref program, filter, 1, PCAP_NETMASK_UNKNOWN) == -1)
                {
                    Log_Error("Error calling pcap_compile: " + NativeMethods.pcap_geterr(adHandle));
                    return false;
                }

                if (NativeMethods.pcap_setfilter(adHandle, ref program) == -1)
                {
                    Log_Error("Error setting filter: " + NativeMethods.pcap_geterr(adHandle));
                    return false;
                }

                NativeMethods.pcap_freecode(ref program);
            }

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

            if ((res = NativeMethods.pcap_next_ex(adHandle, out headerPtr, out pkt_dataPtr)) > 0)
            {
                header = (pcap_pkthdr)Marshal.PtrToStructure(headerPtr, typeof(pcap_pkthdr));

                //Oversized packets (Outbreak when running on Linux)
                //Drop packets
                //TODO, fragment packets instead?
                if (header.len > max_len)
                {
                    Log_Error("Dropped jumbo frame of size: " + header.len);
                    return 0;
                }
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

            if (NativeMethods.pcap_sendpacket(adHandle, data, len) == 0)
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
                NativeMethods.pcap_close(adHandle);
                adHandle = IntPtr.Zero;
            }
            pcapRunning = false;
        }
    }
}
