using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP
{
    partial class SMAP_State
    {
        bool has_link = true; //Is Cable Connected??
        volatile bool fireIntR = false;
        object resetSentry = new object();
        object counterSentry = new object();

        DEV9_State dev9 = null;

        protected AdapterLoader adapter = null;

        public SMAP_State(DEV9_State parDev9)
        {
            dev9 = parDev9;

            //Init SMAP
            int rxbi;

            for (rxbi = 0; rxbi < (DEV9Header.SMAP_BD_SIZE / 8); rxbi++)
            {
                SMAP_bd pbd;
                pbd = new SMAP_bd(dev9.dev9R, (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + (SMAP_bd.GetSize() * rxbi)));

                pbd.CtrlStat = (UInt16)DEV9Header.SMAP_BD_RX_EMPTY;
                pbd.Length = 0;
            }

            adapter = new AdapterLoader(this, dev9);
        }

        public int Open()
        {
            Log_Info("Open SMAP");
            return adapter.Open();
        }
        public void Close()
        {
            adapter.Close();
        }

        //this can return a false positive, but its not problem since it may say it cant recv while it can (no harm done, just delay on packets)
        public virtual bool RxFifoCanRx()
        {
            //check if RX is on & stuff like that here

            //Check if there is space on RXBD
            if (dev9.Dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT) == 64)
                return false;

            //Check if there is space on fifo
            int rd_ptr = (int)dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR);
            int space = dev9.rxFifo.Length -
                ((dev9.rxFifoWrPtr - rd_ptr) & 16383);

            if (space == 0)
                space = (dev9.rxFifo.Length);

            if (space < 1514)
                return false;

            //int soff = (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + dev9.rxbdi * SMAP_bd.GetSize());
            //SMAP_bd pbd = new SMAP_bd(dev9.dev9R, soff);

            //if (!((pbd.CtrlStat & DEV9Header.SMAP_BD_RX_EMPTY) != 0))
            //{
            //    return false;
            //}

            //we can recv a packet !
            return true;
        }
        public virtual void RxProcess(ref NetPacket pk)
        {
            if (!RxFifoCanRx())
            {
                Log_Error("ERROR : !rx_fifo_can_rx at rx_process");
                return;
            }
            //smap_bd_t* pbd = ((smap_bd_t*)&dev9.dev9R[SMAP_BD_RX_BASE & 0xffff]) + dev9.rxbdi;
            int soff = (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + dev9.rxbdi * SMAP_bd.GetSize());
            SMAP_bd pbd = new SMAP_bd(dev9.dev9R, soff);

            int bytes = (pk.size + 3) & (~3);

            if (!((pbd.CtrlStat & DEV9Header.SMAP_BD_RX_EMPTY) != 0))
            {
                Log_Info("(!(pbd->ctrl_stat & SMAP_BD_RX_EMPTY))");
                Log_Info("Discarding " + bytes + " bytes (RX" + dev9.rxbdi + " not ready)");
                return;
            }

            int pstart = (dev9.rxFifoWrPtr) & 16383;
            int i = 0;
            while (i < bytes)
            {
                dev9.Dev9RxFifoWrite(pk.buffer[i++]);
                dev9.rxFifoWrPtr &= 16383;
            }
            lock (resetSentry)
            {
                //increase RXBD
                dev9.rxbdi++;
                dev9.rxbdi &= ((DEV9Header.SMAP_BD_SIZE / 8u) - 1u);

                //Fill the BD with info !
                pbd.Length = (ushort)pk.size;
                pbd.Pointer = (ushort)(0x4000 + pstart); //?
                unchecked //Allow -int to uint
                {
                    pbd.CtrlStat &= (ushort)~DEV9Header.SMAP_BD_RX_EMPTY;
                }

                //increase frame count
                lock (counterSentry)
                {
                    byte framecount = dev9.Dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT);
                    framecount++;
                    dev9.Dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, framecount);
                }
            }
            //spams// emu_printf("Got packet, %d bytes (%d fifo)\n", pk->size,bytes);
            fireIntR = true;
            //DEV9._DEV9irq(DEV9Header.SMAP_INTR_RXEND, 0);//now ? or when the fifo is full ? i guess now atm
            //note that this _is_ wrong since the IOP interrupt system is not thread safe.. but nothing i can do about that
        }

        private UInt32 WordSwap(UInt32 d)
        {
            return (d >> 16) | (d << 16);
        }

        //tx_process
        private void TxProcess()
        {
            //Error.WriteLine("TX");
            //we loop based on count ? or just *use* it ?
            UInt32 cnt = dev9.Dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT);
            //spams// printf("tx_process : %d cnt frames !\n",cnt);

            NetPacket pk = new NetPacket();
            int fc = 0;
            for (fc = 0; fc < cnt; fc++)
            {
                //smap_bd_t *pbd= ((smap_bd_t *)&dev9.dev9R[SMAP_BD_TX_BASE & 0xffff])+dev9.txbdi;

                SMAP_bd pbd;
                pbd = new SMAP_bd(dev9.dev9R, (int)((DEV9Header.SMAP_BD_TX_BASE & 0xffff) + (SMAP_bd.GetSize() * dev9.txbdi)));

                if (!((pbd.CtrlStat & DEV9Header.SMAP_BD_TX_READY) != 0))
                {
                    Log_Error("ERROR : !pbd->ctrl_stat&SMAP_BD_TX_READ");
                    break;
                }
                if ((pbd.Length & 3) != 0)
                {
                    //spams// Log_Error("WARN : !pbd->length not alligned");
                }

                if (pbd.Length > 1514)
                {
                    Log_Error("ERROR : Trying to send packet too big");
                }
                else
                {
                    UInt32 _base = (UInt32)((pbd.Pointer - 0x1000) & 16383);
                    Log_Verb("Sending Packet from base " + _base.ToString("X") + ", size " + pbd.Length);
                    //The 1st packet we send should be base 0, size 1514
                    //spams// emu_printf("Sending Packet from base %x, size %d\n", base, pbd->length);

                    pk.size = pbd.Length;

                    if (!(pbd.Pointer >= 0x1000))
                    {
                        Log_Error("ERROR: odd , !pbd->pointer>0x1000 | 0x" + pbd.Pointer.ToString("X") + " " + pbd.Length.ToString());
                    }

                    if (_base + pbd.Length > 16384)
                    {
                        UInt32 was = 16384 - _base;
                        Utils.memcpy(pk.buffer, 0, dev9.txFifo, (int)_base, (int)was);
                        Utils.memcpy(pk.buffer, (int)was, dev9.txFifo, 0, (int)(pbd.Length - was));
                        Log_Verb("Warped read, was=" + was + ", sz=" + pbd.Length + ", sz-was=" + (pbd.Length - was));
                    }
                    else
                    {
                        Utils.memcpy(pk.buffer, 0, dev9.txFifo, (int)_base, (int)pbd.Length);
                    }
                    adapter.net.TxPut(ref pk);
                }

                unchecked
                {
                    pbd.CtrlStat &= (UInt16)(~DEV9Header.SMAP_BD_TX_READY);
                }

                //increase TXBD
                dev9.txbdi++;
                dev9.txbdi &= (DEV9Header.SMAP_BD_SIZE / 8) - 1;

                //decrease frame count -- this is not thread safe
                //dev9Ru8(SMAP_R_TXFIFO_FRAME_CNT)--;
                dev9.Dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, (byte)(dev9.Dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT) - 1));
            }

            //spams// emu_printf("processed %d frames, %d count, cnt = %d\n",fc,dev9Ru8(SMAP_R_TXFIFO_FRAME_CNT),cnt);
            //if some error/early exit signal TXDNV
            if (fc != cnt || cnt == 0)
            {
                Log_Error("WARN : (fc!=cnt || cnt==0) but packet send request was made oO..");
                dev9.DEV9irq(DEV9Header.SMAP_INTR_TXDNV, 0);
            }
            //if we actualy send something send TXEND
            if (fc != 0)
                dev9.DEV9irq(DEV9Header.SMAP_INTR_TXEND, 100);//now ? or when the fifo is empty ? i guess now atm
        }

        public void SMAP_Async(UInt32 cycles)
        {
            if (fireIntR)
            {
                fireIntR = false;
                //Is this used to signal each individual packet, or just when there are packets in the RX fifo?
                dev9.DEV9irq(DEV9Header.SMAP_INTR_RXEND, 0); //Make the call to _DEV9irq in a thread safe way
            }
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.SMAP, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.SMAP, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.SMAP, str);
        }
    }
}
