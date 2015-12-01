using System;
using System.Diagnostics;
using System.IO;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        DEV9_State dev9 = null;

        FileStream hddimage = null;

        public ATA_State(DEV9_State pardev9)
        {
            dev9 = pardev9;
            //should be in open
            dev9.dev9Wu16((int)DEV9Header.ATA_R_NSECTOR, 1);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_SECTOR, 1);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, 1);

            //Fillout Command table (inspired from MegaDev9)
            //This is actully a pretty neat way of doing this
            for (int i = 0; i < 256; i++)
            {
                HDDcmds[i] = HDDunk;
                //HDDcmdNames[i] = "unknown";
            }

            HDDcmds[0x00] = HDDnop; //HDDcmdNames[0x00] = "nop";

            HDDcmds[0xB0] = HDDsmart; //HDDcmdNames[0xB0] = "SMART";

            HDDcmds[0xC8] = HDDreadDMA; //HDDcmdNames[0xC8] = "DMA read";
            HDDcmds[0xCA] = HDDwriteDMA; //HDDcmdNames[0xCA] = "DMA write";
            /*	HDDcmds[0x25] = HDDreadDMA_ext;*/
            //HDDcmdNames[0x25] = "DMA read (48-bit)";		// 48-bit
            /*	HDDcmds[0x35] = HDDwriteDMA_ext;*/
            //HDDcmdNames[0x35] = "DMA write (48-bit)";		// 48-bit

            HDDcmds[0xE3] = HDDidle; //HDDcmdNames[0xE3] = "idle";

            HDDcmds[0xE7] = HDDflushCache; //HDDcmdNames[0xE7] = "flush cache";
            /*	HDDcmds[0xEA] = HDDflushCacheExt;*/
            //HDDcmdNames[0xEA] = "flush cache (48-bit)";		// 48-bit

            HDDcmds[0xEC] = HDDidentifyDevice; //HDDcmdNames[0xEC] = "identify device";
            /*	HDDcmds[0xA1] = HDDidentifyPktDevice;*/
            //HDDcmdNames[0xA1] = "identify ATAPI device";	// For ATAPI devices

            HDDcmds[0xEF] = HDDsetFeatures; //HDDcmdNames[0xEF] = "set features";

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
            HDDcmds[0x8E] = HDDsceSecCtrl; //HDDcmdNames[0x8E] = "SCE security control";

            // Set the number of sectors
            Int32 nbSectors = (((DEV9Header.config.HddSize * 1024) / 512) * 1024 * 1024);
            hddInfo[60 * 2] = BitConverter.GetBytes((UInt16)(nbSectors & 0xFFFF))[0];
            hddInfo[60 * 2 + 1] = BitConverter.GetBytes((UInt16)(nbSectors & 0xFFFF))[1];
            hddInfo[61 * 2] = BitConverter.GetBytes((UInt16)((nbSectors >> 16)))[0];
            hddInfo[61 * 2 + 1] = BitConverter.GetBytes((UInt16)((nbSectors >> 16)))[1];

            //Open File
            if(File.Exists(DEV9Header.config.Hdd))
            {
                hddimage = new FileStream(DEV9Header.config.Hdd, FileMode.Open, FileAccess.ReadWrite);
            }
        }

        public UInt16 ATAread16(UInt32 addr)
        {
            switch (addr)
            {
                case DEV9Header.ATA_R_DATA:
                    if (pio_count < pio_size)
                    {
                        UInt16 ret = BitConverter.ToUInt16(pio_buf, pio_count * 2);
                        pio_count++;
                        return ret;
                    }
                    break;

                case DEV9Header.ATA_R_NSECTOR:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR);

                case DEV9Header.ATA_R_SECTOR:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_SECTOR);

                case DEV9Header.ATA_R_LCYL:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_LCYL);

                case DEV9Header.ATA_R_HCYL:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_HCYL);

                case DEV9Header.ATA_R_SELECT:
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT);

                case DEV9Header.ATA_R_STATUS:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);

                case DEV9Header.ATA_R_ALT_STATUS:
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            }


            Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x"));
            return dev9.dev9Ru16((int)addr); ;
        }

        public void ATAwrite16(UInt32 addr, UInt16 value)
        {
            switch (addr)
            {
                case DEV9Header.ATA_R_FEATURE:
                    feature = value;
                    break;
                case DEV9Header.ATA_R_NSECTOR:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_NSECTOR, value);
                    break;
                case DEV9Header.ATA_R_SECTOR:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_SECTOR, value);
                    break;
                case DEV9Header.ATA_R_LCYL:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_LCYL, value);
                    break;
                case DEV9Header.ATA_R_HCYL:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_HCYL, value);
                    break;
                case DEV9Header.ATA_R_SELECT:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_SELECT, value);
                    break;
                case DEV9Header.ATA_R_CMD:
                    command = value;
                    HDDcmds[value]();
                    break;
                case DEV9Header.ATA_R_CONTROL:
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_CONTROL, value);
                    break;
            }
        }

        static int rd_transferred;
        static int wr_transferred;
        public void ATAreadDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if (((xfer_mode & 0xF0) == 0x40) &&
                (dev9.dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & DEV9Header.SPD_IF_DMA_ENABLE) != 0)
            {
                size >>= 1;
                Log_Verb("DEV9 : DMA read, size " + size + ", transferred " + rd_transferred + ", total size " + dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512);
                if (HDDseek() == 0)
                {
                    //read
                    byte[] temp = new byte[size];
                    hddimage.Read(temp, 0, size);
                    pMem.Write(temp, 0, size);
                }
                rd_transferred += size;
                if (rd_transferred >= (dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512))
                {
                    dev9.DEV9irq(3, 0x6C);
                    rd_transferred = 0;
                }
            }
        }
        public void ATAwriteDMA8Mem(System.IO.UnmanagedMemoryStream pMem, int size)
        {
            if (((xfer_mode & 0xF0) == 0x40) &&
                (dev9.dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & DEV9Header.SPD_IF_DMA_ENABLE) != 0)
            {
                size >>= 1;
                Log_Verb("DEV9 : DMA write, size " + size + ", transferred " + rd_transferred + ", total size " + dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512);
                if (HDDseek() == 0)
                {
                    //write
                    byte[] temp = new byte[size];
                    pMem.Read(temp, 0, size);
                    hddimage.Write(temp, 0, size);
                }
                rd_transferred += size;
                if (rd_transferred >= (dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512))
                {
                    dev9.DEV9irq(3, 0x6C);
                    rd_transferred = 0;
                }
            }
        }

        int HDDgetLBA()
            {
                if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x40) != 0)
	            {
		            return ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SECTOR)) |
				            (dev9.dev9Ru16((int)DEV9Header.ATA_R_LCYL) << 8) |
				            (dev9.dev9Ru16((int)DEV9Header.ATA_R_HCYL) << 16) |
				            (dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT & 0x0f)<< 24));
	            }
	            else
	            {
                    UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
                    UInt16 error = dev9.dev9Ru16((int)DEV9Header.ATA_R_ERROR);

                    status |= DEV9Header.ATA_STAT_ERR;
                    error |= DEV9Header.ATA_ERR_ABORT;

                    dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, error);

		            Log_Error("DEV9 ERROR : tried to get LBA address while LBA mode disabled\n");

		            return -1;
	            }

	            return -1;
        }

        int HDDseek()
        {
            int lba;
            long pos;

            lba = HDDgetLBA();
            if (lba == -1)
                return -1;

            pos = ((long)lba * 512);
            //fsetpos(hddImage, &pos);
            hddimage.Seek(pos, SeekOrigin.Begin);

            return 0;
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.ATA, "ATA", str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.ATA, "ATA", str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.ATA, "ATA", str);
        }
    }
}
