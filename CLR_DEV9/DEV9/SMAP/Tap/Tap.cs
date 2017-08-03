using CLRDEV9.DEV9.SMAP.Data;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Tap
{
    sealed partial class TAPAdapter : DirectAdapter
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

        public TAPAdapter(DEV9_State parDev9, string parDevice)
            : base(parDev9)
        {
            htap = TAPOpen(parDevice);

            htapstream = new FileStream(htap, FileAccess.ReadWrite, 16 * 1024, true);

            if (DEV9Header.config.DirectConnectionSettings.InterceptDHCP)
            {
                NetworkInterface hostAdapter = GetAdapterFromGuid(parDevice);
                if (hostAdapter == null)
                {
                    if (BridgeHelper.IsInBridge(parDevice) == true)
                    {
                        hostAdapter = GetAdapterFromGuid(BridgeHelper.GetBridgeGUID());
                    }
                }
                if (hostAdapter == null)
                {
                    //System.Windows.Forms.MessageBox.Show("Failed to GetAdapter");
                    throw new NullReferenceException("Failed to GetAdapter");
                }
                InitDHCP(hostAdapter);
            }

            byte[] hostMAC = TAPGetMac(htap);

            SetMAC(null);
            byte[] wMAC = (byte[])ps2MAC.Clone();
            //wMAC[3] = hostMAC[3];
            wMAC[5] = hostMAC[4];
            wMAC[4] = hostMAC[5];
            SetMAC(wMAC);
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

            int readSize = 0;
            //bool result = false;
            try
            {
                readSize = htapstream.Read(pkt.buffer, 0, pkt.buffer.Length);
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
            if (!Verify(pkt, readSize))
            {
                return false;
            }
            pkt.size = readSize;
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
                Log_Error("incomplete Send");
                return false;
            }

            return true;
        }

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Tap, str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Tap, str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Tap, str);
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
                if (htap != null)
                {
                    TAPSetStatus(htap, false);
                }
                if (htapstream != null)
                {
                    htapstream.Close();
                    htapstream = null;
                }
                if (htap != null)
                {
                    htap.Close();
                    htap = null;
                }
            }
        }
    }
}
