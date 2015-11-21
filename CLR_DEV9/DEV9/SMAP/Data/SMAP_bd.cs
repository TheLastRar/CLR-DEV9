using System;

namespace CLRDEV9.DEV9.SMAP.Data
{
    class SMAP_bd
    {
        int _startoff = 0;
        byte[] basedata;
        public SMAP_bd(byte[] data, int startoffset)
        {
            basedata = data;
            _startoff = startoffset;
        }
        public UInt16 ctrl_stat
        {
            get
            {
                return BitConverter.ToUInt16(basedata, _startoff);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref basedata, _startoff, var, 0, var.Length);
            }
        }
        public UInt16 reserved
        {
            get
            {
                return BitConverter.ToUInt16(basedata, _startoff + 2);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref basedata, _startoff + 2, var, 0, var.Length);
            }
        }
        public UInt16 length
        {
            get
            {
                return BitConverter.ToUInt16(basedata, _startoff + 4);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref basedata, _startoff + 4, var, 0, var.Length);
            }
        }
        public UInt16 pointer
        {
            get
            {
                return BitConverter.ToUInt16(basedata, _startoff + 6);
            }
            set
            {
                byte[] var = BitConverter.GetBytes(value);
                Utils.memcpy(ref basedata, _startoff + 6, var, 0, var.Length);
            }
        }

        public static int GetSize()
        {
            //4 16bit numbers
            return ((16 * 4) / 8);
        }
    }
}
