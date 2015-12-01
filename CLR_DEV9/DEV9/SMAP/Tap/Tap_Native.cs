using System;
using System.Runtime.InteropServices;

namespace CLRDEV9.DEV9.SMAP.Tap
{
    partial class TAPAdapter
    {
        const string USERMODEDEVICEDIR = "\\\\.\\Global\\";
        const string TAPSUFFIX = ".tap";

        struct version
        {
            ulong major;
            ulong minor;
            ulong debug;
        }

        #region 'PInvoke mess'
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string lpFileName, System.UInt32 dwDesiredAccess, System.UInt32 dwShareMode, IntPtr pSecurityAttributes, System.UInt32 dwCreationDisposition, System.UInt32 dwFlagsAndAttributes, IntPtr hTemplateFile);
        const UInt32 GENERIC_READ = (0x80000000);
        const UInt32 GENERIC_WRITE = (0x40000000);
        const UInt32 OPEN_EXISTING = 3;
        const UInt32 FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle hDevice, UInt32 dwIoControlCode, ref version lpInBuffer, UInt32 nInBufferSize, ref version lpOutBuffer, UInt32 nOutBufferSize, ref UInt32 lpBytesReturned, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle hDevice, UInt32 dwIoControlCode, ref bool lpInBuffer, UInt32 nInBufferSize, out bool lpOutBuffer, UInt32 nOutBufferSize, out UInt32 lpBytesReturned, IntPtr lpOverlapped);

        const UInt32 FILE_DEVICE_UNKNOWN = 0x00000022;
        const UInt32 FILE_ANY_ACCESS = 0;
        const UInt32 METHOD_BUFFERED = 0;

        const UInt32 TAP_IOCTL_GET_VERSION = ((FILE_DEVICE_UNKNOWN) << 16) | ((FILE_ANY_ACCESS) << 14) | ((2) << 2) | (METHOD_BUFFERED);//TAP_CONTROL_CODE(2, METHOD_BUFFERED);
        const UInt32 TAP_IOCTL_SET_MEDIA_STATUS = ((FILE_DEVICE_UNKNOWN) << 16) | ((FILE_ANY_ACCESS) << 14) | ((6) << 2) | (METHOD_BUFFERED);//TAP_CONTROL_CODE(6, METHOD_BUFFERED);
        #endregion
        //Set the connection status
        bool TAPSetStatus(Microsoft.Win32.SafeHandles.SafeFileHandle handle, bool status)
        {
            UInt32 len = 0;

            IntPtr nullptr = IntPtr.Zero;
            return DeviceIoControl(handle, TAP_IOCTL_SET_MEDIA_STATUS,
                ref status, (UInt32)Marshal.SizeOf(status),
                out status, (UInt32)Marshal.SizeOf(status), out len, nullptr);
        }

        //Open the TAP adapter and set the connection to enabled :)
        Microsoft.Win32.SafeHandles.SafeFileHandle TAPOpen(string device_guid)
        {
            string device_path;

            UInt32 version_len = 0;

            //_snprintf (device_path, sizeof(device_path), "%s%s%s",
            //          USERMODEDEVICEDIR,
            //          device_guid,
            //          TAPSUFFIX);
            device_path = USERMODEDEVICEDIR + device_guid + TAPSUFFIX;

            Microsoft.Win32.SafeHandles.SafeFileHandle handle = CreateFile(
                device_path,
                GENERIC_READ | GENERIC_WRITE,
                0,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_SYSTEM | FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                Log_Error("Error @ CF " + Marshal.GetLastWin32Error());
                //return INVALID_HANDLE_VALUE;
                return new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(-1), true);
            }
            version ver = new version();

            IntPtr nullptr = IntPtr.Zero;
            bool bret = DeviceIoControl(handle, TAP_IOCTL_GET_VERSION,
                                   ref ver, (UInt32)Marshal.SizeOf(ver),
                                   ref ver, (UInt32)Marshal.SizeOf(ver), ref version_len, nullptr);
            if (bret == false)
            {
                Log_Error("Error @ DIOC " + Marshal.GetLastWin32Error());
                handle.Close();
                return new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(-1), true);
            }

            if (!TAPSetStatus(handle, true))
            {
                Log_Error("Error @ TAPSETSTAT " + Marshal.GetLastWin32Error());
                return new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(-1), true);
            }

            return handle;
        }
    }
}
