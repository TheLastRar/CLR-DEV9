using System;

namespace CLR_DEV9
{
    static class smap
    {
        static bool has_link = true;
        volatile static bool fireIntR = false;
        static Object reset_sentry = new Object();
        static Object counter_sentry = new Object();
        //this can return a false positive, but its not problem since it may say it cant recv while it can (no harm done, just delay on packets)
        public static bool rx_fifo_can_rx()
        {
            //check if RX is on & stuff like that here

            //Check if there is space on RXBD
            if (DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT) == 64)
                return false;

            //Check if there is space on fifo
            int rd_ptr = (int)DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR);
            int space = DEV9Header.dev9.rxfifo.Length -
                ((DEV9Header.dev9.rxfifo_wr_ptr - rd_ptr) & 16383);

            if (space == 0)
                space = (DEV9Header.dev9.rxfifo.Length);

            if (space < 1514)
                return false;

            //we can recv a packet !
            return true;
        }
        public static void rx_process(ref netHeader.NetPacket pk)
        {
            if (!rx_fifo_can_rx())
            {
                Console.Error.WriteLine("ERROR : !rx_fifo_can_rx at rx_process");
                return;
            }
            //smap_bd_t* pbd = ((smap_bd_t*)&dev9.dev9R[SMAP_BD_RX_BASE & 0xffff]) + dev9.rxbdi;
            int soff = (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + DEV9Header.dev9.rxbdi * DEV9Header.smap_bd.GetSize());
            DEV9Header.smap_bd pbd = new DEV9Header.smap_bd(DEV9Header.dev9.dev9R, soff);

            int bytes = (pk.size + 3) & (~3);

            if (!((pbd.ctrl_stat & DEV9Header.SMAP_BD_RX_EMPTY) != 0))
            {
                Console.Error.WriteLine("ERROR (!(pbd->ctrl_stat & SMAP_BD_RX_EMPTY))");
                Console.Error.WriteLine("ERROR : Discarding " + bytes + " bytes (RX" + DEV9Header.dev9.rxbdi + " not ready)");
                return;
            }

            int pstart = (DEV9Header.dev9.rxfifo_wr_ptr) & 16383;
            int i = 0;
            while (i < bytes)
            {
                DEV9Header.dev9_rxfifo_write(pk.buffer[i++]);
                DEV9Header.dev9.rxfifo_wr_ptr &= 16383;
            }
            lock (reset_sentry)
            {
                //increase RXBD
                DEV9Header.dev9.rxbdi++;
                DEV9Header.dev9.rxbdi &= (uint)((DEV9Header.SMAP_BD_SIZE / 8) - 1);

                //Fill the BD with info !
                pbd.length = (ushort)pk.size;
                pbd.pointer = (ushort)(0x4000 + pstart);
                unchecked //Allow -int to uint
                {
                    pbd.ctrl_stat &= (ushort)~DEV9Header.SMAP_BD_RX_EMPTY;
                }

                //increase frame count
                lock (counter_sentry)
                {
                    byte framecount = DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT);
                    framecount++;
                    DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, framecount);
                }
            }
            //spams// emu_printf("Got packet, %d bytes (%d fifo)\n", pk->size,bytes);
            fireIntR = true;
            //DEV9._DEV9irq(DEV9Header.SMAP_INTR_RXEND, 0);//now ? or when the fifo is full ? i guess now atm
            //note that this _is_ wrong since the IOP interrupt system is not thread safe.. but nothing i can do about that
        }

        private static UInt32 wswap(UInt32 d)
        {
            return (d >> 16) | (d << 16);
        }

        //tx_process
        private static void tx_process()
        {
            //Console.Error.WriteLine("TX");
            //we loop based on count ? or just *use* it ?
            UInt32 cnt = DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT);
            //spams// printf("tx_process : %d cnt frames !\n",cnt);

            netHeader.NetPacket pk = new netHeader.NetPacket();
            int fc = 0;
            for (fc = 0; fc < cnt; fc++)
            {
                //smap_bd_t *pbd= ((smap_bd_t *)&dev9.dev9R[SMAP_BD_TX_BASE & 0xffff])+dev9.txbdi;

                DEV9Header.smap_bd pbd;
                pbd = new DEV9Header.smap_bd(DEV9Header.dev9.dev9R, (int)((DEV9Header.SMAP_BD_TX_BASE & 0xffff) + (DEV9Header.smap_bd.GetSize() * DEV9Header.dev9.txbdi)));

                if (!((pbd.ctrl_stat & DEV9Header.SMAP_BD_TX_READY) != 0))
                {
                    Console.Error.WriteLine("ERROR : !pbd->ctrl_stat&SMAP_BD_TX_READY\n");
                    break;
                }
                if ((pbd.length & 3) != 0)
                {
                    //spams// emu_printf("WARN : pbd->length not alligned %d\n",pbd->length);
                }

                if (pbd.length > 1514)
                {
                    Console.Error.WriteLine("ERROR : Trying to send packet too big.\n");
                }
                else
                {
                    UInt32 _base = (UInt32)((pbd.pointer - 0x1000) & 16383);
                    DEV9.DEV9_LOG("Sending Packet from base " + _base.ToString("X") + ", size " + pbd.length);
                    //The 1st packet we send should be base 0, size 1514
                    //spams// emu_printf("Sending Packet from base %x, size %d\n", base, pbd->length);

                    pk.size = pbd.length;

                    if (!(pbd.pointer >= 0x1000))
                    {
                        DEV9.DEV9_LOG("ERROR: odd , !pbd->pointer>0x1000 | 0x" + pbd.pointer.ToString("X") + " " + pbd.length.ToString());
                    }

                    if (_base + pbd.length > 16384)
                    {
                        UInt32 was = 16384 - _base;
                        Utils.memcpy(ref pk.buffer, 0, DEV9Header.dev9.txfifo, (int)_base, (int)was);
                        Utils.memcpy(ref pk.buffer, (int)was, DEV9Header.dev9.txfifo, 0, (int)(pbd.length - was)); //I thingk this was a bug in the original plugin
                        Console.Error.WriteLine("Warped read, was=" + was + ", sz=" + pbd.length + ", sz-was=" + (pbd.length - was));
                    }
                    else
                    {
                        Utils.memcpy(ref pk.buffer, 0, DEV9Header.dev9.txfifo, (int)_base, (int)pbd.length);
                    }
                    net.tx_put(ref pk);
                }

                unchecked
                {
                    pbd.ctrl_stat &= (UInt16)(~DEV9Header.SMAP_BD_TX_READY);
                }

                //increase TXBD
                DEV9Header.dev9.txbdi++;
                DEV9Header.dev9.txbdi &= (DEV9Header.SMAP_BD_SIZE / 8) - 1;

                //decrease frame count -- this is not thread safe
                //dev9Ru8(SMAP_R_TXFIFO_FRAME_CNT)--;
                DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, (byte)(DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT) - 1));
            }

            //spams// emu_printf("processed %d frames, %d count, cnt = %d\n",fc,dev9Ru8(SMAP_R_TXFIFO_FRAME_CNT),cnt);
            //if some error/early exit signal TXDNV
            if (fc != cnt || cnt == 0)
            {
                Console.Error.WriteLine("WARN : (fc!=cnt || cnt==0) but packet send request was made oO..");
                DEV9._DEV9irq(DEV9Header.SMAP_INTR_TXDNV, 0);
            }
            //if we actualy send something send TXEND
            if (fc != 0)
                DEV9._DEV9irq(DEV9Header.SMAP_INTR_TXEND, 100);//now ? or when the fifo is empty ? i guess now atm
        }

        private static void emac3_write(UInt32 addr)
        {
            UInt32 value = wswap(DEV9Header.dev9Ru32((int)addr));
            switch (addr)
            {
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_MODE0 write " + value.ToString("X"));
                    value = (value & (~DEV9Header.SMAP_E3_SOFT_RESET)) | DEV9Header.SMAP_E3_TXMAC_IDLE | DEV9Header.SMAP_E3_RXMAC_IDLE;
                    UInt16 tmp = (UInt16)(DEV9Header.dev9Ru16((int)DEV9Header.SMAP_R_EMAC3_STA_CTRL_H) | DEV9Header.SMAP_E3_PHY_OP_COMP);
                    DEV9Header.dev9Wu16((int)DEV9Header.SMAP_R_EMAC3_STA_CTRL_H, tmp);
                    break;
                case DEV9Header.SMAP_R_EMAC3_TxMODE0_L:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_TxMODE0_L write " + value.ToString("X"));
                    //spams// emu_printf("SMAP: SMAP_R_EMAC3_TxMODE0_L write %x\n", value);
                    //Process TX  here ?
                    if (!(value != 0) & (DEV9Header.SMAP_E3_TX_GNP_0 != 0))
                        Console.Error.WriteLine("SMAP_R_EMAC3_TxMODE0_L: SMAP_E3_TX_GNP_0 not set");

                    tx_process();
                    value = value & ~DEV9Header.SMAP_E3_TX_GNP_0;
                    if (value != 0)
                        Console.Error.WriteLine("SMAP_R_EMAC3_TxMODE0_L: extra bits set !");
                    break;
                case DEV9Header.SMAP_R_EMAC3_TxMODE1_L:
                    Console.Error.WriteLine("SMAP_R_EMAC3_TxMODE1_L 32bit write " + value.ToString("X"));
                    if (value == 0x380f0000)
                    {
                        Console.Error.WriteLine("Adapter Detection Hack - Resetting RX/TX");
                        DEV9._DEV9irq(DEV9Header.SMAP_INTR_RXEND | DEV9Header.SMAP_INTR_TXEND | DEV9Header.SMAP_INTR_TXDNV, 5);
                    }
                    break;
                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_STA_CTRL write " + value.ToString("X"));
                    {
                        if ((value & (DEV9Header.SMAP_E3_PHY_READ)) != 0)
                        {
                            value |= DEV9Header.SMAP_E3_PHY_OP_COMP;
                            int reg = (int)(value & (DEV9Header.SMAP_E3_PHY_REG_ADDR_MSK));
                            UInt16 val = DEV9Header.dev9.phyregs[reg];
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
                            DEV9.DEV9_LOG("phy_read " + reg.ToString() + ": " + val.ToString("X"));
                            value = (uint)((value & 0xFFFFu) | (uint)((int)val << 16));
                        }
                        if ((value & (DEV9Header.SMAP_E3_PHY_WRITE)) != 0)
                        {
                            value |= DEV9Header.SMAP_E3_PHY_OP_COMP;
                            int reg = (int)(value & (DEV9Header.SMAP_E3_PHY_REG_ADDR_MSK));
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
                            DEV9.DEV9_LOG("phy_write " + reg.ToString() + ": " + val.ToString("X"));
                            DEV9Header.dev9.phyregs[reg] = val;
                        }
                    }
                    break;
                default:
                    DEV9.DEV9_LOG("SMAP: emac3 write  " + addr.ToString("X8") + "=" + value.ToString("X"));
                    break;
            }
            DEV9Header.dev9Wu32((int)addr, wswap(value));
        }
        public static byte smap_read8(UInt32 addr)
        {
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //printf("SMAP_R_TXFIFO_FRAME_CNT read 8\n");
                    break;
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //printf("SMAP_R_RXFIFO_FRAME_CNT read 8\n");
                    break;

                case DEV9Header.SMAP_R_BD_MODE:
                    DEV9.DEV9_LOG("SMAP_R_BD_MODE 8bit read value " + DEV9Header.dev9.bd_swap.ToString("X"));
                    return DEV9Header.dev9.bd_swap;

                default:
                    DEV9.DEV9_LOG("SMAP : Unknown 8 bit read @ " + addr.ToString("X") + ", v=" + DEV9Header.dev9Ru8((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru8((int)addr);
            }

            DEV9.DEV9_LOG("SMAP : error , 8 bit read @ " + addr.ToString("X") + ", v=" + DEV9Header.dev9Ru8((int)addr).ToString("X"));
            return DEV9Header.dev9Ru8((int)addr);
        }
        public static UInt16 smap_read16(UInt32 addr)
        {
            if (addr >= DEV9Header.SMAP_BD_TX_BASE && addr < (DEV9Header.SMAP_BD_TX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rv = DEV9Header.dev9Ru16((int)addr);
                if (DEV9Header.dev9.bd_swap != 0)
                {
                    DEV9.DEV9_LOG("SMAP : Generic TX read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                DEV9.DEV9_LOG("SMAP : Generic TX read " + rv.ToString("X"));
                return (UInt16)rv;
            }
            else if (addr >= DEV9Header.SMAP_BD_RX_BASE && addr < (DEV9Header.SMAP_BD_RX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rv = DEV9Header.dev9Ru16((int)addr);
                if (DEV9Header.dev9.bd_swap != 0)
                {
                    DEV9.DEV9_LOG("SMAP : Generic RX read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                DEV9.DEV9_LOG("SMAP : Generic RX read " + rv.ToString("X"));
                return (UInt16)rv;
            }

            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //printf("SMAP_R_TXFIFO_FRAME_CNT read 16\n");
                    DEV9.DEV9_LOG("SMAP_R_TXFIFO_FRAME_CNT 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //printf("SMAP_R_RXFIFO_FRAME_CNT read 16\n");
                    DEV9.DEV9_LOG("SMAP_R_RXFIFO_FRAME_CNT 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_MODE0_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE0_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_MODE0_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE1_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_MODE1_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_MODE1_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_MODE1_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_RxMODE_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_RxMODE_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_RxMODE_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_RxMODE_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_INTR_STAT_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_STAT_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_INTR_STAT_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_INTR_ENABLE_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_INTR_ENABLE_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_INTR_ENABLE_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE0_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_TxMODE0_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE0_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_TxMODE0_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X")); ;
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE1_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_TxMODE1_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_TxMODE1_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_TxMODE1_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_STA_CTRL_L 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);

                case DEV9Header.SMAP_R_EMAC3_STA_CTRL_H:
                    DEV9.DEV9_LOG("SMAP_R_EMAC3_STA_CTRL_H 16bit read " + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);
                default:
                    DEV9.DEV9_LOG("SMAP: Unknown 16 bit read @ " + addr.ToString("X") + ", v=" + DEV9Header.dev9Ru16((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru16((int)addr);
            }

            //DEV9.DEVLOG_shared.LogWriteLine("SMAP : error , 16 bit read @ " + addr.ToString("X") + ", v=" + DEV9Header.dev9Ru16((int)addr).ToString());
            //return DEV9Header.dev9Ru16((int)addr);

        }
        public static UInt32 smap_read32(UInt32 addr)
        {
            if (addr >= DEV9Header.SMAP_EMAC3_REGBASE && addr < DEV9Header.SMAP_EMAC3_REGEND)
            {
                DEV9.DEV9_LOG("SMAP : 32bit read is double 16bit read");
                UInt32 hi = smap_read16(addr);
                UInt32 lo = (UInt32)((int)smap_read16(addr + 2) << 16);
                DEV9.DEV9_LOG("SMAP : Double 16bit read combined value " + (hi | lo).ToString("X"));
                return hi | lo;
            }
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_CNT:
                    //Console.Error.WriteLine("SMAP_R_TXFIFO_FRAME_CNT read 32" + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru32((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    //Console.Error.WriteLine("SMAP_R_RXFIFO_FRAME_CNT read 32\n" + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru32((int)addr);
                    //This Case is handled in above if statement relating to EMAC regs
                //case DEV9Header.SMAP_R_EMAC3_STA_CTRL_L:
                //    DEV9.DEV9_LOG("SMAP_R_EMAC3_STA_CTRL_L 32bit read value " + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                //    return DEV9Header.dev9Ru32((int)addr);

                case DEV9Header.SMAP_R_RXFIFO_DATA:
                    {
                        int rd_ptr = (int)DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) & 16383;

                        //int rv = *((u32*)(dev9.rxfifo + rd_ptr));
                        int rv = BitConverter.ToInt32(DEV9Header.dev9.rxfifo, rd_ptr);

                        DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, (UInt32)((rd_ptr + 4) & 16383));

                        if (DEV9Header.dev9.bd_swap != 0)
                        {
                            rv = (rv << 24) | (rv >> 24) | ((rv >> 8) & 0xFF00) | ((rv << 8) & 0xFF0000);
                        }

                        DEV9.DEV9_LOG("SMAP_R_RXFIFO_DATA 32bit read " + rv.ToString("X"));
                        return (uint)rv;
                    }
                default:
                    DEV9.DEV9_LOG("SMAP : Unknown 32 bit read @ " + addr.ToString("X8") + ",v=" + DEV9Header.dev9Ru32((int)addr).ToString("X"));
                    return DEV9Header.dev9Ru32((int)addr);
            }

            // DEV9_LOG("SMAP : error , 32 bit read @ %X,v=%X\n", addr, dev9Ru32(addr));
            //return DEV9Header.dev9Ru32((int)addr);
        }
        //
        public static void smap_write8(UInt32 addr, byte value)
        {
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_FRAME_INC:
                    DEV9.DEV9_LOG("SMAP_R_TXFIFO_FRAME_INC 8bit write " + value);
                    {
                        //DEV9Header.dev9Ru8(DEV9Header.SMAP_R_TXFIFO_FRAME_CNT)++;
                        DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, (byte)(DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT) + 1));
                    }
                    return;

                case DEV9Header.SMAP_R_RXFIFO_FRAME_DEC:
                    DEV9.DEV9_LOG("SMAP_R_RXFIFO_FRAME_DEC 8bit write " + value);
                    lock (counter_sentry)
                    {
                        DEV9Header.dev9Wu8((int)addr, value); //yes this is a write
                        {
                            DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, (byte)(DEV9Header.dev9Ru8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT) - 1));
                        }
                    }
                    return;

                case DEV9Header.SMAP_R_TXFIFO_CTRL:
                    DEV9.DEV9_LOG("SMAP_R_TXFIFO_CTRL 8bit write " + value.ToString("X"));
                    if ((value & DEV9Header.SMAP_TXFIFO_RESET) != 0)
                    {
                        DEV9Header.dev9.txbdi = 0;
                        DEV9Header.dev9.txfifo_rd_ptr = 0;
                        DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_TXFIFO_FRAME_CNT, 0);	//this actualy needs to be atomic (lock mov ...)
                        DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, 0);
                        DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_SIZE, 16384);
                    }
                    unchecked
                    {
                        value &= (byte)(~DEV9Header.SMAP_TXFIFO_RESET);
                    }
                    DEV9Header.dev9Wu8((int)addr, value);
                    return;

                case DEV9Header.SMAP_R_RXFIFO_CTRL:
                    DEV9.DEV9_LOG("SMAP_R_RXFIFO_CTRL 8bit write " + value.ToString("X"));
                    if ((value & DEV9Header.SMAP_RXFIFO_RESET) != 0)
                    {
                        lock (reset_sentry)
                        {
                            lock (counter_sentry)
                            {
                                DEV9Header.dev9.rxbdi = 0;
                                DEV9Header.dev9.rxfifo_wr_ptr = 0;
                                DEV9Header.dev9Wu8((int)DEV9Header.SMAP_R_RXFIFO_FRAME_CNT, 0);
                                DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, 0);
                                DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_SIZE, 16384);
                            }
                        }
                    }
                    unchecked
                    {
                        value &= (byte)(~DEV9Header.SMAP_RXFIFO_RESET);
                    }
                    DEV9Header.dev9Wu8((int)addr, value);
                    return;

                case DEV9Header.SMAP_R_BD_MODE:
                    if ((value & DEV9Header.SMAP_BD_SWAP) != 0)
                    {
                        DEV9.DEV9_LOG("SMAP_R_BD_MODE: byteswapped.");
                        Console.Error.WriteLine("BD Byteswapping enabled");
                        DEV9Header.dev9.bd_swap = 1;
                    }
                    else
                    {
                        DEV9.DEV9_LOG("SMAP_R_BD_MODE: NOT byteswapped");
                        Console.Error.WriteLine("BD Byteswapping disabled");
                        DEV9Header.dev9.bd_swap = 0;
                    }
                    return;
                default:
                    DEV9.DEV9_LOG("SMAP : Unknown 8 bit write @ " + addr.ToString("X8") + " ,v=" + value.ToString("X"));
                    DEV9Header.dev9Wu8((int)addr, value);
                    return;
            }

            // DEV9.DEV9_LOG("SMAP : error , 8 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu8((int)addr, value);
        }
        public static void smap_write16(UInt32 addr, UInt16 value)
        {
            if (addr >= DEV9Header.SMAP_BD_TX_BASE && addr < (DEV9Header.SMAP_BD_TX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                if (DEV9Header.dev9.bd_swap != 0)
                    value = (UInt16)((value >> 8) | (value << 8));
                DEV9Header.dev9Wu16((int)addr, value);

                return;
            }
            else if (addr >= DEV9Header.SMAP_BD_RX_BASE && addr < (DEV9Header.SMAP_BD_RX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rx_index = (int)((addr - DEV9Header.SMAP_BD_RX_BASE) >> 3);
                if (DEV9Header.dev9.bd_swap != 0)
                    value = (UInt16)((value >> 8) | (value << 8));
                DEV9Header.dev9Wu16((int)addr, value);
                return;
            }

            switch (addr)
            {
                case DEV9Header.SMAP_R_INTR_CLR:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_INTR_CLR 16bit write " + value.ToString("X"));
                    DEV9Header.dev9.irqcause &= ~value;
                    return;

                case DEV9Header.SMAP_R_TXFIFO_WR_PTR:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_TXFIFO_WR_PTR 16bit write " + value.ToString("X"));
                    DEV9Header.dev9Wu16((int)addr, value);
                    return;

                //handle L writes
                //#define EMAC3_L_WRITE(name) \
                //    case name: \
                //        DEV9_LOG("SMAP: " #name " 16 bit write %x\n", value); \
                //        dev9Ru16(addr) = value; \
                //        return;
                case DEV9Header.SMAP_R_EMAC3_MODE0_L:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_SMAP_R_EMAC3_MODE0_L 16bit write " + value.ToString("X"));
                    DEV9Header.dev9Wu16((int)addr, value);
                    return;
                case DEV9Header.SMAP_R_EMAC3_MODE1_L:
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_SMAP_R_EMAC3_MODE1_L 16bit write " + value.ToString("X"));
                    DEV9Header.dev9Wu16((int)addr, value);
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
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_***(L_Write) 16bit write " + value.ToString("X"));
                    //Look at all that logging I'm not doing
                    DEV9Header.dev9Wu16((int)addr, value);
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
                    DEV9.DEV9_LOG("SMAP: SMAP_R_EMAC3_***(H_Write) 16bit write " + value.ToString("X"));
                    DEV9Header.dev9Wu16((int)addr, value);
                    emac3_write(addr - 2);
                    return;

                default:
                    DEV9.DEV9_LOG("SMAP : Unknown 16 bit write @" + addr.ToString("X8") + ",v=" + value.ToString("X"));
                    DEV9Header.dev9Wu16((int)addr, value);
                    return;
            }

            // DEV9.DEV9_LOG("SMAP : error , 16 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu16((int)addr, value);
        }
        public static void smap_write32(UInt32 addr, UInt32 value)
        {
            if (addr >= DEV9Header.SMAP_EMAC3_REGBASE && addr < DEV9Header.SMAP_EMAC3_REGEND)
            {
                smap_write16(addr, (UInt16)(value & 0xFFFF));
                smap_write16(addr + 2, (UInt16)(value >> 16));
                return;
            }
            switch (addr)
            {
                case DEV9Header.SMAP_R_TXFIFO_DATA:
                    if (DEV9Header.dev9.bd_swap != 0)
                        value = (value << 24) | (value >> 24) | ((value >> 8) & 0xFF00) | ((value << 8) & 0xFF0000);

                    DEV9.DEV9_LOG("SMAP_R_TXFIFO_DATA 32bit write " + value.ToString("X"));
                    //*((u32*)(DEV9Header.dev9.txfifo + dev9Ru32(DEV9Header.SMAP_R_TXFIFO_WR_PTR))) = value; //I'm sorry but what??
                    // I think this is how its supposed to work
                    byte[] valuebytes = BitConverter.GetBytes(value);
                    Utils.memcpy(ref DEV9Header.dev9.txfifo, (int)DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR), valuebytes, 0, 4);
                    //end of that one line
                    DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, (DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) + 4) & 16383);
                    return;
                default:
                    DEV9.DEV9_LOG("SMAP : Unknown 32 bit write @ " + addr.ToString("X8") + ",v=" + value.ToString("X"));
                    DEV9Header.dev9Wu32((int)addr, value);
                    return;
            }

            //DEV9.DEV9_LOG("SMAP : error , 32 bit write @ %X,v=%X\n", addr, value);
            //DEV9Header.dev9Wu32((int)addr, value);
        }

        public static void smap_readDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if ((DEV9Header.dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & DEV9Header.SMAP_RXFIFO_DMAEN) != 0)
            {
                int pMemAddr = 0;
                int valueSize = 4;
                DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) & 16383);
                size >>= 1;
                DEV9.DEV9_LOG(" * * SMAP DMA READ START: rd_ptr=" + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + DEV9Header.dev9.rxfifo_wr_ptr.ToString());
                while (size > 0)
                {
                    //*pMem = *((u32*)(DEV9Header.dev9.rxfifo + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR)));
                    int pMemIn = BitConverter.ToInt32(DEV9Header.dev9.rxfifo, (int)DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR));
                    byte[] pMemInBytes = BitConverter.GetBytes(pMemIn);
                    pMem.Seek(pMemAddr, System.IO.SeekOrigin.Begin);
                    pMem.Write(pMemInBytes, 0, 4);
                    //End write to pMem
                    pMemAddr += valueSize;//pMem++;
                    DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, (DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) + 4) & 16383);

                    size -= valueSize;
                }
                DEV9.DEV9_LOG(" * * SMAP DMA READ END:   rd_ptr=" + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + DEV9Header.dev9.rxfifo_wr_ptr.ToString());

                DEV9Header.dev9Wu16((int)DEV9Header.SMAP_R_RXFIFO_CTRL, (UInt16)(DEV9Header.dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & ~DEV9Header.SMAP_RXFIFO_DMAEN));
            }
        }
        public static void smap_writeDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if ((DEV9Header.dev9Ru16((int)DEV9Header.SMAP_R_TXFIFO_CTRL) & DEV9Header.SMAP_TXFIFO_DMAEN) != 0)
            {
                int pMemAddr = 0;
                int valueSize = 4;
                DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) & 16383);
                size >>= 1;
                DEV9.DEV9_LOG(" * * SMAP DMA WRITE START: wr_ptr=" + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR).ToString() + ", rd_ptr=" + DEV9Header.dev9.txfifo_rd_ptr.ToString());
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
                    Utils.memcpy(ref DEV9Header.dev9.txfifo, (int)DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR), valueBytes2, 0, 4);
                    //End 
                    DEV9Header.dev9Wu32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR, (DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR) + 4) & 16383);
                    size -= valueSize;
                }
                DEV9.DEV9_LOG(" * * SMAP DMA WRITE END:   wr_ptr=" + DEV9Header.dev9Ru32((int)DEV9Header.SMAP_R_TXFIFO_WR_PTR).ToString() + ", rd_ptr=" + DEV9Header.dev9.txfifo_rd_ptr.ToString());

                DEV9Header.dev9Wu16((int)DEV9Header.SMAP_R_TXFIFO_CTRL, (UInt16)(DEV9Header.dev9Ru16((int)DEV9Header.SMAP_R_TXFIFO_CTRL) & ~DEV9Header.SMAP_TXFIFO_DMAEN));

            }
        }
        public static void smap_async(UInt32 cycles)
        {
            if (fireIntR)
            {
                fireIntR = false;
                //Is this used to signal each individual packet, or just when there are packets in the RX fifo?
                DEV9._DEV9irq(DEV9Header.SMAP_INTR_RXEND, 0); //Make the call to _DEV9irq in a thread safe way
            }
        }
    }
}
