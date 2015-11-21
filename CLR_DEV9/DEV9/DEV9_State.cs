using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CLRDEV9.DEV9.FLASH;
using CLRDEV9.DEV9.SMAP;

namespace CLRDEV9.DEV9
{
    partial class DEV9_State
    {
        FLASH_State flash = null;
        SMAP_State smap = null;
        //Init
        public DEV9_State()
        {
            flash = new FLASH_State();
            smap = new SMAP_State(this);

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
            //Init SMAP
            int rxbi;

            for (rxbi = 0; rxbi < (DEV9Header.SMAP_BD_SIZE / 8); rxbi++)
            {
                SMAP.Data.SMAP_bd pbd;
                pbd = new SMAP.Data.SMAP_bd(dev9R, (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + (SMAP.Data.SMAP_bd.GetSize() * rxbi)));

                pbd.ctrl_stat = (UInt16)DEV9Header.SMAP_BD_RX_EMPTY;
                pbd.length = 0;
            }
        }
        //Open
        public int Open()
        {
            //flash.Open()
            return smap.Open() | 0;
        }
        //Close
        public void Close()
        {
            //flash.Close()
            smap.Close();
        }
        
        public int _DEV9irqHandler()
        {
            //Console.Error.WriteLine("_DEV9irqHandler");
            CLR_DEV9.DEV9_LOG("_DEV9irqHandler " + irqcause.ToString("X") + ", " + dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("x"));
            if ((irqcause & dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK)) != 0)
                return 1;
            return 0;
        }

        public void DEV9irq(int cause, int cycles)
        {
            //Console.Error.WriteLine("_DEV9irq");
            CLR_DEV9.DEV9_LOG("_DEV9irq " + cause.ToString("X") + ", " + dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("X"));

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
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        return ata_read<1>(addr);
                //#else
                return 0;
                //#endif
            }
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
                    CLR_DEV9.DEV9_LOG("SPD_R_PIO_DATA read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.DEV9_R_REV:
                    hard = 0x32; // expansion bay
                    break;

                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        return (byte)flash.FLASHread32(addr, 1);
                    }

                    hard = dev9Ru8((int)addr);
                    CLR_DEV9.DEV9_LOG("*Unknown 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
                    return hard;
            }

            //DEV9_LOG("DEV9read8");
            CLR_DEV9.DEV9_LOG("*Known 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
            return hard;
        }
        
        public ushort DEV9read16(uint addr)
        {
            //DEV9_LOG("DEV9read16");
            UInt16 hard;
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        return ata_read<2>(addr);
                //#else
                return 0;
                //#endif
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
                    CLR_DEV9.DEV9_LOG("SPD_R_INTR_STAT read " + irqcause.ToString("X"));
                    return (UInt16)irqcause;

                case DEV9Header.DEV9_R_REV:
                    hard = 0x0030; // expansion bay
                    //DEV9_LOG("DEV9_R_REV 16bit read " + hard.ToString("X"));
                    break;

                case DEV9Header.SPD_R_REV_1:
                    hard = 0x0011;
                    //DEV9_LOG("STD_R_REV_1 16bit read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.SPD_R_REV_3:
                    // bit 0: smap
                    // bit 1: hdd
                    // bit 5: flash
                    hard = 0;
                    /*if (config.hddEnable) {
                        hard|= 0x2;
                    }*/
                    if (DEV9Header.config.ethEnable != 0)
                    {
                        hard |= 0x1;
                    }
                    hard |= 0x20;//flash
                    break;

                case DEV9Header.SPD_R_0e:
                    hard = 0x0002;
                    break;
                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        //DEV9_LOG("DEV9read16(FLASH)");
                        return (UInt16)flash.FLASHread32(addr, 2);
                    }
                    // DEV9_LOG("DEV9read16");
                    hard = dev9Ru16((int)addr);
                    CLR_DEV9.DEV9_LOG("*Unknown 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }

            //DEV9_LOG("DEV9read16");
            CLR_DEV9.DEV9_LOG("*Known 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            return hard;
        }
        
        public uint DEV9read32(uint addr)
        {
            //DEV9_LOG("DEV9read32");
            UInt32 hard;
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        return ata_read<4>(addr);
                //#else
                return 0;
                //#endif
            }
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
                        return (UInt32)flash.FLASHread32(addr, 4);
                    }

                    hard = dev9Ru32((int)addr);
                    CLR_DEV9.DEV9_LOG("*Unknown 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }

            //PluginLog.LogWriteLine("*Known 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            //return hard;
        }
        
        public void DEV9write8(uint addr, byte value)
        {
            //Console.Error.WriteLine("DEV9write8");
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        ata_write<1>(addr,value);
                //#endif
                return;
            }
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                smap.smap_write8(addr, value);
                return;
            }
            switch (addr)
            {
                case 0x10000020:
                    irqcause = 0xff;
                    break;
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
                    }

                    return;

                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        flash.FLASHwrite32(addr, (UInt32)value, 1);
                        return;
                    }

                    dev9Wu8((int)addr, value);
                    CLR_DEV9.DEV9_LOG("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            dev9Wu8((int)addr, value);
            CLR_DEV9.DEV9_LOG("*Known 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9write16(uint addr, ushort value)
        {
            //Console.Error.WriteLine("DEV9write16");
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //#ifdef ENABLE_ATA
                //        ata_write<2>(addr,value);
                //#endif
                //Console.Error.WriteLine("DEV9write16 ATA");
                return;
            }
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //Console.Error.WriteLine("DEV9write16 SMAP");
                smap.smap_write16(addr, value);
                return;
            }
            switch (addr)
            {
                case DEV9Header.SPD_R_INTR_MASK:
                    if ((dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) != value) && (((dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) | value) & irqcause) != 0))
                    {
                        CLR_DEV9.DEV9_LOG("SPD_R_INTR_MASK16=0x" + value.ToString("X") + " , checking for masked/unmasked interrupts");
                        DEV9Header.DEV9irq(1);
                    }
                    break;

                default:

                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        Console.Error.WriteLine("DEV9write16 flash");
                        flash.FLASHwrite32(addr, (UInt32)value, 2);
                        return;
                    }
                    dev9Wu16((int)addr, value);
                    CLR_DEV9.DEV9_LOG("*Unknown 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            dev9Wu16((int)addr, value);
            CLR_DEV9.DEV9_LOG("*Known 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9write32(uint addr, uint value)
        {
            //Console.Error.WriteLine("DEV9write32");
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
                    Console.Error.WriteLine("SPD_R_INTR_MASK	, WTFH ?\n");
                    break;
                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        flash.FLASHwrite32(addr, (UInt32)value, 4);
                        return;
                    }

                    dev9Wu32((int)addr, value);
                    CLR_DEV9.DEV9_LOG("*Unknown 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            dev9Wu32((int)addr, value);
            CLR_DEV9.DEV9_LOG("*Known 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }

        public void DEV9readDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            smap.smap_readDMA8Mem(pMem, size);
            //#ifdef ENABLE_ATA
            //    ata_readDMA8Mem(pMem,size);
            //#endif
        }

        public void DEV9writeDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            smap.smap_writeDMA8Mem(pMem, size);
            //#ifdef ENABLE_ATA
            //    ata_writeDMA8Mem(pMem,size);
            //#endif
        }

        public void DEV9async(uint cycles)
        {
            smap.smap_async(cycles);
        }
    }
}
