using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System.Runtime.Serialization;

namespace CLRDEV9.Config
{
    //TODO Create config screen to add these.
    //TODO support TCP
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigIncomingPort
    {
        [DataMember]
        public string Desc = "";
        [DataMember]
        public IPType Protocol = (IPType)0;
        [DataMember]
        public ushort Port = 0;
        [DataMember]
        public bool Enabled = false;

        public override bool Equals(object obj)
        {
            return obj is ConfigIncomingPort && this == (ConfigIncomingPort)obj;
        }
        public override int GetHashCode()
        {
            //Size of class is 8 + 16 = 24bits
            //hash is 32bits
            //store full class information in hash
            return (byte)Protocol + (Port << 8);
        }
        public static bool operator ==(ConfigIncomingPort x, ConfigIncomingPort y)
        {
            return x.Protocol == y.Protocol &&
                x.Port == y.Port;
        }
        public static bool operator !=(ConfigIncomingPort x, ConfigIncomingPort y)
        {
            return !(x == y);
        }
    }
}
