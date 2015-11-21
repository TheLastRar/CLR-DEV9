using System;

namespace CLRDEV9.DEV9
{
    partial class DEV9_State
    {
        //TODO Move to constants
        static byte[] inital_eeprom = {
	        //0x6D, 0x76, 0x63, 0x61, 0x31, 0x30, 0x08, 0x01
	        0x76, 0x6D, 0x61, 0x63, 0x30, 0x31, 0x07, 0x02,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        public byte[] dev9R = new byte[0x10000]; //changed to unsigned
        public byte eeprom_state;
        public byte eeprom_command;
        public byte eeprom_address;
        public byte eeprom_bit;
        public byte eeprom_dir;
        public ushort[] eeprom;//[32];

        public UInt32 rxbdi;
        public byte[] rxfifo = new byte[16 * 1024];
        public UInt16 rxfifo_wr_ptr;

        public UInt32 txbdi;
        public byte[] txfifo = new byte[16 * 1024];
        public UInt16 txfifo_rd_ptr;

        public byte bd_swap;
        public UInt16[] atabuf = new UInt16[1024];
        //public UInt32 atacount;
        //public UInt32 atasize;
        public UInt16[] phyregs = new UInt16[32];
        public int irqcause;
        //public byte atacmd;
        //public UInt32 atasector;
        //public UInt32 atansector;

        public void dev9_rxfifo_write(byte x) { rxfifo[rxfifo_wr_ptr++] = x; }

        //public static sbyte dev9Rs8(int mem)	{return dev9.dev9R[mem & 0xffff];}
        //#define dev9Rs16(int mem)	{return dev9.dev9R[mem & 0xffff];}
        //#define dev9Rs32(mem)	(*(s32*)&dev9.dev9R[(mem) & 0xffff])
        public byte dev9Ru8(int mem) { return dev9R[mem & 0xffff]; }
        public UInt16 dev9Ru16(int mem) { return BitConverter.ToUInt16(dev9R, (mem) & 0xffff); }
        public UInt32 dev9Ru32(int mem) { return BitConverter.ToUInt32(dev9R, (mem) & 0xffff); }

        public void dev9Wu8(int mem, byte value) { dev9R[mem & 0xffff] = value; }
        public void dev9Wu16(int mem, UInt16 value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Utils.memcpy(ref dev9R, (mem & 0xffff), tmp, 0, tmp.Length);
        }
        public void dev9Wu32(int mem, UInt32 value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Utils.memcpy(ref dev9R, (mem & 0xffff), tmp, 0, tmp.Length);
        }
    }
}
