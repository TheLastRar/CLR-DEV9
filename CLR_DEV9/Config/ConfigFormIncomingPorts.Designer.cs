namespace CLRDEV9.Config
{
    partial class ConfigFormIncomingPorts
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
            this.dgPorts = new System.Windows.Forms.DataGridView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnDel = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cProtocol = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.cPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cEnable = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgPorts)).BeginInit();
            this.SuspendLayout();
            // 
            // dgPorts
            // 
            this.dgPorts.AllowUserToAddRows = false;
            this.dgPorts.AllowUserToDeleteRows = false;
            this.dgPorts.AllowUserToResizeColumns = false;
            this.dgPorts.AllowUserToResizeRows = false;
            this.dgPorts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgPorts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.cDesc,
            this.cProtocol,
            this.cPort,
            this.cEnable});
            this.dgPorts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgPorts.Location = new System.Drawing.Point(12, 12);
            this.dgPorts.MultiSelect = false;
            this.dgPorts.Name = "dgPorts";
            this.dgPorts.RowHeadersVisible = false;
            this.dgPorts.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgPorts.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgPorts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgPorts.Size = new System.Drawing.Size(282, 177);
            this.dgPorts.TabIndex = 0;
            this.dgPorts.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgPorts_CellClick);
            this.dgPorts.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.dgPorts_SortCompare);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(300, 12);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnDel
            // 
            this.btnDel.Location = new System.Drawing.Point(300, 41);
            this.btnDel.Name = "btnDel";
            this.btnDel.Size = new System.Drawing.Size(75, 23);
            this.btnDel.TabIndex = 2;
            this.btnDel.Text = "Delete";
            this.btnDel.UseVisualStyleBackColor = true;
            this.btnDel.Click += new System.EventHandler(this.btnDel_Click);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(300, 137);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(300, 166);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cDesc
            // 
            this.cDesc.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.cDesc.HeaderText = "Name";
            this.cDesc.Name = "cDesc";
            // 
            // cProtocol
            // 
            this.cProtocol.FillWeight = 60F;
            this.cProtocol.HeaderText = "Protocol";
            this.cProtocol.Items.AddRange(new object[] {
            "UDP"});
            this.cProtocol.Name = "cProtocol";
            this.cProtocol.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.cProtocol.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.cProtocol.Width = 60;
            // 
            // cPort
            // 
            this.cPort.FillWeight = 40F;
            this.cPort.HeaderText = "Port";
            this.cPort.Name = "cPort";
            this.cPort.Width = 40;
            // 
            // cEnable
            // 
            this.cEnable.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.cEnable.FalseValue = "";
            this.cEnable.FillWeight = 50F;
            this.cEnable.HeaderText = "Enabled";
            this.cEnable.Name = "cEnable";
            this.cEnable.TrueValue = "";
            this.cEnable.Width = 50;
            // 
            // ConfigFormIncomingPorts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(387, 201);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnDel);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.dgPorts);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ConfigFormIncomingPorts";
            this.Text = "ConfigFormIncomingPorts";
            ((System.ComponentModel.ISupportInitialize)(this.dgPorts)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgPorts;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDel;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.DataGridViewTextBoxColumn cDesc;
        private System.Windows.Forms.DataGridViewComboBoxColumn cProtocol;
        private System.Windows.Forms.DataGridViewTextBoxColumn cPort;
        private System.Windows.Forms.DataGridViewCheckBoxColumn cEnable;
    }
}