using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void HDDunk()
        {
            Log_Error("DEV9 HDD error : unknown cmd " + command.ToString("X"));

            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_ERR;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, DEV9Header.ATA_ERR_ABORT);
        }

        void HDDnop()
        {
            Log_Error("HDDnop");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDsmart()
        {
            Log_Verb("HDDSmart");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            switch (feature)
            {
                case 0xD8:
                    smart_on = 1;
                    break;
                case 0xDA: //return status (change a reg if fault in disk)
                    break;
                default:
                    Log_Error("DEV9 : Unknown SMART command " + feature.ToString("X"));
                    break;
            }

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDreadDMA()
        {
            Log_Verb("HDDreadDMA");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // here do stuffs

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //	triggerIRQ(1, 0x1000);

            //Differs from MegaDev9 (How did it work in MegaDev9?)
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDwriteDMA()
        {
            Log_Verb("HDDwriteDMA");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // here do stuffs

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //	triggerIRQ(1, 0x1000);
            //Differs from MegaDev9 (How did it work in MegaDev9?)
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDidle()
        {
            Log_Verb("HDDidle");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Very simple implementation, nothing ATM :P

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDflushCache()
        {
            Log_Verb("HDDflushCache");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Write cache not supported yet

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDflushCacheExt()
        {
            Log_Verb("HDDflushCacheExt");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Write cache not supported yet

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDidentifyDevice()
        {
            Log_Verb("HddidentifyDevice");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            Utils.memcpy(ref pio_buf, 0, hddInfo, 0, Math.Min(pio_buf.Length, hddInfo.Length));
            pio_count = 0;
            pio_size = 256;

            status |= (DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_READY);
            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //dev9.DEV9irq(2, 0x6C); //Changed from MegaDev9
            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDsetFeatures()
        {
            Log_Verb("HddsetFeatures");
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            switch (feature)
            {
                case 0x03:
                    xfer_mode = dev9.dev9Ru16((int)DEV9Header.ATA_R_NSECTOR);
                    break;
            }

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDsceSecCtrl()
        {
            Log_Info("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND " + feature.ToString("X"));

            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            Utils.memset(ref pio_buf, 0, 0, pio_buf.Length);
            pio_count = 0;
            pio_size = 256;

            status |= DEV9Header.ATA_STAT_DRQ;
            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //dev9.DEV9irq(2, 0x6C);  //Changed from MegaDev9
            dev9.DEV9irq(1, 1);

            //Differs from MegaDev9
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }
    }
}
