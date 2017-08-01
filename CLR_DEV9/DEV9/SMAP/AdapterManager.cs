using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;
using LOG = PSE.CLR_PSE_PluginLog;

namespace CLRDEV9.DEV9.SMAP
{
    partial class AdapterManager
    {
        NetAdapter nif = null;
        System.Threading.Thread rxThread;

        volatile bool RxRunning = false;

        SMAP_State smap = null;

        public AdapterManager(SMAP_State parSMAP)
        {
            smap = parSMAP;
        }

        //rx thread
        void NetRxThread()
        {
            try {
                NetPacket tmp = new NetPacket();
                while (RxRunning)
                {
                    while (smap.RxFifoCanRx() && nif.Recv(ref tmp))
                    {
                        smap.RxProcess(ref tmp);
                    }

                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                LOG.MsgBoxError(e);
                throw;
            }
            //return 0;
        }

        //public byte[] GetPS2HWAddress()
        //{
        //    if (nif != null)
        //        return nif.PS2HWAddress;
        //    return null;
        //}

        public void TxPut(ref NetPacket pkt)
        {
            if (nif != null)
                nif.Send(pkt);
            //pkt must be copied if its not processed by here, since it can be allocated on the callers stack
        }

        public void InitNet(NetAdapter ad)
        {
            nif = ad;
            RxRunning = true;
            //System.Threading.ParameterizedThreadStart rx_setup = new System.Threading.ParameterizedThreadStart()
            //rx_setup
            rxThread = new System.Threading.Thread(NetRxThread);
            rxThread.Priority = System.Threading.ThreadPriority.Highest;
            //rx_thread = CreateThread(0, 0, NetRxThread, 0, CREATE_SUSPENDED, 0);

            //SetThreadPriority(rx_thread, THREAD_PRIORITY_HIGHEST);
            //ResumeThread(rx_thread);
            rxThread.Start();
        }
        public void TermNet()
        {
            if (RxRunning)
            {
                RxRunning = false;
                nif.Close();
                LOG.WriteLine(TraceEventType.Information, (int)DEV9LogSources.NetAdapter, "Waiting for RX-net thread to terminate..");
                rxThread.Join();
                LOG.WriteLine(TraceEventType.Information, (int)DEV9LogSources.NetAdapter, "Done");
                nif.Dispose();
                nif = null;
            }
        }
    }
}
