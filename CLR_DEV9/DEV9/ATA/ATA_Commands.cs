using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void ide_exec_cmd(UInt16 value)
        {
            bool compleate;

            //Check if CMD valid

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_BUSY;
            error = 0;
            //io_buffer_offset = 0;

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

        void ide_transfer_stop()
        {
            end_transfer_func = ide_transfer_stop;
            data_ptr = 0;
            data_end = 0;
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
        }

        bool HDDunk()
        {
            Log_Error("DEV9 HDD error : unknown cmd " + command.ToString("X"));

            status |= (byte)DEV9Header.ATA_STAT_ERR;
            return true;
        }

        bool HDDnop()
        {
            Log_Error("HDDnop");
            return true;
        }

        //TODO multi-Sector read support
        byte[] piosectorbuffer;
        int piobufferindex = -1;

        bool HDDreadPIO()
        {
            Log_Verb("HDDreadPIO");

            req_nb_sectors = 1;
            SectorRead();

            return false;
        }
        void SectorRead()
        {
            Log_Verb("SectorRead");
            //IDE sector read
            
            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK; //Set Ready
            error = 0;

            uint n = nsector;

            if (n == 0)
            {
                ide_transfer_stop();
                piobufferindex = -1;
                return;
            }

            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            if (n > req_nb_sectors)
            {
                n = (uint)req_nb_sectors;
            }

            //QEMU Dose async read here

            
            if (piobufferindex == -1)
            {
                if (HDDseek() == -1)
                {
                    //ide_rw_error
                    return;
                }
                piosectorbuffer = new byte[512 * n];
                piobufferindex = 0;
            }

            hddimage.Read(piosectorbuffer, 0, piosectorbuffer.Length);

            //IDE sector read cb
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));

            nsector -= n;

            //Set Next Sector (TODO)

            //Start Transfer
            //Sector size is 512b
            Utils.memcpy(ref pio_buffer, 0, piosectorbuffer, piobufferindex * 512, pio_buffer.Length);
            piobufferindex += 1;
            end_transfer_func = SectorRead;
            data_ptr = 0;
            data_end = 256;
            if ((status & DEV9Header.ATA_STAT_ERR) == 0)
            {
                status |= (byte)DEV9Header.ATA_STAT_DRQ;
            }

            if (sendIRQ) dev9.DEV9irq(1, 1);
            //end sector read
        }

        bool HDDsmart()
        {
            Log_Verb("HDDSmart");

            switch (feature)
            {
                case 0xD8:
                    smart_on = 1;
                    break;
                case 0xDA: //return status (change a reg if fault in disk)
                    break;
                default:
                    Log_Error("DEV9 : Unknown SMART command " + feature.ToString("X"));
                    break;
            }

            return true;
        }

        bool HDDreadDMA()
        {
            Log_Verb("HDDreadDMA");

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;
            //Ready set after DMA compleation
            // here do stuffs
            if (HDDseek() != 0)
            {
                return false;
            }

            return false;
        }

        bool HDDwriteDMA()
        {
            Log_Verb("HDDwriteDMA");

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;
            //Ready set after DMA compleation
            // here do stuffs
            if (HDDseek() != 0)
            {
                return false;
            }

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
            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            // Write cache not supported yet

            status &= unchecked((byte)~DEV9Header.ATA_STAT_BUSY);

            if (sendIRQ) dev9.DEV9irq(1, 1);

            status |= (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK;

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
            error = 0;

            //IDE transfer start
            Utils.memcpy(ref pio_buffer, 0, identify_data, 0, Math.Min(pio_buffer.Length, identify_data.Length));
            end_transfer_func = ide_transfer_stop;
            data_ptr = 0;
            data_end = 256;
            if ((status & DEV9Header.ATA_STAT_ERR) == 0)
            {
                status |= (byte)DEV9Header.ATA_STAT_DRQ;
            }

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

        bool HDDsceSecCtrl()
        {
            Log_Info("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND " + feature.ToString("X"));

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK; //Set Ready
            error = 0;

            Utils.memset(ref pio_buffer, 0, 0, pio_buffer.Length);
            end_transfer_func = ide_transfer_stop;
            data_ptr = 0;
            data_end = 256;

            if ((status & DEV9Header.ATA_STAT_ERR) == 0)
            {
                status |= (byte)DEV9Header.ATA_STAT_DRQ;
            }

            if (sendIRQ) dev9.DEV9irq(1, 0x6C);

            return false;
        }
    }
}
