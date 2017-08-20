
namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State //SCE
    {
        byte[] sceSec = new byte[256 * 2];
        void HDD_SCE()
        {
            Log_Info("DEV9 : SONY-SPECIFIC SECURITY CONTROL COMMAND " + regFeature.ToString("X"));

            switch (regFeature)
            {
                case 0xEC:
                    SCE_IDENTIFY_DRIVE();
                    break;
                default:
                    Log_Error("DEV9 : Unknown SCE command " + regFeature.ToString("X"));
                    CmdNoDataAbort();
                    return;
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

        void SCE_IDENTIFY_DRIVE()
        {
            PreCmd();

            //HDD_IdentifyDevice(); //Maybe?

            pioDRQEndTransferFunc = null;
            DRQCmdPIODataToHost(sceSec, 0, 256 * 2, true);
        }
    }
}
