using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;

namespace CLRDEV9.DEV9.SMAP.Tap
{
    partial class TAPAdapter
    {
        const string USERMODEDEVICEDIR = "\\\\.\\Global\\";
        const string TAPSUFFIX = ".tap";

        [StructLayout(LayoutKind.Sequential)]
        struct Version
        {
            UInt32 major;
            UInt32 minor;
            UInt32 debug;
        }

        #region 'PInvoke mess'

        private class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(string lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr pSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DeviceIoControl(SafeFileHandle hDevice, UInt32 dwIoControlCode, ref Version lpInBuffer, UInt32 nInBufferSize, ref Version lpOutBuffer, UInt32 nOutBufferSize, ref UInt32 lpbytesReturned, IntPtr lpOverlapped);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DeviceIoControl(SafeFileHandle hDevice, UInt32 dwIoControlCode, ref bool lpInBuffer, UInt32 nInBufferSize, out bool lpOutBuffer, UInt32 nOutBufferSize, out UInt32 lpbytesReturned, IntPtr lpOverlapped);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DeviceIoControl(SafeFileHandle hDevice, UInt32 dwIoControlCode, byte[] lpInBuffer, UInt32 nInBufferSize, byte[] lpOutBuffer, UInt32 nOutBufferSize, ref UInt32 lpbytesReturned, IntPtr lpOverlapped);
        }
        //CreateFile
        const UInt32 GENERIC_READ = (0x80000000);
        const UInt32 GENERIC_WRITE = (0x40000000);
        const UInt32 OPEN_EXISTING = 3;
        const UInt32 FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;
        //DeviceIoControl
        const UInt32 FILE_DEVICE_UNKNOWN = 0x00000022;
        const UInt32 FILE_ANY_ACCESS = 0;
        const UInt32 METHOD_BUFFERED = 0;

        const UInt32 TAP_IOCTL_GET_MAC = ((FILE_DEVICE_UNKNOWN) << 16) | ((FILE_ANY_ACCESS) << 14) | ((1) << 2) | (METHOD_BUFFERED);//TAP_CONTROL_CODE(1, METHOD_BUFFERED);
        const UInt32 TAP_IOCTL_GET_VERSION = ((FILE_DEVICE_UNKNOWN) << 16) | ((FILE_ANY_ACCESS) << 14) | ((2) << 2) | (METHOD_BUFFERED);//TAP_CONTROL_CODE(2, METHOD_BUFFERED);
        const UInt32 TAP_IOCTL_SET_MEDIA_STATUS = ((FILE_DEVICE_UNKNOWN) << 16) | ((FILE_ANY_ACCESS) << 14) | ((6) << 2) | (METHOD_BUFFERED);//TAP_CONTROL_CODE(6, METHOD_BUFFERED);
        #endregion
        //Set the connection status
        bool TAPSetStatus(SafeFileHandle handle, bool status)
        {
            UInt32 len = 0;

            IntPtr nullPtr = IntPtr.Zero;
            return NativeMethods.DeviceIoControl(handle, TAP_IOCTL_SET_MEDIA_STATUS,
                ref status, (UInt32)Marshal.SizeOf(status),
                out status, (UInt32)Marshal.SizeOf(status), out len, nullPtr);
        }

        //Open the TAP adapter and set the connection to enabled :)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke")]
        SafeFileHandle TAPOpen(string deviceGUID)
        {
            string devicePath;

            UInt32 versionLen = 0;

            //_snprintf (device_path, sizeof(device_path), "%s%s%s",
            //          USERMODEDEVICEDIR,
            //          device_guid,
            //          TAPSUFFIX);
            devicePath = USERMODEDEVICEDIR + deviceGUID + TAPSUFFIX;

            SafeFileHandle handle = NativeMethods.CreateFile(
                devicePath,
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
                return new SafeFileHandle(new IntPtr(-1), true);
            }
            Version ver = new Version();

            IntPtr nullptr = IntPtr.Zero;
            bool bret = NativeMethods.DeviceIoControl(handle, TAP_IOCTL_GET_VERSION,
                                   ref ver, (UInt32)Marshal.SizeOf(ver),
                                   ref ver, (UInt32)Marshal.SizeOf(ver), ref versionLen, nullptr);
            if (bret == false)
            {
                Log_Error("Error @ DIOC " + Marshal.GetLastWin32Error());
                handle.Close();
                return new SafeFileHandle(new IntPtr(-1), true);
            }

            if (!TAPSetStatus(handle, true))
            {
                Log_Error("Error @ TAPSETSTAT " + Marshal.GetLastWin32Error());
                return new SafeFileHandle(new IntPtr(-1), true);
            }

            return handle;
        }

        byte[] TAPGetMac(SafeFileHandle handle)
        {
            UInt32 retLen = 0;

            byte[] ret = new byte[6];

            IntPtr nullptr = IntPtr.Zero;
            bool bret = NativeMethods.DeviceIoControl(handle, TAP_IOCTL_GET_MAC,
                                   ret, 6,
                                   ret, 6, ref retLen, nullptr);
            if (bret == false)
            {
                Log_Error("Error @ DIOC " + Marshal.GetLastWin32Error());
                return null;
            }
            return ret;
        }

        private static List<string[]> TAPGetAdaptersWMI()
        {
            List<string[]> names = new List<string[]>();

            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

            ObjectQuery query = new ObjectQuery("SELECT NetConnectionID,Description,ServiceName,PNPDeviceID,GUID FROM Win32_NetworkAdapter WHERE ServiceName LIKE 'tap%'");
            using (ManagementObjectSearcher netSearcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection netQueryCollection = netSearcher.Get())
                {
                    foreach (ManagementObject netMO in netQueryCollection)
                    {
                        //NetConnectionID is what the user has named the connection
                        names.Add(new string[] { (string)netMO["NetConnectionID"], (string)netMO["Description"], (string)netMO["GUID"] });
                    }
                }
            }
            return names;
        }
    }
}
