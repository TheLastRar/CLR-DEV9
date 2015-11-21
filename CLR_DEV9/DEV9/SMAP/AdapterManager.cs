using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CLRDEV9.DEV9.SMAP.Data;

namespace CLRDEV9.DEV9.SMAP
{
    partial class AdapterManager
    {
        NetAdapter nif = null;
        System.Threading.Thread rx_thread;

        volatile bool RxRunning = false;

        SMAP_State smap = null;
        
        public AdapterManager(SMAP_State parsmap)
        {
            smap = parsmap;
        }

        //rx thread
        void NetRxThread()
        {
            NetPacket tmp = new NetPacket();
            while (RxRunning)
            {
                while (smap.rx_fifo_can_rx() && nif.recv(ref tmp))
                {
                    smap.rx_process(ref tmp);
                }

                System.Threading.Thread.Sleep(1);
            }

            //return 0;
        }

        public void tx_put(ref NetPacket pkt)
        {
            if (nif != null)
                nif.send(pkt);
            //pkt must be copied if its not processed by here, since it can be allocated on the callers stack
        }

        public void InitNet(NetAdapter ad)
        {
            nif = ad;
            RxRunning = true;
            //System.Threading.ParameterizedThreadStart rx_setup = new System.Threading.ParameterizedThreadStart()
            //rx_setup
            rx_thread = new System.Threading.Thread(NetRxThread);
            rx_thread.Priority = System.Threading.ThreadPriority.Highest;
            //rx_thread = CreateThread(0, 0, NetRxThread, 0, CREATE_SUSPENDED, 0);

            //SetThreadPriority(rx_thread, THREAD_PRIORITY_HIGHEST);
            //ResumeThread(rx_thread);
            rx_thread.Start();
        }
        public void TermNet()
        {
            if (RxRunning)
            {
                RxRunning = false;
                Console.Error.WriteLine("Waiting for RX-net thread to terminate..");
                rx_thread.Join();
                Console.Error.WriteLine(".done\n");
                nif.Dispose();
                nif = null;
            }
        }
    }
}
