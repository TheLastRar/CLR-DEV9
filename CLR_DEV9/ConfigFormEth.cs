using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CLRDEV9.DEV9.SMAP.Winsock;
using CLRDEV9.DEV9.SMAP.Tap;
using CLRDEV9.DEV9.SMAP.WinPcap;


namespace CLRDEV9
{
    public partial class ConfigFormEth : Form
    {
        List<string[]> winsockAdapters = null;
        List<string[]> tapAdapters = null;
        List<string[]> winPcapAdapters = null;

        List<string[]> selectedAPIAdapters = null;

        public ConfigFormEth()
        {
            InitializeComponent();
        }
        //TODO keep track of which api has which index

        Dictionary<Config.EthAPI, int> apiIndex = new Dictionary<Config.EthAPI, int>();

        private void ConfigFormEth_Load(object sender, EventArgs e)
        {

            int curIndex = 1;
            cbAPI.Items.Clear();
            //Detech which API's we have
            //Winsock
            winsockAdapters = Winsock.GetAdapters();
            apiIndex.Add(Config.EthAPI.Winsock, curIndex);
            cbAPI.Items.Add("Sockets (Winsock)");
            curIndex++;
            //Tap
            tapAdapters = TAPAdapter.GetAdapters();
            if (tapAdapters != null)
            {
                cbAPI.Items.Add("Tap");
                apiIndex.Add(Config.EthAPI.Tap, curIndex);
                curIndex++;
            }
            //WinPcap
            winPcapAdapters = WinPcapAdapter.GetAdapters();
            if (winPcapAdapters != null)
            {
                cbAPI.Items.Add("WinPcap Bridged");
                apiIndex.Add(Config.EthAPI.WinPcapBridged, curIndex);
                curIndex++;
                cbAPI.Items.Add("WinPcap Switched (Promiscuous)");
                apiIndex.Add(Config.EthAPI.WinPcapSwitched, curIndex);
                curIndex++;
            }

            cbAPI.SelectedIndex = apiIndex[DEV9Header.config.EthType] - 1;

            //Find Selected Adapter in list
            for (int i = 0; i< selectedAPIAdapters.Count; i++)
            {
                if (selectedAPIAdapters[i][2] == DEV9Header.config.Eth)
                {
                    cbAdapter.SelectedIndex = i;
                    break;
                }
            }
        }

        private void cbAPI_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbAdapter.Items.Clear();
            cbAdapter.SelectedIndex = -1;
            //We will need to find if the selected adapter appears in the adapter list
            //And select it (TODO)

            //Get API selected
            KeyValuePair<Config.EthAPI,int> ret = apiIndex.FirstOrDefault(x => x.Value == cbAPI.SelectedIndex + 1);
            if (ret.Value == 0)
            {
                MessageBox.Show("Something when wrong");
                return;
            }

            string targetID = DEV9Header.config.Eth;

            switch (ret.Key)
            {
                case Config.EthAPI.Winsock:
                    selectedAPIAdapters = winsockAdapters;
                    //cbAdapter.Items.Add("Winock");
                    break;
                case Config.EthAPI.Tap:
                    //cbAdapter.Items.Add("Tap");
                    selectedAPIAdapters = tapAdapters;
                    break;
                case Config.EthAPI.WinPcapBridged:
                    //cbAdapter.Items.Add("WinPcapBridged");
                    selectedAPIAdapters = winPcapAdapters;
                    break;
                case Config.EthAPI.WinPcapSwitched:
                    //cbAdapter.Items.Add("WinPcapSwitched");
                    selectedAPIAdapters = winPcapAdapters;
                    break;
            }

            //Reselect adapter of same guid
            for (int i = 0; i < selectedAPIAdapters.Count; i++)
            {
                cbAdapter.Items.Add(selectedAPIAdapters[i][0] + " - " + selectedAPIAdapters[i][1]);
                if (selectedAPIAdapters[i][2] == targetID)
                {
                    cbAdapter.SelectedIndex = i;
                }
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            //Get API selected
            KeyValuePair<Config.EthAPI, int> ret = apiIndex.FirstOrDefault(x => x.Value == cbAPI.SelectedIndex + 1);
            if (ret.Value == 0)
            {
                MessageBox.Show("Please select an API");
                return;
            }
            if (cbAdapter.SelectedIndex == -1)
            {
                MessageBox.Show("Please select an adapter");
                return;
            }
            DEV9Header.config.EthType = ret.Key;
            DEV9Header.config.Eth = selectedAPIAdapters[cbAdapter.SelectedIndex][2];
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
