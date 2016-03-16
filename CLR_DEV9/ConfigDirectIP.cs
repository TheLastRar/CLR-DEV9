using System.Runtime.Serialization;

namespace CLRDEV9
{
    [DataContract]
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

        //[OnDeserializing]
        //void OnDeserializing(StreamingContext context)
        //{
        //    Hdd = DEV9Header.HDD_DEF;
        //    HddSize = 8 * 1024;
        //    Eth = DEV9Header.ETH_DEF;
        //    EthType = EthAPI.Winsock;
        //    EthEnable = true;
        //    HddEnable = false;

        //    DirectConnectionSettings = new ConfigDirectIP();
        //    SocketConnectionSettings = new ConfigSocketIP();
        //}
    }
}
