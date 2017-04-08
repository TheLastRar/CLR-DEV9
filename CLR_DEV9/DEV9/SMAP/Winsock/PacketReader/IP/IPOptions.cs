namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP
{
    abstract class IPOptions : TCPOption
    {
        //Code represents option type/value
        public bool CopyOnFragment
        {
            get { return ((Code & (1 << 0x7)) != 0); }
            //set
            //{
            //    if (value) { type |= (1 << 0x7); }
            //    else { type &= unchecked((byte)(~(1 << 0x7))); }
            //}
        }
        public byte Class //0 = control, 2 = debugging and measurement
        {
            get { return (byte)((Code >> 5) & 0x3); }
            //set { type = (byte)((type & ~(0x3 << 5)) | ((value & 0x3) << 5)); }
        }
        public byte Number
        {
            get { return (byte)(Code & 0x1F); }
            //set { type = (byte)((type & ~0x1F) | (value << 5)); }
        }
    }

    //class IPopEOOL
    //{

    //}

    class IPopNOP : IPOptions
    {
        public IPopNOP()
        {

        }
        public override byte Length { get { return 1; } }
        public override byte Code { get { return 1; } }

        public override byte[] GetBytes()
        {
            return new byte[] { Code };
        }
    }
}
