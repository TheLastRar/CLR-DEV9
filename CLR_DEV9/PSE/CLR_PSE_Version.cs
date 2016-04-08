using System;

namespace PSE
{
    internal struct CLR_PSE_Version_Plugin
    {
        private enum CLR_PSE_Type_Version : int
        {
            GS = 0x0006,
            PAD = 0x0002,
            SPU2 = 0x0005,
            SPU2_NewIOP_DMA = 0x0006,
            CDVD = 0x0005,
            DEV9 = 0x0003,
            DEV9_NewIOP_DMA = 0x0004, //Not supported by PCSX2
            USB = 0x0003,
            FW = 0x0002
        }

        private byte _VersionHi;   //Plugin = PATCH       
        //private CLR_Type_Version _VersionMid;  //Plugin = API Version 
        private byte _VersionLo;   //Plugin = MAJOR(rev)  
        private byte _VersionLower;//Plugin = MINOR(build)

        public CLR_PSE_Version_Plugin(byte major, byte minor, byte patch)
        {
            _VersionHi = patch;
            //_VersionMid = 0;
            _VersionLo = major;
            _VersionLower = minor;
        }

        public byte Major
        {
            get
            {
                return _VersionLo;
            }
        }
        public byte Minor
        {
            get
            {
                return _VersionLower;
            }
        }
        public byte Patch
        {
            get
            {
                return _VersionHi;
            }
        }

        public int ToInt32(CLR_PSE_Type type)
        {
            CLR_PSE_Type_Version version = 0;
            switch (type)
            {
                case CLR_PSE_Type.GS:
                    version = CLR_PSE_Type_Version.GS;
                    break;
                case CLR_PSE_Type.PAD:
                    version = CLR_PSE_Type_Version.PAD;
                    break;
                case CLR_PSE_Type.SPU2:
                    version = CLR_PSE_Type_Version.SPU2;
                    break;
                case CLR_PSE_Type.CDVD:
                    version = CLR_PSE_Type_Version.CDVD;
                    break;
                case CLR_PSE_Type.DEV9:
                    version = CLR_PSE_Type_Version.DEV9;
                    break;
                case CLR_PSE_Type.USB:
                    version = CLR_PSE_Type_Version.USB;
                    break;
                case CLR_PSE_Type.FW:
                    version = CLR_PSE_Type_Version.FW;
                    break;
                default:
                    break;
            }
            return (Patch << 24 | (byte)version << 16 | Major << 8 | Minor);
        }
    }

    internal struct CLR_PSE_Version_PCSX2
    {
        private byte _VersionHi; //PCSX2 = MAJOR
        private byte _VersionMid;//PCSX2 = MINOR
        private byte _VersionLo; //PCSX2 = PATCH
        //private byte _VersionLower; //PCSX2 = UNUSED

        public CLR_PSE_Version_PCSX2(byte major, byte minor, byte patch)
        {
            _VersionHi = major;
            _VersionMid = minor;
            _VersionLo = patch;
            //_VersionLower = 0;
        }

        public byte Major
        {
            get
            {
                return _VersionHi;
            }
        }
        public byte Minor
        {
            get
            {
                return _VersionMid;
            }
        }
        public byte Patch
        {
            get
            {
                return _VersionLo;
            }
        }

        public int ToInt32()
        {
            return (_VersionHi << 24 | _VersionMid << 16 | _VersionLo << 8 | 0);
        }
        public static CLR_PSE_Version_PCSX2 ToVersion(int ver)
        {
            byte major = (byte)((ver >> 24) & 0xFF);
            byte minor = (byte)((ver >> 16) & 0xFF);
            byte patch = (byte)((ver >> 8) & 0xFF);
            //least significant byte unused
            return new CLR_PSE_Version_PCSX2(major, minor, patch);
        }

        public static bool operator <(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            if (ver1.Major < ver2.Major)
            {
                return true;
            } else if (!(ver1.Major == ver2.Major))
            {
                return false;
            }
            if (ver1.Minor < ver2.Minor)
            {
                return true;
            }
            else if (!(ver1.Minor == ver2.Minor))
            {
                return false;
            }
            if (ver1.Patch < ver2.Patch)
            return true;
            return false;
        }
        public static bool operator >(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            if (ver1.Major > ver2.Major)
            {
                return true;
            }
            else if (!(ver1.Major == ver2.Major))
            {
                return false;
            }
            if (ver1.Minor > ver2.Minor)
            {
                return true;
            }
            else if (!(ver1.Minor == ver2.Minor))
            {
                return false;
            }
            if (ver1.Patch > ver2.Patch)
                return true;
            return false;
        }

        public static bool operator ==(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            return ver1.Major == ver2.Major
                & ver1.Minor == ver2.Minor
                & ver1.Patch == ver2.Patch;
        }
        public static bool operator !=(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            return !(ver1 == ver2);
        }

        public static bool operator >=(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            return (ver1 > ver2) || (ver1 == ver2);
        }
        public static bool operator <=(CLR_PSE_Version_PCSX2 ver1, CLR_PSE_Version_PCSX2 ver2)
        {
            return (ver1 < ver2) || (ver1 == ver2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CLR_PSE_Version_PCSX2)) return false;

            return this == (CLR_PSE_Version_PCSX2)obj;
        }

        public override int GetHashCode()
        {
            return ToInt32();
        }
    }
}
