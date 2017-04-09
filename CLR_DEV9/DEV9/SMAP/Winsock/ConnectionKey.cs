
namespace CLRDEV9.DEV9.SMAP.Winsock
{
    struct ConnectionKey
    {
        public byte IP0;
        public byte IP1;
        public byte IP2;
        public byte IP3;
        public byte Protocol;
        public ushort PS2Port;
        public ushort SRVPort;

        public override bool Equals(object obj)
        {
            return obj is ConnectionKey && this == (ConnectionKey)obj;
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + IP0.GetHashCode();
                hash = hash * 23 + IP1.GetHashCode();
                hash = hash * 23 + IP2.GetHashCode();
                hash = hash * 23 + IP3.GetHashCode();
                hash = hash * 23 + Protocol.GetHashCode();
                hash = hash * 23 + PS2Port.GetHashCode();
                hash = hash * 23 + SRVPort.GetHashCode();
                return hash;
            }
        }
        public static bool operator ==(ConnectionKey x, ConnectionKey y)
        {
            return x.IP0 == y.IP0 &&
                x.IP1 == y.IP1 &&
                x.IP2 == y.IP2 &&
                x.IP3 == y.IP3 &&
                x.Protocol == y.Protocol &&
                x.PS2Port == y.PS2Port &&
                x.SRVPort == y.SRVPort;
        }
        public static bool operator !=(ConnectionKey x, ConnectionKey y)
        {
            return !(x == y);
        }

        public override string ToString()
        {
            return (IP0 + "." + IP1 + "." + IP2 + "." + IP3 +
                    "-" + Protocol + "-" + PS2Port + ":" + SRVPort);
        }
    }
}
