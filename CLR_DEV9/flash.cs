using System;

namespace CLR_DEV9
{
    static class flash
    {
        const int PAGE_SIZE_BITS = 9;
        const int PAGE_SIZE = (1 << PAGE_SIZE_BITS);
        const int ECC_SIZE = (16);
        const int PAGE_SIZE_ECC = (PAGE_SIZE + ECC_SIZE);
        const int BLOCK_SIZE = (16 * PAGE_SIZE);
        const int BLOCK_SIZE_ECC = (16 * PAGE_SIZE_ECC);
        const int CARD_SIZE = (1024 * BLOCK_SIZE);
        const int CARD_SIZE_ECC = (1024 * BLOCK_SIZE_ECC);

        static volatile UInt32 ctrl, address, id, counter, addrbyte;
        static volatile UInt32 cmd = unchecked((UInt32)(-1));
        static byte[] data = new byte[PAGE_SIZE_ECC], file = new byte[CARD_SIZE_ECC];

        static void calculateECC(byte[] page)
        {
            Utils.memset(ref page, PAGE_SIZE, 0x00, ECC_SIZE);
            xfromman_call20_calculateXors(page, 0 * (PAGE_SIZE >> 2), page, PAGE_SIZE + 0 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 1 * (PAGE_SIZE >> 2), page, PAGE_SIZE + 1 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 2 * (PAGE_SIZE >> 2), page, PAGE_SIZE + 2 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 3 * (PAGE_SIZE >> 2), page, PAGE_SIZE + 3 * 3);//(ECC_SIZE>>2));
        }

        static string getCmdName(UInt32 cmd)
        {
            switch (cmd)
            {
                case DEV9Header.SM_CMD_READ1: return "READ1";
                case DEV9Header.SM_CMD_READ2: return "READ2";
                case DEV9Header.SM_CMD_READ3: return "READ3";
                case DEV9Header.SM_CMD_RESET: return "RESET";
                case DEV9Header.SM_CMD_WRITEDATA: return "WRITEDATA";
                case DEV9Header.SM_CMD_PROGRAMPAGE: return "PROGRAMPAGE";
                case DEV9Header.SM_CMD_ERASEBLOCK: return "ERASEBLOCK";
                case DEV9Header.SM_CMD_ERASECONFIRM: return "ERASECONFIRM";
                case DEV9Header.SM_CMD_GETSTATUS: return "GETSTATUS";
                case DEV9Header.SM_CMD_READID: return "READID";
                default: return "unknown";
            }
        }

        static public void FLASHinit()
        {
            //FILE* fd;

            id = DEV9Header.FLASH_ID_64MBIT;
            counter = 0;
            addrbyte = 0;

            address = 0;
            Utils.memset(ref data, 0, 0xFF, PAGE_SIZE);
            calculateECC(data);
            ctrl = DEV9Header.FLASH_PP_READY;

            //if (fd = fopen("flash.dat", "rb"))
            //{
            //    fread(file, 1, CARD_SIZE_ECC, fd);
            //    fclose(fd);
            //}
            //else {
            Utils.memset(ref file, 0, 0xFF, CARD_SIZE_ECC);
            //}
        }

