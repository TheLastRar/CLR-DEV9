using System;

namespace CLRDEV9.DEV9.SPEED
{
    partial class SPEED_State
    {
        public UInt16 regIntStat;
        public UInt16 regIntrMask;
        const UInt16 regRev1 = 0x0011; //v17
        //Rev3
        //Rev8
        UInt16 regDMACtrl;
        //PIO Regs
        UInt16 regXFRCtrl;
        //reg38 seems to store the amount of sectors that can be transferred (up to 0x1F)
        //This would give 31 sectors or 15.5KB of data (based on PS2SDK)
        //A value is written to this reg (0x3, specifily) by both homebrew and official titles
        //no idea why, maybe it's a min amount of data before the ATA1 IRQ gets set (maybe)
        UInt16 reg38;
        public UInt16 regIFCtrl;
        UInt16 regPIOMode;
        UInt16 regMDMAMode;
        UInt16 regUDMAMode;
    }
}
