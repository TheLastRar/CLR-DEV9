using System.Net;
using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigLogging
    {
        [DataMember]
        public bool Error = true;
        [DataMember]
        public bool Verbose = true;
        [DataMember]
        public bool Information = true;
    }
}
