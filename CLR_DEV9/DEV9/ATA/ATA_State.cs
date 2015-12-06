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

            //Fillout Command table (inspired from MegaDev9)
            //This is actully a pretty neat way of doing this
            for (int i = 0; i < 256; i++)
            {
                HDDcmds[i] = HDDunk;
            }

            HDDcmds[0x00] = HDDnop;

            HDDcmds[0x20] = HDDreadPIO;

            HDDcmds[0xB0] = HDDsmart;

            HDDcmds[0xC8] = HDDreadDMA;
            HDDcmds[0xCA] = HDDwriteDMA;
            //HDDcmds[0x25] = HDDreadDMA48;
            /*	HDDcmds[0x35] = HDDwriteDMA_ext;*/
            //HDDcmdNames[0x35] = "DMA write (48-bit)";		// 48-bit

            HDDcmds[0xE3] = HDDidle;

            HDDcmds[0xE7] = HDDflushCache;
            /*	HDDcmds[0xEA] = HDDflushCacheExt;*/
            //HDDcmdNames[0xEA] = "flush cache (48-bit)";		// 48-bit

            HDDcmds[0xEC] = HDDidentifyDevice;
            /*	HDDcmds[0xA1] = HDDidentifyPktDevice;*/
            //HDDcmdNames[0xA1] = "identify ATAPI device";	// For ATAPI devices

            HDDcmds[0xEF] = HDDsetFeatures;

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
        }

        public int Open(string hddPath)
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_NSECTOR, 1);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_SECTOR, 1);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, 0x40);

            // Set the number of sectors

            //Int32 nbSectors = (((DEV9Header.config.HddSize) / 512) * 1024 * 1024);
            //Log_Verb("HddSize : " + DEV9Header.config.HddSize);
            //Log_Verb("HddSector 1: " + nbSectors);
            //Log_Verb("HddSector 2: " + (nbSectors >> 16));
            //hddInfo[60 * 2] = BitConverter.GetBytes((UInt16)(nbSectors & 0xFFFF))[0];
            //hddInfo[60 * 2 + 1] = BitConverter.GetBytes((UInt16)(nbSectors & 0xFFFF))[1];
            //hddInfo[61 * 2] = BitConverter.GetBytes((UInt16)((nbSectors >> 16)))[0];
            //hddInfo[61 * 2 + 1] = BitConverter.GetBytes((UInt16)((nbSectors >> 16)))[1];
            CreateHDDid(DEV9Header.config.HddSize);

            //Open File
            if (File.Exists(hddPath))
            {
                hddimage = new FileStream(hddPath, FileMode.Open, FileAccess.ReadWrite);
            }
            else
            {
                //Need to Zero fill the hdd image
                HddCreate hddcreator = new HddCreate();
                hddcreator.neededSize = DEV9Header.config.HddSize;
                hddcreator.filePath = hddPath;

                hddcreator.ShowDialog();
                hddcreator.Dispose();

                hddimage = new FileStream(hddPath, FileMode.Open, FileAccess.ReadWrite);
            }

            return 0;
        }

        public void Close()
        {
            if (hddimage != null)
            {
                hddimage.Close();
                hddimage.Dispose();
                hddimage = null;
            }
        }

        public UInt16 ATAread16(UInt32 addr)
        {
            switch (addr)
            {
                case DEV9Header.ATA_R_DATA:
                    Log_Verb("*ATA_R_DATA 16bit read at address " + addr.ToString("x") + " pio_count " + pio_count + " pio_size " + pio_size);
                    if (pio_count < pio_size)
                    {
                        UInt16 ret = BitConverter.ToUInt16(pio_buf, pio_count * 2);
                        //ret = (UInt16)System.Net.IPAddress.HostToNetworkOrder((Int16)ret);
                        Log_Verb("*ATA_R_DATA returned value is  " + ret.ToString("x"));
                        pio_count++;
                        if (pio_count == pio_size) //Fnished transfer (Changed from MegaDev9)
                        {
                            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
                            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ));
                            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
                        }
                        return ret;
                    }
                    break;

                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR);

                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_SECTOR 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_SECTOR);

                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_LCYL);

                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_HCYL);

                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x"));
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT);

                case DEV9Header.ATA_R_STATUS:
                    Log_Verb("*ATA_R_STATUS 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);

                case DEV9Header.ATA_R_ALT_STATUS:
                    Log_Verb("*ATA_R_ALT_STATUS 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)(int)DEV9Header.ATA_R_STATUS).ToString("x") + " Active " + ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) == 0));
                    if ((dev9.dev9Ru16((int)DEV9Header.ATA_R_SELECT) & 0x10) != 0)
                        return 0;
                    return dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
                default:
                    Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x"));
                    return dev9.dev9Ru16((int)addr);
            }


            Log_Error("*Unknown 16bit read at address " + addr.ToString("x") + " value " + dev9.dev9Ru16((int)addr).ToString("x"));
            return dev9.dev9Ru16((int)addr); ;
        }

        public void ATAwrite16(UInt32 addr, UInt16 value)
        {
            switch (addr)
            {
                case DEV9Header.ATA_R_FEATURE:
                    Log_Verb("*ATA_R_FEATURE 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    feature = value;
                    break;
                case DEV9Header.ATA_R_NSECTOR:
                    Log_Verb("*ATA_R_NSECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_NSECTOR, value);
                    break;
                case DEV9Header.ATA_R_SECTOR:
                    Log_Verb("*ATA_R_SECTOR 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_SECTOR, value);
                    break;
                case DEV9Header.ATA_R_LCYL:
                    Log_Verb("*ATA_R_LCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_LCYL, value);
                    break;
                case DEV9Header.ATA_R_HCYL:
                    Log_Verb("*ATA_R_HCYL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_HCYL, value);
                    break;
                case DEV9Header.ATA_R_SELECT:
                    Log_Verb("*ATA_R_SELECT 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_SELECT, value);
                    break;
                case DEV9Header.ATA_R_CMD:
                    Log_Verb("*ATA_R_CMD 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    command = value;
                    HDDcmds[value]();
                    break;
                case DEV9Header.ATA_R_CONTROL:
                    Log_Verb("*ATA_R_CONTROL 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
                    dev9.dev9Wu16((int)DEV9Header.ATA_R_CONTROL, value);
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
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_NSECTOR, 1);
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_SECTOR, 1);
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, 0x40);
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_LCYL, 0);
                        dev9.dev9Wu16((int)DEV9Header.ATA_R_HCYL, 0);
                        feature = 0;
                        xfer_mode = 0;
                        pio_count = 0;
                        //command = 0;
                    }
                    break;
                default:
                    dev9.dev9Wu16((int)addr, value);
                    Log_Error("*Unknown 16bit write at address " + addr.ToString("x") + " value " + value.ToString("x"));
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

                    //read
                    byte[] temp = new byte[size];
                    hddimage.Read(temp, 0, size);
                    pMem.Write(temp, 0, size);

                rd_transferred += size;
                if (rd_transferred >= (dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512))
                {
                    if (sendIRQ) dev9.DEV9irq(3, 0x6C); //0x6c
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
                Log_Verb("DEV9 : DMA write, size " + size + ", transferred " + wr_transferred + ", total size " + dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512);

                    //write
                    byte[] temp = new byte[size];
                    pMem.Read(temp, 0, size);
                    hddimage.Write(temp, 0, size);

                wr_transferred += size;
                if (wr_transferred >= (dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR) * 512))
                {
                    hddimage.Flush();
                    if (sendIRQ) dev9.DEV9irq(3, 0x6C); //0x6C
                    wr_transferred = 0;
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

	            //return -1;
        }

        int HDDseek()
        {
            int lba;
            long pos;

            lba = HDDgetLBA();
            if (lba == -1)
                return -1;
            Log_Verb("LBA :" + lba);
            pos = ((long)lba * 512);
            //fsetpos(hddImage, &pos);
            hddimage.Seek(pos, SeekOrigin.Begin);

            return 0;
        }

        void CreateHDDid(int sizeMb)
        {
            int sectorSize = 512;
            Log_Verb("HddSize : " + DEV9Header.config.HddSize);
            int nbSectors = (((sizeMb) / sectorSize) * 1024 * 1024);
            Log_Verb("nbSectors : " + nbSectors);

            hddInfo = new byte[512];
            UInt16 heads = 255;
            UInt16 sectors = 63;
            UInt16 cylinders = (UInt16)(nbSectors / heads / sectors);
            int oldsize = cylinders * heads * sectors;

            Utils.memcpy(ref hddInfo, 0 * 2, BitConverter.GetBytes((UInt16)0x0040),0,2);
            Utils.memcpy(ref hddInfo, 1 * 2, BitConverter.GetBytes((UInt16)cylinders), 0, 2);
            //3
            Utils.memcpy(ref hddInfo, 3 * 2, BitConverter.GetBytes((UInt16)heads), 0, 2);
            Utils.memcpy(ref hddInfo, 4 * 2, BitConverter.GetBytes((UInt16)(sectorSize * sectors)), 0, 2);
            Utils.memcpy(ref hddInfo, 5 * 2, BitConverter.GetBytes((UInt16)sectorSize), 0, 2);
            Utils.memcpy(ref hddInfo, 6 * 2, BitConverter.GetBytes((UInt16)sectors), 0, 2);
            //8
            Utils.memcpy(ref hddInfo, 10 * 2,
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
            Utils.memcpy(ref hddInfo, 20 * 2, BitConverter.GetBytes((UInt16)/*3*/0), 0, 2); //??
            Utils.memcpy(ref hddInfo, 21 * 2, BitConverter.GetBytes((UInt16)/*512*/ 0), 0, 2); //cache size in sectors
            Utils.memcpy(ref hddInfo, 22 * 2, BitConverter.GetBytes((UInt16)/*4*/0), 0, 2); //ecc bytes
            Utils.memcpy(ref hddInfo, 23 * 2,
                new byte[] {
	                    (byte)'F', (byte)'I', // firmware
	                    (byte)'R', (byte)'M',
	                    (byte)'1', (byte)'0',
	                    (byte)'0', (byte)' ',
                            }, 0, 8);
            Utils.memcpy(ref hddInfo, 27 * 2,
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
            //??
            Utils.memcpy(ref hddInfo, 48 * 2, BitConverter.GetBytes((UInt16)1), 0, 2); //dword IO
            Utils.memcpy(ref hddInfo, 49 * 2, BitConverter.GetBytes((UInt16)((1 << 11) | (1 << 9) | (1 << 8))), 0, 2); //DMA and LBA supported
            //
            Utils.memcpy(ref hddInfo, 51 * 2, BitConverter.GetBytes((UInt16)0x200), 0, 2); //PIO transfer cycle
            Utils.memcpy(ref hddInfo, 52 * 2, BitConverter.GetBytes((UInt16)0x200), 0, 2); //DMA transfer cycle
            Utils.memcpy(ref hddInfo, 53 * 2, BitConverter.GetBytes((UInt16)(1 | (1 << 1) | (1 << 2))), 0, 2); // words 54-58,64-70,88 are valid (??)
            Utils.memcpy(ref hddInfo, 54 * 2, BitConverter.GetBytes((UInt16)cylinders), 0, 2);
            Utils.memcpy(ref hddInfo, 55 * 2, BitConverter.GetBytes((UInt16)heads), 0, 2);
            Utils.memcpy(ref hddInfo, 56 * 2, BitConverter.GetBytes((UInt16)sectors), 0, 2);
            Utils.memcpy(ref hddInfo, 57 * 2, BitConverter.GetBytes((UInt16)oldsize), 0, 2);
            Utils.memcpy(ref hddInfo, 58 * 2, BitConverter.GetBytes((UInt16)(oldsize >> 16)), 0, 2);
            //??
            Utils.memcpy(ref hddInfo, 60 * 2, BitConverter.GetBytes((UInt16)nbSectors), 0, 2);
            Utils.memcpy(ref hddInfo, 61 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 16)), 0, 2);
            Utils.memcpy(ref hddInfo, 62 * 2, BitConverter.GetBytes((UInt16)0x07), 0, 2); //single word dma0-2 supported
            Utils.memcpy(ref hddInfo, 63 * 2, BitConverter.GetBytes((UInt16)0x07), 0, 2); //mdma0-2 supported
            Utils.memcpy(ref hddInfo, 64 * 2, BitConverter.GetBytes((UInt16)0x03), 0, 2); //pio3-4 supported
            Utils.memcpy(ref hddInfo, 65 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            Utils.memcpy(ref hddInfo, 66 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            Utils.memcpy(ref hddInfo, 67 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            Utils.memcpy(ref hddInfo, 68 * 2, BitConverter.GetBytes((UInt16)120), 0, 2);
            //??
            //Many ??s
            Utils.memcpy(ref hddInfo, 80 * 2, BitConverter.GetBytes((UInt16)0xf0), 0, 2);
            Utils.memcpy(ref hddInfo, 81 * 2, BitConverter.GetBytes((UInt16)0x16), 0, 2);
            Utils.memcpy(ref hddInfo, 82 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 5) | 1)), 0, 2);
            Utils.memcpy(ref hddInfo, 83 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | (1 << 13) | (1 << 12) /*| (1 << 10)*/)), 0, 2); //48bit
            Utils.memcpy(ref hddInfo, 84 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | 0)), 0, 2); //no WWN
            Utils.memcpy(ref hddInfo, 85 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | 1)), 0, 2); //no WCACHE 
            Utils.memcpy(ref hddInfo, 86 * 2, BitConverter.GetBytes((UInt16)((1 << 13) | (1 << 12) /*| (1 << 10)*/)), 0, 2); //48bit
            Utils.memcpy(ref hddInfo, 87 * 2, BitConverter.GetBytes((UInt16)((1 << 14) | 0)), 0, 2); //no WWN
            Utils.memcpy(ref hddInfo, 88 * 2, BitConverter.GetBytes((UInt16)(0x3f | (1 << 13))), 0, 2); //udma5 set and supported
            //
            Utils.memcpy(ref hddInfo, 93 * 2, BitConverter.GetBytes((UInt16)(1 | (1 << 14) | 0x2000)), 0, 2);
            //
            Utils.memcpy(ref hddInfo, 100 * 2, BitConverter.GetBytes((UInt16)nbSectors), 0, 2);
            Utils.memcpy(ref hddInfo, 101 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 16)), 0, 2);
            Utils.memcpy(ref hddInfo, 102 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 32)), 0, 2);
            Utils.memcpy(ref hddInfo, 103 * 2, BitConverter.GetBytes((UInt16)(nbSectors >> 48)), 0, 2);
            //Many ??s
        }

        public void _ATAirqHandler()
        {
            //	dev9.intr_stat |= dev9.irq_cause;
            dev9.dev9Wu16((int)DEV9Header.SPD_R_INTR_STAT, (UInt16)dev9.irqcause);//dev9.intr_stat = dev9.irqcause;
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
