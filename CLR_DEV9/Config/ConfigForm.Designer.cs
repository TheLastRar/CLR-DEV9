
namespace CLRDEV9.Config
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            base.Dispose(disposing);
        }

#region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbEth = new System.Windows.Forms.CheckBox();
            this.btnEthOp = new System.Windows.Forms.Button();
            this.cbHdd = new System.Windows.Forms.CheckBox();
            this.btnHddOp = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbEth
            // 
            this.cbEth.AutoSize = true;
            this.cbEth.Location = new System.Drawing.Point(12, 12);
            this.cbEth.Name = "cbEth";
            this.cbEth.Size = new System.Drawing.Size(102, 17);
            this.cbEth.TabIndex = 0;
            this.cbEth.Text = "Enable Ethernet";
            this.cbEth.UseVisualStyleBackColor = true;
            // 
            // btnEthOp
            // 
            this.btnEthOp.Location = new System.Drawing.Point(120, 8);
            this.btnEthOp.Name = "btnEthOp";
            this.btnEthOp.Size = new System.Drawing.Size(75, 23);
            this.btnEthOp.TabIndex = 2;
            this.btnEthOp.Text = "Options";
            this.btnEthOp.UseVisualStyleBackColor = true;
            this.btnEthOp.Click += new System.EventHandler(this.btnEthOp_Click);
            // 
            // cbHdd
            // 
            this.cbHdd.AutoSize = true;
            this.cbHdd.Location = new System.Drawing.Point(12, 41);
            this.cbHdd.Name = "cbHdd";
            this.cbHdd.Size = new System.Drawing.Size(82, 17);
            this.cbHdd.TabIndex = 3;
            this.cbHdd.Text = "Enable Hdd";
            this.cbHdd.UseVisualStyleBackColor = true;
            // 
            // btnHddOp
            // 
            this.btnHddOp.Location = new System.Drawing.Point(120, 37);
            this.btnHddOp.Name = "btnHddOp";
            this.btnHddOp.Size = new System.Drawing.Size(75, 23);
            this.btnHddOp.TabIndex = 5;
            this.btnHddOp.Text = "Options";
            this.btnHddOp.UseVisualStyleBackColor = true;
            this.btnHddOp.Click += new System.EventHandler(this.btnHddOp_Click);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(12, 66);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 6;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(93, 66);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(207, 101);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnHddOp);
            this.Controls.Add(this.cbHdd);
            this.Controls.Add(this.btnEthOp);
            this.Controls.Add(this.cbEth);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "ConfigForm";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private System.Windows.Forms.CheckBox cbEth;
        private System.Windows.Forms.Button btnEthOp;
        private System.Windows.Forms.CheckBox cbHdd;
        private System.Windows.Forms.Button btnHddOp;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
    }
}
