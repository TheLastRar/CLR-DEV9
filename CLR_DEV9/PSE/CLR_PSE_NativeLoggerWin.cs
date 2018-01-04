using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PSE
{
    internal sealed class CLR_PSE_NativeLoggerWin : TextWriter
    {
        const UInt16 STDIN = 0;
        const UInt16 STDOUT = 1;
        const UInt16 STDERR = 2;

        private class NativeMethods
        {
            [DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr __acrt_iob_func(UInt16 var);
            [DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 __stdio_common_vfprintf(UInt64 _Options, IntPtr _Stream, byte[] _Format, IntPtr _Local, IntPtr[] _ArgList);
        }

        Encoding enc = new UTF8Encoding();
        byte[] fmtBytes;
        UInt16 std;

        public CLR_PSE_NativeLoggerWin(bool useError)
        {
            //Init fixed format
            byte[] strBytes = new byte[enc.GetByteCount("%s") + 1];
            Array.Copy(enc.GetBytes("%s"), strBytes, strBytes.Length - 1);
            fmtBytes = strBytes;

            if ((useError))
            {
                std = STDERR;
            }
            else
            {
                std = STDOUT;
            }
            //printf will auto-expand it to a \r\n
            NewLine = "\n";
        }

        public override void Write(char value)
        {
            Write(new char[] { value });
        }

        public override void Write(char[] value)
        {
            //Convert string to bytes of needed encoding
            byte[] strBytes = new byte[enc.GetByteCount(value) + 1];
            Array.Copy(enc.GetBytes(value), strBytes, strBytes.Length - 1);
            GCHandle strHandle = GCHandle.Alloc(strBytes, GCHandleType.Pinned);

            try
            {
                //write bytes to stdstream
                NativeMethods.__stdio_common_vfprintf(0, NativeMethods.__acrt_iob_func(std), fmtBytes, IntPtr.Zero, new IntPtr[] { strHandle.AddrOfPinnedObject() });
            }
            finally
            {
                strHandle.Free();
            }
        }

        public override Encoding Encoding
        {
            get { return enc; }
        }
    }
}
