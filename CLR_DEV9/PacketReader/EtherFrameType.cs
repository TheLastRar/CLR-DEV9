
namespace CLR_DEV9.PacketReader
{
    enum EtherFrameType : ushort
    {
        NULL = 0x0000,
        IPv4 = 0x0008,
        ARP = 0x0608,
        VLAN_TAGGED_FRAME = 0x0081
    }
}
