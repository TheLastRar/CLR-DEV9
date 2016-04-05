using System;
using System.Diagnostics;

namespace CLRDEV9.DEV9.FLASH
{
    partial class FLASH_State
    {
        public FLASH_State()
        {
            FLASHinit();
        }

        void CalculateECC(byte[] page)
        {
            Utils.memset(ref page, FLASH_Constants.PAGE_SIZE, 0x00,
                FLASH_Constants.ECC_SIZE);

            xfromman_call20_calculateXors(page, 0 * (FLASH_Constants.PAGE_SIZE >> 2), page,
                FLASH_Constants.PAGE_SIZE + 0 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 1 * (FLASH_Constants.PAGE_SIZE >> 2), page,
                FLASH_Constants.PAGE_SIZE + 1 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 2 * (FLASH_Constants.PAGE_SIZE >> 2), page,
                FLASH_Constants.PAGE_SIZE + 2 * 3);//(ECC_SIZE>>2));
            xfromman_call20_calculateXors(page, 3 * (FLASH_Constants.PAGE_SIZE >> 2), page,
                FLASH_Constants.PAGE_SIZE + 3 * 3);//(ECC_SIZE>>2));
        }

        string GetCmdName(UInt32 cmd)
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

        public void FLASHinit()
        {
            //FILE* fd;

            id = DEV9Header.FLASH_ID_64MBIT;
            counter = 0;
            addrbyte = 0;

            address = 0;
            Utils.memset(ref data, 0, 0xFF, FLASH_Constants.PAGE_SIZE);
            CalculateECC(data);
            ctrl = DEV9Header.FLASH_PP_READY;

            //if (fd = fopen("flash.dat", "rb"))
            //{
            //    fread(file, 1, CARD_SIZE_ECC, fd);
            //    fclose(fd);
            //}
            //else {
            Utils.memset(ref file, 0, 0xFF, FLASH_Constants.CARD_SIZE_ECC);
            //}
        }

        public UInt32 FLASHread(UInt32 addr, int size)
        {
            UInt32 valueInt;
            UInt32 refill = 0;
            byte[] valueByte = new byte[4]; //allocate Uint32 space

            switch (addr)
            {
                case DEV9Header.FLASH_R_DATA:
                    Utils.memcpy(ref valueByte, 0, data, (int)counter, size);
                    counter += (uint)size;

                    Log_Verb("*FLASH DATA " + (size * 8).ToString() + "bit read 0x" + BitConverter.ToUInt32(valueByte, 0).ToString("X8") + " " + (((ctrl & DEV9Header.FLASH_PP_READ) != 0) ? "READ_ENABLE" : "READ_DISABLE").ToString());
                    if (cmd == DEV9Header.SM_CMD_READ3)
                    {
                        if (counter >= FLASH_Constants.PAGE_SIZE_ECC)
                        {
                            counter = FLASH_Constants.PAGE_SIZE;
                            refill = 1;
                        }
                    }
                    else
                    {
                        if (((ctrl & DEV9Header.FLASH_PP_NOECC) != 0) &&
                            (counter >= FLASH_Constants.PAGE_SIZE))
                        {
                            counter %= FLASH_Constants.PAGE_SIZE;
                            refill = 1;
                        }
                        else
                            if (!((ctrl & DEV9Header.FLASH_PP_NOECC) != 0) &&
                                (counter >= FLASH_Constants.PAGE_SIZE_ECC))
                        {
                            counter %= FLASH_Constants.PAGE_SIZE_ECC;
                            refill = 1;
                        }
                    }
                    if (refill != 0)
                    {
                        unchecked
                        {
                            ctrl &= (uint)~DEV9Header.FLASH_PP_READY;
                        }
                        address += FLASH_Constants.PAGE_SIZE;
                        address %= FLASH_Constants.CARD_SIZE;
                        Utils.memcpy(ref data, 0, file, (int)((address >> FLASH_Constants.PAGE_SIZE_BITS) * FLASH_Constants.PAGE_SIZE_ECC), FLASH_Constants.PAGE_SIZE);
                        CalculateECC(data);	//calculate ECC; should be in the file already
                        ctrl |= DEV9Header.FLASH_PP_READY;
                    }

                    return BitConverter.ToUInt32(valueByte, 0);

                case DEV9Header.FLASH_R_CMD:
                    Log_Error("*FLASH CMD " + (size * 8).ToString() + "bit read " + GetCmdName(cmd) + " DENIED\n");
                    return cmd;

                case DEV9Header.FLASH_R_ADDR:
                    Log_Error("*FLASH ADDR " + (size * 8).ToString() + "bit read DENIED\n");
                    return 0;

                case DEV9Header.FLASH_R_CTRL:
                    Log_Verb("*FLASH CTRL " + (size * 8).ToString() + "bit read " + ctrl.ToString("X8"));
                    return ctrl;

                case DEV9Header.FLASH_R_ID:
                    if (cmd == DEV9Header.SM_CMD_READID)
                    {
                        Log_Verb("*FLASH ID " + (size * 8).ToString() + "bit read " + id.ToString("X8"));
                        return id; //0x98=Toshiba/0xEC=Samsung maker code should be returned first
                    }
                    else
                        if (cmd == DEV9Header.SM_CMD_GETSTATUS)
                    {
                        valueInt = 0x80 | ((ctrl & 1) << 6); //0:0=pass, 6:ready/busy, 7:1=not protected
                        Log_Verb("*FLASH STATUS " + (size * 8).ToString() + "bit read " + valueInt.ToString("X8"));
                        return valueInt;
                    } //else fall off
                      //dosn't like falling though...
                    Log_Error("*FLASH Unkwnown " + (size * 8).ToString() + "bit read at address " + addr.ToString("X8"));
                    return 0;
                default:
                    Log_Error("*FLASH Unkwnown " + (size * 8).ToString() + "bit read at address " + addr.ToString("X8"));
                    return 0;
            }
        }

