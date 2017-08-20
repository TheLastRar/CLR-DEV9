using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        DEV9_State dev9 = null;

        FileStream hddImage = null;

        public ATA_State(DEV9_State parDev9)
        {
            dev9 = parDev9;

            //Power on
            ResetBegin();
            //Would do self-Diag + Hardware Init

            //Fillout Command table (inspired from MegaDev9)
            //This is actully a pretty neat way of doing this
            for (int i = 0; i < 256; i++)
            {
                hddCmds[i] = HDD_Unk;
            }

            hddCmds[0x00] = HDD_Nop;

            hddCmds[0x20] = () => HDD_ReadSectors(false);
            //0x21

            hddCmds[0x40] = () => HDD_ReadVerifySectors(false);
            //0x41

            hddCmds[0x70] = HDD_SeekCmd;

            hddCmds[0x90] = HDD_ExecuteDeviceDiag;
            hddCmds[0x91] = HDD_InitDevParameters;

            hddCmds[0xB0] = HDD_Smart;

            hddCmds[0xC4] = () => HDD_ReadMultiple(false);

            hddCmds[0xC8] = () => HDD_ReadDMA(false);
            //0xC9
            hddCmds[0xCA] = () => HDD_WriteDMA(false);
            //0xCB
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

            hddCmds[0xEF] = HDD_SetFeatures;

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
            hddCmds[0x8E] = HDD_SCE; //HDDcmdNames[0x8E] = "SCE security control";

            //

            ResetEnd(true);
        }

        public int Open(string hddPath)
        {
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

            ioThread = new Thread(IO_Thread);
            ioRead = new ManualResetEvent(false);
            ioWrite = new ManualResetEvent(false);
            ioClose = new AutoResetEvent(false);

            ioThread.Start();

            return 0;
        }

        public void Close()
        {
            //Wait for async code to finish
            ioClose.Set();
            ioWrite.Set();

            ioThread.Join();

            ioClose.Dispose();
            ioWrite.Dispose();
            //Close File Handle
            if (hddImage != null)
            {
                hddImage.Close();
                hddImage.Dispose();
                hddImage = null;
            }
        }

        void ResetBegin()
        {
            PreCmdExecuteDeviceDiag();
        }
        void ResetEnd(bool hard)
        {
            curHeads = 16;
            curSectors = 63;
            curCylinders = 0;
            curMultipleSectorsSetting = 128;

            //UDMA Mode setting is preserved
            //across SRST
            if (hard)
            {
                pioMode = 4;
                sdmaMode = -1;
                mdmaMode = 2;
                udmaMode = -1;
            }
            else
            {
                pioMode = 4;
                if (udmaMode == -1)
                {
                    sdmaMode = -1;
                    mdmaMode = 2;
                }
            }

            regControlEnableIRQ = false;
            HDD_ExecuteDeviceDiag();
            regControlEnableIRQ = true;
        }

        public void ATA_HardReset()
        {
            Log_Verb("*ATA_HARD RESET");
            ResetBegin();
            ResetEnd(false);
        }

        public UInt16 ATA_Read16(UInt32 addr)
        {
            switch (addr)
            {
                case DEV9Header.ATA_R_DATA:
                    return ATAreadPIO();
                case DEV9Header.ATA_R_ERROR:
                    Log_Verb("*ATA_R_ERROR 16bit read at address " + addr.ToString("x") + " value " + regError.ToString("x") + " Active " + (regSelectDev == 0));
                    if (regSelectDev != 0)
                        return 0;
                    return regError;
                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit read at address " + addr.ToString("x") + " value " + nsector.ToString("x") + " Active " + (regSelectDev == 0));
                    if (regSelectDev != 0)
                        return 0;
                    if (!regControlHOBRead)
                        return regNsector;
                    else
                        return regNsectorHOB;
                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit read at address " + addr.ToString("x") + " value " + regSector.ToString("x") + " Active " + (regSelectDev == 0));
                    if (regSelectDev != 0)
                        return 0;
                    if (!regControlHOBRead)
                        return regSector;
                    else
                        return regSectorHOB;
                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit read at address " + addr.ToString("x") + " value " + regLcyl.ToString("x") + " Active " + (regSelectDev == 0));
                    if (regSelectDev != 0)
                        return 0;
                    if (!regControlHOBRead)
                        return regLcyl;
                    else
                        return regLcylHOB;
                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit read at address " + addr.ToString("x") + " value " + regHcyl.ToString("x") + " Active " + (regSelectDev == 0));
                    if (regSelectDev != 0)
                        return 0;
                    if (!regControlHOBRead)
                        return regHcyl;
                    else
                        return regHcylHOB;
                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit read at address " + addr.ToString("x") + " value " + regSelect.ToString("x") + " Active " + (regSelectDev == 0));
                    return regSelect;
                case DEV9Header.ATA_R_STATUS:
                    Log_Verb("*ATA_R_STATUS (redirecting to ATA_R_ALT_STATUS)");
                    //Clear irqcause
                    dev9.spd.regIntStat &= unchecked((UInt16)~DEV9Header.ATA_INTR_INTRQ);
                    return ATA_Read16(DEV9Header.ATA_R_ALT_STATUS);
                case DEV9Header.ATA_R_ALT_STATUS:
                    Log_Verb("*ATA_R_ALT_STATUS 16bit read at address " + addr.ToString("x") + " value " + regStatus.ToString("x") + " Active " + (regSelectDev == 0));
                    //raise IRQ?
                    if (regSelectDev != 0)
                        return 0;
                    return regStatus;
                default:
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x"));
                    return 0xff;
            }
        }

        public void ATA_Write16(UInt32 addr, UInt16 value)
        {
            if (addr != DEV9Header.ATA_R_CMD & (regStatus & (DEV9Header.ATA_STAT_BUSY | DEV9Header.ATA_STAT_DRQ)) != 0)
            {
                Log_Error("*DEVICE BUSY, DROPPING WRITE");
                return;
            }
            switch (addr)
            {
                case DEV9Header.ATA_R_FEATURE:
                    Log_Verb("*ATA_R_FEATURE 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    regFeatureHOB = regFeature;
                    regFeature = (byte)value;
                    break;
                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    regNsectorHOB = regNsector;
                    regNsector = (byte)value;
                    break;
                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_SECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    regSectorHOB = regSector;
                    regSector = (byte)value;
                    break;
                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    regLcylHOB = regLcyl;
                    regLcyl = (byte)value;
                    break;
                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    IDE_ClearHOB();
                    regHcylHOB = regHcyl;
                    regHcyl = (byte)value;
                    break;
                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    regSelect = (byte)value;
                    //bus->ifs[0].select = (val & ~0x10) | 0xa0;
                    //bus->ifs[1].select = (val | 0x10) | 0xa0;
                    break;
                case DEV9Header.ATA_R_CONTROL:
                    Log_Verb("*ATA_R_CONTROL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.Dev9Wu16((int)DEV9Header.ATA_R_CONTROL, value);
                    if ((value & 0x2) != 0)
                    {
                        //Supress all IRQ
                        dev9.spd.regIntStat &= unchecked((UInt16)~DEV9Header.ATA_INTR_INTRQ);
                        regControlEnableIRQ = false;
                    }
                    else
                    {
                        regControlEnableIRQ = true;
                    }
                    if ((value & 0x4) != 0)
                    {
                        Log_Verb("*ATA_R_CONTROL RESET");
                        ResetBegin();
                        ResetEnd(false);
                    }
                    if ((value & 0x80) != 0)
                    {
                        regControlHOBRead = true;
                    }
                    break;
                case DEV9Header.ATA_R_CMD:
                    Log_Verb("*ATA_R_CMD 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    regCommand = value;
                    regControlHOBRead = false;
                    dev9.spd.regIntStat &= unchecked((UInt16)~DEV9Header.ATA_INTR_INTRQ);
                    IDE_ExecCmd(value);
                    break;
                default:
                    Log_Error("*UNKOWN 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    break;
            }
        }

        public void ATA_Async(uint cycles)
        {
            ManageAsync();
        }

        void ManageAsync()
        {
            if ((regStatus & (DEV9Header.ATA_STAT_BUSY | DEV9Header.ATA_STAT_DRQ)) == 0 |
                awaitFlush | (waitingCmd != null))
            {
                WaitHandle[] ioWaits = new WaitHandle[] { ioRead, ioWrite };
                if (WaitHandle.WaitAny(ioWaits, 0) != WaitHandle.WaitTimeout)
                {
                    //IO Running
                    return;
                }

                //Note, ioThread may still be working.
                if (waitingCmd != null) //Are we waiting to continue a command?
                {
                    //Log_Info("Running waiting command");
                    Cmd cmd = waitingCmd;
                    waitingCmd = null;
                    cmd();
                }
                else if (!WriteCacheSectors.IsEmpty) //Flush cache
                {
                    //Log_Info("Starting async write");
                    ioWrite.Set();
                }
                else if (awaitFlush) //Fire IRQ on flush completion?
                {
                    //Log_Info("Flush done, raise IRQ");
                    awaitFlush = false;
                    PostCmdNoData();
                }
            }
        }

        long HDD_GetLBA()
        {
            if ((regSelect & 0x40) != 0)
            {
                if (!lba48)
                {
                    return (regSector |
                            (regLcyl << 8) |
                            (regHcyl << 16) |
                            ((regSelect & 0x0f) << 24));
                }
                else
                {
                    return ((long)regHcylHOB << 40) |
                            ((long)regLcylHOB << 32) |
                            ((long)regSectorHOB << 24) |
                            ((long)regHcyl << 16) |
                            ((long)regLcyl << 8) |
                            regSector;
                }
            }
            else
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ABORT;

                Log_Error("DEV9 ERROR : tried to get LBA address while LBA mode disabled\n");
                //(c.Nh + h).Ns+(s-1)
                long CHSasLBA = ((regLcyl & regHcyl << 8) * curHeads + (regSelect & 0x0f)) * curSectors + (regSector - 1);
                return -1;
            }
        }

        void HDD_SetLBA(long sectorNum)
        {
            if ((regSelect & 0x40) != 0)
            {
                if (!lba48)
                {
                    regSelect = (byte)((regSelect & 0xf0) | (int)((sectorNum >> 24) & 0x0f));
                    regHcyl = (byte)(sectorNum >> 16);
                    regLcyl = (byte)(sectorNum >> 8);
                    regSector = (byte)(sectorNum);
                }
                else
                {
                    regSector = (byte)sectorNum;
                    regLcyl = (byte)(sectorNum >> 8);
                    regHcyl = (byte)(sectorNum >> 16);
                    regSectorHOB = (byte)(sectorNum >> 24);
                    regLcylHOB = (byte)(sectorNum >> 32);
                    regHcylHOB = (byte)(sectorNum >> 40);
                }
            }
            else
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ABORT;

                Log_Error("DEV9 ERROR : tried to get LBA address while LBA mode disabled\n");
            }
        }

        bool HDD_CanSeek()
        {
            int sectors = 0;
            return HDD_CanAccess(ref sectors);
        }

        bool HDD_CanAccess(ref int sectors)
        {
            long lba;
            long posStart;
            long posEnd;
            long maxLBA;

            maxLBA = Math.Max(DEV9Header.config.HddSize * 1024L * 1024L, hddImage.Length) / 512;
            if ((regSelect & 0x40) == 0) //CHS mode
            {
                Math.Max(maxLBA, curCylinders * curHeads * curSectors);
            }

            lba = HDD_GetLBA();
            if (lba == -1)
                return false;

            Log_Verb("LBA :" + lba);
            posStart = lba;

            if (posStart > maxLBA)
            {
                sectors = -1;
                return false;
            }

            posEnd = posStart + sectors;

            if (posEnd > maxLBA)
            {
                long overshoot = posEnd - maxLBA;
                long space = sectors - overshoot;
                sectors = (int)space;
                return false;
            }

            return true;
        }

        //QEMU stuff
        void IDE_ClearHOB()
        {
            /* any write clears HOB high bit of device control register */
            regControlHOBRead = false;
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
