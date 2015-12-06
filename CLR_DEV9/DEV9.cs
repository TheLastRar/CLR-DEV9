using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CLRDEV9
{
    class CLR_DEV9
    {
        private static string LogFolderPath = "logs";
        private static string IniFolderPath = "inis";
        private static DEV9.DEV9_State dev9 = null;
        const bool DoLog = true;
        private static void LogInit()
        {
            if (DoLog)
            {
                if (LogFolderPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                {
                    PSE.CLR_PSE_PluginLog.Open(LogFolderPath.TrimEnd('/'), "CLR_DEV9.log");
                }
                else
                {
                    PSE.CLR_PSE_PluginLog.Open(LogFolderPath, "CLR_DEV9.log");
                }
                //TODO Set Log Options
#if DEBUG
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.All, (int)DEV9LogSources.ATA);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Error, (int)DEV9LogSources.Dev9);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Error, (int)DEV9LogSources.PluginInterface);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Error, (int)DEV9LogSources.SMAP);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Error, (int)DEV9LogSources.Tap);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Verbose, (int)DEV9LogSources.TCP);
                PSE.CLR_PSE_PluginLog.SetLogLevel(SourceLevels.Information, (int)DEV9LogSources.Winsock);
#endif
            }
        }

        public static Int32 Init()
        {
#if DEBUG
            try
            {
#endif
                LogInit();
                Log_Info("Init");
                dev9 = new DEV9.DEV9_State();
                Log_Info("Init ok");
                return 0;
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                return -1;
            }
#endif
        }
        public static Int32 Open(IntPtr winHandle)
        {
#if DEBUG
            try
            {
#endif
                Log_Info("Open");
                Config.LoadConf(IniFolderPath, "CLR_DEV9.ini");

                if (DEV9Header.config.Hdd.Contains("\\") || DEV9Header.config.Hdd.Contains("/"))
                    return dev9.Open(DEV9Header.config.Hdd);
                else
                    return dev9.Open(IniFolderPath + "\\" + DEV9Header.config.Hdd);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                return -1;
            }
#endif
        }
        public static void Close()
        {
#if DEBUG
            try
            {
#endif
                dev9.Close();
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
        public static void Shutdown()
        {
#if DEBUG
            try
            {
#endif
                Log_Info("Shutdown");
                PSE.CLR_PSE_PluginLog.Close();
                //PluginLog.Close(); //fclose(dev9Log);

                if (irqHandle.IsAllocated)
                {
                    irqHandle.Free(); //allow garbage collection
                }
                //Do dispose()?
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
        public static void SetSettingsDir(string dir)
        {
            IniFolderPath = dir;
        }
        public static void SetLogDir(string dir)
        {
            LogFolderPath = dir;
            //LogInit();
        }

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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }

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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }

        unsafe public static void DEV9readDMA8Mem(byte* memPointer, int size)
        {
#if DEBUG
            try
            {
#endif
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Write);
                dev9.DEV9readDMA8Mem(pMem, size);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
        unsafe public static void DEV9writeDMA8Mem(byte* memPointer, int size)
        {
#if DEBUG
            try
            {
#endif
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Read);
                dev9.DEV9writeDMA8Mem(pMem, size);
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }

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
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }

        public static void DEV9irqCallback(PSE.CLR_PSE_Callbacks.CLR_CyclesCallback callback)
        {
            DEV9Header.DEV9irq = callback;
        }
        static GCHandle irqHandle;
        public static int _DEV9irqHandler()
        {
            #if DEBUG
            try
            {
#endif
            return dev9._DEV9irqHandler();
#if DEBUG
            }
            catch (Exception e)
            {
                PSE.CLR_PSE.MsgBoxError(e, LogFolderPath);
                throw e;
            }
#endif
        }
        public static PSE.CLR_PSE_Callbacks.CLR_IRQHandler DEV9irqHandler()
        {
            // Pass our handler to pcsx2.
            if (irqHandle.IsAllocated)
            {
                irqHandle.Free(); //allow garbage collection
            }
            Log_Info("Get IRQ");
            PSE.CLR_PSE_Callbacks.CLR_IRQHandler fp = new PSE.CLR_PSE_Callbacks.CLR_IRQHandler(_DEV9irqHandler);
            irqHandle = GCHandle.Alloc(fp); //prevent GC
            return fp;
        }

        //freeze
        //config
        //about
        //test
        public static int Test()
        {
            return 0;
        }
        public static void Configure()
        {
            Config.LoadConf(IniFolderPath, "CLR_DEV9.ini");
            Config.DoConfig(IniFolderPath, "CLR_DEV9.ini");
            Config.SaveConf(IniFolderPath, "CLR_DEV9.ini");
        }


        private static void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.PluginInterface, "Plugin", str);
        }
        private static void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.PluginInterface, "Plugin", str);
        }
        private static void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "Plugin", str);
        }
    }
}
