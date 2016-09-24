using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void IDE_ExecCmd(UInt16 value)
        {
            bool compleate;

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_BUSY;
            error = 0;

            compleate = hddCmds[value]();
            if (compleate)
            {
                status &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
                //assert(!!s->error == !!(s->status & ERR_STAT));

                if ((hddCmdDoesSeek[value]) && !(error != 0))
                {
                    status |= (byte)DEV9Header.ATA_STAT_SEEK;
                }

                //ide_cmd_done(s);
                if (sendIRQ) dev9.DEV9irq(1, 1);
            }
        }

        void IDE_CmdLBA48Transform(bool islba48)
        {
            lba48 = islba48;
            //TODO
            /* handle the 'magic' 0 nsector count conversion here. to avoid
             * fiddling with the rest of the read logic, we just store the
             * full sector count in ->nsector and ignore ->hob_nsector from now
             */
            if (!lba48)
            {
                if (nsector == 0)
                {
                    nsector = 256;
                }
            }
            else
            {
                if (nsector == 0 && hobNsector == 0)
                {
                    nsector = 65536;
                }
                else
                {
                    int lo = nsector;
                    int hi = hobNsector;

                    nsector = (hi << 8) | lo;
                }
            }
        }

        void IDE_AbortCommand()
        {
            IDE_TransferStop();
            status = (byte)DEV9Header.ATA_STAT_READY | (byte)DEV9Header.ATA_STAT_ERR;
            error = (byte)DEV9Header.ATA_ERR_ABORT;
        }

        bool HDD_Unk()
        {
            Log_Error("DEV9 HDD error : unknown cmd " + command.ToString("X"));

            IDE_AbortCommand();
            return true;
        }

        bool HDD_Nop()
        {
            Log_Error("HDDnop");
            return true;
        }

        //CFA REQUEST EXTENDED ERROR CODE
        //DEV RESET (for PACKET supporting devices only)
        //READ SECTOR
        //READ DMA EXT
        //WRITE SECTOR
        //WRITE LONG
        //WRITE DME EXT
        //CFA WRITE SECTORS WITHOUT ERASE
        //READ VERIFY SECTOR
        //READ VERUFY SECTOR EXT
        //SEEK
        //CFA TRANSLATE SECTOR
        //EXECUTE DEV DIAG

        bool HDD_InitDevParameters()
        {
            Log_Info("HDDinitDevParameters");
            curSectors = (UInt16)nsector;
            curHeads = (UInt16)((select & 0x7) + 1);
            return true;
        }

        //DOWNLOAD MICROCODE
        //IDENTIFY PACKET DEVICE

        bool HDD_Smart()
        {
            Log_Verb("HDDSmart");

            if (hcyl != 0xC2 || lcyl != 0x4F)
            {
                HDD_SmartFail();
                return true;
            }

            if (!fetSmartEnabled && feature != 0xD8)
            {
                HDD_SmartFail();
                return true;
            }

            switch (feature)
            {
                case 0xD9: //SMART_DISABLE
                    fetSmartEnabled = false;
                    return true;
                case 0xD8: //SMART_ENABLE
                    fetSmartEnabled = true;
                    return true;
                case 0xD2: //SMART_ATTR_AUTOSAVE
                    switch (sector)
                    {
                        case 0x00:
                            smartAutosave = false;
                            break;
                        case 0xF1:
                            smartAutosave = true;
                            break;
                        default:
                            Log_Error("DEV9 : Unknown SMART_ATTR_AUTOSAVE command " + sector.ToString("X"));
                            HDD_SmartFail();
                            return true;
                    }
                    return true;
                case 0xDA: //SMART_STATUS (is fault in disk?)
                    if (!smartErrors)
                    {
                        hcyl = 0xC2;
                        lcyl = 0x4F;
                    }
                    else
                    {
                        hcyl = 0x2C;
                        lcyl = 0xF4;
                    }
                    return true;
                case 0xD1: //SMART_READ_THRESH
                    Log_Error("DEV9 : SMART_READ_THRESH Not Impemented");
                    HDD_SmartFail();
                    return true;
                case 0xD0: //SMART_READ_DATA
                    Log_Error("DEV9 : SMART_READ_DATA Not Impemented");
                    HDD_SmartFail();
                    return true;
                case 0xD5: //SMART_READ_LOG
                    Log_Error("DEV9 : SMART_READ_LOG Not Impemented");
                    HDD_SmartFail();
                    return true;
                case 0xD4: //SMART_EXECUTE_OFFLINE
                    switch (sector)
                    {
                        case 0: /* off-line routine */
                        case 1: /* short self test */
                        case 2: /* extended self test */
                            smartSelfTestCount++;
                            if (smartSelfTestCount > 21)
                            {
                                smartSelfTestCount = 1;
                            }
                            int n = 2 + (smartSelfTestCount - 1) * 24;
                            //s->smart_selftest_data[n] = s->sector;
                            //s->smart_selftest_data[n + 1] = 0x00; /* OK and finished */
                            //s->smart_selftest_data[n + 2] = 0x34; /* hour count lsb */
                            //s->smart_selftest_data[n + 3] = 0x12; /* hour count msb */
                            break;
                        default:
                            HDD_SmartFail();
                            return true;
                    }
                    return true;
                default:
                    Log_Error("DEV9 : Unknown SMART command " + feature.ToString("X"));
                    HDD_SmartFail();
                    return true;
            }
        }
        void HDD_SmartFail()
        {
            IDE_TransferStop();
        }

        //CFA ERASE SECTORS
        //READ MULTIPLE
        //WRITE MULTIPLE
        //SET MULTIPLE

        bool HDD_ReadDMA(bool isLBA48)
        {
            Log_Verb("HDDreadDMA");

            IDE_CmdLBA48Transform(isLBA48);
            IDE_SectorStartDma(true);

            return false;
        }

        bool HDD_WriteDMA(bool isLBA48)
        {
            Log_Verb("HDDwriteDMA");

            IDE_CmdLBA48Transform(isLBA48);
            IDE_SectorStartDma(false);
            //status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_BUSY;
            ////Ready set after DMA compleation
            //// here do stuffs
            //if (HDDseek() != 0)
            //{
            //    return false;
            //}

            return false;
        }

        //CFA WRITE MULTIPLE WITHOUT ERASE
        //GET MEDIA STATUS
        //MEDIA LOCK
        //MEDIA UNLOCK
        //STANDBY IMMEDIAYTE
        //IDLE IMMEDIATE
        //STANBY

        bool HDD_Idle()
        {
            Log_Verb("HDDidle");

            // Very simple implementation, nothing ATM :P
            nsector = 0xff;

            return true;
        }

        //READ BUFFER
        //CHECK POWER MODE
        //SLEEP

        bool HDD_FlushCache()
        {
            Log_Verb("HDDflushCache");

            IDE_FlushCache();

            return false;
        }

        //WRITE BUFFER

        bool HDD_FlushCacheExt()
        {
            Log_Verb("HDDflushCacheExt");
            return HDD_FlushCache();
        }

        bool HDD_IdentifyDevice()
        {
            Log_Verb("HddidentifyDevice");

            status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK; //Set Ready

            //IDE transfer start
            CreateHDDinfo(DEV9Header.config.HddSize);
            IDE_TransferStart(identifyData, 0, 256 * 2, IDE_TransferStop);

            if (sendIRQ) dev9.DEV9irq(1, 0x6C);

            return false;
        }

        //MEDIA EJECT

        bool HDD_SetFeatures()
        {
            Log_Verb("HDDsetFeatures");

            switch (feature)
            {
                case 0x03: // set transfer mode
                    xferMode = (UInt16)nsector; //Set Transfer mode

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
                            pioMode = -1;
                            sdmaMode = mode;
                            mdmaMode = -1;
                            udmaMode = -1;
                            break;
                        case 0x04: //Multi word dma mode (0,1,2)
                            Log_Error("MDMA Mode " + mode);
                            pioMode = -1;
                            sdmaMode = -1;
                            mdmaMode = mode;
                            udmaMode = -1;
                            break;
                        case 0x08: //Ulta dma mode (0,1,2,3,4,5,6)
                            Log_Error("UDMA Mode " + mode);
                            pioMode = -1;
                            sdmaMode = -1;
                            mdmaMode = -1;
                            udmaMode = mode;
                            break;
                        default:
                            Log_Error("Unkown transfer mode");
                            IDE_AbortCommand();
                            break;

                    }
                    break;
            }

            return true;
        }

        //SECURITY SET PASSWORD
        //SECURITY UNLOCK
        //SECUTIRY ERASE PREPARE
        //SECURITY ERASE UNIT
        //SECURITY FREEZE LOCK
        //SECURITY DIABLE PASSWORD
        //READ NATIVE MAX ADDRESS
        //SET MAX ADDRESS

        bool HDD_ReadPIO(bool isLBA48)
        {
            Log_Verb("HDDreadPIO");

            IDE_CmdLBA48Transform(isLBA48);
            reqNbSectors = 1;
            ide_sector_read();

            return false;
        }

        byte[] sceSec = new byte[256 * 2];
        bool HDD_sceSecCtrl()
        {
            Log_Info("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND " + feature.ToString("X"));

            switch (feature)
            {
                case 0xEC:
                    status = DEV9Header.ATA_STAT_READY | DEV9Header.ATA_STAT_SEEK; //Set Ready

                    IDE_TransferStart(sceSec, 0, 256 * 2, IDE_TransferStop);

                    if (sendIRQ) dev9.DEV9irq(1, 0x6C);
                    //HDD_IdentifyDevice(); //Maybe?
                    return false;
                default:
                    Log_Error("DEV9 : Unknown SCE command " + feature.ToString("X"));
                    IDE_AbortCommand();
                    return false;
            }
        }
        //Has 
        //ATA_SCE_IDENTIFY_DRIVE @ 0xEC

        //ATA_SCE_SECURITY_ERASE_PREPARE @ 0xF1
        //ATA_SCE_SECURITY_ERASE_UNIT
        //ATA_SCE_SECURITY_FREEZE_LOCK
        //ATA_SCE_SECURITY_SET_PASSWORD
        //ATA_SCE_SECURITY_UNLOCK

        //ATA_SCE_SECURITY_WRITE_ID @ 0x20
        //ATA_SCE_SECURITY_READ_ID @ 0x30
    }
}
