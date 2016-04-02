using CLRDEV9.DEV9.SMAP.Data;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace CLRDEV9.DEV9.SMAP.Tap
{
    partial class TAPAdapter : DirectAdapter
    {
        SafeFileHandle htap = null;
        FileStream htapstream = null;

        public static List<string[]> GetAdapters()
        {
            List<string[]> ret = TAPGetAdaptersWMI();
            if (ret.Count == 0)
                return null;
            return ret;
        }

        public TAPAdapter(DEV9_State pardev9, string parDevice)
            : base(pardev9)
        {
            htap = TAPOpen(parDevice);

            htapstream = new FileStream(htap, FileAccess.ReadWrite, 16 * 1024, true);

            if (DEV9Header.config.DirectConnectionSettings.InterceptDHCP)
            {
                NetworkInterface host_adapter = GetAdapterFromGuid(parDevice);
                if (host_adapter == null)
                {
                    if (BridgeHelper.IsInBridge(parDevice) == true)
                    {
                        host_adapter = GetAdapterFromGuid(BridgeHelper.GetBridgeGUID());
                    }
                }
                InitDHCP(host_adapter);
            }
        }

        public override bool Blocks()
        {
            return true;	//we use blocking io
        }
        public override bool IsInitialised()
        {
            if (htap == null)
                return false;
            if (htap.IsInvalid)
                return false;
            return true;
        }
        //gets a packet.rv :true success

        public override bool Recv(ref NetPacket pkt)
        {
            if (base.Recv(ref pkt)) { return true; }

            int read_size = 0;
            //bool result = false;
            try
            {
                read_size = htapstream.Read(pkt.buffer, 0, pkt.buffer.Length);
                //result = true;
            }
            catch (OperationCanceledException)
            {
                return false;
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
            if (!Verify(pkt, read_size))
            {
                return false;
            }
            pkt.size = read_size;
            return true;
            //}
            //else
            //    return false;
        }
        //sends the packet and deletes it when done (if successful).rv :true success
        public override bool Send(NetPacket pkt)
        {
            if (base.Send(pkt)) { return true; }

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

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Tap, "TAP", str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Tap, "TAP", str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Tap, "TAP", str);
        }

        public override void Close()
        {
            Dispose();
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(true);
            if (disposing)
            {
                Log_Info("Shutdown Tap");
                if (htapstream != null)
                {
                    htapstream.Close();
                    htapstream = null;
                }
                if (htap == null)
                {
                    TAPSetStatus(htap, false);
                    htap.Close();
                    htap = null;
                }
            }
        }
    }
}
