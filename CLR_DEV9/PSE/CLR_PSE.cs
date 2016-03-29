using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace PSE
{
    //Multi-in-one is not supported
    public enum CLR_Type : int
    {
        GS = 0x01,
        PAD = 0x02,
        SPU2 = 0x04,
        CDVD = 0x08,
        DEV9 = 0x10,
        USB = 0x20,
        FW = 0x40
    }
    public enum CLR_Type_Version : int
    {
        GS = 0x0006,
        PAD = 0x0002,
        SPU2 = 0x0005,
        SPU2_NewIOP_DMA = 0x0006,
        CDVD = 0x0005,
        DEV9 = 0x0003,
        DEV9_NewIOP_DMA = 0x0004, //Not supported by PCSX2
        USB = 0x0003,
        FW = 0x0002
    }

    public class CLR_PSE
    {
        #region NativeExport
        [DllExport("PS2EgetLibName", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static string nat_PS2EgetLibName() { return PS2EgetLibName(); }
        [DllExport("PS2EgetLibType", CallingConvention = CallingConvention.StdCall)]
        private static CLR_Type nat_PS2EgetLibType() { return PS2EgetLibType(); }
        [DllExport("PS2EgetLibVersion2", CallingConvention = CallingConvention.StdCall)]
        private static int nat_PS2EgetLibVersion2(CLR_Type type) { return PS2EgetLibVersion2(type); }
        #endregion

        //major
        public const byte revision = 0;
        //minor
        public const byte build = 4;

#if DEBUG
        private const string libraryName = "CLR DEV9 DEBUG Test";
#else
        private const string libraryName = "CLR DEV9 Test";
#endif

        public static string PS2EgetLibName()
        {
            return libraryName;
        }

        public static CLR_Type PS2EgetLibType()
        {
            return CLR_Type.DEV9;
        }

        public static int PS2EgetLibVersion2(CLR_Type type)
        {
            CLR_Type_Version version = 0;
            switch (type)
            {
                case CLR_Type.GS:
                    version = CLR_Type_Version.GS;
                    break;
                case CLR_Type.PAD:
                    version = CLR_Type_Version.PAD;
                    break;
                case CLR_Type.SPU2:
                    version = CLR_Type_Version.SPU2;
                    break;
                case CLR_Type.CDVD:
                    version = CLR_Type_Version.CDVD;
                    break;
                case CLR_Type.DEV9:
                    version = CLR_Type_Version.DEV9;
                    break;
                case CLR_Type.USB:
                    version = CLR_Type_Version.USB;
                    break;
                case CLR_Type.FW:
                    version = CLR_Type_Version.FW;
                    break;
                default:
                    break;
            }
            byte rev = revision;
            byte bui = build;
            return ((int)version << 16) | (rev << 8) | bui;
        }
    }
}
