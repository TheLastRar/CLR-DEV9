using CLRDEV9.Config;
using CLRDEV9.DEV9.SMAP.Data;
using System.Diagnostics;
using LOG = PSE.CLR_PSE_PluginLog;

namespace CLRDEV9.DEV9.SMAP
{
    class AdapterLoader
    {
        public AdapterManager net = null;
        DEV9_State dev9 = null;

        public AdapterLoader(SMAP_State parSmap, DEV9_State parDev9)
        {
            dev9 = parDev9;
            net = new AdapterManager(parSmap);
        }

        NetAdapter GetNetAdapter()
        {
            NetAdapter na = null;
            //TODO Make this use EthType
            switch (DEV9Header.config.EthType)
            {
                case ConfigFile.EthAPI.Null:
                    return null;
                case ConfigFile.EthAPI.Winsock:
                    na = new Winsock.Winsock(dev9, DEV9Header.config.Eth);
                    break;
                case ConfigFile.EthAPI.Tap:
                    na = new Tap.TAPAdapter(dev9, DEV9Header.config.Eth);
                    break;
                case ConfigFile.EthAPI.WinPcapBridged:
                    na = new WinPcap.WinPcapAdapter(dev9, DEV9Header.config.Eth, false);
                    break;
                case ConfigFile.EthAPI.WinPcapSwitched:
                    na = new WinPcap.WinPcapAdapter(dev9, DEV9Header.config.Eth, true);
                    break;
                default:
                    return null;
            }

            if (!na.IsInitialised())
            {
                na.Dispose();
                return null;
            }
            return na;
        }

        public int Open()
        {
            NetAdapter na = GetNetAdapter();
            if (na == null)
            {
                LOG.WriteLine(TraceEventType.Critical, (int)DEV9LogSources.NetAdapter, "Failed to GetNetAdapter()");
            }
            else
            {
                net.InitNet(na);
            }
            return 0;
        }
        public void Close()
        {
            net.TermNet();
        }
    }
}