        public static UInt32 FLASHread32(UInt32 addr, int size)
        {
            UInt32 valueInt;
            UInt32 refill = 0;
            byte[] valueByte = new byte[4]; //allocate Uint32 space

            switch (addr)
            {
                case DEV9Header.FLASH_R_DATA:
                    Utils.memcpy(ref valueByte, 0, data, (int)counter, size);
                    counter += (uint)size;
                    DEV9.DEV9_LOG("*FLASH DATA " + (size * 8).ToString() + "bit read 0x" + BitConverter.ToUInt32(valueByte, 0).ToString("X8") + " " + (((ctrl & DEV9Header.FLASH_PP_READ) != 0) ? "READ_ENABLE" : "READ_DISABLE").ToString());
                    if (cmd == DEV9Header.SM_CMD_READ3)
                    {
                        if (counter >= PAGE_SIZE_ECC)
                        {
                            counter = PAGE_SIZE;
                            refill = 1;
                        }
                    }
                    else
                    {
                        if (((ctrl & DEV9Header.FLASH_PP_NOECC) != 0) && (counter >= PAGE_SIZE))
                        {
                            counter %= PAGE_SIZE;
                            refill = 1;
                        }
                        else
                            if (!((ctrl & DEV9Header.FLASH_PP_NOECC) != 0) && (counter >= PAGE_SIZE_ECC))
                            {
                                counter %= PAGE_SIZE_ECC;
                                refill = 1;
                            }
                    }

                    if (refill != 0)
                    {
                        unchecked
                        {
                            ctrl &= (uint)~DEV9Header.FLASH_PP_READY;
                        }
                        address += PAGE_SIZE;
                        address %= CARD_SIZE;
                        Utils.memcpy(ref data, 0, file, (int)((address >> PAGE_SIZE_BITS) * PAGE_SIZE_ECC), PAGE_SIZE);
                        calculateECC(data);	// calculate ECC; should be in the file already
                        ctrl |= DEV9Header.FLASH_PP_READY;
                    }

                    return BitConverter.ToUInt32(valueByte, 0);

                case DEV9Header.FLASH_R_CMD:
                    DEV9.DEV9_LOG("*FLASH CMD " + (size * 8).ToString() + "bit read " + getCmdName(cmd) + " DENIED\n");
                    return cmd;

                case DEV9Header.FLASH_R_ADDR:
                    DEV9.DEV9_LOG("*FLASH ADDR " + (size * 8).ToString() + "bit read DENIED\n");
                    return 0;

                case DEV9Header.FLASH_R_CTRL:
                    DEV9.DEV9_LOG("*FLASH CTRL " + (size * 8).ToString() + "bit read " + ctrl.ToString("X8"));
                    return ctrl;

                case DEV9Header.FLASH_R_ID:
                    if (cmd == DEV9Header.SM_CMD_READID)
                    {
                        DEV9.DEV9_LOG("*FLASH ID " + (size * 8).ToString() + "bit read " + id.ToString("X8"));
                        return id;//0x98=Toshiba/0xEC=Samsung maker code should be returned first
                    }
                    else
                        if (cmd == DEV9Header.SM_CMD_GETSTATUS)
                        {
                            valueInt = 0x80 | ((ctrl & 1) << 6);	// 0:0=pass, 6:ready/busy, 7:1=not protected
                            DEV9.DEV9_LOG("*FLASH STATUS " + (size * 8).ToString() + "bit read " + valueInt.ToString("X8"));
                            return valueInt;
                        }//else fall off
                    //dosn't like falling though...
                    DEV9.DEV9_LOG("*FLASH Unkwnown " + (size * 8).ToString() + "bit read at address " + addr.ToString("X8"));
                    return 0;
                default:
                    DEV9.DEV9_LOG("*FLASH Unkwnown " + (size * 8).ToString() + "bit read at address " + addr.ToString("X8"));
                    return 0;
            }
        }

