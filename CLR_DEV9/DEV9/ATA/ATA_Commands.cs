using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void HDDunk()
        {
            Console.Error.WriteLine("DEV9 HDD error : unknown cmd %02X\n", command);

            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_ERR;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, DEV9Header.ATA_ERR_ABORT);
        }

        void HDDnop()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_BUSY | DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR));
            status |= DEV9Header.ATA_STAT_READY;
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);
        }

        void HDDsmart()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            switch (feature)
            {
                case 0xD8:
                    smart_on = 1;
                    break;

                default:
                    Console.Error.WriteLine("DEV9 : Unknown SMART command %02X\n", feature);
                    break;
            }

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDreadDMA()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // here do stuffs

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //	triggerIRQ(1, 0x1000);
        }

        void HDDwriteDMA()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // here do stuffs

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            //	triggerIRQ(1, 0x1000);
        }

        void HDDidle()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Very simple implementation, nothing ATM :P

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDflushCache()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Write cache not supported yet

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDflushCacheExt()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            // Write cache not supported yet

            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDidentifyDevice()
        {
            dev9.dev9Wu16((int)DEV9Header.ATA_R_ERROR, 0);
            UInt16 status = dev9.dev9Ru16((int)DEV9Header.ATA_R_STATUS);
            status &= unchecked((UInt16)~(DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_ERR | DEV9Header.ATA_STAT_READY));
            status |= DEV9Header.ATA_STAT_BUSY;

            Utils.memcpy(ref pio_buf, 0, hddInfo, 0, pio_buf.Length);
            pio_count = 0;
            pio_size = 256;

            status |= (DEV9Header.ATA_STAT_DRQ | DEV9Header.ATA_STAT_READY);
            status &= unchecked((UInt16)~DEV9Header.ATA_STAT_BUSY);
            dev9.dev9Wu16((int)DEV9Header.ATA_R_STATUS, status);

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDsetFeatures()
        {
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

            dev9.DEV9irq(1, 0x6C);
        }

        void HDDsceSecCtrl()
        {
            Console.Error.WriteLine("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND (%02X)\n", feature);

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

            dev9.DEV9irq(1, 0x6C);
        }


    }
}
