using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace CLRDEV9
{
    class CLR_DEV9
    {
        public static string LogFolderPath = "logs";
        private static PSE.CLR_PSE_PluginLog DEVLOG_shared;
        private static DEV9.DEV9_State dev9 = null;
        const bool DoLog = false;
        private static void LogInit()
        {
#pragma warning disable 0162
            if (DoLog)
            {
                if (LogFolderPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                {
                    //PluginLog.Open(LogFolderPath + "dev9clr.log");
                }
                else
                {
                    //PluginLog.Open(LogFolderPath + System.IO.Path.DirectorySeparatorChar + "dev9clr.log");
                }
                //PluginLog.SetWriteToFile(true);
                DEVLOG_shared = null; //PluginLog;
            }
#pragma warning restore 0162
        }
        public static void DEV9_LOG(string basestr)
        {
            if (DoLog)
            {
                Console.Error.WriteLine(basestr);
                //DEVLOG_shared.LogWriteLine(basestr);
            }
        }

        [DllExport("DEV9init", CallingConvention = CallingConvention.StdCall)]
        public static Int32 Init()
        {
            LogInit();
            DEV9_LOG("DEV9init");
            dev9 = new DEV9.DEV9_State();
            DEV9_LOG("DEV9init ok");

            return 0;
        }
        [DllExport("DEV9shutdown", CallingConvention = CallingConvention.StdCall)]
        public static void Shutdown()
        {
            DEV9_LOG("DEV9shutdown\n");
            //PluginLog.Close(); //fclose(dev9Log);
            DEVLOG_shared = null;
            irqHandle.Free();
            //Do dispose()?
        }
        [DllExport("DEV9open", CallingConvention = CallingConvention.StdCall)]
        public static Int32 DEV9open(IntPtr winHandle)
        {
            DEV9_LOG("DEV9open");
            Config.LoadConf();
            DEV9_LOG("open r+: " + DEV9Header.config.Hdd);

            DEV9Header.config.HddSize = 8 * 1024;

            return dev9.Open();
        }
        [DllExport("DEV9close", CallingConvention = CallingConvention.StdCall)]
        public static void Close()
        {
            dev9.Close();
        }

        static GCHandle irqHandle;
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static PSE.CLR_PSE_Callbacks.CLR_IRQHandler DEV9irqHandler()
        {
            // Pass our handler to pcsx2.
            if (irqHandle.IsAllocated)
            {
                irqHandle.Free(); //allow garbage collection
            }
            DEV9_LOG("Get IRQ");
            PSE.CLR_PSE_Callbacks.CLR_IRQHandler fp = new PSE.CLR_PSE_Callbacks.CLR_IRQHandler(_DEV9irqHandler);
            irqHandle = GCHandle.Alloc(fp); //prevent GC
            return fp;
        }
        public static int _DEV9irqHandler()
        {
            return dev9._DEV9irqHandler();
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static byte DEV9read8(uint addr)
        {
#if DEBUG
            try
            {
#endif
            return dev9.DEV9read8(addr);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static ushort DEV9read16(uint addr)
        {
#if DEBUG
            try
            {
#endif
            return dev9.DEV9read16(addr);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static uint DEV9read32(uint addr)
        {
#if DEBUG
            try
            {
#endif
            return dev9.DEV9read32(addr);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write8(uint addr, byte value)
        {
#if DEBUG
            try
            {
#endif
            dev9.DEV9write8(addr, value);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write16(uint addr, ushort value)
        {
#if DEBUG
            try
            {
#endif
            dev9.DEV9write16(addr, value);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9write32(uint addr, uint value)
        {
#if DEBUG
            try
            {
#endif
            dev9.DEV9write32(addr, value);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        unsafe public static void DEV9readDMA8Mem(byte* memPointer, int size)
        {
#if DEBUG
            try
            {
#endif
                DEV9_LOG("*DEV9readDMA8Mem: size " + size.ToString("X"));
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Write);
                Console.Error.WriteLine("rDMA");
                dev9.DEV9readDMA8Mem(pMem, size);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        unsafe public static void DEV9writeDMA8Mem(byte* memPointer, int size)
        {
#if DEBUG
            try
            {
#endif
                DEV9_LOG("*DEV9writeDMA8Mem: size " + size.ToString("X"));
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Read);
                Console.Error.WriteLine("wDMA");
                dev9.DEV9writeDMA8Mem(pMem, size);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9irqCallback(PSE.CLR_PSE_Callbacks.CLR_CyclesCallback callback)
        {
            DEV9Header.DEV9irq = callback;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void DEV9async(uint cycles)
        {
#if DEBUG
            try
            {
#endif
                dev9.DEV9async(cycles);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e);
                throw e;
            }
#endif
        }

        [DllExport("DEV9test", CallingConvention = CallingConvention.StdCall)]
        public static int Test()
        {
            return 0;
        }
        [DllExport("DEV9setSettingsDir", CallingConvention = CallingConvention.StdCall)]
        public static void SetSettingsDir(string dir)
        {
            //throw new NotImplementedException();
        }
        [DllExport("DEV9setLogDir", CallingConvention = CallingConvention.StdCall)]
        public static void SetLogDir(string dir)
        {
            //throw new NotImplementedException();
        }
        //public static byte[] FreezeSave()
        //{
        //    throw new NotImplementedException();
        //}
        //public static int FreezeLoad(byte[] data)
        //{
        //    throw new NotImplementedException();
        //}
        //public static int FreezeSize()
        //{
        //    return 0;
        //}
    }
}
