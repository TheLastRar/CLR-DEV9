using System;
using System.Runtime.InteropServices;

namespace PSE
{
    public enum CLR_PSE_FreezeMode : int
    {
        Load = 0,
        Save = 1,
        Size = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CLR_PSE_FreezeData
    {
        public int size;
        public IntPtr data;
    }
}
