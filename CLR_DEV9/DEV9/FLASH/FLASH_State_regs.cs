using System;

namespace CLRDEV9.DEV9.FLASH
{
    partial class FLASH_State
    {
        UInt32 ctrl, address, id, counter, addrbyte;
        UInt32 cmd = unchecked((UInt32)(-1));

        byte[] data = new byte[FLASH_Constants.PAGE_SIZE_ECC], file = new byte[FLASH_Constants.CARD_SIZE_ECC];
    }
}
