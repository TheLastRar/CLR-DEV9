using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void IDE_ExecCmd(UInt16 value)
        {
            hddCmds[value]();
        }

        void IDE_CmdLBA48Transform(bool islba48)
        {
            lba48 = islba48;
            //TODO
            /* handle the 'magic' 0 nsector count conversion here. to avoid
             * fiddling with the rest of the read logic, we just store the
             * full sector count in ->nsector
             */
            if (!lba48)
            {
                if (regNsector == 0)
                {
                    nsector = 256;
                }
                else
                {
                    nsector = regNsector;
                }
            }
            else
            {
                if (regNsector == 0 & regNsectorHOB == 0)
                {
                    nsector = 65536;
                }
                else
                {
                    int lo = regNsector;
                    int hi = regNsectorHOB;

                    nsector = (hi << 8) | lo;
                }
            }
        }

        void HDD_Unk()
        {
            Log_Error("DEV9 HDD error : unknown cmd " + regCommand.ToString("X"));

            PreCmd();

            regError |= (byte)DEV9Header.ATA_ERR_ABORT;
            regStatus |= (byte)DEV9Header.ATA_STAT_ERR;
            PostCmdNoData();
        }

        bool PreCmd()
        {
            if ((regStatus & DEV9Header.ATA_STAT_READY) == 0)
            {
                //Ignore CMD write except for EXECUTE DEVICE DIAG and INITIALIZE DEVICE PARAMETERS
                return false;
            }
            regStatus |= (byte)DEV9Header.ATA_STAT_BUSY;

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_WRERR));
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_ERR));

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_SEEK));

            regError = 0;

            return true;
        }

        //OTHER FEATURE SETS BELOW (TODO)

        //CFA ERASE SECTORS
        //WRITE MULTIPLE
        //SET MULTIPLE

        //CFA WRITE MULTIPLE WITHOUT ERASE
        //GET MEDIA STATUS
        //MEDIA LOCK
        //MEDIA UNLOCK
        //STANDBY IMMEDIAYTE
        //IDLE IMMEDIATE
        //STANBY

        //CHECK POWER MODE
        //SLEEP

        //MEDIA EJECT

        //SECURITY SET PASSWORD
        //SECURITY UNLOCK
        //SECUTIRY ERASE PREPARE
        //SECURITY ERASE UNIT
        //SECURITY FREEZE LOCK
        //SECURITY DIABLE PASSWORD
        //READ NATIVE MAX ADDRESS
        //SET MAX ADDRESS
    }
}
