using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CLRDEV9.Config
{
    public partial class ConfigFormIncomingPorts : Form
    {
        private const int CELL_DESC = 0;
        private const int CELL_PROT = 1;
        private const int CELL_PORT = 2;
        private const int CELL_EN = 3;
        public ConfigFormIncomingPorts()
        {
            InitializeComponent();

            cProtocol.DataSource = new IPType[] { IPType.UDP };
            cProtocol.ValueType = typeof(IPType);

            cEnable.ValueType = typeof(bool);
            cEnable.TrueValue = true;
            cEnable.FalseValue = false;

            foreach (ConfigIncomingPort port in DEV9Header.config.SocketConnectionSettings.IncomingPorts)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(dgPorts);
                row.Cells[CELL_DESC].Value = port.Desc;
                row.Cells[CELL_PROT].Value = port.Protocol;
                row.Cells[CELL_PORT].Value = port.Port.ToString();
                row.Cells[CELL_EN].Value = port.Enabled;
                dgPorts.Rows.Add(row);
            }
        }

        private void dgPorts_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column.Equals(cDesc))
            {
                int res = AlphanumComparator(e.CellValue1 as string, e.CellValue2 as string);
                if (res == 0)
                {
                    e.Handled = true;
                    e.SortResult = res;
                }
            }
        }

        private int AlphanumComparator(string x, string y)
        {
            // [1] Validate the arguments.
            string s1 = x;
            if (s1 == null)
            {
                return 0;
            }

            string s2 = y;
            if (s2 == null)
            {
                return 0;
            }

            int len1 = s1.Length;
            int len2 = s2.Length;
            int marker1 = 0;
            int marker2 = 0;

            // [2] Loop over both Strings.
            while (marker1 < len1 & marker2 < len2)
            {
                // [3] Get Chars.
                char ch1 = s1[marker1];
                char ch2 = s2[marker2];

                char[] space1 = new char[len1];
                int loc1 = 0;
                char[] space2 = new char[len2];
                int loc2 = 0;

                // [4] Collect digits for String one.
                do
                {
                    space1[loc1] = ch1;
                    loc1 += 1;
                    marker1 += 1;

                    if (marker1 < len1)
                    {
                        ch1 = s1[marker1];
                    }
                    else
                    {
                        break;
                    }
                } while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                // [5] Collect digits for String two.
                do
                {
                    space2[loc2] = ch2;
                    loc2 += 1;
                    marker2 += 1;

                    if (marker2 < len2)
                    {
                        ch2 = s2[marker2];
                    }
                    else
                    {
                        break;
                    }
                } while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                // [6] Convert to Strings.
                string str1 = new string(space1);
                string str2 = new string(space2);

                // [7] Parse Strings into Integers.
                int result = 0;
                if (char.IsDigit(space1[0]) & char.IsDigit(space2[0]))
                {
                    int thisNumericChunk = int.Parse(str1);
                    int thatNumericChunk = int.Parse(str2);
                    result = thisNumericChunk.CompareTo(thatNumericChunk);
                }
                else
                {
                    result = str1.CompareTo(str2);
                }

                // [8] Return result if not equal.
                if (!(result == 0))
                {
                    return result;
                }
            }

            // [9] Compare lengths.
            return len1 - len2;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dgPorts);
            row.Cells[CELL_DESC].Value = "";
            row.Cells[CELL_PROT].Value = IPType.UDP;
            row.Cells[CELL_PORT].Value = "0";
            row.Cells[CELL_EN].Value = true;
            dgPorts.Rows.Add(row);
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            if (dgPorts.SelectedCells.Count != 0)
            {
                dgPorts.Rows.RemoveAt(dgPorts.SelectedCells[0].RowIndex);
            }
        }

        private void dgPorts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dgPorts.BeginEdit(true);
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            HashSet<ConfigIncomingPort> newconfig = new HashSet<ConfigIncomingPort>();

            foreach (DataGridViewRow row in dgPorts.Rows)
            {
                ConfigIncomingPort port = new ConfigIncomingPort();
                port.Desc = (string)row.Cells[CELL_DESC].Value;
                port.Protocol = (IPType)row.Cells[CELL_PROT].Value;
                if ((!UInt16.TryParse((string)row.Cells[CELL_PORT].Value, out port.Port)) || port.Port == 0)
                {
                    MessageBox.Show("Invalid port specified for " + port.Desc);
                    return;
                }
                port.Enabled = (bool)row.Cells[CELL_EN].Value;
                newconfig.Add(port);
            }
            DEV9Header.config.SocketConnectionSettings.IncomingPorts = newconfig;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
