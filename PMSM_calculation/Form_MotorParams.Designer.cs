namespace calc_from_geometryOfMotor
{
    partial class Form_MotorParams
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
            this.dgv_motorParams = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.contextMenu_transient = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu_Motor = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.changeMotorTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_motorParams)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenu_transient.SuspendLayout();
            this.contextMenu_Motor.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv_motorParams
            // 
            this.dgv_motorParams.AllowUserToAddRows = false;
            this.dgv_motorParams.AllowUserToDeleteRows = false;
            this.dgv_motorParams.AllowUserToResizeRows = false;
            this.dgv_motorParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_motorParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_motorParams.Location = new System.Drawing.Point(0, 0);
            this.dgv_motorParams.Name = "dgv_motorParams";
            this.dgv_motorParams.RowHeadersVisible = false;
            this.dgv_motorParams.Size = new System.Drawing.Size(415, 339);
            this.dgv_motorParams.TabIndex = 10;
            this.dgv_motorParams.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_motorParams_CellContentClick);
            this.dgv_motorParams.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_motorParams_CellEndEdit);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv_motorParams);
            this.splitContainer1.Size = new System.Drawing.Size(628, 339);
            this.splitContainer1.SplitterDistance = 209;
            this.splitContainer1.TabIndex = 12;
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.LabelEdit = true;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(209, 339);
            this.treeView1.TabIndex = 12;
            this.treeView1.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView1_BeforeLabelEdit);
            this.treeView1.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.treeView1_AfterLabelEdit);
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            this.treeView1.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // contextMenu_transient
            // 
            this.contextMenu_transient.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.contextMenu_transient.Name = "contextMenu_transient";
            this.contextMenu_transient.Size = new System.Drawing.Size(118, 70);
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.runToolStripMenuItem.Text = "Run";
            this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // contextMenu_Motor
            // 
            this.contextMenu_Motor.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeMotorTypeToolStripMenuItem});
            this.contextMenu_Motor.Name = "contextMenu_Motor";
            this.contextMenu_Motor.Size = new System.Drawing.Size(178, 48);
            // 
            // changeMotorTypeToolStripMenuItem
            // 
            this.changeMotorTypeToolStripMenuItem.Name = "changeMotorTypeToolStripMenuItem";
            this.changeMotorTypeToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.changeMotorTypeToolStripMenuItem.Text = "Change motor type";
            // 
            // Form_MotorParams
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(628, 339);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form_MotorParams";
            this.Text = "Form_MotorParams";
            this.Load += new System.EventHandler(this.Form_MotorParams_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_motorParams)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.contextMenu_transient.ResumeLayout(false);
            this.contextMenu_Motor.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_motorParams;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ContextMenuStrip contextMenu_transient;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenu_Motor;
        private System.Windows.Forms.ToolStripMenuItem changeMotorTypeToolStripMenuItem;
    }
}