using System;
using System.Diagnostics;
using System.IO;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        DEV9_State dev9 = null;

        FileStream hddImage = null;

        public ATA_State(DEV9_State parDev9)
        {
            dev9 = parDev9;

            //Fillout Command table (inspired from MegaDev9)
            //This is actully a pretty neat way of doing this
            for (int i = 0; i < 256; i++)
            {
                hddCmds[i] = HDD_Unk;
            }

            hddCmds[0x00] = HDD_Nop;

            hddCmds[0x20] = () => HDD_ReadPIO(false);

            hddCmds[0xB0] = HDD_Smart; hddCmdDoesSeek[0xB0] = true;

            hddCmds[0xC8] = () => HDD_ReadDMA(false);
            hddCmds[0xCA] = () => HDD_WriteDMA(false);
            //HDDcmds[0x25] = HDDreadDMA48;
            /*	HDDcmds[0x35] = HDDwriteDMA_ext;*/
            //HDDcmdNames[0x35] = "DMA write (48-bit)";		// 48-bit

            hddCmds[0xE3] = HDD_Idle;

            hddCmds[0xE7] = HDD_FlushCache;
            /*	HDDcmds[0xEA] = HDDflushCacheExt;*/
            //HDDcmdNames[0xEA] = "flush cache (48-bit)";		// 48-bit

            hddCmds[0xEC] = HDD_IdentifyDevice;
            /*	HDDcmds[0xA1] = HDDidentifyPktDevice;*/
            //HDDcmdNames[0xA1] = "identify ATAPI device";	// For ATAPI devices

            hddCmds[0xEF] = HDD_SetFeatures; hddCmdDoesSeek[0xEF] = true;

            ///*	HDDcmds[0xF1] = HDDsecSetPassword;*/
            //HDDcmdNames[0xF1] = "security set password";
            ///*	HDDcmds[0xF2] = HDDsecUnlock;*/
            //HDDcmdNames[0xF2] = "security unlock";
            ///*	HDDcmds[0xF3] = HDDsecErasePrepare;*/
            //HDDcmdNames[0xF3] = "security erase prepare";
            ///*	HDDcmds[0xF4] = HDDsecEraseUnit;*/
            //HDDcmdNames[0xF4] = "security erase unit";

            /* This command is Sony-specific and isn't part of the IDE standard */
            /* The Sony HDD has a modified firmware that supports this command */
            /* Sending this command to a standard HDD will give an error */
            /* We roughly emulate it to make programs think the HDD is a Sony one */
            /* However, we only send null, if anyting checks the returned data */
            /* it will fail */
            hddCmds[0x8E] = HDD_sceSecCtrl; //HDDcmdNames[0x8E] = "SCE security control";
        }

        public int Open(string hddPath)
        {
            nsector = 1;
            sector = 1;
            status = 0x40;

            CreateHDDinfo(DEV9Header.config.HddSize);

            //Open File
            if (File.Exists(hddPath))
            {
                hddImage = new FileStream(hddPath, FileMode.Open, FileAccess.ReadWrite);
            }
            else
            {
                //Need to Zero fill the hdd image
                HddCreate hddCreator = new HddCreate();
                hddCreator.neededSize = DEV9Header.config.HddSize;
                hddCreator.filePath = hddPath;

                hddCreator.ShowDialog();
                hddCreator.Dispose();

                hddImage = new FileStream(hddPath, FileMode.Open, FileAccess.ReadWrite);
            }

            return 0;
        }

        public void Close()
        {
            if (hddImage != null)
            {
                hddImage.Close();
                hddImage.Dispose();
                hddImage = null;
            }
        }

        public UInt16 ATAread16(UInt32 addr)
        {
            //TODO figure out hob
            bool hob = false;

            switch (addr)
            {
                case DEV9Header.ATA_R_DATA:
                    Log_Verb("*ATA_R_DATA 16bit read at address " + addr.ToString("x") + " pio_count " + dataPtr + " pio_size " + dataEnd);
                    if (dataPtr < dataEnd)
                    {
                        UInt16 ret = BitConverter.ToUInt16(pioBuffer, dataPtr * 2);
                        //ret = (UInt16)System.Net.IPAddress.HostToNetworkOrder((Int16)ret);
                        Log_Verb("*ATA_R_DATA returned value is  " + ret.ToString("x"));
                        dataPtr++;
                        if (dataPtr == dataEnd) //Fnished transfer (Changed from MegaDev9)
                        {
                            endTransferFunc();
                        }
                        return ret;
                    }
                    return 0xFF;
                case DEV9Header.ATA_R_ERROR:
                    Log_Verb("*ATA_R_ERROR 16bit read at address " + addr.ToString("x") + " value " + error.ToString("x") + " Active " + ((select & 0x10) == 0));
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(error);
                    else
                        return hobFeature;
                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit read at address " + addr.ToString("x") + " value " + nsector.ToString("x") + " Active " + ((select & 0x10) == 0));
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(nsector & 0xff);
                    else
                        return hobNsector;
                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit read at address " + addr.ToString("x") + " value " + sector.ToString("x") + " Active " + ((select & 0x10) == 0));
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(sector);
                    else
                        return hobSector;
                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit read at address " + addr.ToString("x") + " value " + lcyl.ToString("x") + " Active " + ((select & 0x10) == 0));
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(lcyl);
                    else
                        return hobLcyl;
                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit read at address " + addr.ToString("x") + " value " + hcyl.ToString("x") + " Active " + ((select & 0x10) == 0));
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(hcyl);
                    else
                        return hobHcyl;
                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit read at address " + addr.ToString("x") + " value " + select.ToString("x") + " Active " + ((select & 0x10) == 0));
                    //if ((select & 0x10) != 0)
                    //    return 0;
                    return select;
                case DEV9Header.ATA_R_STATUS:
                    Log_Verb("*ATA_R_STATUS (redirecting to ATA_R_ALT_STATUS)");
                    //Clear irqcause
                    dev9.irqCause &= ~(1 | 3);
                    return ATAread16(DEV9Header.ATA_R_ALT_STATUS);
                case DEV9Header.ATA_R_ALT_STATUS:
                    Log_Verb("*ATA_R_ALT_STATUS 16bit read at address " + addr.ToString("x") + " value " + status.ToString("x") + " Active " + ((select & 0x10) == 0));
                    //raise IRQ?
                    if ((select & 0x10) != 0)
                        return 0;
                    if (!hob)
                        return (UInt16)(status);
                    else
                        return status;
                default:
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x"));
                    return 0xff;
            }
        }

        public void ATAwrite16(UInt32 addr, UInt16 value)
        {
            if (addr != DEV9Header.ATA_R_CMD & (status & (DEV9Header.ATA_STAT_BUSY | DEV9Header.ATA_STAT_DRQ)) != 0)
            {
                Log_Error("*DEVICE BUSY, DROPPING WRITE");
                return;
            }
            switch (addr)
            {
                case DEV9Header.ATA_R_FEATURE:
                    Log_Verb("*ATA_R_FEATURE 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    hobFeature = feature;
                    feature = (byte)value;
                    break;
                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    hobNsector = (byte)nsector;
                    nsector = value;
                    break;
                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_SECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    hobSector = sector;
                    sector = (byte)value;
                    break;
                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    hobLcyl = lcyl;
                    lcyl = (byte)value;
                    break;
                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    hobHcyl = hcyl;
                    hcyl = (byte)value;
                    break;
                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    /* FIXME: HOB readback uses bit 7 */
                    select = (byte)value;
                    //bus->ifs[0].select = (val & ~0x10) | 0xa0;
                    //bus->ifs[1].select = (val | 0x10) | 0xa0;
                    /* select drive */
                    //Also have LBA bit
                    unit = (uint)((value >> 4) & 1);
                    break;
                case DEV9Header.ATA_R_CONTROL:
                    Log_Verb("*ATA_R_CONTROL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.Dev9Wu16((int)DEV9Header.ATA_R_CONTROL, value);
                    if ((value & 0x2) != 0)
                    {
                        //Supress all IRQ
                        sendIRQ = false;
                    }
                    else
                    {
                        sendIRQ = true;
                    }
                    if ((value & 0x4) != 0)
                    {
                        Log_Verb("*ATA_R_CONTROL RESET");
                        error = 0;
                        nsector = 1;
                        sector = 1;
                        status = 0x40;
                        lcyl = 0;
                        hcyl = 0;
                        feature = 0;
                        xferMode = 0;
                        dataEnd = 0;
                        //command = 0;
                    }
                    break;
                case DEV9Header.ATA_R_CMD:
                    Log_Verb("*ATA_R_CMD 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    command = value;
                    IDE_ExecCmd(value);
                    break;
                default:
                    Log_Error("*UNKOWN 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    break;

            }
        }

        static int rdTransferred;
        static int wrTransferred;
        public void ATAreadDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if (((xferMode & 0xF0) == 0x40) &&
                (dev9.Dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & DEV9Header.SPD_IF_DMA_ENABLE) != 0)
            {
                size >>= 1;
                Log_Verb("DEV9 : DMA read, size " + size + ", transferred " + rdTransferred + ", total size " + nsector * 512);

                //read
                byte[] temp = new byte[size];
                hddImage.Read(temp, 0, size);
                pMem.Write(temp, 0, size);

                rdTransferred += size;
                if (rdTransferred >= nsector * 512)
                {
                    //Set Sector
                    long currSect = HDD_GetLBA();
                    currSect += nsector;
                    HDD_SetLBA(currSect);

                    nsector = 0;
                    status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK;
                    if (sendIRQ) dev9.DEV9irq(3, 1); //0x6c
                    rdTransferred = 0;
                }
            }
        }
        public void ATAwriteDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if (((xferMode & 0xF0) == 0x40) &&
                (dev9.Dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & DEV9Header.SPD_IF_DMA_ENABLE) != 0)
            {
                size >>= 1;
                Log_Verb("DEV9 : DMA write, size " + size + ", transferred " + wrTransferred + ", total size " + nsector * 512);

                //write
                byte[] temp = new byte[size];
                pMem.Read(temp, 0, size);
                hddImage.Write(temp, 0, size);

                wrTransferred += size;
                if (wrTransferred >= nsector * 512)
                {
                    hddImage.Flush();

                    //Set Sector
                    long currSect = HDD_GetLBA();
                    currSect += nsector;
                    HDD_SetLBA(currSect);

                    nsector = 0;
                    status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK;
                    if (sendIRQ) dev9.DEV9irq(3, 1); //0x6C
                    wrTransferred = 0;
                }
            }
        }

        long HDD_GetLBA()
        {
            if ((select & 0x40) != 0)
            {
                if (!lba48)
                {
                    return (sector |
                            (lcyl << 8) |
                            (hcyl << 16) |
                            ((select & 0x0f) << 24));
                }
                else
                {
                    return ((long)hobHcyl << 40) |
                            ((long)hobLcyl << 32) |
                            ((long)hobSector << 24) |
                            ((long)hcyl << 16) |
                            ((long)lcyl << 8) | sector;
                }
            }
            else
            {
                status |= (byte)DEV9Header.ATA_STAT_ERR;
                error |= (byte)DEV9Header.ATA_ERR_ABORT;

                Log_Error("DEV9 ERROR : tried to get LBA address while LBA mode disabled\n");

                return -1;
            }
            //return -1;
        }

        void HDD_SetLBA(long sectorNum)
        {
            if ((select & 0x40) != 0)
            {
                if (!lba48)
                {
                    select = (byte)((select & 0xf0) | ((int)sectorNum >> 24));
                    hcyl = (byte)(sectorNum >> 16);
                    lcyl = (byte)(sectorNum >> 8);
                    sector = (byte)(sectorNum);
                }
                else
                {
                    sector = (byte)sectorNum;
                    lcyl = (byte)(sectorNum >> 8);
                    hcyl = (byte)(sectorNum >> 16);
                    hobSector = (byte)(sectorNum >> 24);
                    hobLcyl = (byte)(sectorNum >> 32);
                    hobHcyl = (byte)(sectorNum >> 40);
                }
            }
            else
            {
                status |= (byte)DEV9Header.ATA_STAT_ERR;
                error |= (byte)DEV9Header.ATA_ERR_ABORT;

                Log_Error("DEV9 ERROR : tried to get LBA address while LBA mode disabled\n");
            }
        }

        int HDD_Seek()
        {
            long lba;
            long pos;

            lba = HDD_GetLBA();
            if (lba == -1)
                return -1;
            Log_Verb("LBA :" + lba);
            pos = ((long)lba * 512);
            hddImage.Seek(pos, SeekOrigin.Begin);

            return 0;
        }

        void CreateHDDinfo(int sizeMb)
        {
            int sectorSize = 512;
            Log_Verb("HddSize : " + DEV9Header.config.HddSize);
            long nbSectors = (((long)sizeMb / (long)sectorSize) * 1024L * 1024L);
            Log_Verb("nbSectors : " + nbSectors);

            identifyData = new byte[512];
            UInt16 heads = 255;
            UInt16 sectors = 63;
            UInt16 cylinders = (UInt16)(nbSectors / heads / sectors);
            int oldsize = cylinders * heads * sectors;

            //M-General configuration bit-significant information:
            Utils.memcpy(ref identifyData, 0 * 2, BitConverter.GetBytes((UInt16)0x0040), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 1 * 2, BitConverter.GetBytes((UInt16)cylinders), 0, 2);
            //Specific configuration
            //2
            //Obsolete
            Utils.memcpy(ref identifyData, 3 * 2, BitConverter.GetBytes((UInt16)heads), 0, 2);
            //Retired
            Utils.memcpy(ref identifyData, 4 * 2, BitConverter.GetBytes((UInt16)(sectorSize * sectors)), 0, 2);
            //Retired
            Utils.memcpy(ref identifyData, 5 * 2, BitConverter.GetBytes((UInt16)sectorSize), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 6 * 2, BitConverter.GetBytes((UInt16)sectors), 0, 2);
            //Reserved for assignment by the CompactFlash™ Association
            //7-8
            //M-Serial number (20 ASCII characters)
            Utils.memcpy(ref identifyData, 10 * 2,
                new byte[] {
                        (byte)'C', (byte)'L', // serial
	                    (byte)'R', (byte)'-',
                        (byte)'D', (byte)'E',
                        (byte)'V', (byte)'9',
                        (byte)'-', (byte)'A',
                        (byte)'I', (byte)'R',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                            }, 0, 20);
            //Retired
            Utils.memcpy(ref identifyData, 20 * 2, BitConverter.GetBytes((UInt16)/*3*/0), 0, 2); //??
            //Retired
            Utils.memcpy(ref identifyData, 21 * 2, BitConverter.GetBytes((UInt16)/*512*/ 0), 0, 2); //cache size in sectors
            //Obsolete
            Utils.memcpy(ref identifyData, 22 * 2, BitConverter.GetBytes((UInt16)/*4*/0), 0, 2); //ecc bytes
            //M-Firmware revision (8 ASCII characters)
            Utils.memcpy(ref identifyData, 23 * 2,
                new byte[] {
                        (byte)'F', (byte)'I', // firmware
	                    (byte)'R', (byte)'M',
                        (byte)'1', (byte)'0',
                        (byte)'0', (byte)' ',
                            }, 0, 8);
            //M-Model number (40 ASCII characters)
            Utils.memcpy(ref identifyData, 27 * 2,
                new byte[] {
                        (byte)'C', (byte)'L', // model
                        (byte)'R', (byte)'-',
                        (byte)'D', (byte)'E',
                        (byte)'V', (byte)'9',
                        (byte)' ', (byte)'H',
                        (byte)'D', (byte)'D',
                        (byte)' ', (byte)'A',
                        (byte)'I', (byte)'R',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                        (byte)' ', (byte)' ',
                            }, 0, 40);
            //M-READ/WRIE MULI max sectors
            //47
            //Trusted Computing feature set options
            Utils.memcpy(ref identifyData, 48 * 2, BitConverter.GetBytes((UInt16)1), 0, 2); //dword IO
            //M-Capabilities (8-DMA, 9-LBA, 10-IORDY may be disabled, 11-IORDY supported, 13 - Standby timer values as specified in this standard are supported)
            Utils.memcpy(ref identifyData, 49 * 2, BitConverter.GetBytes((UInt16)((1 << 11) | (1 << 9) | (1 << 8))), 0, 2); //DMA and LBA supported
            //M-Capabilities (0-Shall be set to one to indicate a device specific Standby timer value minimum)
            //50
            //Obsolete
            Utils.memcpy(ref identifyData, 51 * 2, BitConverter.GetBytes((UInt16)0x200), 0, 2); //PIO transfer cycle
            //Obsolete
            Utils.memcpy(ref identifyData, 52 * 2, BitConverter.GetBytes((UInt16)0x200), 0, 2); //DMA transfer cycle
            //M-2 = the fields reported in word 88 are valid, 1 = the fields reported in words (70:64) are valid
            Utils.memcpy(ref identifyData, 53 * 2, BitConverter.GetBytes((UInt16)(1 | (1 << 1) | (1 << 2))), 0, 2); // words 54-58,64-70,88 are valid (??)
            //Obsolete
            Utils.memcpy(ref identifyData, 54 * 2, BitConverter.GetBytes((UInt16)cylinders), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 55 * 2, BitConverter.GetBytes((UInt16)heads), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 56 * 2, BitConverter.GetBytes((UInt16)sectors), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 57 * 2, BitConverter.GetBytes((UInt16)oldsize), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 58 * 2, BitConverter.GetBytes((UInt16)(oldsize >> 16)), 0, 2);
            //M-8 - Multiple sector setting is valid, 7:0  xxh = Current setting for number of logical sectors that shall be transferred per DRQ data block on READ/WRITE Multiple commands
            //59
            //Total number of user addressable logical sectors
            Utils.memcpy(ref identifyData, 60 * 2, BitConverter.GetBytes((UInt16)nbSectors), 0, 2);
            //Total number of user addressable logical sectors(part 2)
            Utils.memcpy(ref identifyData, 61 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 16)), 0, 2);
            //Obsolete
            Utils.memcpy(ref identifyData, 62 * 2, BitConverter.GetBytes((UInt16)0x07), 0, 2); //single word dma0-2 supported
            //M-bit 0-2-Multiword DMA0-2 supported, 8-10, Multiword DMA0-2 selected
            Utils.memcpy(ref identifyData, 63 * 2, BitConverter.GetBytes((UInt16)0x07), 0, 2); //mdma0-2 supported
            //M-Bit 0-7-PIO modes supported
            Utils.memcpy(ref identifyData, 64 * 2, BitConverter.GetBytes((UInt16)0x03), 0, 2); //pio3-4 supported
            //M-Minimum Multiword DMA transfer cycle time per word
            Utils.memcpy(ref identifyData, 65 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            //M-Manufacturer’s recommended Multiword DMA transfer cycle time
            Utils.memcpy(ref identifyData, 66 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            //M-Minimum PIO transfer cycle time without flow control
            Utils.memcpy(ref identifyData, 67 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            //M-Minimum PIO transfer cycle time with IORDY flow control
            Utils.memcpy(ref identifyData, 68 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            //Reserved
            //69-70
            //Reserved
            //71-74
            //Queue depth (4bit, Maximum queue depth - 1)
            //75
            //Reserved
            //76-79
            //M-Major revision number (1-3-Obsolete, 4-8-ATA4-8 supported)
            Utils.memcpy(ref identifyData, 80 * 2, BitConverter.GetBytes((UInt16)0xf0), 0, 2);
            //M-Minor revision numbe
            Utils.memcpy(ref identifyData, 81 * 2, BitConverter.GetBytes((UInt16)0x16), 0, 2);
            //M-Command set supported (14-NOP, 13-READ BUFFER, 12-WRITE BUFFER, 10-Host Protected Area feature set supported,
            //9-DEVICE RESET command supported, 8-SERVICE interrupt supported, 7-release interrupt supported, 6-look-ahead supported
            //5-write cache supported, 4-ATAPI support, 3-mandatory Power Management feature set supported
            //1-Security Mode feature set supported, 0-SMART)
            Utils.memcpy(ref identifyData, 82 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 5) | /*(1 << 1) | (1 << 10) |*/ 1)), 0, 2); //1-security, 10-Host Protected Area feature set
            //M-Command sets supported. (14-Set to one, 13-FLUSH CACHE EXT command supported, 12-FLUSH CACHE command supported,
            //11-Device Configuration Overlay feature set supported, 10-48-bit Address feature set supported, 
            //9-Automatic Acoustic Management feature set supported, 8-SET MAX security extension supported, 
            //7-See Address Offset Reserved Area Boot, INCITS TR27:2001, 6-SET FEATURES subcommand required to spin-up after power-up
            //5-Power-Up In Standby feature set supported, 3-Advanced Power Management feature set supported, 2-CFA feature set supported,
            //1-READ/WRITE DMA QUEUED supported, 0-DOWNLOAD MICROCODE command supported)
            Utils.memcpy(ref identifyData, 83 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 13) | (1 << 12) /*| (1 << 8)*/ /*| (1 << 10)*/)), 0, 2); //48bit, 8-SET MAX security extension supported
            //M-Command set/feature supported (14-Set to one, 13-1 = IDLE IMMEDIATE with UNLOAD FEATURE supported, 8-64-bit World wide name supported,
            //7-WRITE DMA QUEUED FUA EXT command supported, 6-WRITE DMA FUA EXT and WRITE MULTIPLE FUA EXT commands supported
            //5-General Purpose Logging feature set supported, 4-Streaming feature set supported, 3-Media Card Pass Through Command feature set supported
            //2-Media serial number supported, 1-SMART self-test supported, 0-SMART error logging supported)
            Utils.memcpy(ref identifyData, 84 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 1) | 1)), 0, 2); //no WWN
            //M-Command set/feature enabled/supported (14-NOP supported, 13-READ BUFFER command supported, 12-WRITE BUFFER command supported
            //10-host Protected Area has been established (i.e., the maximum LBA is less than the maximum native LBA, DEVICE RESET command supported, 
            //8-SERVICE interrupt enabled, 7-release interrupt enabled, 6-look-ahead enabled, 5-write cache enabled, 4-ATAPI, 
            //3-Power Management feature set enabled, 1-Security Mode feature set enabled, 0-SMART enabled)
            Utils.memcpy(ref identifyData, 85 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 1) | 1)), 0, 2); //no WCACHE 8-security 
            //M-Command set/feature enabled/supported. (15-Words 120:119 are valid, 13-FLUSH CACHE EXT command supported, 12-FLUSH CACHE command supported,
            //11-Device Configuration Overlay feature set supported, 10-48-bit Address feature set supported, 
            //9-Automatic Acoustic Management feature set enabled, 8-SET MAX security extension enabled by SET MAX SET PASSWORD, 
            //6-SET FEATURES subcommand required to spin-up after power-up, 5-Power-Up In Standby feature set enabled, 
            //3-Advanced Power Management feature set enabled, 2-CFA feature set supported,
            //1-READ/WRITE DMA QUEUED supported, 0-DOWNLOAD MICROCODE command supported)
            Utils.memcpy(ref identifyData, 86 * 2, BitConverter.GetBytes((UInt16)((1 << 13) | (1 << 12) /*| (1 << 10)*/)), 0, 2); //48bit
            //M-Command set/feature enabled/supported (14-Set to one, 13-1 = IDLE IMMEDIATE with UNLOAD FEATURE supported, 8-64-bit World wide name supported,
            //7-WRITE DMA QUEUED FUA EXT command supported, 6-WRITE DMA FUA EXT and WRITE MULTIPLE FUA EXT commands supported
            //5-General Purpose Logging feature set supported, 3-Media Card Pass Through Command feature set supported
            //2-Media serial number is valid, 1-SMART self-test supported, 0-SMART error logging supported)
            Utils.memcpy(ref identifyData, 87 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 1) | 1)), 0, 2); //no WWN
            //Ultra DMA modes (8-14-Ultra DMA 0-6 selected, 0-6-Ultra DMA0-6 supported
            Utils.memcpy(ref identifyData, 88 * 2, BitConverter.GetBytes((UInt16)(0x3f | (1 << 13))), 0, 2); //udma5 set and supported
            //Time required for security erase unit completion
            //89
            // Time required for Enhanced security erase completion
            //90
            //Current advanced power management value
            //91
            //Master Password Identifier
            //92
            //Hardware reset result.  The contents of bits (12:0) of this word shall change only during the execution of a hardware reset.
            //See 7.16.7.41 for more information.
            //14-Set to one, 13-device detected CBLID- above ViH, 12-8 Dev1 results
            //7-0, Device 0 hardware reset result. Device 1 shall clear these bits to zero. Device 0 shall set these bits as follows
            //0-set to one.
            Utils.memcpy(ref identifyData, 93 * 2, BitConverter.GetBytes((UInt16)(1 | (1 << 14) | 0x2000)), 0, 2);
            //Vendor’s recommended acoustic management value.
            //94
            //Stream Minimum Request Size
            //95
            //Streaming Transfer Time - DMA
            //96
            //Streaming Access Latency - DMA and PIO
            //97
            //Streaming Performance Granularity
            //98-99
            //Total Number of User Addressable Sectors for the 48-bit Address feature set.
            Utils.memcpy(ref identifyData, 100 * 2, BitConverter.GetBytes((UInt16)nbSectors), 0, 2);
            Utils.memcpy(ref identifyData, 101 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 16)), 0, 2);
            Utils.memcpy(ref identifyData, 102 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 32)), 0, 2);
            Utils.memcpy(ref identifyData, 103 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 48)), 0, 2);
            //Streaming Transfer Time - PIO
            //104
            //Reserved
            //105
            //Physical sector size / Logical Sector Size
            //(14-set to one, 13-Device has multiple logical sectors per physical sector, 12-Device Logical Sector Longer than 256 Words
            //3-0, 2^X logical sectors per physical sector
            Utils.memcpy(ref identifyData, 106 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | 0)), 0, 2);
            //Inter-seek delay for ISO-7779acoustic testing in microseconds
            //107
            //M-WNN
            //108-111
            //Reserved
            //112-115
            //Reserved
            //116
            //Words per Logical Sector
            //117-118
            //M-Supported Settings (Continued from words 84:82)
            //14-Set to one, 4-The Segmented feature for DOWNLOAD MICROCODE is supported, 3-READ and WRITE DMA EXT GPL optional commands are supported
            //3-READ and WRITE DMA EXT GPL optional commands are supported, 2-WRITE UNCORRECTABLE is supported, 1-Write-Read-Verify feature set is supported
            //119 (TODO)
            //M-Same as 119 but  1-Write-Read-Verify feature set is enabled
            //120 (TODO)
            //Reserved
            //121-126
            //Obsolete
            //127
            //Security status (8-Security level 0 = High, 1 = Maximum, 1-Enhanced security erase supported, 4-Security count expired
            //3-Security frozen, 2-Security locked, 1-Security enabled, 0-Security supported)
            //Utils.memcpy(ref identify_data, 128 * 2, BitConverter.GetBytes((UInt16)(1)), 0, 2);
            //Vendor Specific
            //129-159
            //CFA power mode 1
            //160
            //Reserved
            //161-175
            //Current media serial number (60 ASCII characters)
            //176-205
            //SCT Command Transport
            //206
            //Reserved
            //206-208
            //Alignment of logical blocks within a larger physical block
            //209
            //Write-Read-Verify Sector Count Mode 3 Only
            //210-211
            //Verify Sector Count Mode 2 Only
            //212-213
            //NV Cache Capabilities
            //214
            //NV Cache Size in Logical Blocks (LSW)
            //215
            //NV Cache Size in Logical Blocks (MSW)
            //216
            //NV Cache Read Transfer Speed in MB/s
            //217
            //NV Cache Write Transfer Speed in MB/s
            //218
            //NV Cache Options
            //219
            //Write-Read-Verify feature set current mode (bit 0-7)
            //220
            //Reserved
            //221
            //Transport Major revision number.  0000h or FFFFh = device does not report version
            //M-222
            //M-Transport Minor revision number
            //223
            //Reserved
            //224-223
            //Minimum number of 512 byte units per DOWNLOAD MICROCODE command mode 3
            //234
            //Maximum number of 512 byte units per DOWNLOAD MICROCODE command mode 3
            //235
            //Reserved
            //236-254
            //M-Integrity word
            //15:8 Checksum, 7:0 Signature
            CreateHDDinfoCsum();
        }
        void CreateHDDinfoCsum() //Is this correct?
        {
            byte counter = 0;
            unchecked
            {
                for (int i = 0; i < (512 - 1); i++)
                {
                    counter += identifyData[i];
                }
                counter += 0xA5;
            }
            identifyData[510] = 0xA5;
            identifyData[511] = (byte)(255 - counter + 1);
            counter = 0;
            unchecked
            {
                for (int i = 0; i < (512); i++)
                {
                    counter += identifyData[i];
                }
                Log_Error(counter.ToString());
            }
        }

        public void _ATAirqHandler()
        {
            //	dev9.intr_stat |= dev9.irq_cause;
            //dev9.dev9Wu16((int)DEV9Header.SPD_R_INTR_STAT, (UInt16)dev9.irqcause);//dev9.intr_stat = dev9.irqcause;
        }

        //QEMU stuff
        void IDE_ClearHOB()
        {
            /* any write clears HOB high bit of device control register */
            select &= unchecked((byte)(~(1 << 7)));
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.ATA, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.ATA, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.ATA, str);
        }
    }
}
