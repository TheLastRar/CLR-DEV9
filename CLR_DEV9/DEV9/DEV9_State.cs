using System;
using CLRDEV9.DEV9.FLASH;
using CLRDEV9.DEV9.SMAP;
using CLRDEV9.DEV9.ATA;
using System.Diagnostics;

namespace CLRDEV9.DEV9
{
    partial class DEV9_State
    {
        FLASH_State flash = null;
        SMAP_State smap = null;
        ATA_State ata = null;
        //Init
        public DEV9_State()
        {
            flash = new FLASH_State();
            smap = new SMAP_State(this);
            ata = new ATA_State(this);

            eeprom = new ushort[inital_eeprom.Length / 2];
            for (int i = 0; i < inital_eeprom.Length; i += 2)
            {
                //this is confusing
                byte[] byte1 = BitConverter.GetBytes(inital_eeprom[i]);
                byte[] byte2 = BitConverter.GetBytes(inital_eeprom[i + 1]);
                byte[] shortBytes = new byte[2];
                Utils.memcpy(ref shortBytes, 0, byte1, 0, 1);
                Utils.memcpy(ref shortBytes, 1, byte2, 0, 1);
                eeprom[i / 2] = BitConverter.ToUInt16(shortBytes, 0);
            }

        }
        //Open
        public int Open(string hddPath)
        {
            //flash.Open()
            int ret = 0;
            if (DEV9Header.config.HddEnable)
                ret |= ata.Open(hddPath);
            if (DEV9Header.config.EthEnable)
                ret |= smap.Open();

            return ret;
        }
        //Close
        public void Close()
        {
            //flash.Close()
            ata.Close();
            smap.Close();
        }

        public int _DEV9irqHandler()
        {
            Log_Verb("_DEV9irqHandler " + irqcause.ToString("X") + ", " + dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("x"));

            //Pass IRQ to other handlers
            int ret = 0;
            //Check if should return
            if ((irqcause & dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK)) != 0)
            {
                ret = 1;
            }
            ata._ATAirqHandler();
            return ret;
        }

        public void DEV9irq(int cause, int cycles)
        {
            Log_Verb("_DEV9irq " + cause.ToString("X") + ", " + dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("X"));

            irqcause |= cause;

            if (cycles < 1)
                DEV9Header.DEV9irq(1);
            else
                DEV9Header.DEV9irq(cycles);
        }

        public byte DEV9read8(uint addr)
        {
            //DEV9_LOG("DEV9read8");
            byte hard;
            //if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            //{
            //    //#ifdef ENABLE_ATA
            //    //        return ata_read<1>(addr);
            //    //#else
            //    return 0; //ATA only has 16bit regs
            //    //#endif
            //}
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //DEV9_LOG("DEV9read8(SMAP)");
                byte ret = smap.smap_read8(addr);
                return ret;
            }

            switch (addr)
            {
                case DEV9Header.SPD_R_PIO_DATA:

                    /*if(dev9.eeprom_dir!=1)
                    {
                        hard=0;
                        break;
                    }*/
                    //DEV9_LOG("DEV9read8");

                    if (eeprom_state == DEV9Header.EEPROM_TDATA)
                    {
                        if (eeprom_command == 2) //read
                        {
                            if (eeprom_bit == 0xFF)
                                hard = 0;
                            else
                                hard = (byte)(((eeprom[eeprom_address] << eeprom_bit) & 0x8000) >> 11);
                            eeprom_bit++;
                            if (eeprom_bit == 16)
                            {
                                eeprom_address++;
                                eeprom_bit = 0;
                            }
                        }
                        else hard = 0;
                    }
                    else hard = 0;
                    Log_Verb("SPD_R_PIO_DATA read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.DEV9_R_REV:
                    hard = 0x32; // expansion bay
                    return hard;
                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        return (byte)flash.FLASHread(addr, 1);
                    }

                    hard = dev9Ru8((int)addr);
                    Log_Error("*Unknown 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
                    return hard;
            }

            //DEV9_LOG("DEV9read8");
            //CLR_DEV9.DEV9_LOG("*Known 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
            //return hard;
        }

