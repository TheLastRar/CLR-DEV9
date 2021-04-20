#define ALLOW_WIDE_EEPROM

using CLRDEV9.DEV9.ATA;
using CLRDEV9.DEV9.FLASH;
using CLRDEV9.DEV9.SMAP;
using CLRDEV9.DEV9.SPEED;
using System;
using System.Diagnostics;

namespace CLRDEV9.DEV9
{
    partial class DEV9_State
    {
        public SPEED_State spd = null;
        public SMAP_State smap = null;
        public ATA_State ata = null;
        FLASH_State flash = null;

        //Init
        public DEV9_State()
        {
            spd = new SPEED_State(this);
            smap = new SMAP_State(this);
            ata = new ATA_State(this);
            flash = new FLASH_State();

            dev9R = new byte[0x10000];
            InitEEPROM();
        }
        protected void InitEEPROM()
        {
            eeprom = new ushort[initalEEPROM.Length / 2];
            for (int i = 0; i < initalEEPROM.Length; i += 2)
            {
                //this is confusing
                byte[] byte1 = BitConverter.GetBytes(initalEEPROM[i]);
                byte[] byte2 = BitConverter.GetBytes(initalEEPROM[i + 1]);
                byte[] shortBytes = new byte[2];
                Utils.memcpy(shortBytes, 0, byte1, 0, 1);
                Utils.memcpy(shortBytes, 1, byte2, 0, 1);
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
            Log_Verb("DEV9irqHandler " + spd.regIntStat.ToString("X") + ", " + spd.regIntrMask.ToString("x"));

            //Pass IRQ to other handlers
            int ret = 0;
            //Check if should return
            if ((spd.regIntStat & spd.regIntrMask) != 0)
            {
                ret = 1;
            }
            return ret;
        }

        public void DEV9irq(UInt16 cause, int cycles)
        {
            Log_Verb("DEV9irq " + cause.ToString("X") + ", " + spd.regIntrMask.ToString("X"));

            spd.regIntStat |= cause;

            if (cycles < 1)
                DEV9Header.DEV9irq(1);
            else
                DEV9Header.DEV9irq(cycles);
        }

        public byte DEV9read8(uint addr)
        {
            //Log_Verb("DEV9read8 @ " + addr.ToString("X"));
            byte hard;
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                Log_Error("ATA does not support 8bit reads");
                return 0;
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //DEV9_LOG("DEV9read8(SMAP)");
                byte ret = smap.SMAP_Read8(addr);
                return ret;
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                return (byte)flash.FLASHread(addr, 1);
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.SMAP_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                return spd.SPEED_Read8(addr);
            }
            //Other
            switch (addr)
            {
                case DEV9Header.SPD_R_PIO_DATA:

                    /*if(dev9.eeprom_dir!=1)
                    {
                        hard=0;
                        break;
                    }*/
                    //DEV9_LOG("DEV9read8");

                    if (eepromState == DEV9Header.EEPROM_TDATA)
                    {
                        if (eepromCommand == 2) //read
                        {
                            if (eepromBit == 0xFF)
                                hard = 0;
                            else
                                hard = (byte)(((eeprom[eepromAddress] << eepromBit) & 0x8000) >> 11);
                            eepromBit++;
                            if (eepromBit == 16)
                            {
                                eepromAddress++;
                                eepromBit = 0;
                            }
                        }
                        else
                            hard = 0;
                    }
                    else
                        hard = 0;

                    Log_Verb("SPD_R_PIO_DATA 8bit read " + hard.ToString("X"));
                    return hard;

                case DEV9Header.DEV9_R_REV:
                    hard = 0x32; // expansion bay
                    Log_Verb("DEV9_R_REV 8bit read " + hard.ToString("X"));
                    return hard;
                default:
                    hard = Dev9Ru8((int)addr);
                    Log_Error("*Unknown 8bit read at address " + addr.ToString("X") + " value " + hard.ToString("X"));
                    return hard;
            }
        }

