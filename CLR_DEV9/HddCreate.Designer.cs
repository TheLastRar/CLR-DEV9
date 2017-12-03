#if NETCOREAPP2_0
#else

namespace CLRDEV9
{
    partial class HddCreate
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

#region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbProgress = new System.Windows.Forms.Label();
            this.pbFile = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lbProgress
            // 
            this.lbProgress.AutoSize = true;
            this.lbProgress.Location = new System.Drawing.Point(12, 51);
            this.lbProgress.Name = "lbProgress";
            this.lbProgress.Size = new System.Drawing.Size(48, 13);
            this.lbProgress.TabIndex = 0;
            this.lbProgress.Text = "Progress";
            // 
            // pbFile
            // 
            this.pbFile.Location = new System.Drawing.Point(13, 13);
            this.pbFile.Name = "pbFile";
            this.pbFile.Size = new System.Drawing.Size(571, 35);
            this.pbFile.TabIndex = 1;
            // 
            // HddCreate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(596, 73);
            this.Controls.Add(this.pbFile);
            this.Controls.Add(this.lbProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HddCreate";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Creating Hdd";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.HddCreate_FormClosing);
            this.Load += new System.EventHandler(this.HddCreate_Load);
            this.Shown += new System.EventHandler(this.HddCreate_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private System.Windows.Forms.Label lbProgress;
        private System.Windows.Forms.ProgressBar pbFile;
    }
}
#endif