        public ushort DEV9read16(uint addr)
        {
            //DEV9_LOG("DEV9read16");
            UInt16 hard;
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //ata
                return ata.ATAread16(addr);
            }
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //DEV9_LOG("DEV9read16(SMAP)");
                return smap.smap_read16(addr);
            }

            switch (addr)
            {
                case DEV9Header.SPD_R_INTR_STAT:
                    //DEV9_LOG("DEV9read16");
                    Log_Verb("SPD_R_INTR_STAT read " + irqcause.ToString("X"));
                    return (UInt16)irqcause;

                case DEV9Header.SPD_R_INTR_MASK:
                    return dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK);

                case DEV9Header.DEV9_R_REV:
                    //hard = 0x0030; // expansion bay
                    hard = 0x0032; // expansion bay
                    //DEV9_LOG("DEV9_R_REV 16bit read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.SPD_R_REV_1:
                    hard = 0x0011;
                    //DEV9_LOG("STD_R_REV_1 16bit read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.SPD_R_REV_3:
                    // bit 0: smap
                    // bit 1: hdd
                    // bit 5: flash
                    hard = 0;
                    if (DEV9Header.config.HddEnable)
                    {
                        hard|= 0x2;
                    }
                    if (DEV9Header.config.EthEnable)
                    {
                        hard |= 0x1;
                    }
                    hard |= 0x20;//flash
                    return hard;

                case DEV9Header.SPD_R_0e:
                    hard = 0x0002;
                    return hard;
                case DEV9Header.SPD_R_38:
                    return dev9Ru16((int)DEV9Header.SPD_R_38);
                case DEV9Header.SPD_R_IF_CTRL:
                    return dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL);
                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        //DEV9_LOG("DEV9read16(FLASH)");
                        return (UInt16)flash.FLASHread(addr, 2);
                    }
                    // DEV9_LOG("DEV9read16");
                    hard = dev9Ru16((int)addr);
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }

            //DEV9_LOG("DEV9read16");
            //CLR_DEV9.DEV9_LOG("*Known 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            //return hard;
        }

        public uint DEV9read32(uint addr)
        {
            //DEV9_LOG("DEV9read32");
            UInt32 hard;
            //if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            //{
            //    //#ifdef ENABLE_ATA
            //    //        return ata_read<4>(addr);
            //    //#else
            //    return 0;
            //    //#endif
            //}
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                return smap.smap_read32(addr);
            }
            switch (addr)
            {

                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        return (UInt32)flash.FLASHread(addr, 4);
                    }

                    hard = dev9Ru32((int)addr);
                    Log_Error("*Unknown 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("X"));
                    return hard;
            }

            //PluginLog.LogWriteLine("*Known 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            //return hard;
        }

        public void DEV9write8(uint addr, byte value)
        {
            //Error.WriteLine("DEV9write8");
            //if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            //{
            //    //#ifdef ENABLE_ATA
            //    //        ata_write<1>(addr,value);
            //    //#endif
            //    return;
            //}
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                smap.smap_write8(addr, value);
                return;
            }
            switch (addr)
            {
                case 0x10000020: //irqcause?
                    irqcause = 0xff;
                    return;
                case DEV9Header.SPD_R_INTR_STAT:
                    //emu_printf("SPD_R_INTR_STAT	, WTFH ?\n");
                    irqcause = value;
                    return;
                case DEV9Header.SPD_R_INTR_MASK:
                    //emu_printf("SPD_R_INTR_MASK8	, WTFH ?\n");
                    break;

                case DEV9Header.SPD_R_PIO_DIR:
                    //DEV9.DEV9_LOG("SPD_R_PIO_DIR 8bit write " + value.ToString("X"));

                    if ((value & 0xc0) != 0xc0)
                        return;

                    if ((value & 0x30) == 0x20)
                    {
                        eeprom_state = 0;
                    }
                    eeprom_dir = (byte)((value >> 4) & 3);

                    return;

                case DEV9Header.SPD_R_PIO_DATA:
                    //DEV9.DEV9_LOG("SPD_R_PIO_DATA 8bit write " + value.ToString("X"));

                    if ((value & 0xc0) != 0xc0)
                        return;

                    switch (eeprom_state)
                    {
                        case DEV9Header.EEPROM_READY:
                            eeprom_command = 0;
                            eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_OPCD0:
                            eeprom_command = (byte)((value >> 4) & 2);
                            eeprom_state++;
                            eeprom_bit = 0xFF;
                            break;
                        case DEV9Header.EEPROM_OPCD1:
                            eeprom_command |= (byte)((value >> 5) & 1);
                            eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_ADDR0:
                        case DEV9Header.EEPROM_ADDR1:
                        case DEV9Header.EEPROM_ADDR2:
                        case DEV9Header.EEPROM_ADDR3:
                        case DEV9Header.EEPROM_ADDR4:
                        case DEV9Header.EEPROM_ADDR5:
                            eeprom_address = (byte)
                                ((eeprom_address & (63 ^ (1 << (eeprom_state - DEV9Header.EEPROM_ADDR0)))) |
                                ((value >> (eeprom_state - DEV9Header.EEPROM_ADDR0)) & (0x20 >> (eeprom_state - DEV9Header.EEPROM_ADDR0))));
                            eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_TDATA:
                            {
                                if (eeprom_command == 1) //write
                                {
                                    eeprom[eeprom_address] = (byte)
                                        ((eeprom[eeprom_address] & (63 ^ (1 << eeprom_bit))) |
                                        ((value >> eeprom_bit) & (0x8000 >> eeprom_bit)));
                                    eeprom_bit++;
                                    if (eeprom_bit == 16)
                                    {
                                        eeprom_address++;
                                        eeprom_bit = 0;
                                    }
                                }
                            }
                            break;
                        default:
                            Log_Error("Unkown EEPROM COMMAND");
                            break;
                    }

                    return;

                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        flash.FLASHwrite(addr, (UInt32)value, 1);
                        return;
                    }

                    dev9Wu8((int)addr, value);
                    Log_Error("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            Log_Error("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
            dev9Wu8((int)addr, value);
            //CLR_DEV9.DEV9_LOG("*Known 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9write16(uint addr, ushort value)
        {
            //Error.WriteLine("DEV9write16");
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                ata.ATAwrite16(addr, value);
                //Error.WriteLine("DEV9write16 ATA");
                return;
            }
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //Error.WriteLine("DEV9write16 SMAP");
                smap.smap_write16(addr, value);
                return;
            }
            switch (addr)
            {
                case DEV9Header.SPD_R_DMA_CTRL: //??
                    Log_Verb("SPD_R_DMA_CTRL=0x" + value.ToString("X"));
                    dev9Wu16((int)DEV9Header.SPD_R_DMA_CTRL, value);
                    return;
                case DEV9Header.SPD_R_INTR_MASK:
                    if ((dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) != value) && (((dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) | value) & irqcause) != 0))
                    {
                        Log_Verb("SPD_R_INTR_MASK16=0x" + value.ToString("X") + " , checking for masked/unmasked interrupts");
                        DEV9Header.DEV9irq(1);
                    }
                    dev9Wu16((int)DEV9Header.SPD_R_INTR_MASK, value);
                    return;
                case DEV9Header.SPD_R_XFR_CTRL: //??
                    Log_Verb("SPD_R_XFR_CTRL=0x" + value.ToString("X"));
                    dev9Wu16((int)DEV9Header.SPD_R_XFR_CTRL, value);
                    break;
                case DEV9Header.SPD_R_38:
                    Log_Verb("SPD_R_38=0x" + value.ToString("X"));
                    dev9Wu16((int)DEV9Header.SPD_R_38, value);
                    break;
                case DEV9Header.SPD_R_IF_CTRL:
                    Log_Verb("SPD_R_IF_CTRL=0x" + value.ToString("X"));
                    dev9Wu16((int)DEV9Header.SPD_R_IF_CTRL, value);
                    break;
                case DEV9Header.SPD_R_PIO_MODE: //??
                    Log_Verb("SPD_R_PIO_MODE=0x" + value.ToString("X"));
                    dev9Wu16((int)DEV9Header.SPD_R_PIO_MODE, value);
                    break;
                default:

                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        Log_Verb("DEV9write16 flash");
                        flash.FLASHwrite(addr, (UInt32)value, 2);
                        return;
                    }
                    dev9Wu16((int)addr, value);
                    Log_Error("*Unknown 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            //dev9Wu16((int)addr, value);
            //CLR_DEV9.DEV9_LOG("*Known 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9write32(uint addr, uint value)
        {
            //Error.WriteLine("DEV9write32");
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        ata_write<4>(addr,value);
                //#endif
                return;
            }
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                smap.smap_write32(addr, value);
                return;
            }
            switch (addr)
            {
                case DEV9Header.SPD_R_INTR_MASK:
                    Log_Error("SPD_R_INTR_MASK	, WTFH ?\n");
                    break;
                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        flash.FLASHwrite(addr, (UInt32)value, 4);
                        return;
                    }

                    dev9Wu32((int)addr, value);
                    Log_Error("*Unknown 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            dev9Wu32((int)addr, value);
            Log_Error("*Known 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9readDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            Log_Verb("*DEV9readDMA8Mem: size " + size.ToString("X"));
            smap.smap_readDMA8Mem(pMem, size);
            Log_Info("rDMA");
            //#ifdef ENABLE_ATA
            ata.ATAreadDMA8Mem(pMem, size);
            //#endif
        }

        public void DEV9writeDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            Log_Verb("*DEV9writeDMA8Mem: size " + size.ToString("X"));
            smap.smap_writeDMA8Mem(pMem, size);
            Log_Info("wDMA");
            //#ifdef ENABLE_ATA
            ata.ATAwriteDMA8Mem(pMem, size);
            //#endif
        }

        public void DEV9async(uint cycles)
        {
            smap.smap_async(cycles);
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Dev9, "DEV9", str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Dev9, "DEV9", str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Dev9, "DEV9", str);
        }
    }
}
