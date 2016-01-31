using System.Runtime.Serialization;

namespace CLRDEV9
{
    [DataContract]
    class ConfigDirectIP
    {
        [DataMember]
        public bool InterceptDHCP = false;
        [DataMember]
        public string PS2IP = "";
        [DataMember]
        public bool AutoSubNet = true;
        [DataMember]
        public string SubNet = "";
        [DataMember]
        public bool AutoGateway = true;
        [DataMember]
        public string Gateway = "";
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
