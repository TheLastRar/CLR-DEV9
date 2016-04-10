using System;

namespace CLRDEV9.DEV9.ATA
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    partial class ATA_State
    {
        //cache
        void IDE_FlushCache()
        {
            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            IDE_FlushCB();
        }
        void IDE_FlushCB()
        {
            // Write cache not supported yet

            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK;
            if (sendIRQ) dev9.DEV9irq(1, 1);
        }
        //PIO
        void IDE_TransferStop()
        {
            endTransferFunc = IDE_TransferStop;
            dataPtr = 0;
            dataEnd = 0;
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
        }

        void IDE_TransferStart(byte[] buf, int buffIndex, int size, EndTransfer endFunc)
        {
            endTransferFunc = endFunc;
            dataPtr = 0;
            dataEnd = size >> 1;
            if ((status & DEV9Header.ATA_STAT_ERR) == 0)
            {
                status |= (byte)DEV9Header.ATA_STAT_DRQ;
            }

            Utils.memcpy(ref pioBuffer, 0, buf, buffIndex, Math.Min(size, buf.Length - buffIndex));
        }

        //TODO multi-Sector read support
        byte[] pioSectorBuffer;
        int pioBufferIndex = -1;
        void ide_sector_read()
        {
            Log_Verb("SectorRead");
            //IDE sector read

            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_SEEK; //Set Ready
            error = 0;

            int n = nsector;

            if (n == 0)
            {
                IDE_TransferStop();
                pioBufferIndex = -1;
                return;
            }

            status |= (byte)DEV9Header.ATA_STAT_BUSY;

            if (n > reqNbSectors)
            {
                n = reqNbSectors;
            }

            //QEMU Does async read here
            if (pioBufferIndex == -1)
            {
                if (HDD_Seek() == -1)
                {
                    //ide_rw_error
                    return;
                }
                pioSectorBuffer = new byte[512 * nsector];
                pioBufferIndex = 0;
                hddImage.Read(pioSectorBuffer, 0, pioSectorBuffer.Length);
            }

            IDE_SectorReadCB();
        }

        void IDE_SectorReadCB()
        {
            status &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));

            int n = nsector;
            if (n > reqNbSectors)
            {
                n = reqNbSectors;
            }

            //Set Next Sector (TODO)

            nsector -= n;

            //Start Transfer
            //Sector size is 512b
            IDE_TransferStart(pioSectorBuffer, pioBufferIndex * 512, n * 512, ide_sector_read);
            pioBufferIndex++;
            if (sendIRQ) dev9.DEV9irq(1, 1);
        }

        //DMA
        void IDE_StartDMA()
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

        void IDE_SectorStartDma(bool read)
        {
            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;

            if (HDD_Seek() != 0)
            {
                return;
            }

            IDE_StartDMA();
        }
    }
}
