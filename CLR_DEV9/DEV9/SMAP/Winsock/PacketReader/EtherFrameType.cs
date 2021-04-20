
namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader
{
    enum EtherFrameType : ushort
    {
        NULL = 0x0000,
        RESET = 0x000C,
        IPv4 = 0x0008,
        ARP = 0x0608,
        VLAN_TAGGED_FRAME = 0x0081
    }
}
