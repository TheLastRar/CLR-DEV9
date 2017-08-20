using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        const bool lba48Supported = false;

        int pioMode;
        int sdmaMode;
        int mdmaMode;
        int udmaMode;

        delegate void Cmd();

        Cmd[] hddCmds = new Cmd[256];

        byte[] identifyData; //512 bytes in size

        bool lba48 = false;

        //Enable/disable features
        bool fetSmartEnabled = true;
        bool fetSecurityEnabled = false;
        bool fetWriteCacheEnabled = true;
        bool fetHostProtectedAreaEnabled = false;

        //Regs

        UInt16 regCommand; //WriteOnly, Only to be written BSY and DRQ are cleared, DMACK is not set and device is not sleeping, except for DEVICE RESET
        //PIO Read/Write, Only to be written DMACK is not set and DRQ is 1
        //COMMAND REG (WriteOnly) Only to be written DMACK is not set
        //Bit 0 = 0
        bool regControlEnableIRQ = false;//Bit 1 = 1 Disable Interrupt
        //Bit 2 = 1 Software Reset
        bool regControlHOBRead = false; //Bit 7 = HOB (cleared by any write to RegCommand, Sets if Low order or High order bytes are read in ATAread16)
        //End COMMAND REG
        byte regError; //ReadOnly

        //DEVICE REG (Read/Write)
        byte regSelect;
        //Bit 0-3: LBA Bits 24-27 (Unused in 48bit) or Command Dependent
        byte regSelectDev
        {
            get { return (byte)((regSelect >> 4) & 1); }
            set
            {
                if (value == 1) { regSelect |= (1 << 4); }
                else { regSelect &= unchecked((byte)(~(1 << 4))); }
            }
        }
        //Bit 5: Obsolete (All?)
        //Bit 6: Command Dependent
        //Bit 7: Obsolete (All?)
        //End COMMAND REG
        byte regFeature; //WriteOnly, Only to be written BSY and DRQ are cleared and DMACK is not set
        byte regFeatureHOB;

        //Following regs are Read/Write, Only to be written BSY and DRQ are cleared and DMACK is not set
        byte regSector; //Sector Number or LBA Low
        byte regSectorHOB;
        byte regLcyl; //LBA Mid
        byte regLcylHOB;
        byte regHcyl; //LBA High
        byte regHcylHOB;
        //TODO handle nsector code
        byte regNsector;
        byte regNsectorHOB;

        byte regStatus; //ReadOnly. When read via AlternateStatus pending interrupts are not cleared
    }
}
