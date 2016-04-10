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
