using CLRDEV9.Config;
using PSE;
using PSE.CLR_PSE_Callbacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace CLRDEV9
{
    internal class CLR_DEV9
    {
#if DEBUG
        private const string libraryName = "CLR DEV9 DEBUG";
#else
        private const string libraryName = "CLR DEV9";
#endif

        private static string logFolderPath = "logs";
        private static string iniFolderPath = "inis";
        private static DEV9.DEV9_State dev9 = null;
        private static bool doLog = true;

        public static string Name { get { return libraryName; } }

        private static void LogInit()
        {
            if (doLog)
            {
                //some legwork to setup the logger
                Dictionary<ushort, string> logSources = new Dictionary<ushort, string>();
                IEnumerable<DEV9LogSources> sources = Enum.GetValues(typeof(DEV9LogSources)).Cast<DEV9LogSources>();

                foreach (DEV9LogSources source in sources)
                {
                    logSources.Add((ushort)source, source.ToString());
                }

                CLR_PSE_PluginLog.Open(logFolderPath, "DEV9_CLR.log", "CLR_DEV9", logSources);

                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.Test);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.Dev9);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.SPEED);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.SMAP);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.ATA);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.Winsock);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.NetAdapter);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.UDPSession);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.DNSPacket);
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.DNSSession);
#if DEBUG
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.PluginInterface);
                //CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.TCPSession);
                //Info is defualt
                CLR_PSE_PluginLog.SetFileLevel(SourceLevels.All);
                //CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.ATA);
                //CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.TCPSession);
                CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.Dev9);
                CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.SMAP);
#endif
                doLog = false;
            }
        }

        public static Int32 Init()
        {
            try
            {
                LogInit();
                Log_Info("Init");
                dev9 = new DEV9.DEV9_State();
                Log_Info("Init ok");
                return 0;
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                return -1;
            }
        }
        public static Int32 Open(IntPtr winHandle)
        {
            try
            {
                Log_Info("Open");
                ConfigFile.LoadConf(iniFolderPath, "CLR_DEV9.ini");
                if (DEV9Header.config.Hdd.Contains("\\") || DEV9Header.config.Hdd.Contains("/"))
                    return dev9.Open(DEV9Header.config.Hdd);
                else
                    return dev9.Open(iniFolderPath + "\\" + DEV9Header.config.Hdd);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                return -1;
            }
        }
        public static void Close()
        {
            try
            {
                dev9.Close();
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static void Shutdown()
        {
            try
            {
                Log_Info("Shutdown");
                CLR_PSE_PluginLog.Close();
                //PluginLog.Close(); //fclose(dev9Log);

                if (irqHandle.IsAllocated)
                {
                    irqHandle.Free(); //allow garbage collection
                }
                //Do dispose()? (of what?)
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static void SetSettingsDir(string dir)
        {
            try
            {
                iniFolderPath = dir;
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static void SetLogDir(string dir)
        {
            try
            {
                logFolderPath = dir;
                //LogInit();
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        public static byte DEV9read8(uint addr)
        {
            try
            {
                return dev9.DEV9read8(addr);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static ushort DEV9read16(uint addr)
        {
            try
            {
                return dev9.DEV9_Read16(addr);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static uint DEV9read32(uint addr)
        {
            try
            {
                return dev9.DEV9_Read32(addr);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        public static void DEV9write8(uint addr, byte value)
        {
            try
            {
                dev9.DEV9_Write8(addr, value);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static void DEV9write16(uint addr, ushort value)
        {
            try
            {
                dev9.DEV9_Write16(addr, value);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static void DEV9write32(uint addr, uint value)
        {
            try
            {
                dev9.DEV9_Write32(addr, value);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        unsafe public static void DEV9readDMA8Mem(byte* memPointer, int size)
        {
            try
            {
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Write);
                dev9.DEV9_ReadDMA8Mem(pMem, size);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        unsafe public static void DEV9writeDMA8Mem(byte* memPointer, int size)
        {
            try
            {
                System.IO.UnmanagedMemoryStream pMem = new System.IO.UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Read);
                dev9.DEV9_WriteDMA8Mem(pMem, size);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        public static void DEV9async(uint cycles)
        {
            try
            {
                dev9.DEV9_Async(cycles);
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        public static void DEV9irqCallback(CLR_CyclesCallback callback)
        {
            try
            {
                DEV9Header.DEV9irq = callback;
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        static GCHandle irqHandle;
        public static int _DEV9irqHandler()
        {
            try
            {
                return dev9._DEV9irqHandler();
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }
        public static CLR_IRQHandler DEV9irqHandler()
        {
            try
            {
                // Pass our handler to pcsx2.
                if (irqHandle.IsAllocated)
                {
                    irqHandle.Free(); //allow garbage collection
                }
                Log_Info("Get IRQ");
                CLR_IRQHandler fp = new CLR_IRQHandler(_DEV9irqHandler);
                irqHandle = GCHandle.Alloc(fp); //prevent GC
                return fp;
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        //freeze
        //config
        //about
        public static int Test()
        {
            try
            {
                CLR_PSE_Version_PCSX2 minVer = new CLR_PSE_Version_PCSX2(1, 3, 1);
                if (CLR_PSE.EmuName != "PCSX2")
                {
                    return 0; //Hope it works
                }
                if (CLR_PSE.EmuVersion >= minVer)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                return 1;
            }
        }
        public static void Configure()
        {
            try
            {
                LogInit();
                ConfigFile.LoadConf(iniFolderPath, "CLR_DEV9.ini");
                ConfigFile.DoConfig(iniFolderPath, "CLR_DEV9.ini");
                ConfigFile.SaveConf(iniFolderPath, "CLR_DEV9.ini");
            }
            catch (Exception e)
            {
                CLR_PSE_PluginLog.MsgBoxErrorTrapper(e);
                throw;
            }
        }

        private static void Log_Error(string str)
        {
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.PluginInterface, str);
        }
        private static void Log_Info(string str)
        {
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.PluginInterface, str);
        }
        private static void Log_Verb(string str)
        {
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, str);
        }
    }
}
