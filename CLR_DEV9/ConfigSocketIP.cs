using System.Runtime.Serialization;

namespace CLRDEV9
{
    [DataContract]
    class ConfigSocketIP
    {
        [DataMember]
        public bool AutoDNS1 = true;
        [DataMember]
        public string DNS1 = "";
        [DataMember]
        public bool AutoDNS2 = true;
        [DataMember]
        public string DNS2 = "";
    }
}
