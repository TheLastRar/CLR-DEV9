using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        //cache
        void ide_flush_cache()
        {
            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            ide_flush_cb();
        }
        void ide_flush_cb()
        {
            // Write cache not supported yet

            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK;
            if (sendIRQ) dev9.DEV9irq(1, 1);
        }
        //PIO
        void ide_transfer_stop()
        {
            end_transfer_func = ide_transfer_stop;
            data_ptr = 0;
            data_end = 0;
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
        }

        void ide_transfer_start(byte[] buf, int buffIndex, int size, EndTransfer endFunc)
        {
            end_transfer_func = endFunc;
            data_ptr = 0;
            data_end = size >> 1;
            if ((status & DEV9Header.ATA_STAT_ERR) == 0)
            {
                status |= (byte)DEV9Header.ATA_STAT_DRQ;
            }

            Utils.memcpy(ref pio_buffer, 0, buf, buffIndex, Math.Min(size, buf.Length - buffIndex));
        }

        //TODO multi-Sector read support
        byte[] piosectorbuffer;
        int piobufferindex = -1;
        void ide_sector_read()
        {
            Log_Verb("SectorRead");
            //IDE sector read

            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK; //Set Ready
            error = 0;

            int n = nsector;

            if (n == 0)
            {
                ide_transfer_stop();
                piobufferindex = -1;
                return;
            }

            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            if (n > req_nb_sectors)
            {
                n = req_nb_sectors;
            }

            //QEMU Does async read here
            if (piobufferindex == -1)
            {
                if (HDDseek() == -1)
                {
                    //ide_rw_error
                    return;
                }
                piosectorbuffer = new byte[512 * nsector];
                piobufferindex = 0;
                hddimage.Read(piosectorbuffer, 0, piosectorbuffer.Length);
            }

            ide_sector_read_cb();
        }

        void ide_sector_read_cb()
        {
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));

            int n = nsector;
            if (n > req_nb_sectors)
            {
                n = req_nb_sectors;
            }

            //Set Next Sector (TODO)

            nsector -= n;

            //Start Transfer
            //Sector size is 512b
            ide_transfer_start(piosectorbuffer, piobufferindex * 512, n * 512, ide_sector_read);
            piobufferindex++;
            if (sendIRQ) dev9.DEV9irq(1, 1);
        }

        //DMA
        void ide_start_dma()
        {
            //dummy function
            //This would normally set dma_cb in qemu,
            //but for us it is hardcoded
            //as the ATAreadDMA8Mem/ATAwriteDMA8Mem
        }

        //void ide_dma_cb(int ret, bool read, System.IO.UnmanagedMemoryStream pMem, int size)
        //{
        //    int n = size >> 9;
        //    long sector_num;

        //    if (n > nsector)
        //    {
        //        n = nsector;
        //        //Expect another transfer
        //    }

        //    sector_num = HDDgetLBA();
        //    if (n > 0)
        //    {
        //        //dma commit??
        //        sector_num += n;
        //        HDDsetLBA(sector_num);
        //        nsector -= 1;
        //    }

        //    //end of transfer?
        //    if (nsector == 0)
        //    {
        //        status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK;
        //        if (sendIRQ) dev9.DEV9irq(3, 1);
        //        goto eot;
        //    }
        //    /* launch next transfer */
        //    n = nsector;

        //}

        void ide_sector_start_dma(bool read)
        {
            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;

            if (HDDseek() != 0)
            {
                return;
            }

            ide_start_dma();
        }
    }
}
