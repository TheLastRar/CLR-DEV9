using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
#if NETCOREAPP2_0
#else
using System.Windows.Forms;
#endif

namespace CLRDEV9
{
    partial class HddCreate
#if NETCOREAPP2_0
        : IDisposable
#else
        : Form
#endif
    {
        public string filePath;
        public int neededSize;

        public Thread fileThread;

        private ManualResetEvent compleated = new ManualResetEvent(false);

        public HddCreate()
        {
#if NETCOREAPP2_0
#else
            InitializeComponent();
#endif
        }

        private void HddCreate_Load(object sender, EventArgs e)
        {

        }

#if NETCOREAPP2_0
        public void ShowDialog()
        {
            HddCreate_Shown(this, null);
        }
#else
#endif

        private void HddCreate_Shown(object sender, EventArgs e)
        {
#if NETCOREAPP2_0
#else
            pbFile.Maximum = neededSize;
#endif
            SetFileProgress(0);

            fileThread = new Thread(() => WriteImage(filePath, neededSize));
            fileThread.Start();
#if NETCOREAPP2_0
            fileThread.Join();
#endif
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
#if NETCOREAPP2_0
            Log_Info(currentSize + " / " + neededSize + "MiB");
#else
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
                lbProgress.Text = currentSize + " / " + neededSize + "MiB";
            }
#endif
        }

        private void SetError()
        {
#if NETCOREAPP2_0
            Log_Error("Unable to create file");
            DEV9Header.config.HddEnable = false;
            compleated.Set();
            SetClose();
#else
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
#endif
        }

        private void SetClose()
        {
#if NETCOREAPP2_0
#else
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
#endif
        }

#if NETCOREAPP2_0
#else
        private void HddCreate_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!compleated.WaitOne(0))
            {
                e.Cancel = true;
            }
        }
#endif

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.ATA, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.ATA, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.ATA, str);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
#if NETCOREAPP2_0
        public void Dispose()
        {
            compleated.Dispose();
        }
#else
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            compleated.Dispose();

            base.Dispose(disposing);
        }
#endif
    }
}
