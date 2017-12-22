using System;
using System.Diagnostics;

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

                //case DEV9Header.DEV9_R_REV:
                //    //hard = 0x0030; // expansion bay
                //    hard = 0x0032; // expansion bay
                //    Log_Verb("DEV9_R_REV 16bit read " + hard.ToString("X"));
                //    return hard;

                case SPEED_Header.SPD_R_REV_1:
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
                case SPEED_Header.SPD_R_38:
                    Log_Verb("SPD_R_38 16bit read " + reg38.ToString("X"));
                    UInt16 r38 = (UInt16)(reg38 & ~SPEED_Header.SPD_R_38_AVAIL_MASK);
                    if (dev9.ata.dmaReady)
                    {
                        r38 |= (UInt16)Math.Min(dev9.ata.nsectorLeft, SPEED_Header.SPD_R_38_AVAIL_MASK);
                    }
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
                    dev9.isDMAforSMAP = (value & 0x1) == 1;
                    if ((regDMACtrl & 0x1) == 1)
                        Log_Verb("SPD_R_DMA_CTRL DMA For SMAP");
                    else
                        Log_Verb("SPD_R_DMA_CTRL DMA For ATA");
                    //ORed with 0x06
                    regDMACtrl = value;
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
                    break;
                case SPEED_Header.SPD_R_38:  //ATA only?
                    //Always get 3 written?
                    Log_Verb("SPD_R_38 16bit write " + value.ToString("X"));
                    if (value != 3)
                    {
                        Log_Error("SPD_R_38 16bit write " + value.ToString("X") + " Which != 3!!!");
                    }
                    reg38 = value;
                    break;
                case SPEED_Header.SPD_R_IF_CTRL: //ATA only?
                    Log_Verb("SPD_R_IF_CTRL 16bit write " + value.ToString("X"));
                    regIFCtrl = value;
                    #region regIFCtrl
                    if ((regIFCtrl & 0x1) != 0)
                        Log_Verb("IF_CTRL UDMA Enabled");
                    else
                        Log_Verb("IF_CTRL UDMA Disabled");
                    if ((regIFCtrl & 0x2) != 0)
                        Log_Verb("IF_CTRL DMA Is ATA Read");
                    else
                        Log_Verb("IF_CTRL DMA Is ATA Write");
                    if ((regIFCtrl & SPEED_Header.SPD_IF_DMA_ENABLE) != 0)
                        Log_Verb("IF_CTRL DMA Enabled");
                    else
                        Log_Verb("IF_CTRL DMA Disabled");
                    if ((regIFCtrl & 0x8) != 0)
                        Log_Verb("IF_CTRL Unkown Mode Bit A");
                    if ((regIFCtrl & 0x10) != 0)
                        Log_Error("IF_CTRL Unkown Bit 5");
                    if ((regIFCtrl & 0x20) != 0)
                        Log_Error("IF_CTRL Unkown Bit 6");
                    if ((regIFCtrl & 0x40) != 0)
                        Log_Verb("IF_CTRL Unkown Mode Bit B");
                    if ((regIFCtrl & SPEED_Header.SPD_IF_ATA_RESET) != 0)
                    {
                        Log_Info("IF_CTRL ATA Hard Reset");
                        dev9.ata.ATA_HardReset();
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
