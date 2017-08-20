using System;

namespace CLRDEV9.DEV9.SPEED
{
    class SPEED_Header
    {
        public const uint SPD_REGBASE = DEV9Header.SPD_REGBASE;

        public const uint SPD_R_REV = (SPD_REGBASE + 0x00);
        public const uint SPD_R_REV_1 = (SPD_REGBASE + 0x02);
        public const UInt16 SPD_CAPS_SMAP = (1 << 0);
        public const UInt16 SPD_CAPS_ATA = (1 << 1);
        public const UInt16 SPD_CAPS_UART = (1 << 3);
        public const UInt16 SPD_CAPS_DVR = (1 << 4);
        public const UInt16 SPD_CAPS_FLASH = (1 << 5);
        public const uint SPD_R_REV_3 = (SPD_REGBASE + 0x04);
        public const uint SPD_R_0e = (SPD_REGBASE + 0x0e);

        public const uint SPD_R_DMA_CTRL = (SPD_REGBASE + 0x24);
        public const uint SPD_R_INTR_STAT = (SPD_REGBASE + 0x28);
        public const uint SPD_R_INTR_MASK = (SPD_REGBASE + 0x2a);

        //PP and EEPROM regs

        public const uint SPD_R_XFR_CTRL = (SPD_REGBASE + 0x32);
        public const uint SPD_R_38 = (SPD_REGBASE + 0x38);
        public const UInt16 SPD_R_38_AVAIL_MASK = 0x1F;
        public const uint SPD_R_IF_CTRL = (SPD_REGBASE + 0x64);
        public const uint SPD_IF_ATA_RESET = 0x80;
        public const uint SPD_IF_DMA_ENABLE = 0x04;
        public const uint SPD_R_PIO_MODE = (SPD_REGBASE + 0x70);
        public const uint SPD_R_MWDMA_MODE = (SPD_REGBASE + 0x72);
        public const uint SPD_R_UDMA_MODE = (SPD_REGBASE + 0x74);
    }
}
