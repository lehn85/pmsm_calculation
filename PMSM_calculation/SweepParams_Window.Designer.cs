namespace calc_from_geometryOfMotor
{
    partial class SweepParams_Window
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
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgv_params = new System.Windows.Forms.DataGridView();
            this.dgv_results = new System.Windows.Forms.DataGridView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.checkBox_enableFem = new System.Windows.Forms.CheckBox();
            this.bt_sweep = new System.Windows.Forms.Button();
            this.bt_addParam = new System.Windows.Forms.Button();
            this.tb_value = new System.Windows.Forms.TextBox();
            this.comboBox_paramsName = new System.Windows.Forms.ComboBox();
            this.contextMenu_params = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setThisAsCurrentSetOfParamsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_params)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_results)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.contextMenu_params.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgv_params);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv_results);
            this.splitContainer1.Size = new System.Drawing.Size(904, 380);
            this.splitContainer1.SplitterDistance = 438;
            this.splitContainer1.TabIndex = 0;
            // 
            // dgv_params
            // 
            this.dgv_params.AllowUserToAddRows = false;
            this.dgv_params.AllowUserToOrderColumns = true;
            this.dgv_params.AllowUserToResizeRows = false;
            this.dgv_params.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_params.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_params.Location = new System.Drawing.Point(0, 0);
            this.dgv_params.Name = "dgv_params";
            this.dgv_params.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_params.Size = new System.Drawing.Size(438, 380);
            this.dgv_params.TabIndex = 0;
            this.dgv_params.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_params_CellMouseClick);
            // 
            // dgv_results
            // 
            this.dgv_results.AllowUserToAddRows = false;
            this.dgv_results.AllowUserToDeleteRows = false;
            this.dgv_results.AllowUserToOrderColumns = true;
            this.dgv_results.AllowUserToResizeRows = false;
            this.dgv_results.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_results.Location = new System.Drawing.Point(0, 0);
            this.dgv_results.Name = "dgv_results";
            this.dgv_results.ReadOnly = true;
            this.dgv_results.RowHeadersVisible = false;
            this.dgv_results.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_results.Size = new System.Drawing.Size(462, 380);
            this.dgv_results.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.checkBox_enableFem);
            this.splitContainer2.Panel1.Controls.Add(this.bt_sweep);
            this.splitContainer2.Panel1.Controls.Add(this.bt_addParam);
            this.splitContainer2.Panel1.Controls.Add(this.tb_value);
            this.splitContainer2.Panel1.Controls.Add(this.comboBox_paramsName);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Size = new System.Drawing.Size(904, 466);
            this.splitContainer2.SplitterDistance = 82;
            this.splitContainer2.TabIndex = 1;
            // 
            // checkBox_enableFem
            // 
            this.checkBox_enableFem.AutoSize = true;
            this.checkBox_enableFem.Location = new System.Drawing.Point(657, 10);
            this.checkBox_enableFem.Name = "checkBox_enableFem";
            this.checkBox_enableFem.Size = new System.Drawing.Size(119, 17);
            this.checkBox_enableFem.TabIndex = 4;
            this.checkBox_enableFem.Text = "Enable fem analysis";
            this.checkBox_enableFem.UseVisualStyleBackColor = true;
            this.checkBox_enableFem.CheckedChanged += new System.EventHandler(this.checkBox_enableFem_CheckedChanged);
            // 
            // bt_sweep
            // 
            this.bt_sweep.Location = new System.Drawing.Point(540, 4);
            this.bt_sweep.Name = "bt_sweep";
            this.bt_sweep.Size = new System.Drawing.Size(75, 34);
            this.bt_sweep.TabIndex = 3;
            this.bt_sweep.Text = "Sweep";
            this.bt_sweep.UseVisualStyleBackColor = true;
            this.bt_sweep.Click += new System.EventHandler(this.bt_sweep_Click);
            // 
            // bt_addParam
            // 
            this.bt_addParam.Location = new System.Drawing.Point(443, 10);
            this.bt_addParam.Name = "bt_addParam";
            this.bt_addParam.Size = new System.Drawing.Size(75, 23);
            this.bt_addParam.TabIndex = 2;
            this.bt_addParam.Text = "Add";
            this.bt_addParam.UseVisualStyleBackColor = true;
            this.bt_addParam.Click += new System.EventHandler(this.bt_addParam_Click);
            // 
            // tb_value
            // 
            this.tb_value.Location = new System.Drawing.Point(191, 12);
            this.tb_value.Name = "tb_value";
            this.tb_value.Size = new System.Drawing.Size(246, 20);
            this.tb_value.TabIndex = 1;
            // 
            // comboBox_paramsName
            // 
            this.comboBox_paramsName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_paramsName.FormattingEnabled = true;
            this.comboBox_paramsName.Location = new System.Drawing.Point(12, 12);
            this.comboBox_paramsName.Name = "comboBox_paramsName";
            this.comboBox_paramsName.Size = new System.Drawing.Size(160, 21);
            this.comboBox_paramsName.TabIndex = 0;
            this.comboBox_paramsName.SelectedIndexChanged += new System.EventHandler(this.comboBox_paramsName_SelectedIndexChanged);
            // 
            // contextMenu_params
            // 
            this.contextMenu_params.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setThisAsCurrentSetOfParamsToolStripMenuItem});
            this.contextMenu_params.Name = "contextMenu_params";
            this.contextMenu_params.Size = new System.Drawing.Size(242, 48);
            // 
            // setThisAsCurrentSetOfParamsToolStripMenuItem
            // 
            this.setThisAsCurrentSetOfParamsToolStripMenuItem.Name = "setThisAsCurrentSetOfParamsToolStripMenuItem";
            this.setThisAsCurrentSetOfParamsToolStripMenuItem.Size = new System.Drawing.Size(241, 22);
            this.setThisAsCurrentSetOfParamsToolStripMenuItem.Text = "Set this as current set of params";
            this.setThisAsCurrentSetOfParamsToolStripMenuItem.Click += new System.EventHandler(this.setThisAsCurrentSetOfParamsToolStripMenuItem_Click);
            // 
            // SweepParams_Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 466);
            this.Controls.Add(this.splitContainer2);
            this.Name = "SweepParams_Window";
            this.Text = "SweepParams_Window";
            this.Load += new System.EventHandler(this.SweepParams_Window_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_params)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_results)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.contextMenu_params.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgv_params;
        private System.Windows.Forms.DataGridView dgv_results;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Button bt_addParam;
        private System.Windows.Forms.TextBox tb_value;
        private System.Windows.Forms.ComboBox comboBox_paramsName;
        private System.Windows.Forms.Button bt_sweep;
        private System.Windows.Forms.CheckBox checkBox_enableFem;
        private System.Windows.Forms.ContextMenuStrip contextMenu_params;
        private System.Windows.Forms.ToolStripMenuItem setThisAsCurrentSetOfParamsToolStripMenuItem;
    }
}