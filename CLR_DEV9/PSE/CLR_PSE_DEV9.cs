using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace PSE
{
    public class CLR_PSE_DEV9
    {
        #region NativeExport
        [DllExport("DEV9init", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9init() { return DEV9init(); }
        [DllExport("DEV9open", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9open(IntPtr pDsp) { return DEV9open(pDsp); }
        [DllExport("DEV9close", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9close() { DEV9close(); }
        [DllExport("DEV9shutdown", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9shutdown() { DEV9shutdown(); }
        [DllExport("DEV9setSettingsDir", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9setSettingsDir(string dir) { DEV9setSettingsDir(dir); }
        [DllExport("DEV9setLogDir", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9setLogDir(string dir) { DEV9setLogDir(dir); }

        [DllExport("DEV9read8", CallingConvention = CallingConvention.StdCall)]
        private static Byte nat_DEV9read8(UInt32 addr) { return DEV9read8(addr); }
        [DllExport("DEV9read16", CallingConvention = CallingConvention.StdCall)]
        private static UInt16 nat_DEV9read16(UInt32 addr) { return DEV9read16(addr); }
        [DllExport("DEV9read32", CallingConvention = CallingConvention.StdCall)]
        private static UInt32 nat_DEV9read32(UInt32 addr) { return DEV9read32(addr); }
        [DllExport("DEV9write8", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9write8(UInt32 addr, Byte value) { DEV9write8(addr, value); }
        [DllExport("DEV9write16", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9write16(UInt32 addr, UInt16 value) { DEV9write16(addr, value); }
        [DllExport("DEV9write32", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9write32(UInt32 addr, UInt32 value) { DEV9write32(addr, value); }
        //ENABLE_NEW_IOPDMA_DEV9
        [DllExport("DEV9dmaRead", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9dmaRead(Int32 channel, IntPtr data, UInt32 bytesLeft, ref UInt32 bytesProcessed) { return DEV9dmaRead(channel, data, bytesLeft, ref bytesProcessed); }
        [DllExport("DEV9dmaWrite", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9dmaWrite(Int32 channel, IntPtr data, UInt32 bytesLeft, ref UInt32 bytesProcessed) { return DEV9dmaWrite(channel, data, bytesLeft, ref bytesProcessed); }
        [DllExport("DEV9dmaInterrupt", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9dmaInterrupt(Int32 channel) { DEV9dmaInterrupt(channel); }
        //OLD (CURRENT) DMA
        [DllExport("DEV9readDMA8Mem", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9readDMA8Mem(IntPtr memPointer, int size) { DEV9readDMA8Mem(memPointer, size); }
        [DllExport("DEV9writeDMA8Mem", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9writeDMA8Mem(IntPtr memPointer, int size) { DEV9writeDMA8Mem(memPointer, size); }

        [DllExport("DEV9async", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9async(UInt32 cycles) { DEV9async(cycles); }

        [DllExport("DEV9irqCallback", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9irqCallback(PSE.CLR_PSE_Callbacks.CLR_CyclesCallback callback) { DEV9irqCallback(callback); }
        [DllExport("DEV9irqHandler", CallingConvention = CallingConvention.StdCall)]
        private static PSE.CLR_PSE_Callbacks.CLR_IRQHandler nat_DEV9irqHandler() { return DEV9irqHandler(); }

        [DllExport("DEV9freeze", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9freeze(CLR_PSE_FreezeMode mode, ref CLR_PSE_FreezeData data) { return DEV9freeze(mode, ref data); }
        [DllExport("DEV9configure", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9configure() { DEV9configure(); }
        [DllExport("DEV9about", CallingConvention = CallingConvention.StdCall)]
        private static void nat_DEV9about() { DEV9about(); }
        [DllExport("DEV9test", CallingConvention = CallingConvention.StdCall)]
        private static Int32 nat_DEV9test() { return DEV9test(); }
        #endregion

        public static Int32 DEV9init()
        {
            return CLRDEV9.CLR_DEV9.Init();
        }
        public static Int32 DEV9open(IntPtr pDsp)
        {
            return CLRDEV9.CLR_DEV9.Open(pDsp);
        }
        public static void DEV9close()
        {
            CLRDEV9.CLR_DEV9.Close();
        }
        public static void DEV9shutdown()
        {
            CLRDEV9.CLR_DEV9.Shutdown();
        }
        public static void DEV9setSettingsDir(string dir)
        {
            CLRDEV9.CLR_DEV9.SetSettingsDir(dir);
        }
        public static void DEV9setLogDir(string dir)
        {
            CLRDEV9.CLR_DEV9.DEV9setLogDir(dir);
        }

        public static Byte DEV9read8(UInt32 addr)
        {
            return CLRDEV9.CLR_DEV9.DEV9read8(addr);
        }
        public static UInt16 DEV9read16(UInt32 addr)
        {
            return CLRDEV9.CLR_DEV9.DEV9read16(addr);
        }
        public static UInt32 DEV9read32(UInt32 addr)
        {
            return CLRDEV9.CLR_DEV9.DEV9read32(addr);
        }
        public static void DEV9write8(UInt32 addr, Byte value)
        {
            CLRDEV9.CLR_DEV9.DEV9write8(addr, value);
        }
        public static void DEV9write16(UInt32 addr, UInt16 value)
        {
            CLRDEV9.CLR_DEV9.DEV9write16(addr, value);
        }
        public static void DEV9write32(UInt32 addr, UInt32 value)
        {
            CLRDEV9.CLR_DEV9.DEV9write32(addr, value);
        }

        private static Int32 DEV9dmaRead(Int32 channel, IntPtr data, UInt32 bytesLeft, ref UInt32 bytesProcessed)
        {
            throw new NotImplementedException();
        }
        private static Int32 DEV9dmaWrite(Int32 channel, IntPtr data, UInt32 bytesLeft, ref UInt32 bytesProcessed)
        {
            throw new NotImplementedException();
        }
        private static void DEV9dmaInterrupt(int channel)
        {
            throw new NotImplementedException();
        }

        public static void DEV9readDMA8Mem(IntPtr memPointer, int size)
        {
            unsafe
            {
                CLRDEV9.CLR_DEV9.DEV9readDMA8Mem((byte*)memPointer.ToPointer(), size);
            }
        }
        public static void DEV9writeDMA8Mem(IntPtr memPointer, int size)
        {
            unsafe
            {
                CLRDEV9.CLR_DEV9.DEV9writeDMA8Mem((byte*)memPointer.ToPointer(), size);
            }
        }

        public static void DEV9async(UInt32 cycles)
        {
            CLRDEV9.CLR_DEV9.DEV9async(cycles);
        }

        public static void DEV9irqCallback(PSE.CLR_PSE_Callbacks.CLR_CyclesCallback callback)
        {
            CLRDEV9.CLR_DEV9.DEV9irqCallback(callback);
        }
        public static PSE.CLR_PSE_Callbacks.CLR_IRQHandler DEV9irqHandler()
        {
            return CLRDEV9.CLR_DEV9.DEV9irqHandler();
        }

        public static Int32 DEV9freeze(CLR_PSE_FreezeMode mode, ref CLR_PSE_FreezeData data)
        {
            return 0;
        }
        public static void DEV9configure()
        {
        }
        public static void DEV9about() //(When is this called?)
        {
        }
        public static Int32 DEV9test()
        {
            return CLRDEV9.CLR_DEV9.Test();
        }
    }
}