        public static void FLASHwrite32(UInt32 addr, UInt32 value, int size)
        {
            switch (addr & 0x1FFFFFFF)
            {
                case DEV9Header.FLASH_R_DATA:

                    DEV9.DEV9_LOG("*FLASH DATA " + (size * 8).ToString("X8") + "bit write 0x" + value.ToString("X8") + " " + (((ctrl & DEV9Header.FLASH_PP_WRITE) != 0) ? "WRITE_ENABLE" : "WRITE_DISABLE"));
                    byte[] valueBytes = BitConverter.GetBytes(value);
                    Utils.memcpy(ref data, (int)counter, valueBytes, 0, size);
                    counter += (uint)size;
                    counter %= PAGE_SIZE_ECC;//should not get past the last byte, but at the end
                    break;

                case DEV9Header.FLASH_R_CMD:
                    if (!((ctrl & DEV9Header.FLASH_PP_READY) != 0))
                    {
                        if ((value != DEV9Header.SM_CMD_GETSTATUS) && (value != DEV9Header.SM_CMD_RESET))
                        {
                            DEV9.DEV9_LOG("*FLASH CMD " + (size * 8).ToString() + "bit write " + getCmdName(value) + " ILLEGAL in busy mode - IGNORED");
                            break;
                        }
                    }
                    if (cmd == DEV9Header.SM_CMD_WRITEDATA)
                    {
                        if ((value != DEV9Header.SM_CMD_PROGRAMPAGE) && (value != DEV9Header.SM_CMD_RESET))
                        {
                            DEV9.DEV9_LOG("*FLASH CMD " + (size * 8).ToString() + "bit write " + getCmdName(value) + " ILLEGAL after WRITEDATA cmd - IGNORED");
                            unchecked
                            {
                                ctrl &= (uint)(~DEV9Header.FLASH_PP_READY);//go busy, reset is needed
                            }
                            break;
                        }
                    }
                    DEV9.DEV9_LOG("*FLASH CMD " + (size * 8).ToString() + "bit write " + getCmdName(value));
                    switch (value)
                    {																	// A8 bit is encoded in READ cmd;)
                        case DEV9Header.SM_CMD_READ1: counter = 0; if (cmd != DEV9Header.SM_CMD_GETSTATUS) address = counter; addrbyte = 0; break;
                        case DEV9Header.SM_CMD_READ2: counter = PAGE_SIZE / 2; if (cmd != DEV9Header.SM_CMD_GETSTATUS) address = counter; addrbyte = 0; break;
                        case DEV9Header.SM_CMD_READ3: counter = PAGE_SIZE; if (cmd != DEV9Header.SM_CMD_GETSTATUS) address = counter; addrbyte = 0; break;
                        case DEV9Header.SM_CMD_RESET: FLASHinit(); break;
                        case DEV9Header.SM_CMD_WRITEDATA: counter = 0; address = counter; addrbyte = 0; break;
                        case DEV9Header.SM_CMD_ERASEBLOCK: counter = 0; Utils.memset(ref data, 0, 0xFF, PAGE_SIZE); address = counter; addrbyte = 1; break;
                        case DEV9Header.SM_CMD_PROGRAMPAGE:	//fall
                        case DEV9Header.SM_CMD_ERASECONFIRM:
                            unchecked
                            {
                                ctrl &= (uint)(~DEV9Header.FLASH_PP_READY);
                            }
                            calculateECC(data);
                            Utils.memcpy(ref file, (int)((address / PAGE_SIZE) * PAGE_SIZE_ECC), data, 0, PAGE_SIZE_ECC);
                            /*write2file*/
                            ctrl |= DEV9Header.FLASH_PP_READY; break;
                        case DEV9Header.SM_CMD_GETSTATUS: break;
                        case DEV9Header.SM_CMD_READID: counter = 0; address = counter; addrbyte = 0; break;
                        default:
                            unchecked
                            {
                                ctrl &= (uint)~DEV9Header.FLASH_PP_READY;
                            }
                            return;//ignore any other command; go busy, reset is needed
                    }
                    cmd = value;
                    break;

                case DEV9Header.FLASH_R_ADDR:
                    DEV9.DEV9_LOG("*FLASH ADDR " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8"));
                    address |= (uint)((int)(value & 0xFF) << (int)(addrbyte == 0 ? 0 : (1 + 8 * addrbyte)));
                    addrbyte++;
                    DEV9.DEV9_LOG("*FLASH ADDR = 0x" + address + " (addrbyte=" + addrbyte.ToString() + ")\n");
                    if (!((value & 0x100) != 0))
                    {	// address is complete
                        if ((cmd == DEV9Header.SM_CMD_READ1) || (cmd == DEV9Header.SM_CMD_READ2) || (cmd == DEV9Header.SM_CMD_READ3))
                        {
                            unchecked
                            {
                                ctrl &= (uint)~DEV9Header.FLASH_PP_READY;
                            }
                            Utils.memcpy(ref data, 0, file, (int)((address >> PAGE_SIZE_BITS) * PAGE_SIZE_ECC), PAGE_SIZE);
                            calculateECC(data);	// calculate ECC; should be in the file already
                            ctrl |= DEV9Header.FLASH_PP_READY;
                        }
                        addrbyte = 0;		// address reset
                        {
                            UInt32 bytes, pages, blocks;

                            blocks = address / BLOCK_SIZE;
                            pages = address - (blocks * BLOCK_SIZE);
                            bytes = pages % PAGE_SIZE;
                            pages = pages / PAGE_SIZE;
                            DEV9.DEV9_LOG("*FLASH ADDR = 0x" + address.ToString("X8") + " (" + blocks + ":" + pages + ":" + bytes + ") (addrbyte=" + addrbyte + ") FINAL");
                        }
                    }
                    break;

                case DEV9Header.FLASH_R_CTRL:
                    DEV9.DEV9_LOG("*FLASH CTRL " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8"));
                    ctrl = (uint)((ctrl & DEV9Header.FLASH_PP_READY) | (value & ~DEV9Header.FLASH_PP_READY));
                    break;

                case DEV9Header.FLASH_R_ID:
                    DEV9.DEV9_LOG("*FLASH ID " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8") + " DENIED :P");
                    break;

                default:
                    DEV9.DEV9_LOG("*FLASH Unkwnown " + (size * 8).ToString() + "bit write at address 0x" + addr.ToString("X8") + "= 0x" + value.ToString() + " IGNORED");
                    break;
            }
        }

        static byte[] xor_table = {
             0x00, 0x87, 0x96, 0x11, 0xA5, 0x22, 0x33, 0xB4, 0xB4, 0x33, 0x22, 0xA5, 0x11, 0x96, 0x87, 0x00,
             0xC3, 0x44, 0x55, 0xD2, 0x66, 0xE1, 0xF0, 0x77, 0x77, 0xF0, 0xE1, 0x66, 0xD2, 0x55, 0x44, 0xC3,
             0xD2, 0x55, 0x44, 0xC3, 0x77, 0xF0, 0xE1, 0x66, 0x66, 0xE1, 0xF0, 0x77, 0xC3, 0x44, 0x55, 0xD2,
             0x11, 0x96, 0x87, 0x00, 0xB4, 0x33, 0x22, 0xA5, 0xA5, 0x22, 0x33, 0xB4, 0x00, 0x87, 0x96, 0x11,
             0xE1, 0x66, 0x77, 0xF0, 0x44, 0xC3, 0xD2, 0x55, 0x55, 0xD2, 0xC3, 0x44, 0xF0, 0x77, 0x66, 0xE1,
             0x22, 0xA5, 0xB4, 0x33, 0x87, 0x00, 0x11, 0x96, 0x96, 0x11, 0x00, 0x87, 0x33, 0xB4, 0xA5, 0x22,
             0x33, 0xB4, 0xA5, 0x22, 0x96, 0x11, 0x00, 0x87, 0x87, 0x00, 0x11, 0x96, 0x22, 0xA5, 0xB4, 0x33,
             0xF0, 0x77, 0x66, 0xE1, 0x55, 0xD2, 0xC3, 0x44, 0x44, 0xC3, 0xD2, 0x55, 0xE1, 0x66, 0x77, 0xF0,
             0xF0, 0x77, 0x66, 0xE1, 0x55, 0xD2, 0xC3, 0x44, 0x44, 0xC3, 0xD2, 0x55, 0xE1, 0x66, 0x77, 0xF0,
             0x33, 0xB4, 0xA5, 0x22, 0x96, 0x11, 0x00, 0x87, 0x87, 0x00, 0x11, 0x96, 0x22, 0xA5, 0xB4, 0x33,
             0x22, 0xA5, 0xB4, 0x33, 0x87, 0x00, 0x11, 0x96, 0x96, 0x11, 0x00, 0x87, 0x33, 0xB4, 0xA5, 0x22,
             0xE1, 0x66, 0x77, 0xF0, 0x44, 0xC3, 0xD2, 0x55, 0x55, 0xD2, 0xC3, 0x44, 0xF0, 0x77, 0x66, 0xE1,
             0x11, 0x96, 0x87, 0x00, 0xB4, 0x33, 0x22, 0xA5, 0xA5, 0x22, 0x33, 0xB4, 0x00, 0x87, 0x96, 0x11,
             0xD2, 0x55, 0x44, 0xC3, 0x77, 0xF0, 0xE1, 0x66, 0x66, 0xE1, 0xF0, 0x77, 0xC3, 0x44, 0x55, 0xD2,
             0xC3, 0x44, 0x55, 0xD2, 0x66, 0xE1, 0xF0, 0x77, 0x77, 0xF0, 0xE1, 0x66, 0xD2, 0x55, 0x44, 0xC3,
             0x00, 0x87, 0x96, 0x11, 0xA5, 0x22, 0x33, 0xB4, 0xB4, 0x33, 0x22, 0xA5, 0x11, 0x96, 0x87, 0x00};

        static void xfromman_call20_calculateXors(byte[] buffer, int dataoffset, byte[] xor, int xoroffset)
        {
            byte a = 0, b = 0, c = 0;
            int i;

            for (i = dataoffset; i < 128 + dataoffset; i++)
            {
                a ^= xor_table[buffer[i]]; //xor
                if ((xor_table[buffer[i]] & 0x80) != 0)
                {
                    b ^= (byte)(~(i - dataoffset));
                    c ^= (byte)(i - dataoffset);
                }
            }

            xor[xoroffset + 0] = (byte)((~a) & 0x77);
            xor[xoroffset + 1] = (byte)((~b) & 0x7F);
            xor[xoroffset + 2] = (byte)((~c) & 0x7F);
        }
    }
}
