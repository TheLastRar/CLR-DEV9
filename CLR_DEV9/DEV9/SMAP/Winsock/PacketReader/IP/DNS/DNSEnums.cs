
namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS
{
    enum DNSOPCode : byte
    {
        Query = 0,
        IQuery = 1,
        Status = 2,
        Reserved = 3,
        Notify = 4,
        Update = 5
    }
    enum DNSRCode : byte
    {
        NoError = 0,
        FormatError = 1,
        ServerFailure = 2,
        NameError = 3,
        NotImplemented = 4,
        Refused = 5,
        YXDomain = 6,
        YXRRSet = 7,
        NXRRSet = 8,
        NotAuth = 9,
        NotZone = 10,
    }
}
