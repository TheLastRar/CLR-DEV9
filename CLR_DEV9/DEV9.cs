using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace CLR_DEV9
{
    class DEV9
    {
        private static string LogFolderPath = "logs";
        private static PSE.CLR_PSE_PluginLog DEVLOG_shared;
        const bool DoLog = false;
        private static void LogInit()
        {
#pragma warning disable 0162
            if (DoLog)
            {
                if (LogFolderPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                {
                    //PluginLog.Open(LogFolderPath + "dev9clr.log");
                }
                else
                {
                    //PluginLog.Open(LogFolderPath + System.IO.Path.DirectorySeparatorChar + "dev9clr.log");
                }
                //PluginLog.SetWriteToFile(true);
                DEVLOG_shared = null; //PluginLog;
            }
#pragma warning restore 0162
        }
        public static void DEV9_LOG(string basestr)
        {
            if (DoLog)
            {
                Console.Error.WriteLine(basestr);
                //DEVLOG_shared.LogWriteLine(basestr);
            }
        }

        static byte[] eeprom = {
	        //0x6D, 0x76, 0x63, 0x61, 0x31, 0x30, 0x08, 0x01
	        0x76, 0x6D, 0x61, 0x63, 0x30, 0x31, 0x07, 0x02,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        [DllExport("DEV9init", CallingConvention = CallingConvention.StdCall)]
        public static Int32 Init()
        {
            LogInit();
            DEV9_LOG("DEV9init");
            DEV9Header.dev9 = new DEV9Header.dev9DataClass();
            DEV9_LOG("DEV9init2");

            DEV9_LOG("DEV9init3");

            flash.FLASHinit();

            DEV9Header.dev9.eeprom = new ushort[eeprom.Length / 2];
            for (int i = 0; i < eeprom.Length; i += 2)
            {
                byte[] byte1 = BitConverter.GetBytes(eeprom[i]);
                byte[] byte2 = BitConverter.GetBytes(eeprom[i + 1]);
                byte[] shortBytes = new byte[2];
                Utils.memcpy(ref shortBytes, 0, byte1, 0, 1);
                Utils.memcpy(ref shortBytes, 1, byte2, 0, 1);
                DEV9Header.dev9.eeprom[i / 2] = BitConverter.ToUInt16(shortBytes, 0);
            }
            int rxbi;

            for (rxbi = 0; rxbi < (DEV9Header.SMAP_BD_SIZE / 8); rxbi++)
            {
                DEV9Header.smap_bd pbd;
                pbd = new DEV9Header.smap_bd(DEV9Header.dev9.dev9R, (int)((DEV9Header.SMAP_BD_RX_BASE & 0xffff) + (DEV9Header.smap_bd.GetSize() * rxbi)));

                pbd.ctrl_stat = (UInt16)DEV9Header.SMAP_BD_RX_EMPTY;
                pbd.length = 0;
            }

            DEV9_LOG("DEV9init ok");

            return 0;
        }
        [DllExport("DEV9shutdown", CallingConvention = CallingConvention.StdCall)]
        public static void Shutdown()
        {
            DEV9_LOG("DEV9shutdown\n");
            //PluginLog.Close(); //fclose(dev9Log);
            DEVLOG_shared = null;
            irqHandle.Free();
            //Do dispose()?
        }
        [DllExport("DEV9open", CallingConvention = CallingConvention.StdCall)]
        public static Int32 DEV9open(IntPtr winHandle)
        {
            DEV9_LOG("DEV9open");
            Config.LoadConf();
            DEV9_LOG("open r+: " + DEV9Header.config.Hdd);
            DEV9Header.config.HddSize = 8 * 1024;

            return Win32._DEV9open();
        }
        [DllExport("DEV9close", CallingConvention = CallingConvention.StdCall)]
        public static void Close()
        {
            Win32._DEV9close();
        }

        static GCHandle irqHandle;
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static PSE.CLR_PSE_Callbacks.CLR_IRQHandler DEV9irqHandler()
        {
            // Pass our handler to pcsx2.
            if (irqHandle.IsAllocated)
            {
                irqHandle.Free(); //allow garbage collection
            }
            DEV9.DEV9_LOG("Get IRQ");
            PSE.CLR_PSE_Callbacks.CLR_IRQHandler fp = new PSE.CLR_PSE_Callbacks.CLR_IRQHandler(_DEV9irqHandler);
            irqHandle = GCHandle.Alloc(fp); //prevent GC
            return fp;
        }
        public static int _DEV9irqHandler()
        {
            //Console.Error.WriteLine("_DEV9irqHandler");
            DEV9_LOG("_DEV9irqHandler " + DEV9Header.dev9.irqcause.ToString("X") + ", " + DEV9Header.dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("x"));
            if ((DEV9Header.dev9.irqcause & DEV9Header.dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK)) != 0)
                return 1;
            return 0;
        }

        //Static method for smap
        public static void _DEV9irq(int cause, int cycles)
        {
            //Console.Error.WriteLine("_DEV9irq");
            DEV9_LOG("_DEV9irq " + cause.ToString("X") + ", " + DEV9Header.dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK).ToString("X"));

            DEV9Header.dev9.irqcause |= cause;

            if (cycles < 1)
                DEV9Header.DEV9irq(1);
            else
                DEV9Header.DEV9irq(cycles);
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static byte DEV9read8(uint addr)
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

                    if (DEV9Header.dev9.eeprom_state == DEV9Header.EEPROM_TDATA)
                    {
                        if (DEV9Header.dev9.eeprom_command == 2) //read
                        {
                            if (DEV9Header.dev9.eeprom_bit == 0xFF)
                                hard = 0;
                            else
                                hard = (byte)(((DEV9Header.dev9.eeprom[DEV9Header.dev9.eeprom_address] << DEV9Header.dev9.eeprom_bit) & 0x8000) >> 11);
                            DEV9Header.dev9.eeprom_bit++;
                            if (DEV9Header.dev9.eeprom_bit == 16)
                            {
                                DEV9Header.dev9.eeprom_address++;
                                DEV9Header.dev9.eeprom_bit = 0;
                            }
                        }
                        else hard = 0;
                    }
                    else hard = 0;
                    DEV9.DEV9_LOG("SPD_R_PIO_DATA read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.DEV9_R_REV:
                    hard = 0x32; // expansion bay
                    break;

                default:
                    if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
                    {
                        return (byte)flash.FLASHread32(addr, 1);
                    }

                    hard = DEV9Header.dev9Ru8((int)addr);
                    DEV9_LOG("*Unknown 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
                    return hard;
            }

            //DEV9_LOG("DEV9read8");
            DEV9_LOG("*Known 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
            return hard;
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static ushort DEV9read16(uint addr)
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
                    DEV9_LOG("SPD_R_INTR_STAT read " + DEV9Header.dev9.irqcause.ToString("X"));
                    return (UInt16)DEV9Header.dev9.irqcause;

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
                    hard = DEV9Header.dev9Ru16((int)addr);
                    DEV9_LOG("*Unknown 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }

            //DEV9_LOG("DEV9read16");
            DEV9_LOG("*Known 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            return hard;
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static uint DEV9read32(uint addr)
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

                    hard = DEV9Header.dev9Ru32((int)addr);
                    DEV9_LOG("*Unknown 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }

            //PluginLog.LogWriteLine("*Known 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
            //return hard;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write8(uint addr, byte value)
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
                    DEV9Header.dev9.irqcause = 0xff;
                    break;
                case DEV9Header.SPD_R_INTR_STAT:
                    //emu_printf("SPD_R_INTR_STAT	, WTFH ?\n");
                    DEV9Header.dev9.irqcause = value;
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
                        DEV9Header.dev9.eeprom_state = 0;
                    }
                    DEV9Header.dev9.eeprom_dir = (byte)((value >> 4) & 3);

                    return;

                case DEV9Header.SPD_R_PIO_DATA:
                    //DEV9.DEV9_LOG("SPD_R_PIO_DATA 8bit write " + value.ToString("X"));

                    if ((value & 0xc0) != 0xc0)
                        return;

                    switch (DEV9Header.dev9.eeprom_state)
                    {
                        case DEV9Header.EEPROM_READY:
                            DEV9Header.dev9.eeprom_command = 0;
                            DEV9Header.dev9.eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_OPCD0:
                            DEV9Header.dev9.eeprom_command = (byte)((value >> 4) & 2);
                            DEV9Header.dev9.eeprom_state++;
                            DEV9Header.dev9.eeprom_bit = 0xFF;
                            break;
                        case DEV9Header.EEPROM_OPCD1:
                            DEV9Header.dev9.eeprom_command |= (byte)((value >> 5) & 1);
                            DEV9Header.dev9.eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_ADDR0:
                        case DEV9Header.EEPROM_ADDR1:
                        case DEV9Header.EEPROM_ADDR2:
                        case DEV9Header.EEPROM_ADDR3:
                        case DEV9Header.EEPROM_ADDR4:
                        case DEV9Header.EEPROM_ADDR5:
                            DEV9Header.dev9.eeprom_address = (byte)
                                ((DEV9Header.dev9.eeprom_address & (63 ^ (1 << (DEV9Header.dev9.eeprom_state - DEV9Header.EEPROM_ADDR0)))) |
                                ((value >> (DEV9Header.dev9.eeprom_state - DEV9Header.EEPROM_ADDR0)) & (0x20 >> (DEV9Header.dev9.eeprom_state - DEV9Header.EEPROM_ADDR0))));
                            DEV9Header.dev9.eeprom_state++;
                            break;
                        case DEV9Header.EEPROM_TDATA:
                            {
                                if (DEV9Header.dev9.eeprom_command == 1) //write
                                {
                                    DEV9Header.dev9.eeprom[DEV9Header.dev9.eeprom_address] = (byte)
                                        ((DEV9Header.dev9.eeprom[DEV9Header.dev9.eeprom_address] & (63 ^ (1 << DEV9Header.dev9.eeprom_bit))) |
                                        ((value >> DEV9Header.dev9.eeprom_bit) & (0x8000 >> DEV9Header.dev9.eeprom_bit)));
                                    DEV9Header.dev9.eeprom_bit++;
                                    if (DEV9Header.dev9.eeprom_bit == 16)
                                    {
                                        DEV9Header.dev9.eeprom_address++;
                                        DEV9Header.dev9.eeprom_bit = 0;
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

                    DEV9Header.dev9Wu8((int)addr, value);
                    DEV9.DEV9_LOG("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            DEV9Header.dev9Wu8((int)addr, value);
            DEV9.DEV9_LOG("*Known 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write16(uint addr, ushort value)
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
                    if ((DEV9Header.dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) != value) && (((DEV9Header.dev9Ru16((int)DEV9Header.SPD_R_INTR_MASK) | value) & DEV9Header.dev9.irqcause) != 0))
                    {
                        DEV9.DEV9_LOG("SPD_R_INTR_MASK16=0x" + value.ToString("X") + " , checking for masked/unmasked interrupts");
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
                    DEV9Header.dev9Wu16((int)addr, value);
                    DEV9.DEV9_LOG("*Unknown 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            DEV9Header.dev9Wu16((int)addr, value);
            DEV9.DEV9_LOG("*Known 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write32(uint addr, uint value)
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

                    DEV9Header.dev9Wu32((int)addr, value);
                    DEV9_LOG("*Unknown 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
            DEV9Header.dev9Wu32((int)addr, value);
            DEV9_LOG("*Known 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        unsafe public static void DEV9readDMA8Mem(byte* memPointer, int size)
        {
            DEV9_LOG("*DEV9readDMA8Mem: size " + size.ToString("X"));
            System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.ReadWrite);
            Console.Error.WriteLine("rDMA");

            smap.smap_readDMA8Mem(pMem, size);
            //#ifdef ENABLE_ATA
            //    ata_readDMA8Mem(pMem,size);
            //#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        unsafe public static void DEV9writeDMA8Mem(byte* memPointer, int size)
        {
            DEV9_LOG("*DEV9writeDMA8Mem: size " + size.ToString("X"));
            System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.ReadWrite);
            Console.Error.WriteLine("wDMA");

            smap.smap_writeDMA8Mem(pMem, size);
            //#ifdef ENABLE_ATA
            //    ata_writeDMA8Mem(pMem,size);
            //#endif
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9irqCallback(PSE.CLR_PSE_Callbacks.CLR_CyclesCallback callback)
        {
            DEV9Header.DEV9irq = callback;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9async(uint cycles)
        {
            smap.smap_async(cycles);
        }

        [DllExport("DEV9test", CallingConvention = CallingConvention.StdCall)]
        public static int Test()
        {
            return 0;
        }
        [DllExport("DEV9setSettingsDir", CallingConvention = CallingConvention.StdCall)]
        public static void SetSettingsDir(string dir)
        {
            //throw new NotImplementedException();
        }
        [DllExport("DEV9setLogDir", CallingConvention = CallingConvention.StdCall)]
        public static void SetLogDir(string dir)
        {
            //throw new NotImplementedException();
        }
        //public static byte[] FreezeSave()
        //{
        //    throw new NotImplementedException();
        //}
        //public static int FreezeLoad(byte[] data)
        //{
        //    throw new NotImplementedException();
        //}
        //public static int FreezeSize()
        //{
        //    return 0;
        //}
    }
}
