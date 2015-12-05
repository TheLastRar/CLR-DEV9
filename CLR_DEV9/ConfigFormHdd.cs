using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CLRDEV9
{
    public partial class ConfigFormHdd : Form
    {
        public ConfigFormHdd()
        {
            InitializeComponent();
        }

        private void ConfigFormHdd_Load(object sender, EventArgs e)
        {
            tbPath.Text = DEV9Header.config.Hdd;
            comboSize.Text = (DEV9Header.config.HddSize / 1024).ToString();
        }

        private void tbPath_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(tbPath.Text))
            {
                FileInfo f = new FileInfo(tbPath.Text);
                long Size = f.Length / (1014 * 1024 * 1024);
                comboSize.Text = Size.ToString();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            ofHdd.ShowDialog();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ofHdd_FileOk(object sender, CancelEventArgs e)
        {
            tbPath.Text = Path.GetFullPath(ofHdd.FileName);

            if (File.Exists(tbPath.Text))
            {
                FileInfo f = new FileInfo(tbPath.Text);
                long Size = f.Length / (1014 * 1024 * 1024);
                comboSize.Text = Size.ToString();
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            int size = int.Parse(comboSize.Text);
            if (size > 127) //PS2 limit is apparently 1TiB (1024GiB)
            {
                //Limit Size as we don't support 48bit stuff
                size = 127;
            }
            DEV9Header.config.HddSize = size * 1024;

            DEV9Header.config.Hdd = tbPath.Text;

            if (!File.Exists(tbPath.Text))
            {
                //Need to Zero fill the hdd image
                HddCreate hddcreator = new HddCreate();
                hddcreator.neededSize = DEV9Header.config.HddSize;
                hddcreator.filePath = tbPath.Text;

                hddcreator.ShowDialog();
                hddcreator.Dispose();
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
