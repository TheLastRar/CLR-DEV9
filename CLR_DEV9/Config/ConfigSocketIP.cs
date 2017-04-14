using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    [DataContract]
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigSocketIP
    {
        [DataMember]
        public bool AutoDNS1 = true;
        [DataMember]
        public string DNS1 = "0.0.0.0";
        [DataMember]
        public bool AutoDNS2 = true;
        [DataMember]
        public string DNS2 = "0.0.0.0";
    }
}
