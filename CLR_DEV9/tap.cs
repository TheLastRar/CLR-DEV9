using System;
using System.Runtime.InteropServices;

namespace CLR_DEV9
{
    class tap
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
        static bool TAPSetStatus(Microsoft.Win32.SafeHandles.SafeFileHandle handle, bool status)
        {
            UInt32 len = 0;

            IntPtr nullptr = IntPtr.Zero;
            return DeviceIoControl(handle, TAP_IOCTL_SET_MEDIA_STATUS,
                ref status, (UInt32)Marshal.SizeOf(status),
                out status, (UInt32)Marshal.SizeOf(status), out len, nullptr);
        }

        //Open the TAP adapter and set the connection to enabled :)
        static Microsoft.Win32.SafeHandles.SafeFileHandle TAPOpen(string device_guid)
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
                Console.Error.WriteLine("Error @ CF " + Marshal.GetLastWin32Error());
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
                Console.Error.WriteLine("Error @ DIOC " + Marshal.GetLastWin32Error());
                handle.Close();
                return new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(-1), true);
            }

            if (!TAPSetStatus(handle, true))
            {
                Console.Error.WriteLine("Error @ TAPSETSTAT " + Marshal.GetLastWin32Error());
                return new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(-1), true);
            }

            return handle;
        }
        public class TAPAdapter : netHeader.NetAdapter
        {
            Microsoft.Win32.SafeHandles.SafeFileHandle htap;
            System.IO.FileStream htapstream;

            public TAPAdapter()
            {
                htap = TAPOpen(DEV9Header.config.Eth.Substring(4, DEV9Header.config.Eth.Length - 4));

                htapstream = new System.IO.FileStream(htap, System.IO.FileAccess.ReadWrite, 16 * 1024, true);

            }

            public override bool blocks()
            {
                return true;	//we use blocking io
            }
            byte[] broadcast_adddrrrr = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            //gets a packet.rv :true success
            public override bool recv(ref netHeader.NetPacket pkt)
            {
                int read_size = 0;
                //bool result = false;
                try
                {
                    read_size = htapstream.Read(pkt.buffer, 0, pkt.buffer.Length);
                    //result = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Packet Recive Error :" + e.ToString());
                    return false;
                }

                //Console.Error.WriteLine(read_size);

                //Result would always be true, don't other checking it.

                //if (result)
                //{
                //original memcmp returns 0 on perfect match
                //the if statment check if !=0
                byte[] eeprombytes = new byte[6];
                for (int i = 0; i < 3; i++)
                {
                    byte[] tmp = BitConverter.GetBytes(DEV9Header.dev9.eeprom[i]);
                    Utils.memcpy(ref eeprombytes, i * 2, tmp, 0, 2);
                }
                if ((Utils.memcmp(pkt.buffer, 0, eeprombytes, 0, 6) == false) & (Utils.memcmp(pkt.buffer, 0, broadcast_adddrrrr, 0, 6) == false))
                {
                    //ignore strange packets
                    Console.Error.WriteLine("Dropping Strange Packet");
                    return false;
                }

                if (Utils.memcmp(pkt.buffer, 6, eeprombytes, 0, 6) == true)
                {
                    //avoid pcap looping packets
                    Console.Error.WriteLine("Dropping Looping Packet");
                    return false;
                }
                pkt.size = read_size;
                Console.Error.WriteLine("---------------------Recived Packet");
                PacketReader.EthernetFrame ef = new PacketReader.EthernetFrame(pkt);
                return true;
                //}
                //else
                //    return false;
            }
            //sends the packet and deletes it when done (if successful).rv :true success
            public override bool send(netHeader.NetPacket pkt)
            {
                int writen = 0;

                Console.Error.WriteLine("---------------------Sent Packet");
                PacketReader.EthernetFrame ef = new PacketReader.EthernetFrame(pkt);

                htapstream.Write(pkt.buffer, 0, pkt.size);
                htapstream.Flush();
                //return type is void, assume full write
                writen = pkt.size;


                if (writen != pkt.size)
                {
                    Console.Error.WriteLine("incomplete Send " + Marshal.GetLastWin32Error());
                    return false;
                }

                return true;
            }
            public override void Dispose()
            {
                TAPSetStatus(htap, false);
                Console.Error.WriteLine("Shutdown Tap");
                htapstream.Close();
                htap.Close();
            }
        };
    }
}
