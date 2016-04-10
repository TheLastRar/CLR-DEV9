using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using Plugin = CLRDEV9.CLR_DEV9;

namespace PSE
{
    //Multi-in-one is not supported
    public enum CLR_PSE_Type : int
    {
        GS = 0x01,
        PAD = 0x02,
        SPU2 = 0x04,
        CDVD = 0x08,
        DEV9 = 0x10,
        USB = 0x20,
        FW = 0x40
    }

    public class CLR_PSE
    {
        #region NativeExport
        [DllExport("PS2EgetLibName", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static string nat_PS2EgetLibName() { return PS2EgetLibName(); }
        [DllExport("PS2EgetLibType", CallingConvention = CallingConvention.StdCall)]
        private static CLR_PSE_Type nat_PS2EgetLibType() { return PS2EgetLibType(); }
        [DllExport("PS2EgetLibVersion2", CallingConvention = CallingConvention.StdCall)]
        private static int nat_PS2EgetLibVersion2(CLR_PSE_Type type) { return PS2EgetLibVersion2(type); }
        //Windows only
        [DllExport("PS2EsetEmuVersion", CallingConvention = CallingConvention.StdCall)]
        private static void nat_PS2EsetEmuVersion(IntPtr name, int version) { PS2EsetEmuVersion(name, version); }
        #endregion

        //EMU Version (Windows only)
        private static string emuName = "";
        private static CLR_PSE_Version_PCSX2 emuVersion = new CLR_PSE_Version_PCSX2(255, 255, 255);
        internal static string EmuName{ get { return emuName; }}
        internal static CLR_PSE_Version_PCSX2 EmuVersion { get { return emuVersion; } }

        public static string PS2EgetLibName()
        {
            return Plugin.Name;
        }

        public static CLR_PSE_Type PS2EgetLibType()
        {
            //Last remaining constant in this class
            return CLR_PSE_Type.DEV9;
        }

        public static int PS2EgetLibVersion2(CLR_PSE_Type type)
        {
            Version pluginVer = typeof(Plugin).Assembly.GetName().Version;
            CLR_PSE_Version_Plugin version = new CLR_PSE_Version_Plugin((byte)pluginVer.Major, (byte)pluginVer.Minor, (byte)pluginVer.Build);
            return version.ToInt32(type);
        }

        //Only Used on Windows
        public static void PS2EsetEmuVersion(IntPtr name, int version)
        {
            if (name == IntPtr.Zero)
            {
                emuName = "";
            }
            else
            {
                emuName = Marshal.PtrToStringAnsi(name);
            }
            emuVersion = CLR_PSE_Version_PCSX2.ToVersion(version);
        }
    }
}
