using System.Net;
using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    //TODO Create config screen to add these.
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigHost
    {
        [DataMember]
        public string Desc = "";
        [DataMember]
        public string URL = "";
        [DataMember]
        public string IP = "0.0.0.0";
        [DataMember]
        public bool Enabled = false;

        public override bool Equals(object obj)
        {
            return obj is ConfigHost && this == (ConfigHost)obj;
        }
        public override int GetHashCode()
        {
            return IPAddress.Parse(IP).GetHashCode();
        }
        public static bool operator ==(ConfigHost x, ConfigHost y)
        {
            return x.URL == y.URL &&
                x.IP == y.IP;
        }
        public static bool operator !=(ConfigHost x, ConfigHost y)
        {
            return !(x == y);
        }
    }
}
