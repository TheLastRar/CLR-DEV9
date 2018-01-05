using System;
using System.Diagnostics;
using System.IO;

namespace CLRDEV9.DEV9.SPEED
{
    partial class SPEED_State
    {
        DEV9_State dev9 = null;

        public SPEED_State(DEV9_State parDev9)
        {
            dev9 = parDev9;
        }

        public byte SPEED_Read8(uint addr)
        {
            switch (addr)
            {
                //case DEV9Header.SPD_R_PIO_DATA:

                //    /*if(dev9.eeprom_dir!=1)
                //    {
                //        hard=0;
                //        break;
                //    }*/
                //    //DEV9_LOG("DEV9read8");

                //    if (eepromState == DEV9Header.EEPROM_TDATA)
                //    {
                //        if (eepromCommand == 2) //read
                //        {
                //            if (eepromBit == 0xFF)
                //                hard = 0;
                //            else
                //                hard = (byte)(((eeprom[eepromAddress] << eepromBit) & 0x8000) >> 11);
                //            eepromBit++;
                //            if (eepromBit == 16)
                //            {
                //                eepromAddress++;
                //                eepromBit = 0;
                //            }
                //        }
                //        else hard = 0;
                //    }
                //    else hard = 0;
                //    Log_Verb("SPD_R_PIO_DATA 8bit read " + hard.ToString("X"));
                //    return hard;

                //case DEV9Header.DEV9_R_REV:
                //    hard = 0x32; // expansion bay
                //    Log_Verb("DEV9_R_REV 8bit read " + hard.ToString("X"));
                //    return hard;
                default:
                    Log_Error("*Unknown 8bit read at address " + addr.ToString("X") + " value " + dev9.Dev9Ru8((int)addr).ToString("X"));
                    return dev9.Dev9Ru8((int)addr);
            }
        }

        public ushort SPEED_Read16(uint addr)
        {
            switch (addr)
            {
                case SPEED_Header.SPD_R_INTR_STAT:
                    Log_Verb("SPD_R_INTR_STAT 16bit read " + regIntStat.ToString("X"));
                    return regIntStat;

                case SPEED_Header.SPD_R_INTR_MASK:
                    Log_Verb("SPD_R_INTR_MASK 16bit read " + regIntrMask.ToString("X"));
                    return regIntrMask;

                case SPEED_Header.SPD_R_REV_1:
                    Log_Error("SPD_R_REV_1 16bit read " + 0.ToString("X"));
                    return 0;

                case SPEED_Header.SPD_R_REV_2:
                    Log_Verb("STD_R_REV_1 16bit read " + regRev1.ToString("X"));
                    return regRev1;

                case SPEED_Header.SPD_R_REV_3:
                    UInt16 rev3 = 0;
                    if (DEV9Header.config.HddEnable)
                    {
                        rev3 |= SPEED_Header.SPD_CAPS_ATA;
                    }
                    if (DEV9Header.config.EthEnable)
                    {
                        rev3 |= SPEED_Header.SPD_CAPS_SMAP;
                    }
                    rev3 |= SPEED_Header.SPD_CAPS_FLASH;
                    Log_Verb("SPD_R_REV_3 16bit read " + rev3.ToString("X"));
                    return rev3;
                case SPEED_Header.SPD_R_0e:
                    UInt16 r0e = 0x0002; //Have HDD inserted
                    Log_Verb("SPD_R_0e 16bit read " + r0e.ToString("X"));
                    return r0e;
                case SPEED_Header.SPD_R_XFR_CTRL:
                    Log_Verb("SPD_R_XFR_CTRL 16bit read " + regXFRCtrl.ToString("X"));
                    return regXFRCtrl;
                case SPEED_Header.SPD_R_DBUF_STAT:

                    if (ifRead) //Semi async
                    {
                        HDDWriteFIFO(); //Yes this is not a typo
                    }
                    else
                    {
                        HDDReadFIFO();
                    }

                    byte count = (byte)((bytesWriteFIFO - bytesReadFIFO) / 512);
                    UInt16 r38;
                    if (xfrWrite) //or ifRead?
                    {
                        r38 = (byte)(SPEED_Header.SPD_DBUF_AVAIL_MAX - count);
                        r38 |= (count == 0) ? SPEED_Header.SPD_DBUF_STAT_1 : (UInt16)0;
                        r38 |= (count > 0) ? SPEED_Header.SPD_DBUF_STAT_2 : (UInt16)0;
                    }
                    else
                    {
                        r38 = count;
                        r38 |= (count < SPEED_Header.SPD_DBUF_AVAIL_MAX) ? SPEED_Header.SPD_DBUF_STAT_1 : (UInt16)0;
                        r38 |= (count == 0) ? SPEED_Header.SPD_DBUF_STAT_2 : (UInt16)0;
                        //If overflow (HDD->SPEED), set both SPD_DBUF_STAT_2 & SPD_DBUF_STAT_FULL
                        //and overflow INTR set
                    }

                    if (count == SPEED_Header.SPD_DBUF_AVAIL_MAX)
                    {
                        r38 |= SPEED_Header.SPD_DBUF_STAT_FULL;
                    }

                    Log_Verb("SPD_R_38  16bit read " + r38.ToString("X"));

                    return r38;
                case SPEED_Header.SPD_R_IF_CTRL:
                    Log_Verb("SPD_R_IF_CTRL 16bit read " + regIFCtrl.ToString("X"));
                    return regIFCtrl;
                default:
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + dev9.Dev9Ru16((int)addr).ToString("x"));
                    return dev9.Dev9Ru16((int)addr);
            }
        }

