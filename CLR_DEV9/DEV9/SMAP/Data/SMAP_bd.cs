using System;

namespace CLRDEV9.DEV9.SMAP.Data
{
    class SMAP_bd
    {
        int startOff = 0;
        byte[] baseData;
        public SMAP_bd(byte[] data, int startOffset)
        {
            baseData = data;
            startOff = startOffset;
        }
        public UInt16 CtrlStat
        {
            get
            {
                return BitConverter.ToUInt16(baseData, startOff);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref baseData, startOff, var, 0, var.Length);
            }
        }
        public UInt16 Reserved
        {
            get
            {
                return BitConverter.ToUInt16(baseData, startOff + 2);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref baseData, startOff + 2, var, 0, var.Length);
            }
        }
        public UInt16 Length
        {
            get
            {
                return BitConverter.ToUInt16(baseData, startOff + 4);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref baseData, startOff + 4, var, 0, var.Length);
            }
        }
        public UInt16 Pointer
        {
            get
            {
                return BitConverter.ToUInt16(baseData, startOff + 6);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref baseData, startOff + 6, var, 0, var.Length);
            }
        }

        public static int GetSize()
        {
            //4 16bit numbers
            return ((16 * 4) / 8);
        }
    }
}
