using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;
using LOG = PSE.CLR_PSE_PluginLog;

namespace CLRDEV9.DEV9.SMAP
{
    class AdapterLoader
    {
        public AdapterManager net = null;
        DEV9_State dev9 = null;

        public AdapterLoader(SMAP_State parsmap, DEV9_State pardev9)
        {
            dev9 = pardev9;
            net = new AdapterManager(parsmap);
        }

        NetAdapter GetNetAdapter()
        {
            NetAdapter na = null;

            if (DEV9Header.config.Eth.StartsWith("p"))
            {
                //na = new PCAPAdapter(dev9);
                return null;
            }
            else if (DEV9Header.config.Eth.StartsWith("t"))
            {
                na = new Tap.TAPAdapter(dev9);
            }
            else if (DEV9Header.config.Eth.StartsWith("w"))
            {
                na = new Winsock.Winsock(dev9);
            }
            else
                return null;

            if (!na.isInitialised())
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
                LOG.WriteLine(TraceEventType.Critical, (int)DEV9LogSources.PluginInterface, "NetAdapter", "Failed to GetNetAdapter()");
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
