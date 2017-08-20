using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        void PreCmdExecuteDeviceDiag()
        {
            regStatus |= (byte)DEV9Header.ATA_STAT_BUSY;
            regStatus &= unchecked((byte)~DEV9Header.ATA_STAT_READY);
            dev9.spd.regIntStat &= unchecked((UInt16)~DEV9Header.ATA_INTR_INTRQ);
            dev9.spd.regIntStat &= unchecked((UInt16)~DEV9Header.ATA_INTR_1); //Is this correct?
        }

        void PostCmdExecuteDeviceDiag()
        {
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_BUSY));
            regStatus |= (byte)DEV9Header.ATA_STAT_READY;

            regSelectDev = 0;

            if (regControlEnableIRQ) dev9.DEV9irq((int)DEV9Header.ATA_INTR_INTRQ, 1);
        }

        //GENRAL FEATURE SET

        void HDD_ExecuteDeviceDiag()
        {
            PreCmdExecuteDeviceDiag();
            //Perform Self Diag
            Log_Error("ExecuteDeviceDiag");
            //Would check both drives, but the PS2 would only have 1
            regError &= unchecked((byte)(~DEV9Header.ATA_ERR_ICRC));
            //Passed self-Diag
            regError = (byte)(0x01 & (regError & DEV9Header.ATA_ERR_ICRC));

            regNsector = 1;
            regSector = 1;
            regLcyl = 0;
            regHcyl = 0;

            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_DRQ));
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_ECC));
            regStatus &= unchecked((byte)(~DEV9Header.ATA_STAT_ERR));

            PostCmdExecuteDeviceDiag();
        }
    }
}
