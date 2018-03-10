
namespace CLRDEV9.Config
{
    partial class ConfigFormEth
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
            this.label1 = new System.Windows.Forms.Label();
            this.cbAPI = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbAdapter = new System.Windows.Forms.ComboBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbIP = new System.Windows.Forms.TextBox();
            this.tbMask = new System.Windows.Forms.TextBox();
            this.cbIntercept = new System.Windows.Forms.CheckBox();
            this.cbAutoMask = new System.Windows.Forms.CheckBox();
            this.tbGate = new System.Windows.Forms.TextBox();
            this.cbAutoGate = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbDNS1 = new System.Windows.Forms.TextBox();
            this.cbAutoDNS1 = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbDNS2 = new System.Windows.Forms.TextBox();
            this.cbAutoDNS2 = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.cbLANMode = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Connection Method";
            // 
            // cbAPI
            // 
            this.cbAPI.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAPI.FormattingEnabled = true;
            this.cbAPI.Location = new System.Drawing.Point(12, 25);
            this.cbAPI.Name = "cbAPI";
            this.cbAPI.Size = new System.Drawing.Size(263, 21);
            this.cbAPI.TabIndex = 1;
            this.cbAPI.SelectedIndexChanged += new System.EventHandler(this.cbAPI_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Adapter";
            // 
            // cbAdapter
            // 
            this.cbAdapter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAdapter.FormattingEnabled = true;
            this.cbAdapter.Location = new System.Drawing.Point(12, 65);
            this.cbAdapter.Name = "cbAdapter";
            this.cbAdapter.Size = new System.Drawing.Size(263, 21);
            this.cbAdapter.TabIndex = 3;
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(12, 247);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(93, 247);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 118);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "PS2 IP Address";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 144);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Subnet Mask";
            // 
            // tbIP
            // 
            this.tbIP.Enabled = false;
            this.tbIP.Location = new System.Drawing.Point(99, 115);
            this.tbIP.Name = "tbIP";
            this.tbIP.Size = new System.Drawing.Size(122, 20);
            this.tbIP.TabIndex = 8;
            this.tbIP.Text = "0.0.0.0";
            // 
            // tbMask
            // 
            this.tbMask.Enabled = false;
            this.tbMask.Location = new System.Drawing.Point(99, 141);
            this.tbMask.Name = "tbMask";
            this.tbMask.Size = new System.Drawing.Size(122, 20);
            this.tbMask.TabIndex = 9;
            this.tbMask.Text = "0.0.0.0";
            this.tbMask.EnabledChanged += new System.EventHandler(this.tbMask_EnabledChanged);
            // 
            // cbIntercept
            // 
            this.cbIntercept.AutoSize = true;
            this.cbIntercept.Location = new System.Drawing.Point(12, 92);
            this.cbIntercept.Name = "cbIntercept";
            this.cbIntercept.Size = new System.Drawing.Size(101, 17);
            this.cbIntercept.TabIndex = 10;
            this.cbIntercept.Text = "Intercept DHCP";
            this.cbIntercept.UseVisualStyleBackColor = true;
            this.cbIntercept.CheckedChanged += new System.EventHandler(this.cbIntercept_CheckedChanged);
            this.cbIntercept.EnabledChanged += new System.EventHandler(this.cbIntercept_EnabledChanged);
            // 
            // cbAutoMask
            // 
            this.cbAutoMask.AutoSize = true;
            this.cbAutoMask.Enabled = false;
            this.cbAutoMask.Location = new System.Drawing.Point(227, 143);
            this.cbAutoMask.Name = "cbAutoMask";
            this.cbAutoMask.Size = new System.Drawing.Size(48, 17);
            this.cbAutoMask.TabIndex = 11;
            this.cbAutoMask.Text = "Auto";
            this.cbAutoMask.UseVisualStyleBackColor = true;
            this.cbAutoMask.CheckedChanged += new System.EventHandler(this.cbAutoMask_CheckedChanged);
            // 
            // tbGate
            // 
            this.tbGate.Enabled = false;
            this.tbGate.Location = new System.Drawing.Point(99, 167);
            this.tbGate.Name = "tbGate";
            this.tbGate.Size = new System.Drawing.Size(122, 20);
            this.tbGate.TabIndex = 12;
            this.tbGate.Text = "0.0.0.0";
            this.tbGate.EnabledChanged += new System.EventHandler(this.tbGate_EnabledChanged);
            // 
            // cbAutoGate
            // 
            this.cbAutoGate.AutoSize = true;
            this.cbAutoGate.Enabled = false;
            this.cbAutoGate.Location = new System.Drawing.Point(227, 169);
            this.cbAutoGate.Name = "cbAutoGate";
            this.cbAutoGate.Size = new System.Drawing.Size(48, 17);
            this.cbAutoGate.TabIndex = 13;
            this.cbAutoGate.Text = "Auto";
            this.cbAutoGate.UseVisualStyleBackColor = true;
            this.cbAutoGate.CheckedChanged += new System.EventHandler(this.cbAutoGate_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 170);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Gateway IP";
            // 
            // tbDNS1
            // 
            this.tbDNS1.Enabled = false;
            this.tbDNS1.Location = new System.Drawing.Point(99, 193);
            this.tbDNS1.Name = "tbDNS1";
            this.tbDNS1.Size = new System.Drawing.Size(122, 20);
            this.tbDNS1.TabIndex = 15;
            this.tbDNS1.Text = "0.0.0.0";
            this.tbDNS1.EnabledChanged += new System.EventHandler(this.tbDNS1_EnabledChanged);
            // 
            // cbAutoDNS1
            // 
            this.cbAutoDNS1.AutoSize = true;
            this.cbAutoDNS1.Enabled = false;
            this.cbAutoDNS1.Location = new System.Drawing.Point(227, 195);
            this.cbAutoDNS1.Name = "cbAutoDNS1";
            this.cbAutoDNS1.Size = new System.Drawing.Size(48, 17);
            this.cbAutoDNS1.TabIndex = 16;
            this.cbAutoDNS1.Text = "Auto";
            this.cbAutoDNS1.UseVisualStyleBackColor = true;
            this.cbAutoDNS1.CheckedChanged += new System.EventHandler(this.cbAutoDNS1_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 196);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(49, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "DNS1 IP";
            // 
            // tbDNS2
            // 
            this.tbDNS2.Enabled = false;
            this.tbDNS2.Location = new System.Drawing.Point(99, 219);
            this.tbDNS2.Name = "tbDNS2";
            this.tbDNS2.Size = new System.Drawing.Size(122, 20);
            this.tbDNS2.TabIndex = 18;
            this.tbDNS2.Text = "0.0.0.0";
            this.tbDNS2.EnabledChanged += new System.EventHandler(this.tbDNS2_EnabledChanged);
            // 
            // cbAutoDNS2
            // 
            this.cbAutoDNS2.AutoSize = true;
            this.cbAutoDNS2.Enabled = false;
            this.cbAutoDNS2.Location = new System.Drawing.Point(227, 221);
            this.cbAutoDNS2.Name = "cbAutoDNS2";
            this.cbAutoDNS2.Size = new System.Drawing.Size(48, 17);
            this.cbAutoDNS2.TabIndex = 19;
            this.cbAutoDNS2.Text = "Auto";
            this.cbAutoDNS2.UseVisualStyleBackColor = true;
            this.cbAutoDNS2.CheckedChanged += new System.EventHandler(this.cbAutoDNS2_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 222);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(49, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "DNS2 IP";
            // 
            // btnAdvanced
            // 
            this.btnAdvanced.Location = new System.Drawing.Point(200, 247);
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.Size = new System.Drawing.Size(75, 23);
            this.btnAdvanced.TabIndex = 21;
            this.btnAdvanced.Text = "Options";
            this.btnAdvanced.UseVisualStyleBackColor = true;
            this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
            // 
            // cbLANMode
            // 
            this.cbLANMode.AutoSize = true;
            this.cbLANMode.Enabled = false;
            this.cbLANMode.Location = new System.Drawing.Point(227, 118);
            this.cbLANMode.Name = "cbLANMode";
            this.cbLANMode.Size = new System.Drawing.Size(53, 17);
            this.cbLANMode.TabIndex = 22;
            this.cbLANMode.Text = "PC IP";
            this.cbLANMode.UseVisualStyleBackColor = true;
            // 
            // ConfigFormEth
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(287, 282);
            this.Controls.Add(this.cbLANMode);
            this.Controls.Add(this.btnAdvanced);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cbAutoDNS2);
            this.Controls.Add(this.tbDNS2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cbAutoDNS1);
            this.Controls.Add(this.tbDNS1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbAutoGate);
            this.Controls.Add(this.tbGate);
            this.Controls.Add(this.cbAutoMask);
            this.Controls.Add(this.cbIntercept);
            this.Controls.Add(this.tbMask);
            this.Controls.Add(this.tbIP);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.cbAdapter);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbAPI);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigFormEth";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "ConfigEthForm";
            this.Load += new System.EventHandler(this.ConfigFormEth_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbAPI;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbAdapter;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbIP;
        private System.Windows.Forms.TextBox tbMask;
        private System.Windows.Forms.CheckBox cbIntercept;
        private System.Windows.Forms.CheckBox cbAutoMask;
        private System.Windows.Forms.TextBox tbGate;
        private System.Windows.Forms.CheckBox cbAutoGate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbDNS1;
        private System.Windows.Forms.CheckBox cbAutoDNS1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbDNS2;
        private System.Windows.Forms.CheckBox cbAutoDNS2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.CheckBox cbLANMode;
    }
}
