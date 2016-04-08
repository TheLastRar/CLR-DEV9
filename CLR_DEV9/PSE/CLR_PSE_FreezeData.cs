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

    internal class CLR_PSE_FreezeDataMarshal
    {
        public static byte[] Load(CLR_PSE_FreezeData frData)
        {
            byte[] ret = new byte[frData.size];
            Marshal.Copy(frData.data, ret, 0, frData.size);
            return ret;
        }
        public static void Save(ref CLR_PSE_FreezeData frData, byte[] frBytes)
        {
            if ((frData.size < frBytes.Length))
            {
                throw new InsufficientMemoryException();
            }
            Marshal.Copy(frBytes, 0, frData.data, frData.size);
        }
    }
}