        public void FLASHwrite(UInt32 addr, UInt32 value, int size)
        {
            switch (addr & 0x1FFFFFFF)
            {
                case DEV9Header.FLASH_R_DATA:

                    Log_Verb("*FLASH DATA " + (size * 8).ToString("X8") + "bit write 0x" + value.ToString("X8") + " " + (((ctrl & DEV9Header.FLASH_PP_WRITE) != 0) ? "WRITE_ENABLE" : "WRITE_DISABLE"));
                    byte[] valueBytes = BitConverter.GetBytes(value);
                    Utils.memcpy(ref data, (int)counter, valueBytes, 0, size);
                    counter += (uint)size;
                    counter %= FLASH_Constants.PAGE_SIZE_ECC; //should not get past the last byte, but at the end
                    break;

                case DEV9Header.FLASH_R_CMD:
                    if (!((ctrl & DEV9Header.FLASH_PP_READY) != 0))
                    {
                        if ((value != DEV9Header.SM_CMD_GETSTATUS) && (value != DEV9Header.SM_CMD_RESET))
                        {
                            Log_Error("*FLASH CMD " + (size * 8).ToString() + "bit write " + GetCmdName(value) + " ILLEGAL in busy mode - IGNORED");
                            break;
                        }
                    }
                    if (cmd == DEV9Header.SM_CMD_WRITEDATA)
                    {
                        if ((value != DEV9Header.SM_CMD_PROGRAMPAGE) && (value != DEV9Header.SM_CMD_RESET))
                        {
                            Log_Error("*FLASH CMD " + (size * 8).ToString() + "bit write " + GetCmdName(value) + " ILLEGAL after WRITEDATA cmd - IGNORED");
                            unchecked
                            {
                                ctrl &= ~DEV9Header.FLASH_PP_READY; //go busy, reset is needed
                            }
                            break;
                        }
                    }
                    Log_Info("*FLASH CMD " + (size * 8).ToString() + "bit write " + GetCmdName(value));
                    switch (value) //A8 bit is encoded in READ cmd;)
                    {
                        case DEV9Header.SM_CMD_READ1:
                            counter = 0;
                            if (cmd != DEV9Header.SM_CMD_GETSTATUS)
                            {
                                address = counter;
                            }
                            addrbyte = 0;
                            break;
                        case DEV9Header.SM_CMD_READ2:
                            counter = FLASH_Constants.PAGE_SIZE / 2;
                            if (cmd != DEV9Header.SM_CMD_GETSTATUS)
                            {
                                address = counter;
                            }
                            addrbyte = 0;
                            break;
                        case DEV9Header.SM_CMD_READ3:
                            counter = FLASH_Constants.PAGE_SIZE;
                            if (cmd != DEV9Header.SM_CMD_GETSTATUS)
                            {
                                address = counter;
                            }
                            addrbyte = 0;
                            break;
                        case DEV9Header.SM_CMD_RESET:
                            FLASHinit();
                            break;
                        case DEV9Header.SM_CMD_WRITEDATA:
                            counter = 0;
                            address = counter;
                            addrbyte = 0;
                            break;
                        case DEV9Header.SM_CMD_ERASEBLOCK:
                            counter = 0;
                            Utils.memset(ref data, 0, 0xFF, FLASH_Constants.PAGE_SIZE);
                            address = counter;
                            addrbyte = 1;
                            break;
                        case DEV9Header.SM_CMD_PROGRAMPAGE:	//fall
                        case DEV9Header.SM_CMD_ERASECONFIRM:
                            unchecked
                            {
                                ctrl &= ~DEV9Header.FLASH_PP_READY;
                            }
                            CalculateECC(data);
                            Utils.memcpy(ref file, (int)((address / FLASH_Constants.PAGE_SIZE) * FLASH_Constants.PAGE_SIZE_ECC), data, 0, FLASH_Constants.PAGE_SIZE_ECC);
                            /*write2file*/
                            ctrl |= DEV9Header.FLASH_PP_READY;
                            break;
                        case DEV9Header.SM_CMD_GETSTATUS: break;
                        case DEV9Header.SM_CMD_READID: counter = 0; address = counter; addrbyte = 0; break;
                        default:
                            unchecked
                            {
                                ctrl &= ~DEV9Header.FLASH_PP_READY;
                            }
                            return; //ignore any other command; go busy, reset is needed
                    }
                    cmd = value;
                    break;

                case DEV9Header.FLASH_R_ADDR:
                    Log_Verb("*FLASH ADDR " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8"));
                    address |= (uint)((int)(value & 0xFF) << (int)(addrbyte == 0 ? 0 : (1 + 8 * addrbyte)));
                    addrbyte++;
                    Log_Verb("*FLASH ADDR = 0x" + address + " (addrbyte=" + addrbyte.ToString() + ")\n");
                    if (!((value & 0x100) != 0))
                    {	//address is complete
                        if ((cmd == DEV9Header.SM_CMD_READ1) || (cmd == DEV9Header.SM_CMD_READ2) || (cmd == DEV9Header.SM_CMD_READ3))
                        {
                            unchecked
                            {
                                ctrl &= ~DEV9Header.FLASH_PP_READY;
                            }
                            Utils.memcpy(ref data, 0, file,
                                (int)((address >> FLASH_Constants.PAGE_SIZE_BITS) * FLASH_Constants.PAGE_SIZE_ECC),
                                FLASH_Constants.PAGE_SIZE);
                            CalculateECC(data);	//calculate ECC; should be in the file already
                            ctrl |= DEV9Header.FLASH_PP_READY;
                        }
                        addrbyte = 0; //address reset
                        {
                            UInt32 bytes, pages, blocks;

                            blocks = address / FLASH_Constants.BLOCK_SIZE;
                            pages = address - (blocks * FLASH_Constants.BLOCK_SIZE);
                            bytes = pages % FLASH_Constants.PAGE_SIZE;
                            pages = pages / FLASH_Constants.PAGE_SIZE;
                            Log_Verb("*FLASH ADDR = 0x" + address.ToString("X8") + " (" + blocks + ":" + pages + ":" + bytes + ") (addrbyte=" + addrbyte + ") FINAL");
                        }
                    }
                    break;

                case DEV9Header.FLASH_R_CTRL:
                    Log_Verb("*FLASH CTRL " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8"));
                    ctrl = (ctrl & DEV9Header.FLASH_PP_READY) | (value & ~DEV9Header.FLASH_PP_READY);
                    break;

                case DEV9Header.FLASH_R_ID:
                    Log_Error("*FLASH ID " + (size * 8).ToString() + "bit write 0x" + value.ToString("X8") + " DENIED :P");
                    break;

                default:
                    Log_Error("*FLASH Unkwnown " + (size * 8).ToString() + "bit write at address 0x" + addr.ToString("X8") + "= 0x" + value.ToString() + " IGNORED");
                    break;
            }
        }

        void xfromman_call20_calculateXors(byte[] buffer, int dataOffset, byte[] xor, int xorOffset)
        {
            byte a = 0, b = 0, c = 0;
            int i;

            for (i = dataOffset; i < 128 + dataOffset; i++)
            {
                a ^= FLASH_Constants.XOR_TABLE[buffer[i]]; //xor
                if ((FLASH_Constants.XOR_TABLE[buffer[i]] & 0x80) != 0)
                {
                    b ^= (byte)(~(i - dataOffset));
                    c ^= (byte)(i - dataOffset);
                }
            }

            xor[xorOffset + 0] = (byte)((~a) & 0x77);
            xor[xorOffset + 1] = (byte)((~b) & 0x7F);
            xor[xorOffset + 2] = (byte)((~c) & 0x7F);
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.FLASH, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.FLASH, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.FLASH, str);
        }
    }
}
