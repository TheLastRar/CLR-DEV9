using System;

namespace CLRDEV9.DEV9.SMAP
{
    partial class SMAP_State
    {
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
                    Log_Verb("SMAP : Generic TX 16bit read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                Log_Verb("SMAP : Generic TX 16bit read " + rv.ToString("X"));
                return (UInt16)rv;
            }
            else if (addr >= DEV9Header.SMAP_BD_RX_BASE && addr < (DEV9Header.SMAP_BD_RX_BASE + DEV9Header.SMAP_BD_SIZE))
            {
                int rv = dev9.Dev9Ru16((int)addr);
                if (dev9.bdSwap != 0)
                {
                    Log_Verb("SMAP : Generic RX 16bit read " + ((rv << 8) | (rv >> 8)).ToString("X"));
                    return (UInt16)((rv << 8) | (rv >> 8));
                }
                Log_Verb("SMAP : Generic RX 16bit read " + rv.ToString("X"));
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
                    Log_Verb("SMAP_R_TXFIFO_FRAME_CNT 32bit read" + dev9.Dev9Ru32((int)addr).ToString("X"));
                    return dev9.Dev9Ru32((int)addr);
                case DEV9Header.SMAP_R_RXFIFO_FRAME_CNT:
                    Log_Verb("SMAP_R_RXFIFO_FRAME_CNT read 32\n" + dev9.Dev9Ru32((int)addr).ToString("X"));
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

        public void SMAP_ReadDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if ((dev9.Dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & DEV9Header.SMAP_RXFIFO_DMAEN) != 0)
            {
                int pMemAddr = 0;
                int valueSize = 4;
                dev9.Dev9Wu32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR, dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR) & 16383);
                size >>= 1;
                Log_Verb("DMA READ START: rd_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + dev9.rxFifoWrPtr.ToString());
                Log_Info("rSMAP");

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
                Log_Verb("DMA READ END: rd_ptr=" + dev9.Dev9Ru32((int)DEV9Header.SMAP_R_RXFIFO_RD_PTR).ToString() + ", wr_ptr=" + dev9.rxFifoWrPtr.ToString());

                dev9.Dev9Wu16((int)DEV9Header.SMAP_R_RXFIFO_CTRL, (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SMAP_R_RXFIFO_CTRL) & ~DEV9Header.SMAP_RXFIFO_DMAEN));
            }
        }
    }
}
