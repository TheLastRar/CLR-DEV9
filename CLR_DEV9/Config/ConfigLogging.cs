using System.Net;
using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigLogging
    {
        [DataMember]
        public bool Test = true;
        [DataMember]
        public bool DEV9 = true;
        [DataMember]
        public bool SPEED = true;
        [DataMember]
        public bool SMAP = true;
        [DataMember]
        public bool ATA = true;
        [DataMember]
        public bool Winsock = true;
        [DataMember]
        public bool NetAdapter = true;
        [DataMember]
        public bool UDPSession = true;
        [DataMember]
        public bool DNSPacket = true;
        [DataMember]
        public bool DNSSession = true;
    }
}
