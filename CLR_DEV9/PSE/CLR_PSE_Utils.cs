using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSE
{
    internal class CLR_PSE_Utils
    {
        public static bool IsWindows()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            switch (pid)
            {
                //Official support
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return true;
                default:
                    return false;
            }
        }
        public static string MarshalDirectoryString(IntPtr ptr)
        {
            int length = 0;

            for (
                 ; 0 != Marshal.ReadByte(ptr+length)
                 ; ++length)
            { }

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);

            string ret = Encoding.UTF8.GetString(buffer);

            if (ret.Contains("�")) //Old PCSX2 (Input was not UTF8)
                ret = Encoding.Default.GetString(buffer);

            return ret;
        }
    }
}
