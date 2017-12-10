#if NETCOREAPP2_0
#else
using CLRDEV9.DEV9.SMAP.Tap;
using CLRDEV9.DEV9.SMAP.WinPcap;
using CLRDEV9.DEV9.SMAP.Winsock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CLRDEV9.Config
{
    partial class ConfigFormEth : Form
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

        Dictionary<ConfigFile.EthAPI, int> apiIndex = new Dictionary<ConfigFile.EthAPI, int>();

        private void ConfigFormEth_Load(object sender, EventArgs e)
        {
            //Done here to load settings
            cbIntercept.Enabled = false;

            int curIndex = 1;
            cbAPI.Items.Clear();
            //Detect which API's we have
            //Winsock
            winsockAdapters = Winsock.GetAdapters();
            apiIndex.Add(ConfigFile.EthAPI.Winsock, curIndex);
            cbAPI.Items.Add("Sockets (Winsock)");
            curIndex++;
            //Windows Only
            if (PSE.CLR_PSE_Utils.IsWindows())
            {
                //Tap
                tapAdapters = TAPAdapter.GetAdapters();
                if (tapAdapters != null)
                {
                    cbAPI.Items.Add("Tap");
                    apiIndex.Add(ConfigFile.EthAPI.Tap, curIndex);
                    curIndex++;
                }
                //WinPcap
                winPcapAdapters = WinPcapAdapter.GetAdapters();
                if (winPcapAdapters != null)
                {
                    cbAPI.Items.Add("WinPcap Bridged");
                    apiIndex.Add(ConfigFile.EthAPI.WinPcapBridged, curIndex);
                    curIndex++;
                    cbAPI.Items.Add("WinPcap Switched (Promiscuous)");
                    apiIndex.Add(ConfigFile.EthAPI.WinPcapSwitched, curIndex);
                    curIndex++;
                }
            }

            if (apiIndex.ContainsKey(DEV9Header.config.EthType))
            {
                cbAPI.SelectedIndex = apiIndex[DEV9Header.config.EthType] - 1;
            }
            else
            {
                cbAPI.SelectedIndex = (int)ConfigFile.EthAPI.Null;
            }
        }

        private void cbAPI_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbAdapter.Items.Clear();
            cbAdapter.SelectedIndex = -1;
            //We will need to find if the selected adapter appears in the adapter list
            //And select it (TODO)

            //Get API selected
            KeyValuePair<ConfigFile.EthAPI, int> ret = apiIndex.FirstOrDefault(x => x.Value == cbAPI.SelectedIndex + 1);
            if (ret.Value == 0)
            {
                MessageBox.Show("Something when wrong");
                return;
            }

            string targetID = DEV9Header.config.Eth;

            switch (ret.Key)
            {
                case ConfigFile.EthAPI.Null:
                    selectedAPIAdapters = new List<string[]>();
                    break;
                case ConfigFile.EthAPI.Winsock:
                    selectedAPIAdapters = winsockAdapters;

                    break;
                case ConfigFile.EthAPI.Tap:
                    //cbAdapter.Items.Add("Tap");
                    selectedAPIAdapters = tapAdapters;
                    break;
                case ConfigFile.EthAPI.WinPcapBridged:
                    //cbAdapter.Items.Add("WinPcapBridged");
                    selectedAPIAdapters = winPcapAdapters;
                    break;
                case ConfigFile.EthAPI.WinPcapSwitched:
                    //cbAdapter.Items.Add("WinPcapSwitched");
                    selectedAPIAdapters = winPcapAdapters;

                    break;
            }

            switch (ret.Key)
            {
                case ConfigFile.EthAPI.Null:
                case ConfigFile.EthAPI.Winsock:
                    cbIntercept.Enabled = false;
                    cbIntercept.Checked = true;
                    break;
                case ConfigFile.EthAPI.Tap:
                case ConfigFile.EthAPI.WinPcapBridged:
                case ConfigFile.EthAPI.WinPcapSwitched:
                    cbIntercept.Enabled = true;
                    cbIntercept.Checked = DEV9Header.config.DirectConnectionSettings.InterceptDHCP;
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

        private void cbIntercept_CheckedChanged(object sender, EventArgs e)
        {
            if (cbIntercept.Enabled == true)
            {
                if (cbIntercept.Checked == true)
                {
                    tbIP.Enabled = true;
                    tbIP.Text = DEV9Header.config.DirectConnectionSettings.PS2IP;
                    cbAutoMask.Enabled = true;
                    cbAutoGate.Enabled = true;
                    cbAutoDNS1.Enabled = true;
                    cbAutoDNS2.Enabled = true;

                    cbAutoMask.Checked = DEV9Header.config.DirectConnectionSettings.AutoSubNet;
                    cbAutoGate.Checked = DEV9Header.config.DirectConnectionSettings.AutoGateway;
                    cbAutoDNS1.Checked = DEV9Header.config.DirectConnectionSettings.AutoDNS1;
                    cbAutoDNS2.Checked = DEV9Header.config.DirectConnectionSettings.AutoDNS2;

                    tbMask.Enabled = !cbAutoMask.Checked;
                    tbGate.Enabled = !cbAutoGate.Checked;
                    tbDNS1.Enabled = !cbAutoDNS1.Checked;
                    tbDNS2.Enabled = !cbAutoDNS2.Checked;
                }
                else
                {
                    tbIP.Enabled = false;
                    tbIP.Text = "0.0.0.0";
                    cbAutoMask.Enabled = false;
                    cbAutoGate.Enabled = false;
                    cbAutoDNS1.Enabled = false;
                    cbAutoDNS2.Enabled = false;

                    cbAutoMask.Checked = true;
                    cbAutoGate.Checked = true;
                    cbAutoDNS1.Checked = true;
                    cbAutoDNS2.Checked = true;

                    tbMask.Enabled = false;
                    tbGate.Enabled = false;
                    tbDNS1.Enabled = false;
                    tbDNS2.Enabled = false;
                }
            }
        }

        private void cbIntercept_EnabledChanged(object sender, EventArgs e)
        {
            if (cbIntercept.Enabled == true)
            {
                //In Direct Mode
                cbIntercept_CheckedChanged(sender, e);
                btnAdvanced.Enabled = false;

                cbLANMode.Checked = false;
                cbLANMode.Enabled = false;
            }
            else
            {
                tbIP.Enabled = false;
                tbIP.Text = "0.0.0.0";

                tbMask.Enabled = false;
                cbAutoMask.Enabled = false;
                cbAutoMask.Checked = true;

                tbGate.Enabled = false;
                cbAutoGate.Enabled = false;
                cbAutoGate.Checked = true;

                cbAutoDNS1.Enabled = true;
                cbAutoDNS2.Enabled = true;
                cbAutoDNS1.Checked = DEV9Header.config.SocketConnectionSettings.AutoDNS1;
                cbAutoDNS2.Checked = DEV9Header.config.SocketConnectionSettings.AutoDNS2;
                tbDNS1.Enabled = !cbAutoDNS1.Checked;
                tbDNS2.Enabled = !cbAutoDNS2.Checked;

                btnAdvanced.Enabled = true;

                cbLANMode.Enabled = true;
                cbLANMode.Checked = DEV9Header.config.SocketConnectionSettings.LANMode;
            }
            //Make sure UI gets updated
            tbMask_EnabledChanged(sender, e);
            tbGate_EnabledChanged(sender, e);
            tbDNS1_EnabledChanged(sender, e);
            tbDNS2_EnabledChanged(sender, e);
        }

        private void cbAutoMask_CheckedChanged(object sender, EventArgs e) { tbMask.Enabled = !cbAutoMask.Checked; }
        private void cbAutoGate_CheckedChanged(object sender, EventArgs e) { tbGate.Enabled = !cbAutoGate.Checked; }
        private void cbAutoDNS1_CheckedChanged(object sender, EventArgs e) { tbDNS1.Enabled = !cbAutoDNS1.Checked; }
        private void cbAutoDNS2_CheckedChanged(object sender, EventArgs e) { tbDNS2.Enabled = !cbAutoDNS2.Checked; }

        private void tbMask_EnabledChanged(object sender, EventArgs e)
        {
            if (tbMask.Enabled)
            {
                tbMask.Text = DEV9Header.config.DirectConnectionSettings.SubNet;
            }
            else
            {
                tbMask.Text = "0.0.0.0";
            }
        }
        private void tbGate_EnabledChanged(object sender, EventArgs e)
        {
            if (tbGate.Enabled)
            {
                tbGate.Text = DEV9Header.config.DirectConnectionSettings.Gateway;
            }
            else
            {
                tbGate.Text = "0.0.0.0";
            }
        }
        private void tbDNS1_EnabledChanged(object sender, EventArgs e)
        {
            if (tbDNS1.Enabled) //Are we in Direct or Socket mode
            {
                if (cbIntercept.Enabled)
                {
                    tbDNS1.Text = DEV9Header.config.DirectConnectionSettings.DNS1;
                }
                else
                {
                    tbDNS1.Text = DEV9Header.config.SocketConnectionSettings.DNS1;
                }
            }
            else
            {
                tbDNS1.Text = "0.0.0.0";
            }
        }
        private void tbDNS2_EnabledChanged(object sender, EventArgs e)
        {
            if (tbDNS2.Enabled) //Are we in Direct or Socket mode
            {
                if (cbIntercept.Enabled)
                {
                    tbDNS2.Text = DEV9Header.config.DirectConnectionSettings.DNS2;
                }
                else
                {
                    tbDNS2.Text = DEV9Header.config.SocketConnectionSettings.DNS2;
                }
            }
            else
            {
                tbDNS2.Text = "0.0.0.0";
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            //Get API selected
            KeyValuePair<ConfigFile.EthAPI, int> ret = apiIndex.FirstOrDefault(x => x.Value == cbAPI.SelectedIndex + 1);
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

            //Save Advanced settings
            switch (ret.Key)
            {
                case ConfigFile.EthAPI.Winsock:
                    DEV9Header.config.SocketConnectionSettings.LANMode = cbLANMode.Checked;
                    DEV9Header.config.SocketConnectionSettings.AutoDNS1 = cbAutoDNS1.Checked;
                    if (!cbAutoDNS1.Checked)
                    {
                        DEV9Header.config.SocketConnectionSettings.DNS1 = tbDNS1.Text;
                    }

                    DEV9Header.config.SocketConnectionSettings.AutoDNS2 = cbAutoDNS2.Checked;
                    if (!cbAutoDNS2.Checked)
                    {
                        DEV9Header.config.SocketConnectionSettings.DNS2 = tbDNS2.Text;
                    }
                    break;
                case ConfigFile.EthAPI.Tap:
                case ConfigFile.EthAPI.WinPcapBridged:
                case ConfigFile.EthAPI.WinPcapSwitched:
                    DEV9Header.config.DirectConnectionSettings.InterceptDHCP = cbIntercept.Checked;
                    if (cbIntercept.Checked)
                    {
                        DEV9Header.config.DirectConnectionSettings.PS2IP = tbIP.Text;

                        DEV9Header.config.DirectConnectionSettings.AutoSubNet = cbAutoMask.Checked;
                        if (!cbAutoMask.Checked)
                        {
                            DEV9Header.config.DirectConnectionSettings.SubNet = tbMask.Text;
                        }

                        DEV9Header.config.DirectConnectionSettings.AutoGateway = cbAutoGate.Checked;
                        if (!cbAutoGate.Checked)
                        {
                            DEV9Header.config.DirectConnectionSettings.Gateway = tbGate.Text;
                        }

                        DEV9Header.config.DirectConnectionSettings.AutoDNS1 = cbAutoDNS1.Checked;
                        if (!cbAutoDNS1.Checked)
                        {
                            DEV9Header.config.DirectConnectionSettings.DNS1 = tbDNS1.Text;
                        }

                        DEV9Header.config.DirectConnectionSettings.AutoDNS2 = cbAutoDNS2.Checked;
                        if (!cbAutoDNS2.Checked)
                        {
                            DEV9Header.config.DirectConnectionSettings.DNS2 = tbDNS2.Text;
                        }
                    }
                    break;
            }

            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            //Get API selected
            KeyValuePair<ConfigFile.EthAPI, int> ret = apiIndex.FirstOrDefault(x => x.Value == cbAPI.SelectedIndex + 1);
            if (ret.Value == 0)
            {
                MessageBox.Show("Something when wrong");
                return;
            }

            switch (ret.Key)
            {
                case ConfigFile.EthAPI.Null:
                    break;
                case ConfigFile.EthAPI.Winsock:
                    ConfigFormIncomingPorts cfip = new ConfigFormIncomingPorts();
                    cfip.ShowDialog();
                    cfip.Dispose();
                    break;
                case ConfigFile.EthAPI.Tap:
                case ConfigFile.EthAPI.WinPcapBridged:
                case ConfigFile.EthAPI.WinPcapSwitched:

                    break;
            }
        }
    }
}
#endif
