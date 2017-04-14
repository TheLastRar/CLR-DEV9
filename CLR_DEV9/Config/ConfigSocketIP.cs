using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigSocketIP
    {
        [DataMember]
        public bool AutoDNS1;
        [DataMember]
        public string DNS1;
        [DataMember]
        public bool AutoDNS2;
        [DataMember]
        public string DNS2;
        [DataMember(EmitDefaultValue = false)]
        public HashSet<ConfigIncomingPort> IncomingPorts;

        [OnDeserializing]
        void OnDeserializing(StreamingContext context)
        {
            Init();
        }

        private void Init()
        {
            AutoDNS1 = true;
            DNS1 = "0.0.0.0";
            AutoDNS2 = true;
            DNS2 = "0.0.0.0";
            IncomingPorts = new HashSet<ConfigIncomingPort>();
        }

        public ConfigSocketIP()
        {
            Init();
        }
    }
}
