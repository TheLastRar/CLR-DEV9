
namespace CLRDEV9
{
    enum DEV9LogSources : ushort
    {
        NetAdapter = 1,
        PluginInterface = 2,
        Dev9 = 3,
        ATA = 5,
        SMAP = 4,
        FLASH = 9,
        Tap = 6,
        WinPcap = 7,
        Winsock = 8,

        //ABCDE
        //A = 1 (winsock)
        //B = EtherFrameType (1=IP)
        //C = IP Protocol Type (1 = ICMP, 2 = UCP, 3 = TCP, 4 = IGMP)
        //D = Palyload type (i.e. is DHCP?)
        //E = Component
        //10xxx Ether/Other packets/winsock base
        ETHFrame    = 10001,
        //110xx Basic IP packets
        //Session   = XXXX0
        //Packet    = XXXX1
        //Options   = XXXX2
        IPPacket    = 11001,

        ICMPSession = 11100,
        IGMPSession = 11400,

        TCPSession  = 11200,
        TCPPacket   = 11201,
        TCPOption   = 11202,

        UDPSession  = 11301,
        //1131x DHCP (subset of UDP)
        DHCPSession = 11310,
        DHCPPacket  = 11311,
        DHCPOption  = 11312,  
    }
}
