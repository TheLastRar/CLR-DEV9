using PSE;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace CLRDEV9.Config
{
    [DataContract(Name = "Config", Namespace = "http://schemas.datacontract.org/2004/07/CLRDEV9")]
    class ConfigFile
    {
        [DataMember]
        public bool EthEnable;
        [DataMember]
        public string Eth;
        [DataMember]
        public EthAPI EthType;
        [DataMember(EmitDefaultValue = false)]
        public ConfigDirectIP DirectConnectionSettings;
        [DataMember(EmitDefaultValue = false)]
        public ConfigSocketIP SocketConnectionSettings;

        [DataMember(EmitDefaultValue = false)]
        public HashSet<ConfigHost> Hosts;

        [DataMember]
        public bool HddEnable;
        [DataMember]
        public string Hdd;
        [DataMember]
        public int HddSize;

        [DataMember]
        public ConfigLogging EnableLogging;

        [DataMember]
        public ConfigLogging EnableTracing;

        [OnDeserializing]
        void OnDeserializing(StreamingContext context)
        {
            Init();
        }

        private void Init()
        {
            Hdd = DEV9Header.HDD_DEF;
            HddSize = 8 * 1024;
            Eth = DEV9Header.ETH_DEF;
            EthType = EthAPI.Winsock;
            EthEnable = true;
            HddEnable = false;
            DirectConnectionSettings = new ConfigDirectIP();
            SocketConnectionSettings = new ConfigSocketIP();
            EnableLogging = new ConfigLogging();
            EnableTracing = new ConfigLogging();
            EnableTracing.SetAllFalse();
            Hosts = new HashSet<ConfigHost>();
            Hosts.Add(new ConfigHost()
            {
                Desc = "Set DNS to 192.0.2.1 to use this host list",
                URL = "www.example.com",
                IP = "0.0.0.0",
                Enabled = false
            });
        }

        public ConfigFile()
        {
            Init();
        }

        public enum EthAPI : int
        {
            Null = -1,
            Winsock = 0, //Sockets
            Tap = 1,
            WinPcapBridged = 2,
            WinPcapSwitched = 3
        }

        public static void DoConfig(string iniFolderPath, string iniFileName)
        {
            //TODO Figure out Core Config
#if NETCOREAPP
#else
            ConfigForm cfgF = new ConfigForm();
            cfgF.iniFolder = iniFolderPath;
            cfgF.ShowDialog();
            cfgF.Dispose();
#endif
        }

        public static void SaveConf(string iniFolderPath, string iniFileName)
        {
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "Save Config");

            iniFolderPath = iniFolderPath.TrimEnd(Path.DirectorySeparatorChar);
            iniFolderPath = iniFolderPath.TrimEnd(Path.AltDirectorySeparatorChar);

            string filePath = iniFolderPath + Path.DirectorySeparatorChar + iniFileName;

            DataContractSerializer ConfSerializer = new DataContractSerializer(typeof(ConfigFile));

            var settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t"
            };

            FileStream fileWriter = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            using (XmlWriter xmlWriter = XmlWriter.Create(fileWriter, settings))
            {
                ConfSerializer.WriteObject(xmlWriter, DEV9Header.config);
            }
            fileWriter.Close();
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "Done");
        }

        public static void LoadConf(string iniFolderPath, string iniFileName)
        {
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "Load Config");

            iniFolderPath = iniFolderPath.TrimEnd(Path.DirectorySeparatorChar);
            iniFolderPath = iniFolderPath.TrimEnd(Path.AltDirectorySeparatorChar);

            string filePath = iniFolderPath + Path.DirectorySeparatorChar + iniFileName;

            if (File.Exists(filePath))
            {
                DataContractSerializer ConfSerializer = new DataContractSerializer(typeof(ConfigFile));
                FileStream Reader = new FileStream(filePath, FileMode.Open);
                DEV9Header.config = (ConfigFile)ConfSerializer.ReadObject(Reader);
                Reader.Close();
                //Update from old config
                if (DEV9Header.config.Eth == "winsock")
                {
                    DEV9Header.config.Eth = DEV9Header.ETH_DEF;
                }
                CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "Done");
                return;
            }
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.PluginInterface, "No Config, Create Default");
            DEV9Header.config = new ConfigFile();

            SaveConf(iniFolderPath, iniFileName);
        }
    }
}
