using System;

namespace CLR_DEV9
{
    static class net
    {
        static netHeader.NetAdapter nif;
        static System.Threading.Thread rx_thread;

        static volatile bool RxRunning = false;
        //rx thread
        static void NetRxThread()
        {
            netHeader.NetPacket tmp = new netHeader.NetPacket();
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

        public static void tx_put(ref netHeader.NetPacket pkt)
        {
            nif.send(pkt);
            //pkt must be copied if its not processed by here, since it can be allocated on the callers stack
        }

        public static void InitNet(netHeader.NetAdapter ad)
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
        public static void TermNet()
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
