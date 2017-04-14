using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigDirectIP
    {
        [DataMember]
        public bool InterceptDHCP = false;
        [DataMember]
        public string PS2IP = "0.0.0.0";
        [DataMember]
        public bool AutoSubNet = true;
        [DataMember]
        public string SubNet = "0.0.0.0";
        [DataMember]
        public bool AutoGateway = true;
        [DataMember]
        public string Gateway = "0.0.0.0";
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
