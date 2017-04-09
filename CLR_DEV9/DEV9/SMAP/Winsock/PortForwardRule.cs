using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;

namespace CLRDEV9.DEV9.SMAP.Winsock
{
    struct PortForwardRule
    {
        IPType type;
        ushort port;

        public IPType Protocol { get { return type; } }
        public ushort Port { get { return port; } }

        public PortForwardRule(IPType parProtocol, ushort parPort)
        {
            type = parProtocol;
            port = parPort;
        }
    }
}
