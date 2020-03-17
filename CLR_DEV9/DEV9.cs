using CLRDEV9.Config;
using PSE;
using PSE.CLR_PSE_Callbacks;
using System;
using System.IO;
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

        private static readonly bool tryAvoidThrow = true;
        private static bool hasInit = false;

        public static string Name { get { return libraryName; } }

        private static void LogSetup()
        {
            //some legwork to setup the logger
            Dictionary<ushort, string> logSources = new Dictionary<ushort, string>();
            IEnumerable<DEV9LogSources> sources = Enum.GetValues(typeof(DEV9LogSources)).Cast<DEV9LogSources>();
            foreach (DEV9LogSources source in sources)
            {
                logSources.Add((ushort)source, source.ToString());
            }
            CLR_PSE_PluginLog.Open(logFolderPath, "DEV9_CLR.log", "CLR_DEV9", logSources);
        }

        private static void LogInit()
        {
            //Log to info to console
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.Test, (int)DEV9LogSources.Test);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.DEV9, (int)DEV9LogSources.Dev9);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.SPEED, (int)DEV9LogSources.SPEED);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.SMAP, (int)DEV9LogSources.SMAP);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.ATA, (int)DEV9LogSources.ATA);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.Winsock, (int)DEV9LogSources.Winsock);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.NetAdapter, (int)DEV9LogSources.NetAdapter);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.UDPSession, (int)DEV9LogSources.UDPSession);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.DNSPacket, (int)DEV9LogSources.DNSPacket);
            CLR_PSE_PluginLog.SetSourceUseStdOut(DEV9Header.config.EnableLogging.DNSSession, (int)DEV9LogSources.DNSSession);
            //Log Trace to file
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.Test ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.Test);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.DEV9 ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.Dev9);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.SPEED ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.SPEED);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.SMAP ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.SMAP);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.ATA ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.ATA);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.Winsock ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.Winsock);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.NetAdapter ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.NetAdapter);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.UDPSession ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.UDPSession);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.DNSPacket ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.DNSPacket);
            CLR_PSE_PluginLog.SetSourceLogLevel(DEV9Header.config.EnableTracing.DNSSession ? SourceLevels.Verbose : SourceLevels.Information, (int)DEV9LogSources.DNSSession);
        }

        public static Int32 Init()
        {
            try
            {
                LogSetup();
#if DEBUG
                CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.PluginInterface);
                //CLR_PSE_PluginLog.SetSourceUseStdOut(true, (int)DEV9LogSources.TCPSession);
                //Info is default
                CLR_PSE_PluginLog.SetFileLevel(SourceLevels.All);
                //CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.ATA);
                //CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.TCPSession);
                CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.Dev9);
                CLR_PSE_PluginLog.SetSourceLogLevel(SourceLevels.All, (int)DEV9LogSources.SMAP);
#endif

                ConfigFile.LoadConf(iniFolderPath, "CLR_DEV9.ini");
                Log_Info("Config Loaded");
                LogInit();
                Log_Info("Init");
                dev9 = new DEV9.DEV9_State();
                Log_Info("Init ok");

                hasInit = true;
                return 0;
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
            catch (Exception) when (tryAvoidThrow)
            {
                return -1;
            }
        }
        public static Int32 Open(IntPtr winHandle)
        {
            try
            {
                int ret = 0;

                Log_Info("Open");

                if (DEV9Header.config.Hdd.Contains(Path.DirectorySeparatorChar))
                    ret = dev9.Open(DEV9Header.config.Hdd);
                else
                    ret = dev9.Open(iniFolderPath + Path.DirectorySeparatorChar + DEV9Header.config.Hdd);

                if (ret == 0)
                    Log_Info("Open ok");
                else
                    CLR_PSE_PluginLog.MsgBoxErrorTrapper(new Exception("Open Failed"));

                return ret;
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
            catch (Exception) when (tryAvoidThrow)
            {
                return -1;
            }
        }
        public static void Close()
        {
            try
            {
                dev9.Close();
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
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
                hasInit = false;
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static void SetSettingsDir(string dir)
        {
            try
            {
                iniFolderPath = dir;
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static void SetLogDir(string dir)
        {
            try
            {
                logFolderPath = dir;
                //LogInit();
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        public static byte DEV9read8(uint addr)
        {
            try
            {
                return dev9.DEV9read8(addr);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static ushort DEV9read16(uint addr)
        {
            try
            {
                return dev9.DEV9_Read16(addr);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static uint DEV9read32(uint addr)
        {
            try
            {
                return dev9.DEV9_Read32(addr);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        public static void DEV9write8(uint addr, byte value)
        {
            try
            {
                dev9.DEV9_Write8(addr, value);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static void DEV9write16(uint addr, ushort value)
        {
            try
            {
                dev9.DEV9_Write16(addr, value);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        public static void DEV9write32(uint addr, uint value)
        {
            try
            {
                dev9.DEV9_Write32(addr, value);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        unsafe public static void DEV9readDMA8Mem(byte* memPointer, int size)
        {
            try
            {
                UnmanagedMemoryStream pMem = new UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Write);
                dev9.DEV9_ReadDMA8Mem(pMem, size);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        unsafe public static void DEV9writeDMA8Mem(byte* memPointer, int size)
        {
            try
            {
                UnmanagedMemoryStream pMem = new UnmanagedMemoryStream(memPointer, size, size, System.IO.FileAccess.Read);
                dev9.DEV9_WriteDMA8Mem(pMem, size);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        public static void DEV9async(uint cycles)
        {
            try
            {
                dev9.DEV9_Async(cycles);
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        public static void DEV9irqCallback(CLR_CyclesCallback callback)
        {
            try
            {
                DEV9Header.DEV9irq = callback;
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }
        static GCHandle irqHandle;
        public static int _DEV9irqHandler()
        {
            try
            {
                return dev9._DEV9irqHandler();
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
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
            catch (Exception e) when (Log_Fatal(e)) { throw; }
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
            catch (Exception e) when (Log_Fatal(e)) { throw; }
            catch (Exception) when (tryAvoidThrow)
            {
                return 1;
            }
        }
        public static void Configure()
        {
            try
            {
                //Config can be called without init
                //logging is setup in init()
                //So we need to check if logging is
                //active, incase of errors dealing
                //with plugin config
                if (!hasInit) LogSetup();
                ConfigFile.LoadConf(iniFolderPath, "CLR_DEV9.ini");
                if (!hasInit) LogInit();
                ConfigFile.DoConfig(iniFolderPath, "CLR_DEV9.ini");
                ConfigFile.SaveConf(iniFolderPath, "CLR_DEV9.ini");
                if (!hasInit) CLR_PSE_PluginLog.Close();
            }
            catch (Exception e) when (Log_Fatal(e)) { throw; }
        }

        //Always return false to avoid catching exception
        private static bool Log_Fatal(Exception ex)
        {
            CLR_PSE_PluginLog.MsgBoxErrorTrapper(ex);
            return false;
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
