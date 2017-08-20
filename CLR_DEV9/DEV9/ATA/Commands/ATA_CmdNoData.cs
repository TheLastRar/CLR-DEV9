using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void PostCmdNoData()
        {
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));

            if (regControlEnableIRQ) dev9.DEV9irq((int)DEV9Header.ATA_INTR_INTRQ, 1);
        }

        void CmdNoDataAbort()
        {
            PreCmd();

            regError |= (byte)DEV9Header.ATA_ERR_ABORT;
            regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
            PostCmdNoData();
        }

        //GENRAL FEATURE SET

        void HDD_FlushCache() //Can't when DRQ set
        {
            if (!PreCmd()) return;
            Log_Verb("HDD_FlushCache");

            awaitFlush = true;
            ManageAsync();
        }

        void HDD_InitDevParameters()
        {
            PreCmd(); //Ignore DRDY bit
            Log_Info("HDD_InitDevParameters");

            curSectors = regNsector;
            curHeads = (UInt16)((regSelect & 0x7) + 1);
            PostCmdNoData();
        }

        void HDD_ReadVerifySectors(bool isLBA48)
        {
            if (!PreCmd()) return;
            Log_Verb("HDD_ReadVerifySectors");

            IDE_CmdLBA48Transform(isLBA48);

            int sectors = nsector;
            HDD_CanAssessOrSetError();

            PostCmdNoData();
        }

        void HDD_SeekCmd()
        {
            if (!PreCmd()) return;
            Log_Info("HDD_SeekCmd");

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_SEEK));

            if (HDD_CanSeek())
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
                regError |= (byte)DEV9Header.ATA_ERR_ID;
            }
            else
            {
                regStatus |= (byte)DEV9Header.ATA_STAT_SEEK;
            }

            PostCmdNoData();
        }

        void HDD_SetFeatures()
        {
            if (!PreCmd()) return;
            Log_Verb("HDD_SetFeatures");

            switch (regFeature)
            {
                case 0x02:
                    fetWriteCacheEnabled = true;
                    break;
                case 0x82:
                    fetWriteCacheEnabled = false;
                    awaitFlush = true; //Flush Cache
                    return;
                case 0x03: //Set transfer mode
                    UInt16 xferMode = (UInt16)regNsector; //Set Transfer mode

                    int mode = xferMode & 0x07;
                    switch ((xferMode) >> 3)
                    {
                        case 0x00: //pio default
                            //if mode = 1, disable IORDY
                            Log_Error("PIO Default");
                            pioMode = 4;
                            sdmaMode = -1;
                            mdmaMode = -1;
                            udmaMode = -1;
                            break;
                        case 0x01: //pio mode (3,4)
                            Log_Error("PIO Mode " + mode);
                            pioMode = mode;
                            sdmaMode = -1;
                            mdmaMode = -1;
                            udmaMode = -1;
                            break;
                        case 0x02: //Single word dma mode (0,1,2)
                            Log_Error("SDMA Mode " + mode);
                            //pioMode = -1;
                            sdmaMode = mode;
                            mdmaMode = -1;
                            udmaMode = -1;
                            break;
                        case 0x04: //Multi word dma mode (0,1,2)
                            Log_Error("MDMA Mode " + mode);
                            //pioMode = -1;
                            sdmaMode = -1;
                            mdmaMode = mode;
                            udmaMode = -1;
                            break;
                        case 0x08: //Ulta dma mode (0,1,2,3,4,5,6)
                            Log_Error("UDMA Mode " + mode);
                            //pioMode = -1;
                            sdmaMode = -1;
                            mdmaMode = -1;
                            udmaMode = mode;
                            break;
                        default:
                            Log_Error("Unkown transfer mode");
                            CmdNoDataAbort();
                            break;
                    }
                    break;
            }
            PostCmdNoData();
        }

        void HDD_SetMultipleMode()
        {
            if (!PreCmd()) return;
            Log_Info("HDD_SetMultipleMode");

            curMultipleSectorsSetting = regNsector;

            PostCmdNoData();
        }

        void HDD_Nop()
        {
            if (!PreCmd()) return;

            Log_Info("HDD_Nop");

            if (regFeature == 0)
            {
                //This would abort queues if the
                //PS2 HDD supported them.
            }
            //Always ends in error
            regError |= (byte)DEV9Header.ATA_ERR_ABORT;
            regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
            PostCmdNoData();
        }

        //Other Feature Sets

        void HDD_Idle()
        {
            if (!PreCmd()) return;

            Log_Verb("HDD_Idle");

            long idleTime = 0; //in seconds
            if (regNsector >= 1 & regNsector <= 240)
            {
                idleTime = 5 * regNsector;
            }
            else if (regNsector >= 241 & regNsector <= 251)
            {
                idleTime = 30 * (regNsector - 240) * 60;
            }
            else
            {
                switch (regNsector)
                {
                    case 0:
                        idleTime = 0;
                        break;
                    case 252:
                        idleTime = 21 * 60;
                        break;
                    case 253: //bettween 8 and 12 hrs
                        idleTime = 10 * 60 * 60;
                        break;
                    case 254: //reserved
                        idleTime = -1;
                        break;
                    case 255:
                        idleTime = 21 * 60 + 15;
                        break;
                }
            }

            Log_Verb("HDD_Idle for " + idleTime.ToString() + "s");
            PostCmdNoData();
        }
    }
}
