namespace calc_from_geometryOfMotor
{
    partial class OptimizationResultsWindow
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
            this.zc1 = new ZedGraph.ZedGraphControl();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.f1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.f2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bt_apply_selected = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // zc1
            // 
            this.zc1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zc1.Location = new System.Drawing.Point(0, 0);
            this.zc1.Name = "zc1";
            this.zc1.ScrollGrace = 0D;
            this.zc1.ScrollMaxX = 0D;
            this.zc1.ScrollMaxY = 0D;
            this.zc1.ScrollMaxY2 = 0D;
            this.zc1.ScrollMinX = 0D;
            this.zc1.ScrollMinY = 0D;
            this.zc1.ScrollMinY2 = 0D;
            this.zc1.Size = new System.Drawing.Size(544, 441);
            this.zc1.TabIndex = 1;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.bt_apply_selected);
            this.splitContainer1.Panel1.Controls.Add(this.dgv);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.zc1);
            this.splitContainer1.Size = new System.Drawing.Size(822, 441);
            this.splitContainer1.SplitterDistance = 274;
            this.splitContainer1.TabIndex = 2;
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.f1,
            this.f2});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.MultiSelect = false;
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.ShowEditingIcon = false;
            this.dgv.Size = new System.Drawing.Size(274, 441);
            this.dgv.TabIndex = 0;
            // 
            // f1
            // 
            this.f1.DataPropertyName = "f1";
            this.f1.HeaderText = "f1";
            this.f1.Name = "f1";
            this.f1.ReadOnly = true;
            // 
            // f2
            // 
            this.f2.DataPropertyName = "f2";
            this.f2.HeaderText = "f2";
            this.f2.Name = "f2";
            this.f2.ReadOnly = true;
            // 
            // bt_apply_selected
            // 
            this.bt_apply_selected.Location = new System.Drawing.Point(196, 400);
            this.bt_apply_selected.Name = "bt_apply_selected";
            this.bt_apply_selected.Size = new System.Drawing.Size(75, 38);
            this.bt_apply_selected.TabIndex = 2;
            this.bt_apply_selected.Text = "Apply selected";
            this.bt_apply_selected.UseVisualStyleBackColor = true;
            this.bt_apply_selected.Click += new System.EventHandler(this.bt_apply_selected_Click);
            // 
            // OptimizationResultsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(822, 441);
            this.Controls.Add(this.splitContainer1);
            this.Name = "OptimizationResultsWindow";
            this.Text = "OptimizationResultsWindow";
            this.Load += new System.EventHandler(this.OptimizationResultsWindow_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private ZedGraph.ZedGraphControl zc1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn f1;
        private System.Windows.Forms.DataGridViewTextBoxColumn f2;
        private System.Windows.Forms.Button bt_apply_selected;
    }
}