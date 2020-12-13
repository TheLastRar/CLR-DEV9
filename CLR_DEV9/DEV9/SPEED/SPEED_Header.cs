using System;

namespace CLRDEV9.DEV9.SPEED
{
    class SPEED_Header
    {
        public const UInt16 SPD_INTR_ATA_FIFO_DATA = (1 << 1);
        public const UInt16 SPD_INTR_ATA_FIFO_FULL = (1 << 15); //Or Error/underflow/overfolw(?)
        public const UInt16 SPD_INTR_ATA_FIFO_EMPTY = (1 << 14);
        public const UInt16 SPD_INTR_ATA_FIFO_OVERFLOW = (SPD_INTR_ATA_FIFO_FULL | SPD_INTR_ATA_FIFO_EMPTY); //by HDD only?

        public const uint SPD_REGBASE = DEV9Header.SPD_REGBASE;

        public const uint SPD_R_REV_1 = (SPD_REGBASE + 0x00);
        public const uint SPD_R_REV_2 = (SPD_REGBASE + 0x02);
        public const UInt16 SPD_CAPS_SMAP = (1 << 0);
        public const UInt16 SPD_CAPS_ATA = (1 << 1);
        public const UInt16 SPD_CAPS_UART = (1 << 3);
        public const UInt16 SPD_CAPS_DVR = (1 << 4);
        public const UInt16 SPD_CAPS_FLASH = (1 << 5);
        public const uint SPD_R_REV_3 = (SPD_REGBASE + 0x04);
        public const uint SPD_R_0e = (SPD_REGBASE + 0x0e);

        public const uint SPD_R_DMA_CTRL = (SPD_REGBASE + 0x24);
        public const UInt16 SPD_DMA_TO_SMAP = (1 << 0);
        public const UInt16 SPD_DMA_FASTEST = (1 << 1);
        public const UInt16 SPD_DMA_WIDE = (1 << 2);
        public const UInt16 SPD_DMA_PAUSE = (1 << 4); //Pause SPEED->IOP DMA, by keeping DREQ inactive
        public const uint SPD_R_INTR_STAT = (SPD_REGBASE + 0x28);
        public const uint SPD_R_INTR_MASK = (SPD_REGBASE + 0x2a);

        //PP and EEPROM regs

        public const uint SPD_R_XFR_CTRL = (SPD_REGBASE + 0x32); //ATA only?
        public const UInt16 SPD_XFR_WRITE = (1 << 0);
        public const UInt16 SPD_XFR_DMAEN = (1 << 1);
        public const uint SPD_R_DBUF_STAT = (SPD_REGBASE + 0x38);
        //Read
        public const byte SPD_DBUF_AVAIL_MAX = 0x10;
        public const UInt16 SPD_DBUF_AVAIL_MASK = 0x1F;
        public const UInt16 SPD_DBUF_STAT_1 = (1 << 5); //HDD->SPEED: Buffer has free space,                        IOP->SPEED: Buffer is completely empty
        public const UInt16 SPD_DBUF_STAT_2 = (1 << 6); //HDD->SPEED: Buffer is completely empty, no data written   IOP->SPEED: Buffer has data
        public const UInt16 SPD_DBUF_STAT_FULL = (1 << 7);
        //HDD->SPEED: both SPD_DBUF_STAT_2 and SPD_DBUF_STAT_FULL set to 1 indicates overflow
        //Write
        public const UInt16 SPD_DBUF_RESET_SOMETHING = (1 << 1);
        public const UInt16 SPD_DBUF_RESET_FIFO = (1 << 1); //Set SPD_DBUF_STAT_1 & SPD_DBUF_STAT_2, SPD_DBUF_AVAIL set to 0

        public const uint SPD_R_IF_CTRL = (SPD_REGBASE + 0x64);
        public const UInt16 SPD_IF_UDMA = (1 << 0);
        public const UInt16 SPD_IF_READ = (1 << 1);
        public const UInt16 SPD_IF_ATA_DMAEN = (1 << 2); //Allow HDD<->SPEED DMA, auto cleared on trasfer end
        public const UInt16 SPD_IF_HDD_RESET = (1 << 6); //HDD Hard Reset, 0=act.low/1=inact.high.
        public const UInt16 SPD_IF_ATA_RESET = (1 << 7); //Reset ATA Interface

        public const uint SPD_R_PIO_MODE = (SPD_REGBASE + 0x70);
        public const uint SPD_R_MWDMA_MODE = (SPD_REGBASE + 0x72);
        public const uint SPD_R_UDMA_MODE = (SPD_REGBASE + 0x74);
    }
}
