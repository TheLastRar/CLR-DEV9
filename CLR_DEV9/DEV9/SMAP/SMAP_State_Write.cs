using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLRDEV9.DEV9.SMAP
{
    partial class SMAP_State
    {
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
    }
}
