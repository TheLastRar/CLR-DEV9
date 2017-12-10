
namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State //SMART
    {
#pragma warning disable CS0414 // The field 'ATA_State.smartAutosave' is assigned but its value is never used
        bool smartAutosave = true;
#pragma warning restore CS0414 // The field 'ATA_State.smartAutosave' is assigned but its value is never used
        bool smartErrors = false;
        byte smartSelfTestCount = 0;
        //uint8_t *smart_selftest_data;

        void HDD_Smart()
        {
            Log_Verb("HDD_Smart");

            if ((regStatus & DEV9Header.ATA_STAT_READY) == 0)
            {
                return;
            }

            if (regHcyl != 0xC2 || regLcyl != 0x4F)
            {
                CmdNoDataAbort();
                return;
            }

            if (!fetSmartEnabled && regFeature != 0xD8)
            {
                CmdNoDataAbort();
                return;
            }

            switch (regFeature)
            {
                case 0xD9: //SMART_DISABLE
                    SMART_EnableOps(false);
                    return;
                case 0xD8: //SMART_ENABLE
                    SMART_EnableOps(true);
                    return;
                case 0xD2: //SMART_ATTR_AUTOSAVE
                    SMART_SetAutoSaveAttribute();
                    return;
                case 0xD3: //SMART_ATTR_SAVE
                    return;
                case 0xDA: //SMART_STATUS (is fault in disk?)
                    SMART_ReturnStatus();
                    return;
                case 0xD1: //SMART_READ_THRESH
                    Log_Error("DEV9 : SMART_READ_THRESH Not Impemented");
                    CmdNoDataAbort();
                    return;
                case 0xD0: //SMART_READ_DATA
                    Log_Error("DEV9 : SMART_READ_DATA Not Impemented");
                    CmdNoDataAbort();
                    return;
                case 0xD5: //SMART_READ_LOG
                    Log_Error("DEV9 : SMART_READ_LOG Not Impemented");
                    CmdNoDataAbort();
                    return;
                case 0xD4: //SMART_EXECUTE_OFFLINE
                    SMART_ExecuteOfflineImmediate();
                    return;
                default:
                    Log_Error("DEV9 : Unknown SMART command " + regFeature.ToString("X"));
                    CmdNoDataAbort();
                    return;
            }
        }

        void SMART_SetAutoSaveAttribute()
        {
            PreCmd();
            switch (regSector)
            {
                case 0x00:
                    smartAutosave = false;
                    break;
                case 0xF1:
                    smartAutosave = true;
                    break;
                default:
                    Log_Error("DEV9 : Unknown SMART_ATTR_AUTOSAVE command " + regSector.ToString("X"));
                    CmdNoDataAbort();
                    return;
            }
            PostCmdNoData();
        }

        void SMART_ExecuteOfflineImmediate()
        {
            PreCmd();
            int n = 0;
            switch (regSector)
            {
                case 0: /* off-line routine */
                case 1: /* short self test */
                case 2: /* extended self test */
                    smartSelfTestCount++;
                    if (smartSelfTestCount > 21)
                    {
                        smartSelfTestCount = 1;
                    }
                    n = 2 + (smartSelfTestCount - 1) * 24;
                    //s->smart_selftest_data[n] = s->sector;
                    //s->smart_selftest_data[n + 1] = 0x00; /* OK and finished */
                    //s->smart_selftest_data[n + 2] = 0x34; /* hour count lsb */
                    //s->smart_selftest_data[n + 3] = 0x12; /* hour count msb */
                    break;
                case 127: /* abort off-line routine */
                    break;
                case 129: /* short self test, which holds BSY until complete */
                case 130: /* extended self test, which holds BSY until complete */
                    smartSelfTestCount++;
                    if (smartSelfTestCount > 21)
                    {
                        smartSelfTestCount = 1;
                    }
                    n = 2 + (smartSelfTestCount - 1) * 24;

                    SMART_ReturnStatus();
                    return;
                default:
                    CmdNoDataAbort();
                    return;
            }
            PostCmdNoData();
        }

        void SMART_EnableOps(bool enable)
        {
            PreCmd();
            fetSmartEnabled = enable;
            PostCmdNoData();
        }

        void SMART_ReturnStatus()
        {
            PreCmd();
            if (!smartErrors)
            {
                regHcyl = 0xC2;
                regLcyl = 0x4F;
            }
            else
            {
                regHcyl = 0x2C;
                regLcyl = 0xF4;
            }
            PostCmdNoData();
        }
    }
}
