#if NETCOREAPP2_0
#else
using System;
using System.Windows.Forms;

namespace CLRDEV9.Config
{
    partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        public string iniFolder = "";

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            cbEth.Checked = DEV9Header.config.EthEnable;
            cbHdd.Checked = DEV9Header.config.HddEnable;
        }

        private void btnEthOp_Click(object sender, EventArgs e)
        {
            ConfigFormEth eth = new ConfigFormEth();

            //hdd.iniFolder = iniFolder;
            eth.ShowDialog();
            eth.Dispose();
        }

        private void btnHddOp_Click(object sender, EventArgs e)
        {
            ConfigFormHdd hdd = new ConfigFormHdd();

            hdd.iniFolder = iniFolder;
            hdd.ShowDialog();
            hdd.Dispose();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            DEV9Header.config.EthEnable = cbEth.Checked;
            DEV9Header.config.HddEnable = cbHdd.Checked;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
#endif
