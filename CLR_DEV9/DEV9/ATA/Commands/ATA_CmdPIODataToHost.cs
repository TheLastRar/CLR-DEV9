using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State //PIO Data In (and out?)
    {
        Cmd pioDRQEndTransferFunc;

        void DRQCmdPIODataToHost(byte[] buf, int buffIndex, int size, bool sendIRQ)
        {
            //Data in PIO ready to be sent
            pioPtr = 0;
            pioEnd = size >> 1;

            Utils.memcpy(pioBuffer, 0, buf, buffIndex, Math.Min(size, buf.Length - buffIndex));

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
            regStatus |= (byte)DEV9Header.ATA_STAT_DRQ;

            if (regControlEnableIRQ & sendIRQ) dev9.DEV9irq(DEV9Header.ATA_INTR_INTRQ, 1); //0x6c cycles before
        }
        void PostCmdPIODataToHost()
        {
            pioPtr = 0;
            pioEnd = 0;
            //AnyMoreData?
            if (pioDRQEndTransferFunc != null)
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_BUSY;
                regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
                //Call cmd to retrive more data
                pioDRQEndTransferFunc();
            }
            else
            {
                regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
            }
        }

        //FromHost

        public UInt16 ATAreadPIO()
        {
            Log_Verb("*ATA_R_DATA 16bit read, pio_count " + pioPtr + " pio_size " + pioEnd);
            if (pioPtr < pioEnd)
            {
                UInt16 ret = BitConverter.ToUInt16(pioBuffer, pioPtr * 2);
                //ret = (UInt16)System.Net.IPAddress.HostToNetworkOrder((Int16)ret);
                Log_Verb("*ATA_R_DATA returned value is  " + ret.ToString("x"));
                pioPtr++;
                if (pioPtr >= pioEnd) //Fnished transfer (Changed from MegaDev9)
                {
                    PostCmdPIODataToHost();
                }
                return ret;
            }
            return 0xFF;
        }
        //ATAwritePIO

        void HDD_IdentifyDevice()
        {
            if (!PreCmd()) return;
            Log_Verb("HddidentifyDevice");

            //IDE transfer start
            CreateHDDinfo(DEV9Header.config.HddSize);

            pioDRQEndTransferFunc = null;
            DRQCmdPIODataToHost(identifyData, 0, 256 * 2, true);
        }

        //Read Buffer

        void HDD_ReadMultiple(bool isLBA48)
        {
            sectorsPerInterrupt = curMultipleSectorsSetting;
            HDD_ReadPIO(isLBA48);
        }

        void HDD_ReadSectors(bool isLBA48)
        {
            sectorsPerInterrupt = 1;
            HDD_ReadPIO(isLBA48);
        }

        int sectorsPerInterrupt;
        void HDD_ReadPIO(bool isLBA48)
        {
            //Log_Info("HDD_ReadPIO");
            if (!PreCmd()) return;

            if (sectorsPerInterrupt == 0)
            {
                CmdNoDataAbort();
                return;
            }

            IDE_CmdLBA48Transform(isLBA48);

            if (!HDD_CanSeek())
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ID;
                PostCmdNoData();
                return;
            }

            HDD_ReadSync(HDD_ReadPIOS2);
        }

        void HDD_ReadPIOS2()
        {
            //Log_Info("HDD_ReadPIO Stage 2");
            pioDRQEndTransferFunc = HDD_ReadPIOEndBlock;
            DRQCmdPIODataToHost(readBuffer, 0, 256 * 2, true);
        }

        void HDD_ReadPIOEndBlock()
        {
            //Log_Info("HDD_ReadPIO End Block");
            rdTransferred += 512;
            if (rdTransferred >= nsector * 512)
            {
                //Log_Info("HDD_ReadPIO Done");
                HDD_SetErrorAtTransferEnd();
                regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
                pioDRQEndTransferFunc = null;
                rdTransferred = 0;
            }
            else
            {
                if ((rdTransferred / 512) % sectorsPerInterrupt == 0)
                {
                    DRQCmdPIODataToHost(readBuffer, rdTransferred, 256 * 2, true);
                }
                else
                {
                    DRQCmdPIODataToHost(readBuffer, rdTransferred, 256 * 2, false);
                }
            }
        }

        //Write Buffer

        //Write Multiple

        //Write Sectors

        //Download Microcode (Used for FW updates)
    }
}
