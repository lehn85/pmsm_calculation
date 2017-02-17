namespace calc_from_geometryOfMotor
{
    partial class GraphWindow
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
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.bt_apply = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tb_maxY = new System.Windows.Forms.TextBox();
            this.tb_minY = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tb_maxX = new System.Windows.Forms.TextBox();
            this.tb_minX = new System.Windows.Forms.TextBox();
            this.checkBox_showFourierAnalysis = new System.Windows.Forms.CheckBox();
            this.checkBox_showdata = new System.Windows.Forms.CheckBox();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
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
            this.zc1.Size = new System.Drawing.Size(727, 492);
            this.zc1.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(930, 492);
            this.splitContainer1.SplitterDistance = 199;
            this.splitContainer1.TabIndex = 1;
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
            this.splitContainer2.Panel1.Controls.Add(this.listBox1);
            this.splitContainer2.Panel1MinSize = 300;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.checkBox_showdata);
            this.splitContainer2.Panel2.Controls.Add(this.bt_apply);
            this.splitContainer2.Panel2.Controls.Add(this.label2);
            this.splitContainer2.Panel2.Controls.Add(this.tb_maxY);
            this.splitContainer2.Panel2.Controls.Add(this.tb_minY);
            this.splitContainer2.Panel2.Controls.Add(this.label1);
            this.splitContainer2.Panel2.Controls.Add(this.tb_maxX);
            this.splitContainer2.Panel2.Controls.Add(this.tb_minX);
            this.splitContainer2.Panel2.Controls.Add(this.checkBox_showFourierAnalysis);
            this.splitContainer2.Size = new System.Drawing.Size(199, 492);
            this.splitContainer2.SplitterDistance = 336;
            this.splitContainer2.TabIndex = 1;
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox1.Size = new System.Drawing.Size(199, 336);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // bt_apply
            // 
            this.bt_apply.Location = new System.Drawing.Point(71, 67);
            this.bt_apply.Name = "bt_apply";
            this.bt_apply.Size = new System.Drawing.Size(75, 23);
            this.bt_apply.TabIndex = 7;
            this.bt_apply.Text = "Apply";
            this.bt_apply.UseVisualStyleBackColor = true;
            this.bt_apply.Click += new System.EventHandler(this.bt_apply_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Yaxis";
            // 
            // tb_maxY
            // 
            this.tb_maxY.Location = new System.Drawing.Point(125, 41);
            this.tb_maxY.Name = "tb_maxY";
            this.tb_maxY.Size = new System.Drawing.Size(48, 20);
            this.tb_maxY.TabIndex = 5;
            // 
            // tb_minY
            // 
            this.tb_minY.Location = new System.Drawing.Point(50, 41);
            this.tb_minY.Name = "tb_minY";
            this.tb_minY.Size = new System.Drawing.Size(48, 20);
            this.tb_minY.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Xaxis";
            // 
            // tb_maxX
            // 
            this.tb_maxX.Location = new System.Drawing.Point(125, 10);
            this.tb_maxX.Name = "tb_maxX";
            this.tb_maxX.Size = new System.Drawing.Size(48, 20);
            this.tb_maxX.TabIndex = 2;
            // 
            // tb_minX
            // 
            this.tb_minX.Location = new System.Drawing.Point(50, 10);
            this.tb_minX.Name = "tb_minX";
            this.tb_minX.Size = new System.Drawing.Size(48, 20);
            this.tb_minX.TabIndex = 1;
            // 
            // checkBox_showFourierAnalysis
            // 
            this.checkBox_showFourierAnalysis.AutoSize = true;
            this.checkBox_showFourierAnalysis.Location = new System.Drawing.Point(12, 96);
            this.checkBox_showFourierAnalysis.Name = "checkBox_showFourierAnalysis";
            this.checkBox_showFourierAnalysis.Size = new System.Drawing.Size(125, 17);
            this.checkBox_showFourierAnalysis.TabIndex = 0;
            this.checkBox_showFourierAnalysis.Text = "Show fourier analysis";
            this.checkBox_showFourierAnalysis.UseVisualStyleBackColor = true;
            this.checkBox_showFourierAnalysis.CheckedChanged += new System.EventHandler(this.checkBox_showFourierAnalysis_CheckedChanged);
            // 
            // checkBox_showdata
            // 
            this.checkBox_showdata.AutoSize = true;
            this.checkBox_showdata.Location = new System.Drawing.Point(12, 120);
            this.checkBox_showdata.Name = "checkBox_showdata";
            this.checkBox_showdata.Size = new System.Drawing.Size(77, 17);
            this.checkBox_showdata.TabIndex = 8;
            this.checkBox_showdata.Text = "Show data";
            this.checkBox_showdata.UseVisualStyleBackColor = true;
            this.checkBox_showdata.CheckedChanged += new System.EventHandler(this.checkBox_showdata_CheckedChanged);
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.zc1);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.dataGridView1);
            this.splitContainer3.Panel2Collapsed = true;
            this.splitContainer3.Size = new System.Drawing.Size(727, 492);
            this.splitContainer3.SplitterDistance = 600;
            this.splitContainer3.TabIndex = 1;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(123, 492);
            this.dataGridView1.TabIndex = 0;
            // 
            // GraphWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 492);
            this.Controls.Add(this.splitContainer1);
            this.Name = "GraphWindow";
            this.Text = "testChart";
            this.Load += new System.EventHandler(this.GraphWindow_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private ZedGraph.ZedGraphControl zc1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.CheckBox checkBox_showFourierAnalysis;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tb_maxY;
        private System.Windows.Forms.TextBox tb_minY;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tb_maxX;
        private System.Windows.Forms.TextBox tb_minX;
        private System.Windows.Forms.Button bt_apply;
        private System.Windows.Forms.CheckBox checkBox_showdata;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.DataGridView dataGridView1;



    }
}