using RGiesecke.DllExport;
using System.Runtime.InteropServices;

namespace PSE
{
    public class CLR_PSE
    {
        //Multi-in-one is not supported
        private enum CLR_Type : int
        {
            GS = 0x01,
            PAD = 0x02,
            SPU2 = 0x04,
            CDVD = 0x08,
            DEV9 = 0x10,
            USB = 0x20,
            FW = 0x40
        }
        private enum CLR_Type_Version : int
        {
            GS = 0x0006,
            PAD = 0x0002,
            SPU2 = 0x0005,
            SPU2_NewIOP_DMA = 0x0006,
            CDVD = 0x0005,
            DEV9 = 0x0003,
            DEV9_NewIOP_DMA = 0x0004,
            USB = 0x0003,
            FW = 0x0002
        }
        //major
        public const byte revision = 0;
        //minor
        public const byte build = 1;

        private const string libraryName = "CLR DLLExport DEV9 Test";
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static string PS2EgetLibName()
        {
            return libraryName;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static int PS2EgetLibType()
        {
            return (int)CLR_Type.DEV9;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static int PS2EgetLibVersion2(int type)
        {
            int version = 0;
            switch ((CLR_Type)type)
            {
                case CLR_Type.GS:
                    version = (int)CLR_Type_Version.GS;
                    break;
                case CLR_Type.PAD:
                    version = (int)CLR_Type_Version.PAD;
                    break;
                case CLR_Type.SPU2:
                    version = (int)CLR_Type_Version.SPU2;
                    break;
                case CLR_Type.CDVD:
                    version = (int)CLR_Type_Version.CDVD;
                    break;
                case CLR_Type.DEV9:
                    version = (int)CLR_Type_Version.DEV9;
                    break;
                case CLR_Type.USB:
                    version = (int)CLR_Type_Version.USB;
                    break;
                case CLR_Type.FW:
                    version = (int)CLR_Type_Version.FW;
                    break;
                default:
                    break;
            }
            byte rev = revision;
            byte bui = build;
            return (version << 16) | (rev << 8) | bui;
        }
    }

    //public abstract class CLR_PSE_Config
    //{

    //    protected CLR_PSE_Base Base;
    //    public string IniFolderPath = "inis";
    //    PluginConf calls
    //    public CLR_PSE_Callbacks.Config_Open Open;
    //    public CLR_PSE_Callbacks.Close Close;
    //    public CLR_PSE_Callbacks.Config_WriteInt WriteInt;

    //    public CLR_PSE_Callbacks.Config_ReadInt ReadInt;
    //    public void SetBase(CLR_PSE_Base common)
    //    {
    //        Base = common;
    //    }

    //    public abstract void About();
    //    public abstract void Configure();

    //    public abstract void LoadConfig();
    //    protected abstract void SaveConfig();
    //}
}
