using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP
{
    class SMAP_State
    {
        bool has_link = true; //Is Cable Connected??
        volatile bool fireIntR = false;
        object resetSentry = new object();
        object counterSentry = new object();

        DEV9_State dev9 = null;

        AdapterLoader adapter = null;

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
            return adapter.Open();
        }
        public void Close()
        {
            adapter.Close();
        }

        //this can return a false positive, but its not problem since it may say it cant recv while it can (no harm done, just delay on packets)
        public bool RxFifoCanRx()
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

            //we can recv a packet !
            return true;
        }
        public void RxProcess(ref NetPacket pk)
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
                Log_Error("ERROR (!(pbd->ctrl_stat & SMAP_BD_RX_EMPTY))");
                Log_Error("ERROR : Discarding " + bytes + " bytes (RX" + dev9.rxbdi + " not ready)");
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
                dev9.rxbdi &= (uint)((DEV9Header.SMAP_BD_SIZE / 8) - 1);

                //Fill the BD with info !
                pbd.Length = (ushort)pk.size;
                pbd.Pointer = (ushort)(0x4000 + pstart);
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
                    Log_Error("ERROR : !pbd->ctrl_stat&SMAP_BD_TX_READY\n");
                    break;
                }
                if ((pbd.Length & 3) != 0)
                {
                    //spams// emu_printf("WARN : pbd->length not alligned %d\n",pbd->length);
                }

                if (pbd.Length > 1514)
                {
                    Log_Error("ERROR : Trying to send packet too big.\n");
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
                        Log_Verb("ERROR: odd , !pbd->pointer>0x1000 | 0x" + pbd.Pointer.ToString("X") + " " + pbd.Length.ToString());
                    }

                    if (_base + pbd.Length > 16384)
                    {
                        UInt32 was = 16384 - _base;
                        Utils.memcpy(ref pk.buffer, 0, dev9.txFifo, (int)_base, (int)was);
                        Utils.memcpy(ref pk.buffer, (int)was, dev9.txFifo, 0, (int)(pbd.Length - was)); //I thingk this was a bug in the original plugin
                        Log_Verb("Warped read, was=" + was + ", sz=" + pbd.Length + ", sz-was=" + (pbd.Length - was));
                    }
                    else
                    {
                        Utils.memcpy(ref pk.buffer, 0, dev9.txFifo, (int)_base, (int)pbd.Length);
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

        private void EMAC3_Write(UInt32 addr)
        {
            UInt32 value = WordSwap(dev9.Dev9Ru32((int)addr));
            switch (addr)
            {
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    Log_Verb("SMAP: SMAP_R_EMAC3_MODE0 write " + value.ToString("X"));
                    value = (value & (~DEV9Header.SMAP_E3_SOFT_RESET)) | DEV9Header.SMAP_E3_TXMAC_IDLE | DEV9Header.SMAP_E3_RXMAC_IDLE;
                    UInt16 tmp = (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SMAP_R_EMAC3_STA_CTRL_H) | DEV9Header.SMAP_E3_PHY_OP_COMP);
                    dev9.Dev9Wu16((int)DEV9Header.SMAP_R_EMAC3_STA_CTRL_H, tmp);
                    break;
                case DEV9Header.SMAP_R_EMAC3_TxMODE0_L:
                    Log_Verb("SMAP: SMAP_R_EMAC3_TxMODE0_L write " + value.ToString("X"));
                    //spams// emu_printf("SMAP: SMAP_R_EMAC3_TxMODE0_L write %x\n", value);
                    //Process TX  here ?
                    if (!(value != 0) & (DEV9Header.SMAP_E3_TX_GNP_0 != 0))
                        Log_Error("SMAP_R_EMAC3_TxMODE0_L: SMAP_E3_TX_GNP_0 not set");

                    TxProcess();
                    value = value & ~DEV9Header.SMAP_E3_TX_GNP_0;
                    if (value != 0)
                        Log_Error("SMAP_R_EMAC3_TxMODE0_L: extra bits set !");
                    break;
                case DEV9Header.SMAP_R_EMAC3_TxMODE1_L:
                    Log_Error("SMAP_R_EMAC3_TxMODE1_L 32bit write " + value.ToString("X"));
                    if (value == 0x380f0000)
                    {
                        Log_Error("Adapter Detection Hack - Resetting RX/TX");
                        dev9.DEV9irq(DEV9Header.SMAP_INTR_RXEND | DEV9Header.SMAP_INTR_TXEND | DEV9Header.SMAP_INTR_TXDNV, 5);
                    }
                    break;
                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                    Log_Verb("SMAP: SMAP_R_EMAC3_STA_CTRL write " + value.ToString("X"));
                    {
                        if ((value & (DEV9Header.SMAP_E3_PHY_READ)) != 0)
                        {
                            value |= DEV9Header.SMAP_E3_PHY_OP_COMP;
                            uint reg = (value & (DEV9Header.SMAP_E3_PHY_REG_ADDR_MSK));
                            UInt16 val = dev9.phyRegs[reg];
                            switch (reg)
                            {
                                case DEV9Header.SMAP_DsPHYTER_BMSR:
                                    if (has_link)
                                        val |= DEV9Header.SMAP_PHY_BMSR_LINK | DEV9Header.SMAP_PHY_BMSR_ANCP;
                                    break;
                                case DEV9Header.SMAP_DsPHYTER_PHYSTS:
                                    if (has_link)
                                        val |= DEV9Header.SMAP_PHY_STS_LINK | DEV9Header.SMAP_PHY_STS_100M | DEV9Header.SMAP_PHY_STS_FDX | DEV9Header.SMAP_PHY_STS_ANCP;
                                    break;
                            }
                            Log_Verb("phy_read " + reg.ToString() + ": " + val.ToString("X"));
                            value = (uint)((value & 0xFFFFu) | (uint)((int)val << 16));
                        }
                        if ((value & (DEV9Header.SMAP_E3_PHY_WRITE)) != 0)
                        {
                            value |= DEV9Header.SMAP_E3_PHY_OP_COMP;
                            uint reg = (value & (DEV9Header.SMAP_E3_PHY_REG_ADDR_MSK));
                            UInt16 val = (UInt16)(value >> 16);
                            switch (reg)
                            {
                                case DEV9Header.SMAP_DsPHYTER_BMCR:
                                    unchecked
                                    {
                                        val &= (ushort)(~DEV9Header.SMAP_PHY_BMCR_RST);
                                    }
                                    val |= 0x1;
                                    break;
                            }
                            Log_Verb("phy_write " + reg.ToString() + ": " + val.ToString("X"));
                            dev9.phyRegs[reg] = val;
                        }
                    }
                    break;
                default:
                    Log_Verb("SMAP: emac3 write  " + addr.ToString("X8") + "=" + value.ToString("X"));
                    break;
            }
            dev9.Dev9Wu32((int)addr, WordSwap(value));
        }
        public byte SMAP_Read8(UInt32 addr)
        {
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_CTRL:
                    Log_Verb("SMAP_R_TXFIFO_CTRL 8bit read value " + dev9.Dev9Ru8((int)addr).ToString("X"));
                    return dev9.Dev9Ru8((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_CTRL:
                    Log_Verb("SMAP_R_RXFIFO_CTRL 8bit read value " + dev9.Dev9Ru8((int)addr).ToString("X"));
                    return dev9.Dev9Ru8((int)addr);
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //printf("SMAP_R_TXFIFO_FRAME_CNT read 8\n");
                    break;
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //printf("SMAP_R_RXFIFO_FRAME_CNT read 8\n");
                    break;

                case DEV9Header.SMAP_R_BD_MODE:
                    Log_Verb("SMAP_R_BD_MODE 8bit read value " + dev9.bdSwap.ToString("X"));
                    return dev9.bdSwap;

                default:
                    Log_Error("SMAP : Unknown 8 bit read @ " + addr.ToString("X") + ", v=" + dev9.Dev9Ru8((int)addr).ToString("X"));
                    return dev9.Dev9Ru8((int)addr);
            }

            Log_Error("SMAP : error , 8 bit read @ " + addr.ToString("X") + ", v=" + dev9.Dev9Ru8((int)addr).ToString("X"));
            return dev9.Dev9Ru8((int)addr);
        }
        public UInt16 SMAP_Read16(UInt32 addr)
        {
            if (addr >= DEV9Header.SMAP_BD_TX_BASE && addr < (DEV9Header.SMAP_BD_TX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rv = dev9.Dev9Ru16((int)addr);
                if (dev9.bdSwap != 0)
                {
                    Log_Verb("SMAP : Generic TX read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                Log_Verb("SMAP : Generic TX read " + rv.ToString("X"));
                return (UInt16)rv;
            }
            else if (addr >= DEV9Header.SMAP_BD_RX_BASE && addr < (DEV9Header.SMAP_BD_RX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rv = dev9.Dev9Ru16((int)addr);
                if (dev9.bdSwap != 0)
                {
                    Log_Verb("SMAP : Generic RX read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                Log_Verb("SMAP : Generic RX read " + rv.ToString("X"));
                return (UInt16)rv;
            }

            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //printf("SMAP_R_TXFIFO_FRAME_CNT read 16\n");
                    Log_Verb("SMAP_R_TXFIFO_FRAME_CNT 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //printf("SMAP_R_RXFIFO_FRAME_CNT read 16\n");
                    Log_Verb("SMAP_R_RXFIFO_FRAME_CNT 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    Log_Verb("SMAP_R_EMAC3_MODE0_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE0_H:
                    Log_Verb("SMAP_R_EMAC3_MODE0_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE1_L:
                    Log_Verb("SMAP_R_EMAC3_MODE1_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE1_H:
                    Log_Verb("SMAP_R_EMAC3_MODE1_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_RxMODE_L:
                    Log_Verb("SMAP_R_EMAC3_RxMODE_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_RxMODE_H:
                    Log_Verb("SMAP_R_EMAC3_RxMODE_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_L:
                    Log_Verb("SMAP_R_EMAC3_INTR_STAT_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_H:
                    Log_Verb("SMAP_R_EMAC3_INTR_STAT_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_L:
                    Log_Verb("SMAP_R_EMAC3_INTR_ENABLE_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_H:
                    Log_Verb("SMAP_R_EMAC3_INTR_ENABLE_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE0_L:
                    Log_Verb("SMAP_R_EMAC3_TxMODE0_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE0_H:
                    Log_Verb("SMAP_R_EMAC3_TxMODE0_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X")); ;
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE1_L:
                    Log_Verb("SMAP_R_EMAC3_TxMODE1_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE1_H:
                    Log_Verb("SMAP_R_EMAC3_TxMODE1_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                    Log_Verb("SMAP_R_EMAC3_STA_CTRL_L 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_H:
                    Log_Verb("SMAP_R_EMAC3_STA_CTRL_H 16bit read " + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);
                default:
                    Log_Verb("SMAP: Unknown 16 bit read @ " + addr.ToString("X") + ", v=" + dev9.Dev9Ru16((int)addr).ToString("X"));
                    return dev9.Dev9Ru16((int)addr);
            }

            //DEV9.DEVLOG_shared.LogWriteLine("SMAP : error , 16 bit read @ " + addr.ToString("X") + ", v=" + DEV9Header.dev9Ru16((int)addr).ToString());
            //return DEV9Header.dev9Ru16((int)addr);

        }
        public UInt32 SMAP_Read32(UInt32 addr)
        {
            if (addr >= DEV9Header.SMAP_EMAC3_REGBASE && addr < DEV9Header.SMAP_EMAC3_REGEND)
            {
                Log_Verb("SMAP : 32bit read is double 16bit read");
                UInt32 hi = SMAP_Read16(addr);
                UInt32 lo = (UInt32)((int)SMAP_Read16(addr + 2) << 16);
                Log_Verb("SMAP : Double 16bit read combined value " + (hi | lo).ToString("X"));
                return hi | lo;
            }
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //Error.WriteLine("SMAP_R_TXFIFO_FRAME_CNT read 32" + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                    return dev9.Dev9Ru32((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //Error.WriteLine("SMAP_R_RXFIFO_FRAME_CNT read 32\n" + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                    return dev9.Dev9Ru32((int)addr);
                //This Case is handled in above if statement relating to EMAC regs
                //case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                //    CLR_DEV9.DEV9_LOG("SMAP_R_EMAC3_STA_CTRL_L 32bit read value " + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                //    return DEV9Header.dev9Ru32((int)addr);

                case DEV9Header.SMAP_R_RXFIFO_DATA:
                    {
                        int rd_ptr = (int)dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) & 16383;

                        //int rv = *((u32*)(dev9.rxfifo + rd_ptr));
                        int rv = BitConverter.ToInt32(dev9.rxFifo, rd_ptr);

                        dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, (UInt32)((rd_ptr + 4) & 16383));

                        if (dev9.bdSwap != 0)
                        {
                            rv = (rv << 24) | (rv >> 24) | ((rv >> 8) & 0xFF00) | ((rv << 8) & 0xFF0000);
                        }

                        Log_Verb("SMAP_R_RXFIFO_DATA 32bit read " + rv.ToString("X"));
                        return (uint)rv;
                    }
                default:
                    Log_Error("SMAP : Unknown 32 bit read @ " + addr.ToString("X8") + ",v=" + dev9.Dev9Ru32((int)addr).ToString("X"));
                    return dev9.Dev9Ru32((int)addr);
            }

            // DEV9_LOG("SMAP : error , 32 bit read @ %X,v=%X\n", addr, dev9Ru32(addr));
            //return DEV9Header.dev9Ru32((int)addr);
        }
        //
        public void SMAP_Write8(UInt32 addr, byte value)
        {
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_INC:
                    Log_Verb("SMAP_R_TXFIFO_FRAME_INC 8bit write " + value);
                    {
                        //DEV9Header.dev9Ru8(DEV9Header.SMAP_R_TXFIFO_FRAME_CNT)++;
                        dev9.Dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, (byte)(dev9.Dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT) + 1));
                    }
                    return;

                case DEV9Header.SMAP_R_RXFIFO_FRAME_DEC:
                    Log_Verb("SMAP_R_RXFIFO_FRAME_DEC 8bit write " + value);
                    lock (counterSentry)
                    {
                        dev9.Dev9Wu8((int)addr, value); //yes this is a write
                        {
                            dev9.Dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, (byte)(dev9.Dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT) - 1));
                        }
                    }
                    return;

                case DEV9Header.SMAP_R_TXFIFO_CTRL:
                    Log_Verb("SMAP_R_TXFIFO_CTRL 8bit write " + value.ToString("X"));
                    if ((value & DEV9Header.SMAP_TXFIFO_RESET) != 0)
                    {
                        dev9.txbdi = 0;
                        dev9.txFifoRdPtr = 0;
                        dev9.Dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, 0);	//this actualy needs to be atomic (lock mov ...)
                        dev9.Dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, 0);
                        dev9.Dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_SIZE, 16384);
                    }
                    unchecked
                    {
                        value &= (byte)(~DEV9Header.SMAP_TXFIFO_RESET);
                    }
                    dev9.Dev9Wu8((int)addr, value);
                    return;

                case DEV9Header.SMAP_R_RXFIFO_CTRL:
                    Log_Verb("SMAP_R_RXFIFO_CTRL 8bit write " + value.ToString("X"));
                    if ((value & DEV9Header.SMAP_RXFIFO_RESET) != 0)
                    {
                        lock (resetSentry)
                        {
                            lock (counterSentry)
                            {
                                dev9.rxbdi = 0;
                                dev9.rxFifoWrPtr = 0;
                                dev9.Dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, 0);
                                dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, 0);
                                dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_SIZE, 16384);
                            }
                        }
                    }
                    unchecked
                    {
                        value &= (byte)(~DEV9Header.SMAP_RXFIFO_RESET);
                    }
                    dev9.Dev9Wu8((int)addr, value);
                    return;

                case DEV9Header.SMAP_R_BD_MODE:
                    if ((value & DEV9Header.SMAP_BD_SWAP) != 0)
                    {
                        Log_Verb("SMAP_R_BD_MODE: byteswapped.");
                        Log_Info("BD byteswapping enabled");
                        dev9.bdSwap = 1;
                    }
                    else
                    {
                        Log_Verb("SMAP_R_BD_MODE: NOT byteswapped");
                        Log_Info("BD byteswapping disabled");
                        dev9.bdSwap = 0;
                    }
                    return;
                default:
                    Log_Error("SMAP : Unknown 8 bit write @ " + addr.ToString("X8") + " ,v=" + value.ToString("X"));
                    dev9.Dev9Wu8((int)addr, value);
                    return;
            }

            // CLR_DEV9.DEV9_LOG("SMAP : error , 8 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu8((int)addr, value);
        }
        public void SMAP_Write16(UInt32 addr, UInt16 value)
        {
            if (addr >= DEV9Header.SMAP_BD_TX_BASE && addr < (DEV9Header.SMAP_BD_TX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                if (dev9.bdSwap != 0)
                    value = (UInt16)((value >> 8) | (value << 8));
                dev9.Dev9Wu16((int)addr, value);

                return;
            }
            else if (addr >= DEV9Header.SMAP_BD_RX_BASE && addr < (DEV9Header.SMAP_BD_RX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rx_index = (int)((addr - DEV9Header.SMAP_BD_RX_BASE) >> 3);
                if (dev9.bdSwap != 0)
                    value = (UInt16)((value >> 8) | (value << 8));
                dev9.Dev9Wu16((int)addr, value);
                return;
            }

            switch (addr)
            {

                case DEV9Header.SMAP_R_RXFIFO_RD_PTR:
                    Log_Verb("SMAP: SMAP_R_RXFIFO_RD_PTR 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    break;
                case DEV9Header.SMAP_R_RXFIFO_SIZE:
                    Log_Verb("SMAP: SMAP_R_34 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    break;
                case DEV9Header.SMAP_R_INTR_CLR:
                    Log_Verb("SMAP: SMAP_R_INTR_CLR 16bit write " + value.ToString("X"));
                    dev9.irqCause &= ~value;
                    return;

                case DEV9Header.SMAP_R_TXFIFO_WR_PTR:
                    Log_Verb("SMAP: SMAP_R_TXFIFO_WR_PTR 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    return;
                case DEV9Header.SMAP_R_TXFIFO_SIZE:
                    Log_Verb("SMAP: SMAP_R_TXFIFO_SIZE 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    break;
                //handle L writes
                //#define EMAC3_L_WRITE(name) \
                //    case name: \
                //        DEV9_LOG("SMAP: " #name " 16 bit write %x\n", value); \
                //        dev9Ru16(addr) = value; \
                //        return;
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    Log_Verb("SMAP: SMAP_R_EMAC3_SMAP_R_EMAC3_MODE0_L 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    return;
                case DEV9Header.SMAP_R_EMAC3_MODE1_L:
                    Log_Verb("SMAP: SMAP_R_EMAC3_SMAP_R_EMAC3_MODE1_L 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    return;
                case DEV9Header.SMAP_R_EMAC3_TxMODE0_L:
                case DEV9Header.SMAP_R_EMAC3_TxMODE1_L:
                case DEV9Header.SMAP_R_EMAC3_RxMODE_L:
                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_L:
                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_L:
                case DEV9Header.SMAP_R_EMAC3_ADDR_HI_L:
                case DEV9Header.SMAP_R_EMAC3_ADDR_LO_L:
                case DEV9Header.SMAP_R_EMAC3_VLAN_TPID:
                case DEV9Header.SMAP_R_EMAC3_PAUSE_TIMER_L:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH1:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH2:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH3:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH4:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH1:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH2:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH3:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH4:

                case DEV9Header.SMAP_R_EMAC3_LAST_SA_HI:
                case DEV9Header.SMAP_R_EMAC3_LAST_SA_LO:
                case DEV9Header.SMAP_R_EMAC3_INTER_FRAME_GAP_L:
                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                case DEV9Header.SMAP_R_EMAC3_TX_THRESHOLD_L:
                case DEV9Header.SMAP_R_EMAC3_RX_WATERMARK_L:
                case DEV9Header.SMAP_R_EMAC3_TX_OCTETS:
                case DEV9Header.SMAP_R_EMAC3_RX_OCTETS:
                    Log_Verb("SMAP: SMAP_R_EMAC3_***(L_Write) 16bit write " + value.ToString("X"));
                    //Look at all that logging I'm not doing
                    dev9.Dev9Wu16((int)addr, value);
                    return;

                //#define EMAC3_H_WRITE(name) \
                //    case name: \
                //        DEV9_LOG("SMAP: " #name " 16 bit write %x\n", value); \
                //        dev9Ru16(addr) = value; \
                //        emac3_write(addr-2); \
                //        return;
                //handle H writes
                case DEV9Header.SMAP_R_EMAC3_MODE0_H:
                case DEV9Header.SMAP_R_EMAC3_MODE1_H:
                case DEV9Header.SMAP_R_EMAC3_TxMODE0_H:
                case DEV9Header.SMAP_R_EMAC3_TxMODE1_H:
                case DEV9Header.SMAP_R_EMAC3_RxMODE_H:
                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_H:
                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_H:
                case DEV9Header.SMAP_R_EMAC3_ADDR_HI_H:
                case DEV9Header.SMAP_R_EMAC3_ADDR_LO_H:
                case DEV9Header.SMAP_R_EMAC3_VLAN_TPID + 2:
                case DEV9Header.SMAP_R_EMAC3_PAUSE_TIMER_H:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH1 + 2:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH2 + 2:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH3 + 2:
                case DEV9Header.SMAP_R_EMAC3_INDIVID_HASH4 + 2:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH1 + 2:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH2 + 2:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH3 + 2:
                case DEV9Header.SMAP_R_EMAC3_GROUP_HASH4 + 2:

                case DEV9Header.SMAP_R_EMAC3_LAST_SA_HI + 2:
                case DEV9Header.SMAP_R_EMAC3_LAST_SA_LO + 2:
                case DEV9Header.SMAP_R_EMAC3_INTER_FRAME_GAP_H:
                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_H:
                case DEV9Header.SMAP_R_EMAC3_TX_THRESHOLD_H:
                case DEV9Header.SMAP_R_EMAC3_RX_WATERMARK_H:
                case DEV9Header.SMAP_R_EMAC3_TX_OCTETS + 2:
                case DEV9Header.SMAP_R_EMAC3_RX_OCTETS + 2:
                    // DEV9_LOG("SMAP: " #name " 16 bit write %x\n", value); \
                    Log_Verb("SMAP: SMAP_R_EMAC3_***(H_Write) 16bit write " + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    EMAC3_Write(addr - 2);
                    return;

                default:
                    Log_Error("SMAP : Unknown 16 bit write @" + addr.ToString("X8") + ",v=" + value.ToString("X"));
                    dev9.Dev9Wu16((int)addr, value);
                    return;
            }

            // CLR_DEV9.DEV9_LOG("SMAP : error , 16 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu16((int)addr, value);
        }
        public void SMAP_Write32(UInt32 addr, UInt32 value)
        {
            if (addr >= DEV9Header.SMAP_EMAC3_REGBASE && addr < DEV9Header.SMAP_EMAC3_REGEND)
            {
                SMAP_Write16(addr, (UInt16)(value & 0xFFFF));
                SMAP_Write16(addr + 2, (UInt16)(value >> 16));
                return;
            }
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_DATA:
                    if (dev9.bdSwap != 0)
                        value = (value << 24) | (value >> 24) | ((value >> 8) & 0xFF00) | ((value << 8) & 0xFF0000);

                    Log_Verb("SMAP_R_TXFIFO_DATA 32bit write " + value.ToString("X"));
                    //*((u32*)(DEV9Header.dev9.txfifo + dev9Ru32(DEV9Header.SMAP_R_TXFIFO_WR_PTR))) = value; //I'm sorry but what??
                    // I think this is how its supposed to work
                    byte[] valuebytes = BitConverter.GetBytes(value);
                    Utils.memcpy(ref dev9.txFifo, (int)dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR), valuebytes, 0, 4);
                    //end of that one line
                    dev9.Dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, (dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) + 4) & 16383);
                    return;
                default:
                    Log_Error("SMAP : Unknown 32 bit write @ " + addr.ToString("X8") + ",v=" + value.ToString("X"));
                    dev9.Dev9Wu32((int)addr, value);
                    return;
            }

            //CLR_DEV9.DEV9_LOG("SMAP : error , 32 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu32((int)addr, value);
        }

        public void SMAP_ReadDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if ((dev9.Dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & DEV9Header.SMAP_RXFIFO_DMAEN) != 0)
            {
                int pMemAddr = 0;
                int valueSize = 4;
                dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) & 16383);
                size >>= 1;
                Log_Verb(" * * SMAP DMA READ START: rd_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + dev9.rxFifoWrPtr.ToString());
                while (size > 0)
                {
                    //*pMem = *((u32*)(DEV9Header.dev9.rxfifo + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR)));
                    int pMemIn = BitConverter.ToInt32(dev9.rxFifo, (int)dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR));
                    byte[] pMemInBytes = BitConverter.GetBytes(pMemIn);
                    pMem.Seek(pMemAddr, System.IO.SeekOrigin.Begin);
                    pMem.Write(pMemInBytes, 0, 4);
                    //End write to pMem
                    pMemAddr += valueSize;//pMem++;
                    dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, (dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) + 4) & 16383);

                    size -= valueSize;
                }
                Log_Verb(" * * SMAP DMA READ END:   rd_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + dev9.rxFifoWrPtr.ToString());

                dev9.Dev9Wu16((int)DEV9Header.SMAP_R_RXFIFO_CTRL, (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & ~DEV9Header.SMAP_RXFIFO_DMAEN));
            }
        }
        public void SMAP_WriteDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if ((dev9.Dev9Ru16((int)DEV9Header.SMAP_R_TXFIFO_CTRL) & DEV9Header.SMAP_TXFIFO_DMAEN) != 0)
            {
                int pMemAddr = 0;
                int valueSize = 4;
                dev9.Dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) & 16383);
                size >>= 1;
                Log_Verb(" * * SMAP DMA WRITE START: wr_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR).ToString() + ", rd_ptr=" + dev9.txFifoRdPtr.ToString());
                while (size > 0)
                {
                    pMem.Seek(pMemAddr, System.IO.SeekOrigin.Begin);
                    byte[] valueBytes = new byte[4];
                    pMem.Read(valueBytes, 0, 4);
                    int value = BitConverter.ToInt32(valueBytes, 0);//*pMem;
                    //	value=(value<<24)|(value>>24)|((value>>8)&0xFF00)|((value<<8)&0xFF0000);
                    pMemAddr += valueSize;//pMem++;

                    //*((u32*)(dev9.txfifo + dev9Ru32(SMAP_R_TXFIFO_WR_PTR))) = value;
                    byte[] valueBytes2 = BitConverter.GetBytes(value);
                    Utils.memcpy(ref dev9.txFifo, (int)dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR), valueBytes2, 0, 4);
                    //End 
                    dev9.Dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, (dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) + 4) & 16383);
                    size -= valueSize;
                }
                Log_Verb(" * * SMAP DMA WRITE END:   wr_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR).ToString() + ", rd_ptr=" + dev9.txFifoRdPtr.ToString());

                dev9.Dev9Wu16((int)DEV9Header.SMAP_R_TXFIFO_CTRL, (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SMAP_R_TXFIFO_CTRL) & ~DEV9Header.SMAP_TXFIFO_DMAEN));

            }
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
