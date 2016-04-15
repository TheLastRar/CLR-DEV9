using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CLRDEV9
{
    partial class HddCreate : Form
    {
        public string filePath;
        public int neededSize;

        public Thread fileThread;

        private ManualResetEvent compleated = new ManualResetEvent(false);

        public HddCreate()
        {
            InitializeComponent();
        }

        private void HddCreate_Load(object sender, EventArgs e)
        {

        }

        private void HddCreate_Shown(object sender, EventArgs e)
        {
            pbFile.Maximum = neededSize;
            SetFileProgress(0);

            fileThread = new Thread(() => WriteImage(filePath, neededSize));
            fileThread.Start();
        }

        private void WriteImage(string hddPath, int reqSizeMiB)
        {
            byte[] buff = new byte[4 * 1024]; //4kb

            FileStream newImage = new FileStream(hddPath, FileMode.CreateNew, FileAccess.ReadWrite);

            try
            {
                newImage.SetLength(((long)reqSizeMiB) * 1024L * 1024L);
            }
            catch
            {
                SetError();
                newImage.Close();
                newImage.Dispose();
                File.Delete(filePath);
                return;
            }

            try
            {
                for (int iMiB = 0; iMiB < reqSizeMiB; iMiB++)
                {
                    for (int i4kb = 0; i4kb < 256; i4kb++)
                    {
                        newImage.Write(buff, 0, buff.Length);
                    }
                    SetFileProgress(iMiB + 1);
                }
                newImage.Flush();
            }
            catch
            {
                newImage.Close();
                newImage.Dispose();
                SetError();
                File.Delete(filePath);
                return;
            }

            newImage.Close();
            newImage.Dispose();

            compleated.Set();
            SetClose();
        }

        private void SetFileProgress(int currentSize)
        {
            if (InvokeRequired)
            {
                Invoke((Action)delegate ()
                {
                    SetFileProgress(currentSize);
                });
            }
            else
            {
                pbFile.Value = currentSize;
                lbProgress.Text = currentSize + "//" + neededSize + "MiB";
            }
        }

        private void SetError()
        {
            if (InvokeRequired)
            {
                Invoke((Action)delegate ()
                {
                    SetError();
                });
            }
            else
            {
                MessageBox.Show("Unable to create file");
                DEV9Header.config.HddEnable = false;
                compleated.Set();
                SetClose();
            }
        }

        private void SetClose()
        {
            if (InvokeRequired)
            {
                Invoke((Action)delegate ()
                {
                    Close();
                });
            }
            else
            {
                Close();
            }
        }

        private void HddCreate_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!compleated.WaitOne(0))
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            compleated.Dispose();

            base.Dispose(disposing);
        }
    }
}
