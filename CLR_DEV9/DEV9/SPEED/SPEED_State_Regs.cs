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
        #region 'Bits'
        public bool dmaSMAP
        {
            get { return ((regDMACtrl & SPEED_Header.SPD_DMA_TO_SMAP) != 0); }
            set
            {
                if (value) { regDMACtrl |= SPEED_Header.SPD_DMA_TO_SMAP; }
                else { regDMACtrl &= unchecked((UInt16)(~SPEED_Header.SPD_DMA_TO_SMAP)); }
            }
        }
        #endregion
        //PIO Regs
        UInt16 regXFRCtrl; //ATA only (IOP<->SPEED)?
        #region 'Bits'
        bool xfrWrite
        {
            get { return ((regXFRCtrl & SPEED_Header.SPD_XFR_WRITE) != 0); }
            set
            {
                if (value) { regXFRCtrl |= SPEED_Header.SPD_XFR_WRITE; }
                else { regXFRCtrl &= unchecked((UInt16)(~SPEED_Header.SPD_XFR_WRITE)); }
            }
        }
        bool xfrDMAEN
        {
            get { return ((regXFRCtrl & SPEED_Header.SPD_XFR_DMAEN) != 0); }
            set
            {
                if (value) { regXFRCtrl |= SPEED_Header.SPD_XFR_DMAEN; }
                else { regXFRCtrl &= unchecked((UInt16)(~SPEED_Header.SPD_XFR_DMAEN)); }
            }
        }
        #endregion
        public UInt16 regIFCtrl; //ATA only (HDD<->SPEED)?
        #region 'Bits'
        bool ifRead
        {
            get { return ((regIFCtrl & SPEED_Header.SPD_IF_READ) != 0); }
            set
            {
                if (value) { regIFCtrl |= SPEED_Header.SPD_IF_READ; }
                else { regIFCtrl &= unchecked((UInt16)(~SPEED_Header.SPD_IF_READ)); }
            }
        }
        bool ifDMAEN
        {
            get { return ((regIFCtrl & SPEED_Header.SPD_IF_ATA_DMAEN) != 0); }
            set
            {
                if (value) { regIFCtrl |= SPEED_Header.SPD_IF_ATA_DMAEN; }
                else { regIFCtrl &= unchecked((UInt16)(~SPEED_Header.SPD_IF_ATA_DMAEN)); }
            }
        }
        #endregion

        UInt16 regPIOMode;
        UInt16 regMDMAMode;
        UInt16 regUDMAMode;

        int bytesReadFIFO = 0;
        int bytesWriteFIFO = 0;

    }
}