        public ushort DEV9_Read16(uint addr)
        {
            //Log_Verb("DEV9read16 @ " + addr.ToString("X"));
            UInt16 hard;
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                //ata
                return ata.ATA_Read16(addr);
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                //DEV9_LOG("DEV9read16(SMAP)");
                return smap.SMAP_Read16(addr);
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                //DEV9_LOG("DEV9read16(FLASH)");
                return (UInt16)flash.FLASHread(addr, 2);
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.SMAP_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                return spd.SPEED_Read16(addr);
            }
            //OTHER
            switch (addr)
            {
#if ALLOW_WIDE_EEPROM
                case DEV9Header.SPD_R_PIO_DATA:

                    /*if(dev9.eeprom_dir!=1)
                    {
                        hard=0;
                        break;
                    }*/
                    //DEV9_LOG("DEV9read8");

                    if (eepromState == DEV9Header.EEPROM_TDATA)
                    {
                        if (eepromCommand == 2) //read
                        {
                            if (eepromBit == 0xFF)
                                hard = 0;
                            else
                                hard = (UInt16)(((eeprom[eepromAddress] << eepromBit) & 0x8000) >> 11);
                            eepromBit++;
                            if (eepromBit == 16)
                            {
                                eepromAddress++;
                                eepromBit = 0;
                            }
                        }
                        else hard = 0;
                    }
                    else
                        hard = 0;

                    Log_Verb("SPD_R_PIO_DATA 16bit read " + hard.ToString("X"));
                    return hard;
#endif

                case DEV9Header.DEV9_R_REV:
                    //hard = 0x0030; // expansion bay
                    hard = 0x0032; // expansion bay
                    Log_Verb("DEV9_R_REV 16bit read " + hard.ToString("X"));
                    return hard;
                default:
                    // DEV9_LOG("DEV9read16");
                    hard = Dev9Ru16((int)addr);
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + hard.ToString("x"));
                    return hard;
            }
        }

        public uint DEV9_Read32(uint addr)
        {
            //Log_Verb("DEV9read32 @ " + addr.ToString("X"));
            UInt32 hard;
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                Log_Error("ATA does not support 32bit reads");
                return 0;
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                return smap.SMAP_Read32(addr);
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                return (UInt32)flash.FLASHread(addr, 4);
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.SMAP_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                return spd.SPEED_Read32(addr);
            }
            //OTHER
            switch (addr)
            {
                default:
                    hard = Dev9Ru32((int)addr);
                    Log_Error("*Unknown 32bit read at address " + addr.ToString("x") + " value " + hard.ToString("X"));
                    return hard;
            }
        }

        public void DEV9_Write8(uint addr, byte value)
        {
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                Log_Error("ATA does not support 8bit writes");
                return;
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                smap.SMAP_Write8(addr, value);
                return;
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                flash.FLASHwrite(addr, (UInt32)value, 1);
                return;
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.SMAP_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                spd.SPEED_Write8(addr, value);
                return;
            }
            //OTHER
            switch (addr)
            {
                case DEV9Header.SPD_R_PIO_DIR:
                    Log_Verb("SPD_R_PIO_DIR 8bit write " + value.ToString("X"));
                    if ((value & 0xc0) != 0xc0)
                        return;

                    if ((value & 0x30) == 0x20)
                    {
                        eepromState = 0;
                    }
                    eepromDir = (byte)((value >> 4) & 3);

                    return;

                case DEV9Header.SPD_R_PIO_DATA:
                    Log_Verb("SPD_R_PIO_DATA 8bit write " + value.ToString("X"));

                    if ((value & 0xc0) != 0xc0)
                        return;

                    switch (eepromState)
                    {
                        case DEV9Header.EEPROM_READY:
                            eepromCommand = 0;
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_OPCD0:
                            eepromCommand = (byte)((value >> 4) & 2);
                            eepromState++;
                            eepromBit = 0xFF;
                            break;
                        case DEV9Header.EEPROM_OPCD1:
                            eepromCommand |= (byte)((value >> 5) & 1);
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_ADDR0:
                        case DEV9Header.EEPROM_ADDR1:
                        case DEV9Header.EEPROM_ADDR2:
                        case DEV9Header.EEPROM_ADDR3:
                        case DEV9Header.EEPROM_ADDR4:
                        case DEV9Header.EEPROM_ADDR5:
                            eepromAddress = (byte)
                                ((eepromAddress & (63 ^ (1 << (eepromState - DEV9Header.EEPROM_ADDR0)))) |
                                ((value >> (eepromState - DEV9Header.EEPROM_ADDR0)) & (0x20 >> (eepromState - DEV9Header.EEPROM_ADDR0))));
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_TDATA:
                            {
                                if (eepromCommand == 1) //write
                                {
                                    eeprom[eepromAddress] = (byte)
                                        ((eeprom[eepromAddress] & (63 ^ (1 << eepromBit))) |
                                        ((value >> eepromBit) & (0x8000 >> eepromBit)));
                                    eepromBit++;
                                    if (eepromBit == 16)
                                    {
                                        eepromAddress++;
                                        eepromBit = 0;
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
                    Dev9Wu8((int)addr, value);
                    Log_Error("*Unknown 8bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
        }

        public void DEV9_Write16(uint addr, ushort value)
        {
            //Error.WriteLine("DEV9write16");
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                ata.ATA_Write16(addr, value);
                //Error.WriteLine("DEV9write16 ATA");
                return;
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //Error.WriteLine("DEV9write16 SMAP");
                smap.SMAP_Write16(addr, value);
                return;
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                //Log_Verb("DEV9write16 flash");
                flash.FLASHwrite(addr, (UInt32)value, 2);
                return;
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.SMAP_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                spd.SPEED_Write16(addr, value);
                return;
            }
            switch (addr)
            {
#if ALLOW_WIDE_EEPROM
                case DEV9Header.SPD_R_PIO_DIR:
                    Log_Verb("SPD_R_PIO_DIR 16bit write " + value.ToString("X"));
                    if ((value & 0xc0) != 0xc0)
                        return;

                    if ((value & 0x30) == 0x20)
                    {
                        eepromState = 0;
                    }
                    eepromDir = (byte)((value >> 4) & 3);

                    return;

                case DEV9Header.SPD_R_PIO_DATA:
                    Log_Verb("SPD_R_PIO_DATA 16bit write " + value.ToString("X"));

                    if ((value & 0xc0) != 0xc0)
                        return;

                    switch (eepromState)
                    {
                        case DEV9Header.EEPROM_READY:
                            eepromCommand = 0;
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_OPCD0:
                            eepromCommand = (byte)((value >> 4) & 2);
                            eepromState++;
                            eepromBit = 0xFF;
                            break;
                        case DEV9Header.EEPROM_OPCD1:
                            eepromCommand |= (byte)((value >> 5) & 1);
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_ADDR0:
                        case DEV9Header.EEPROM_ADDR1:
                        case DEV9Header.EEPROM_ADDR2:
                        case DEV9Header.EEPROM_ADDR3:
                        case DEV9Header.EEPROM_ADDR4:
                        case DEV9Header.EEPROM_ADDR5:
                            eepromAddress = (byte)
                                ((eepromAddress & (63 ^ (1 << (eepromState - DEV9Header.EEPROM_ADDR0)))) |
                                ((value >> (eepromState - DEV9Header.EEPROM_ADDR0)) & (0x20 >> (eepromState - DEV9Header.EEPROM_ADDR0))));
                            eepromState++;
                            break;
                        case DEV9Header.EEPROM_TDATA:
                            {
                                if (eepromCommand == 1) //write
                                {
                                    eeprom[eepromAddress] = (byte)
                                        ((eeprom[eepromAddress] & (63 ^ (1 << eepromBit))) |
                                        ((value >> eepromBit) & (0x8000 >> eepromBit)));
                                    eepromBit++;
                                    if (eepromBit == 16)
                                    {
                                        eepromAddress++;
                                        eepromBit = 0;
                                    }
                                }
                            }
                            break;
                        default:
                            Log_Error("Unkown EEPROM COMMAND");
                            break;
                    }
                    return;
#endif

                default:
                    Dev9Wu16((int)addr, value);
                    Log_Error("*Unknown 16bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
        }

        public void DEV9_Write32(uint addr, uint value)
        {
            //Error.WriteLine("DEV9write32");
            //ATA
            if (addr >= DEV9Header.ATA_DEV9_HDD_BASE && addr < DEV9Header.ATA_DEV9_HDD_END)
            {
                Log_Error("ATA does not support 32bit writes");
                return;
            }
            //SMAP
            if (addr >= DEV9Header.SMAP_REGBASE && addr < DEV9Header.FLASH_REGBASE)
            {
                //smap
                smap.SMAP_Write32(addr, value);
                return;
            }
            //FLASH
            if ((addr >= DEV9Header.FLASH_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE + DEV9Header.FLASH_REGSIZE)))
            {
                flash.FLASHwrite(addr, (UInt32)value, 4);
                return;
            }
            //SPEED
            if ((addr >= DEV9Header.SPD_REGBASE) && (addr < (DEV9Header.FLASH_REGBASE))
                && addr != DEV9Header.SPD_R_PIO_DIR && addr != DEV9Header.SPD_R_PIO_DATA)
            {
                spd.SPEED_Write32(addr, value);
                return;
            }
            switch (addr)
            {
                default:
                    Dev9Wu32((int)addr, value);
                    Log_Error("*Unknown 32bit write at address " + addr.ToString("X8") + " value " + value.ToString("X"));
                    return;
            }
        }

        public void DEV9_ReadDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            size >>= 1;

            Log_Verb("*DEV9readDMA8Mem: size " + size.ToString("X"));
            //if (size == 0) return; //What?
            //Log_Info("rDMA");
            long ptr = pMem.Position;

            if (spd.dmaSMAP)
            {
                smap.SMAP_ReadDMA8Mem(pMem, size);
            }
            else
            {
                //ata.ATAreadDMA8Mem(pMem, size);
                spd.SPEEDreadDMA8Mem(pMem, size);
            }

            long delta = pMem.Position - ptr;
            if (delta == 0)
            {
                Log_Error("rDMA No Data Read");
            }
        }

        public void DEV9_WriteDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            size >>= 1;

            Log_Verb("*DEV9writeDMA8Mem: size " + size.ToString("X"));
            //Log_Info("wDMA");
            long ptr = pMem.Position;

            if (spd.dmaSMAP)
            {
                smap.SMAP_WriteDMA8Mem(pMem, size);
            }
            else
            {
                //ata.ATAwriteDMA8Mem(pMem, size);
                spd.SPEEDwriteDMA8Mem(pMem, size);
            }

            long delta = pMem.Position - ptr;
            if (delta == 0)
            {
                Log_Error("wDMA No Data Written");
            }
        }

        public void DEV9_Async(uint cycles)
        {
            smap.SMAP_Async(cycles);
            ata.ATA_Async(cycles);
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Dev9, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Dev9, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Dev9, str);
        }
    }
}