        public uint SPEED_Read32(uint addr)
        {
            switch (addr)
            {
                default:
                    Log_Error("*Unknown 32bit read at address " + addr.ToString("x") + " value " + dev9.Dev9Ru32((int)addr).ToString("X"));
                    return dev9.Dev9Ru32((int)addr);
            }
        }

        public void SPEED_Write8(uint addr, byte value)
        {
            switch (addr)
            {
                case 0x10000020: //irqcause?
                    Log_Error("SPD_R_INTR_CAUSE 8bit , WTFH");
                    regIntStat = 0xff;
                    return;
                case SPEED_Header.SPD_R_INTR_STAT:
                    Log_Error("SPD_R_INTR_STAT , WTFH");
                    //emu_printf("SPD_R_INTR_STAT	, WTFH ?\n");
                    regIntStat = value;
                    return;
                case SPEED_Header.SPD_R_INTR_MASK:
                    Log_Error("SPD_R_INTR_MASK , WTFH");
                    //emu_printf("SPD_R_INTR_MASK8	, WTFH ?\n");
                    break;
                //case DEV9Header.SPD_R_PIO_DIR:
                //    Log_Verb("SPD_R_PIO_DIR 8bit write " + value.ToString("X"));
                //    if ((value & 0xc0) != 0xc0)
                //        return;

                //    if ((value & 0x30) == 0x20)
                //    {
                //        eepromState = 0;
                //    }
                //    eepromDir = (byte)((value >> 4) & 3);

                //    return;
                //    case DEV9Header.SPD_R_PIO_DATA:
                //        Log_Verb("SPD_R_PIO_DATA 8bit write " + value.ToString("X"));

                //        if ((value & 0xc0) != 0xc0)
                //            return;

                //        switch (eepromState)
                //        {
                //            case DEV9Header.EEPROM_READY:
                //                eepromCommand = 0;
                //                eepromState++;
                //                break;
                //            case DEV9Header.EEPROM_OPCD0:
                //                eepromCommand = (byte)((value >> 4) & 2);
                //                eepromState++;
                //                eepromBit = 0xFF;
                //                break;
                //            case DEV9Header.EEPROM_OPCD1:
                //                eepromCommand |= (byte)((value >> 5) & 1);
                //                eepromState++;
                //                break;
                //            case DEV9Header.EEPROM_ADDR0:
                //            case DEV9Header.EEPROM_ADDR1:
                //            case DEV9Header.EEPROM_ADDR2:
                //            case DEV9Header.EEPROM_ADDR3:
                //            case DEV9Header.EEPROM_ADDR4:
                //            case DEV9Header.EEPROM_ADDR5:
                //                eepromAddress = (byte)
                //                    ((eepromAddress & (63 ^ (1 << (eepromState - DEV9Header.EEPROM_ADDR0)))) |
                //                    ((value >> (eepromState - DEV9Header.EEPROM_ADDR0)) & (0x20 >> (eepromState - DEV9Header.EEPROM_ADDR0))));
                //                eepromState++;
                //                break;
                //            case DEV9Header.EEPROM_TDATA:
                //                {
                //                    if (eepromCommand == 1) //write
                //                    {
                //                        eeprom[eepromAddress] = (byte)
                //                            ((eeprom[eepromAddress] & (63 ^ (1 << eepromBit))) |
                //                            ((value >> eepromBit) & (0x8000 >> eepromBit)));
                //                        eepromBit++;
                //                        if (eepromBit == 16)
                //                        {
                //                            eepromAddress++;
                //                            eepromBit = 0;
                //                        }
                //                    }
                //                }
                //                break;
                //            default:
                //                Log_Error("Unkown EEPROM COMMAND");
                //                break;
                //        }

                //        return;

                default:
                    dev9.Dev9Wu8((int)addr, value);
                    Log_Error("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            dev9.Dev9Wu8((int)addr, value);
            Log_Error("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void SPEED_Write16(uint addr, ushort value)
        {
            switch (addr)
            {
                case SPEED_Header.SPD_R_DMA_CTRL:
                    Log_Verb("SPD_R_DMA_CTRL 16bit write " + value.ToString("X"));
                    regDMACtrl = value;

                    if (dmaSMAP)
                        Log_Verb("SPD_R_DMA_CTRL DMA For SMAP");
                    else
                        Log_Verb("SPD_R_DMA_CTRL DMA For ATA");

                    if ((value & SPEED_Header.SPD_DMA_FASTEST) != 0)
                        Log_Verb("SPD_R_DMA_CTRL Fastest DMA Mode");
                    else
                        Log_Verb("SPD_R_DMA_CTRL Slower DMA Mode");

                    if ((value & SPEED_Header.SPD_DMA_WIDE) != 0)
                        Log_Verb("SPD_R_DMA_CTRL Wide(32bit) DMA Mode Set");
                    else
                        Log_Verb("SPD_R_DMA_CTRL 16bit DMA Mode");

                    if ((value & SPEED_Header.SPD_DMA_PAUSE) != 0)
                        Log_Error("SPD_R_DMA_CTRL Pause DMA");

                    if ((value & 0b1111_1111_1110_0000) != 0)
                        Log_Error("SPD_R_DMA_CTRL Unkown value written" + value.ToString("X"));

                    return;
                case SPEED_Header.SPD_R_INTR_MASK:
                    Log_Verb("SPD_R_INTR_MASK16 16bit write " + value.ToString("X") + " , checking for masked/unmasked interrupts");
                    if ((regIntrMask != value) && (((regIntrMask | value) & regIntStat) != 0))
                    {
                        Log_Verb("SPD_R_INTR_MASK16 firing unmasked interrupts");
                        DEV9Header.DEV9irq(1);
                    }
                    regIntrMask = value;
                    return;
                case SPEED_Header.SPD_R_XFR_CTRL:
                    Log_Verb("SPD_R_XFR_CTRL 16bit write " + value.ToString("X"));

                    regXFRCtrl = value;

                    if (xfrWrite)
                        Log_Verb("SPD_R_XFR_CTRL Set Write");
                    else
                        Log_Verb("SPD_R_XFR_CTRL Set Read");

                    if ((value & (1 << 1)) != 0)
                        Log_Verb("SPD_R_XFR_CTRL Unkown Bit 1");

                    if ((value & (1 << 2)) != 0)
                        Log_Verb("SPD_R_XFR_CTRL Unkown Bit 2");

                    if (xfrDMAEN)
                        Log_Verb("SPD_R_XFR_CTRL For DMA Enabled");
                    else
                        Log_Verb("SPD_R_XFR_CTRL For DMA Disabled");

                    if ((value & 0b1111_1111_0111_1000) != 0)
                        Log_Error("SPD_R_DMA_CTRL Unkown value written" + value.ToString("X"));
                    
                    break;
                case SPEED_Header.SPD_R_DBUF_STAT:
                    Log_Verb("SPD_R_38 16bit write " + value.ToString("X"));

                    if ((value & SPEED_Header.SPD_DBUF_RESET_SOMETHING) != 0)
                        Log_Verb("SPD_R_DBUF_STAT Reset Something");

                    if ((value & SPEED_Header.SPD_DBUF_RESET_FIFO) != 0)
                    {
                        Log_Verb("SPD_R_XFR_CTRL Reset FIFO");
                        bytesWriteFIFO = 0;
                        bytesReadFIFO = 0;
                        xfrWrite = false; //??
                        ifRead = true; //??
                        FIFOIntr();
                    }

                    if (value != 3)
                        Log_Error("SPD_R_38 16bit write " + value.ToString("X") + " Which != 3!!!");

                    break;
                case SPEED_Header.SPD_R_IF_CTRL: //ATA only?
                    Log_Verb("SPD_R_IF_CTRL 16bit write " + value.ToString("X"));
                    regIFCtrl = value;
                    #region regIFCtrl
                    if ((regIFCtrl & SPEED_Header.SPD_IF_UDMA) != 0)
                        Log_Verb("IF_CTRL UDMA Enabled");
                    else
                        Log_Verb("IF_CTRL UDMA Disabled");
                    if (ifRead)
                        Log_Verb("IF_CTRL DMA Is ATA Read");
                    else
                        Log_Verb("IF_CTRL DMA Is ATA Write");
                    if (ifDMAEN)
                        Log_Verb("IF_CTRL ATA DMA Enabled");
                    else
                        Log_Verb("IF_CTRL ATA DMA Disabled");

                    if ((regIFCtrl & (1 << 3)) != 0)
                        Log_Verb("IF_CTRL Unkown Bit 3 Set");

                    if ((regIFCtrl & (1 << 4)) != 0)
                        Log_Error("IF_CTRL Unkown Bit 4 Set");
                    if ((regIFCtrl & (1 << 5)) != 0)
                        Log_Error("IF_CTRL Unkown Bit 5 Set");

                    if ((regIFCtrl & SPEED_Header.SPD_IF_HDD_RESET) == 0) //Maybe?????? (TEST)
                    {
                        Log_Info("IF_CTRL HDD Hard Reset");
                        dev9.ata.ATA_HardReset();
                    }
                    if ((regIFCtrl & SPEED_Header.SPD_IF_ATA_RESET) != 0)
                    {
                        Log_Info("IF_CTRL ATA Reset");
                        //0x62    0x0020
                        regIFCtrl = 0x001A;
                        //0x66    0x0001
                        regPIOMode = 0x24;
                        regMDMAMode = 0x45;
                        regUDMAMode = 0x83;
                        //0x74    0x0083
                        //0x76    0x4ABA (And consequently 0x78 = 0x4ABA.)
                    }

                    if ((regIFCtrl & 0xFF00) > 0)
                        Log_Error("IF_CTRL Unkown Bit(s)" + (regIFCtrl & 0xFF00).ToString("X"));
                    #endregion
                    break;
                case SPEED_Header.SPD_R_PIO_MODE: //ATA only? or includes EEPROM?
                    Log_Verb("SPD_R_PIO_MODE 16bit write " + value.ToString("X"));
                    regPIOMode = value;
                    switch (regPIOMode)
                    {
                        case 0x92:
                            Log_Info("SPD_R_PIO_MODE 0");
                            break;
                        case 0x72:
                            Log_Info("SPD_R_PIO_MODE 1");
                            break;
                        case 0x32:
                            Log_Info("SPD_R_PIO_MODE 2");
                            break;
                        case 0x24:
                            Log_Info("SPD_R_PIO_MODE 3");
                            break;
                        case 0x23:
                            Log_Info("SPD_R_PIO_MODE 4");
                            break;
                        default:
                            Log_Error("SPD_R_PIO_MODE UNKOWN MODE " + value.ToString("X"));
                            break;
                    }
                    break;
                case SPEED_Header.SPD_R_MWDMA_MODE: //ATA only?
                    Log_Verb("SPD_R_MDMA_MODE 16bit write " + value.ToString("X"));
                    regMDMAMode = value;
                    switch (regMDMAMode)
                    {
                        case 0xFF:
                            Log_Info("SPD_R_MDMA_MODE 0");
                            break;
                        case 0x45:
                            Log_Info("SPD_R_MDMA_MODE 1");
                            break;
                        case 0x24:
                            Log_Info("SPD_R_MDMA_MODE 2");
                            break;
                        default:
                            Log_Error("SPD_R_MDMA_MODE UNKOWN MODE");
                            break;
                    }
                    break;
                case SPEED_Header.SPD_R_UDMA_MODE: //ATA only?
                    Log_Verb("SPD_R_UDMA_MODE 16bit write " + value.ToString("X"));
                    regUDMAMode = value;
                    switch (regUDMAMode)
                    {
                        case 0xa7:
                            Log_Info("SPD_R_UDMA_MODE 0");
                            break;
                        case 0x85:
                            Log_Info("SPD_R_UDMA_MODE 1");
                            break;
                        case 0x63:
                            Log_Info("SPD_R_UDMA_MODE 2");
                            break;
                        case 0x62:
                            Log_Info("SPD_R_UDMA_MODE 3");
                            break;
                        case 0x61:
                            Log_Info("SPD_R_UDMA_MODE 4");
                            break;
                        default:
                            Log_Error("SPD_R_UDMA_MODE UNKOWN MODE");
                            break;
                    }
                    break;
                default:
                    dev9.Dev9Wu16((int)addr, value);
                    Log_Error("*Unknown 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
        }

        public void SPEED_Write32(uint addr, uint value)
        {
            switch (addr)
            {
                case SPEED_Header.SPD_R_INTR_MASK:
                    Log_Error("SPD_R_INTR_MASK	, WTFH ?\n");
                    break;
                default:
                    dev9.Dev9Wu32((int)addr, value);
                    Log_Error("*Unknown 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
        }

        //Fakes ATA FIFO
        private void HDDWriteFIFO()
        {
            if (dev9.ata.dmaReady & ifDMAEN)
            {
                int unread = (bytesWriteFIFO - bytesReadFIFO);
                int space = SPEED_Header.SPD_DBUF_AVAIL_MAX * 512 - unread;
                if (space < 0) throw new Exception();
                bytesWriteFIFO += Math.Min(dev9.ata.nsectorLeft * 512, space);
            }
            FIFOIntr();
        }
        private void HDDReadFIFO()
        {
            if (dev9.ata.dmaReady & ifDMAEN)
            {
                bytesReadFIFO = bytesWriteFIFO;
            }
            FIFOIntr();
        }
        private void IOPReadFIFO(int bytes)
        {
            bytesReadFIFO += bytes;
            if (bytesReadFIFO > bytesWriteFIFO)
                Log_Error("UNDERFLOW BY IOP");
            FIFOIntr();
        }
        private void IOPWriteFIFO(int bytes)
        {
            bytesWriteFIFO += bytes;
            if (bytesWriteFIFO - SPEED_Header.SPD_DBUF_AVAIL_MAX * 512 > bytesReadFIFO)
                Log_Error("OVERFLOW BY IOP");
            FIFOIntr();
        }
        private void FIFOIntr()
        {
            //FIFO Buffer Full/Empty
            int unread = (bytesWriteFIFO - bytesReadFIFO);

            if (unread == 0)
            {
                if ((regIntStat & SPEED_Header.SPD_INTR_ATA_FIFO_EMPTY) == 0)
                    dev9.DEV9irq(SPEED_Header.SPD_INTR_ATA_FIFO_EMPTY, 1);
            }
            if (unread == SPEED_Header.SPD_DBUF_AVAIL_MAX * 512)
            {
                //Log_Error("FIFO Full");
                //INTR Full?
            }
            //FIFO DATA
            //if (xfrWrite)
            //{
            //    if (unread > 0)
            //    {
            //        if ((regIntStat & SPEED_Header.SPD_INTR_ATA_FIFO_DATA) == 0)
            //            dev9.DEV9irq(SPEED_Header.SPD_INTR_ATA_FIFO_DATA, 1);
            //    }
            //    else
            //    {
            //        regIntStat &= unchecked((UInt16)~(SPEED_Header.SPD_INTR_ATA_FIFO_DATA));
            //    }
            //}
            //else
            //{ //Not sure this is correct
            //    if (unread < SPEED_Header.SPD_DBUF_AVAIL_MAX & dev9.ata.dmaReady) //??
            //    {
            //        if ((regIntStat & SPEED_Header.SPD_INTR_ATA_FIFO_DATA) == 0)
            //            dev9.DEV9irq(SPEED_Header.SPD_INTR_ATA_FIFO_DATA, 1);
            //    }
            //    else
            //    {
            //        regIntStat &= unchecked((UInt16)~(SPEED_Header.SPD_INTR_ATA_FIFO_DATA));
            //    }
            //}
        }

        public void SPEEDreadDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if (xfrDMAEN & !xfrWrite & !dmaSMAP)
            {
                Log_Info("rSPEED");
                HDDWriteFIFO();
                IOPReadFIFO(size);
                dev9.ata.ATAreadDMA8Mem(pMem, size);
            }
        }
        public void SPEEDwriteDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if (xfrDMAEN & xfrWrite & !dmaSMAP)
            {
                Log_Info("wSPEED");
                IOPWriteFIFO(size);
                dev9.ata.ATAwriteDMA8Mem(pMem, size);
                HDDReadFIFO();
            }
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.SPEED, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.SPEED, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.SPEED, str);
        }
    }
}
