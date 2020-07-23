using System;
using System.Net;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace CLRDEV9.Config
{
    enum ConfigLogLevel
    {
        Off = 0,
        Error = 1,
        Information = 2,
        Verbose = 3,
        //Support old config files
        @true = 2,
        @false = 1,
    };


    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigLogging
    {
        //TODO, rest of the log sources
        [DataMember]
        public ConfigLogLevel Test = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel DEV9 = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel SPEED = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel SMAP = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel ATA = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel Winsock = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel NetAdapter = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel UDPSession = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel DNSPacket = ConfigLogLevel.Information;
        [DataMember]
        public ConfigLogLevel DNSSession = ConfigLogLevel.Information;

        public static SourceLevels ToSourceLevel(ConfigLogLevel cll)
        {
            switch (cll)
            {
                case ConfigLogLevel.Off:
                    return SourceLevels.Off;
                case ConfigLogLevel.Error:
                    return SourceLevels.Error;
                case ConfigLogLevel.Information:
                    return SourceLevels.Information;
                case ConfigLogLevel.Verbose:
                    return SourceLevels.Verbose;
                default:
                    throw new Exception("Error Unkown Log Level");
            }
        }

        //public static ConfigLogLevel ToConfigLogLevel(SourceLevels sl)
        //{
        //    switch (sl)
        //    {
        //        case SourceLevels.Off:
        //            return ConfigLogLevel.Off;
        //        case SourceLevels.Error:
        //            return ConfigLogLevel.Error;
        //        case SourceLevels.Information:
        //            return ConfigLogLevel.Information;
        //        case SourceLevels.Verbose:
        //            return ConfigLogLevel.Verbose;
        //        default:
        //            throw new Exception("Error Unkown Log Level");
        //    }
        //}
    }
}
