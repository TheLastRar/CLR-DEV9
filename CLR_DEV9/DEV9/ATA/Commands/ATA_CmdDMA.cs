using System;
using System.IO;
using CLRDEV9.DEV9.SPEED;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void DRQCmdDMADataToHost()
        {
            //Log_Info("HDD_ReadDMA Stage 2");
            //Ready to Start DMA
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
            regStatus |= (byte)DEV9Header.ATA_STAT_DRQ;
            dmaReady = true;
            dev9.DEV9irq(SPEED_Header.SPD_INTR_ATA_FIFO_DATA, 1);
            //PCSX2 will Start DMA
        }
        void PostCmdDMADataToHost()
        {
            //Log_Info("HDD_ReadDMA Done");
            //readBuffer = null;
            nsectorLeft = 0;

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
            dmaReady = false;

            dev9.spd.regIntStat &= unchecked((UInt16)~(SPEED_Header.SPD_INTR_ATA_FIFO_DATA));
            if (regControlEnableIRQ) dev9.DEV9irq(DEV9Header.ATA_INTR_INTRQ, 1);
            //PCSX2 Will Start DMA
        }

        void DRQCmdDMADataFromHost()
        {
            //Log_Verb("WriteDMA Stage 2");
            //Ready to Start DMA
            if (!HDD_CanAssessOrSetError()) return;

            nsectorLeft = nsector;
            currentWrite = new byte[nsector * 512];
            currentWriteSectors = HDD_GetLBA();

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
            regStatus |= (byte)DEV9Header.ATA_STAT_DRQ;
            dmaReady = true;
            dev9.DEV9irq(SPEED_Header.SPD_INTR_ATA_FIFO_DATA, 1);
            //PCSX2 will Start DMA
        }
        void PostCmdDMADataFromHost()
        {
            WriteCache.Enqueue(currentWrite);
            WriteCacheSectors.Enqueue(currentWriteSectors);
            currentWrite = null;
            currentWriteSectors = 0;
            nsectorLeft = 0;

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
            dmaReady = false;

            dev9.spd.regIntStat &= unchecked((UInt16)~(SPEED_Header.SPD_INTR_ATA_FIFO_DATA));

            if (fetWriteCacheEnabled)
            {
                regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
                if (regControlEnableIRQ) dev9.DEV9irq(DEV9Header.ATA_INTR_INTRQ, 1); //0x6C
            }
            else
            {
                awaitFlush = true;
            }

            ManageAsync();
        }

        public void ATAreadDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if ((udmaMode >= 0) &&
                (dev9.spd.regIFCtrl & SPEED.SPEED_Header.SPD_IF_ATA_DMAEN) != 0)
            {
                if (size == 0)
                    return;
                //size >>= 1;
                Log_Verb("DMA read, size " + size + ", transferred " + rdTransferred + ", total size " + nsector * 512);
                Log_Info("rATA");

                //read
                pMem.Write(readBuffer, rdTransferred, size);

                rdTransferred += size;
                nsectorLeft -= size / 512;

                if (rdTransferred >= nsector * 512)
                {
                    HDD_SetErrorAtTransferEnd();

                    nsector = 0;
                    rdTransferred = 0;
                    PostCmdDMADataToHost();
                    //dev9.Dev9Wu16((int)DEV9Header.SPD_R_IF_CTRL, (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & ~DEV9Header.SPD_IF_DMA_ENABLE));
                }
            }
        }
        public void ATAwriteDMA8Mem(UnmanagedMemoryStream pMem, int size)
        {
            if ((udmaMode >= 0) &&
                (dev9.spd.regIFCtrl & SPEED.SPEED_Header.SPD_IF_ATA_DMAEN) != 0)
            {
                //size >>= 1;
                Log_Verb("DEV9 : DMA write, size " + size + ", transferred " + wrTransferred + ", total size " + nsector * 512);
                Log_Info("wATA");

                //write
                pMem.Read(currentWrite, wrTransferred, size);

                wrTransferred += size;
                nsectorLeft -= size / 512;

                if (wrTransferred >= nsector * 512)
                {
                    HDD_SetErrorAtTransferEnd();

                    nsector = 0;
                    wrTransferred = 0;
                    PostCmdDMADataFromHost();
                    //dev9.Dev9Wu16((int)DEV9Header.SPD_R_IF_CTRL, (UInt16)(dev9.Dev9Ru16((int)DEV9Header.SPD_R_IF_CTRL) & ~DEV9Header.SPD_IF_DMA_ENABLE));
                }
            }
        }

        //GENRAL FEATURE SET

        void HDD_ReadDMA(bool isLBA48)
        {
            if (!PreCmd()) return;
            Log_Verb("HDD_ReadDMA");

            IDE_CmdLBA48Transform(isLBA48);

            if (!HDD_CanSeek())
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ID;
                PostCmdNoData();
                return;
            }

            //Do Sync Read
            HDD_ReadSync(DRQCmdDMADataToHost);
        }

        void HDD_WriteDMA(bool isLBA48)
        {
            if (!PreCmd()) return;
            Log_Verb("HDD_WriteDMA");

            IDE_CmdLBA48Transform(isLBA48);

            if (!HDD_CanSeek())
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ID;
                PostCmdNoData();
                return;
            }

            //Do Async write
            DRQCmdDMADataFromHost();
        }
    }
}
