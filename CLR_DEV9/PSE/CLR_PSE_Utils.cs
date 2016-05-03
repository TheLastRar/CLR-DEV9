using System;

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
                ////Needs Wrapper
                //case PlatformID.Unix: //Also MAC
                //    return false;
                ////Not Supported by PCSX2
                //case PlatformID.MacOSX:
                //case PlatformID.Xbox:
                //    return false;
                default:
                    return false;
            }
        }
    }
}
