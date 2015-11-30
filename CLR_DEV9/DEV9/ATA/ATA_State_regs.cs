using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        delegate void Cmd();

        UInt16 xfer_mode;
        int smart_on;

        UInt16 pio_count;
        UInt16 pio_size;
        byte[] pio_buf = new byte[256*2];

        UInt16 feature; //Shars reg with error
        UInt16 command;
        Cmd[] HDDcmds = new Cmd[256];

    }
}
