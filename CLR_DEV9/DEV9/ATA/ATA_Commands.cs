using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void ide_exec_cmd(UInt16 value)
        {
            bool compleate;

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_BUSY;
            error = 0;

            compleate = HDDcmds[value]();
            if (compleate)
            {
                status &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
                //assert(!!s->error == !!(s->status & ERR_STAT));

                if ((HDDcmdDoesSeek[value]) && !(error != 0))
                {
                    status |= (byte)DEV9Header.ATA_STAT_SEEK;
                }

                //ide_cmd_done(s);
                if (sendIRQ) dev9.DEV9irq(1, 1);
            }
        }

        void ide_cmd_lba48_transform(bool islba48)
        {
            lba48 = islba48;
            //TODO
            /* handle the 'magic' 0 nsector count conversion here. to avoid
             * fiddling with the rest of the read logic, we just store the
             * full sector count in ->nsector and ignore ->hob_nsector from now
             */
            if (!lba48)
            {
                if (nsector == 0)
                {
                    nsector = 256;
                }
            }
            else
            {
                if (nsector == 0 && hob_nsector == 0)
                {
                    nsector = 65536;
                }
                else
                {
                    int lo = nsector;
                    int hi = hob_nsector;

                    nsector = (hi << 8) | lo;
                }
            }
        }

        void ide_abort_command()
        {
            ide_transfer_stop();
            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_ERR;
            error = (byte)DEV9Header.ATA_ERR_ABORT;
        }

        bool HDDunk()
        {
            Log_Error("DEV9 HDD error : unknown cmd " + command.ToString("X"));

            ide_abort_command();
            return true;
        }

        bool HDDnop()
        {
            Log_Error("HDDnop");
            return true;
        }

        bool HDDreadPIO(bool islba48)
        {
            Log_Verb("HDDreadPIO");

            ide_cmd_lba48_transform(islba48);
            req_nb_sectors = 1;
            ide_sector_read();

            return false;
        }

        bool HDDsmart()
        {
            Log_Verb("HDDSmart");

            if (hcyl != 0xC2 || lcyl != 0x4F)
            {
                HDDsmartFail();
                return true;
            }

            if (smart_enabled && feature != 0xD8)
            {
                HDDsmartFail();
                return true;
            }

            switch (feature)
            {
                case 0xD9: //SMART_DISABLE
                    smart_enabled = false;
                    return true;
                case 0xD8: //SMART_ENABLE
                    smart_enabled = true;
                    return true;
                case 0xD2: //SMART_ATTR_AUTOSAVE
                    switch (sector)
                    {
                        case 0x00:
                            smart_autosave = false;
                            break;
                        case 0xF1:
                            smart_autosave = true;
                            break;
                        default:
                            Log_Error("DEV9 : Unknown SMART_ATTR_AUTOSAVE command " + sector.ToString("X"));
                            HDDsmartFail();
                            return true;
                    }
                    return true;
                case 0xDA: //SMART_STATUS (is fault in disk?)
                    if (smart_errors)
                    {
                        hcyl = 0xC2;
                        lcyl = 0x4F;
                    }
                    else
                    {
                        hcyl = 0x2C;
                        lcyl = 0xF4;
                    }
                    return true;
                case 0xD1: //SMART_READ_THRESH
                    Log_Error("DEV9 : SMART_READ_THRESH Not Impemented");
                    HDDsmartFail();
                    return true;
                case 0xD0: //SMART_READ_DATA
                    Log_Error("DEV9 : SMART_READ_DATA Not Impemented");
                    HDDsmartFail();
                    return true;
                case 0xD5: //SMART_READ_LOG
                    Log_Error("DEV9 : SMART_READ_LOG Not Impemented");
                    HDDsmartFail();
                    return true;
                case 0xD4: //SMART_EXECUTE_OFFLINE
                    switch (sector)
                    {
                        case 0: /* off-line routine */
                        case 1: /* short self test */
                        case 2: /* extended self test */
                            smart_selftest_count++;
                            if (smart_selftest_count > 21)
                            {
                                smart_selftest_count = 1;
                            }
                            int n = 2 + (smart_selftest_count - 1) * 24;
                            //s->smart_selftest_data[n] = s->sector;
                            //s->smart_selftest_data[n + 1] = 0x00; /* OK and finished */
                            //s->smart_selftest_data[n + 2] = 0x34; /* hour count lsb */
                            //s->smart_selftest_data[n + 3] = 0x12; /* hour count msb */
                            break;
                        default:
                            HDDsmartFail();
                            return true;
                    }
                    return true;
                default:
                    Log_Error("DEV9 : Unknown SMART command " + feature.ToString("X"));
                    HDDsmartFail();
                    return true;
            }
        }

        void HDDsmartFail()
        {
            ide_transfer_stop();
        }

        bool HDDreadDMA(bool islba48)
        {
            Log_Verb("HDDreadDMA");

            ide_cmd_lba48_transform(islba48);
            ide_sector_start_dma(true);

            return false;
        }

        bool HDDwriteDMA(bool islba48)
        {
            Log_Verb("HDDwriteDMA");

            ide_cmd_lba48_transform(islba48);
            ide_sector_start_dma(false);
            //status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;
            ////Ready set after DMA compleation
            //// here do stuffs
            //if (HDDseek() != 0)
            //{
            //    return false;
            //}

            return false;
        }

        bool HDDidle()
        {
            Log_Verb("HDDidle");

            // Very simple implementation, nothing ATM :P
            nsector = 0xff;

            return true;
        }

        bool HDDflushCache()
        {
            Log_Verb("HDDflushCache");

            ide_flush_cache();

            return false;
        }

        bool HDDflushCacheExt()
        {
            Log_Verb("HDDflushCacheExt");
            return HDDflushCache();
        }

        bool HDDidentifyDevice()
        {
            Log_Verb("HddidentifyDevice");

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK; //Set Ready

            //IDE transfer start
            ide_transfer_start(identify_data, 0, 256 * 2, ide_transfer_stop);

            if (sendIRQ) dev9.DEV9irq(1, 0x6C);

            return false;
        }

        bool HDDsetFeatures()
        {
            Log_Verb("HddsetFeatures");


            switch (feature)
            {
                case 0x03:
                    xfer_mode = (UInt16)nsector; //Set Transfer mode
                    switch ((xfer_mode & 0x07) >> 3)
                    {
                        case 0x00:
                        case 0x01:
                        case 0x02:
                        case 0x04:
                        case 0x08:
                            break;
                    }
                    break;
            }

            return true;
        }

        byte[] sceSec = new byte[256 * 2];

        bool HDDsceSecCtrl()
        {
            Log_Info("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND " + feature.ToString("X"));

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK; //Set Ready

            ide_transfer_start(sceSec, 0, 256 * 2, ide_transfer_stop);

            if (sendIRQ) dev9.DEV9irq(1, 0x6C);

            return false;
        }
    }
}
