using System;

namespace CLR_DEV9
{
    static class Win32
    {
        static netHeader.NetAdapter GetNetAdapter()
        {
            if (DEV9Header.config.Eth.StartsWith("p"))
            {
                //try
                //{
                //    return new PCAPAdapter();
                //}
                //catch (Exception e)
                //{
                return null;
                //}
            }
            else if (DEV9Header.config.Eth.StartsWith("t"))
            {
                return new tap.TAPAdapter();
            }
            else if (DEV9Header.config.Eth.StartsWith("w"))
            {
                return new Winsock();
            }
            else
                return null;
        }

        public static int _DEV9open()
        {
            netHeader.NetAdapter na = GetNetAdapter();
            if (na == null)
            {
                Console.Error.WriteLine("Failed to GetNetAdapter()");
            }
            else
            {
                net.InitNet(na);
            }
            return 0;
        }
        public static void _DEV9close()
        {
            net.TermNet();
        }
    }
}
