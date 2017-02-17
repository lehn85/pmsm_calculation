namespace calc_from_geometryOfMotor
{
    partial class CoreLossVisualizer
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
            this.canvasTarget = new System.Windows.Forms.Panel();
            this.bt_copypicture = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.bt_apply_max_min = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tb_min = new System.Windows.Forms.TextBox();
            this.tb_max = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label_coordinate = new System.Windows.Forms.Label();
            this.nud_zoom = new System.Windows.Forms.NumericUpDown();
            this.label_origin_scale = new System.Windows.Forms.Label();
            this.zc = new ZedGraph.ZedGraphControl();
            this.comboBox_palette = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.canvasTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_zoom)).BeginInit();
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
            this.splitContainer1.Panel1.Controls.Add(this.canvasTarget);
            this.splitContainer1.Panel1MinSize = 500;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.zc);
            this.splitContainer1.Size = new System.Drawing.Size(811, 455);
            this.splitContainer1.SplitterDistance = 500;
            this.splitContainer1.TabIndex = 0;
            // 
            // canvasTarget
            // 
            this.canvasTarget.Controls.Add(this.comboBox_palette);
            this.canvasTarget.Controls.Add(this.bt_copypicture);
            this.canvasTarget.Controls.Add(this.label4);
            this.canvasTarget.Controls.Add(this.label3);
            this.canvasTarget.Controls.Add(this.bt_apply_max_min);
            this.canvasTarget.Controls.Add(this.label2);
            this.canvasTarget.Controls.Add(this.tb_min);
            this.canvasTarget.Controls.Add(this.tb_max);
            this.canvasTarget.Controls.Add(this.label1);
            this.canvasTarget.Controls.Add(this.label_coordinate);
            this.canvasTarget.Controls.Add(this.nud_zoom);
            this.canvasTarget.Controls.Add(this.label_origin_scale);
            this.canvasTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvasTarget.Location = new System.Drawing.Point(0, 0);
            this.canvasTarget.Name = "canvasTarget";
            this.canvasTarget.Size = new System.Drawing.Size(500, 455);
            this.canvasTarget.TabIndex = 0;
            this.canvasTarget.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel2_Paint);
            this.canvasTarget.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvasTarget_MouseDown);
            this.canvasTarget.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvasTarget_MouseMove);
            this.canvasTarget.MouseUp += new System.Windows.Forms.MouseEventHandler(this.canvasTarget_MouseUp);
            // 
            // bt_copypicture
            // 
            this.bt_copypicture.Location = new System.Drawing.Point(132, 140);
            this.bt_copypicture.Name = "bt_copypicture";
            this.bt_copypicture.Size = new System.Drawing.Size(102, 23);
            this.bt_copypicture.TabIndex = 11;
            this.bt_copypicture.Text = "Copy to clipboard";
            this.bt_copypicture.UseVisualStyleBackColor = true;
            this.bt_copypicture.Click += new System.EventHandler(this.bt_copypicture_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "zoom";
            // 
            // bt_apply_max_min
            // 
            this.bt_apply_max_min.Location = new System.Drawing.Point(3, 140);
            this.bt_apply_max_min.Name = "bt_apply_max_min";
            this.bt_apply_max_min.Size = new System.Drawing.Size(75, 23);
            this.bt_apply_max_min.TabIndex = 9;
            this.bt_apply_max_min.Text = "Apply";
            this.bt_apply_max_min.UseVisualStyleBackColor = true;
            this.bt_apply_max_min.Click += new System.EventHandler(this.bt_apply_max_min_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 117);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "min (W/kg)";
            // 
            // tb_min
            // 
            this.tb_min.Location = new System.Drawing.Point(83, 114);
            this.tb_min.Name = "tb_min";
            this.tb_min.Size = new System.Drawing.Size(151, 20);
            this.tb_min.TabIndex = 7;
            // 
            // tb_max
            // 
            this.tb_max.Location = new System.Drawing.Point(83, 89);
            this.tb_max.Name = "tb_max";
            this.tb_max.Size = new System.Drawing.Size(151, 20);
            this.tb_max.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 92);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "max (W/kg)";
            // 
            // label_coordinate
            // 
            this.label_coordinate.AutoSize = true;
            this.label_coordinate.Location = new System.Drawing.Point(3, 34);
            this.label_coordinate.Name = "label_coordinate";
            this.label_coordinate.Size = new System.Drawing.Size(35, 13);
            this.label_coordinate.TabIndex = 3;
            this.label_coordinate.Text = "label1";
            // 
            // nud_zoom
            // 
            this.nud_zoom.DecimalPlaces = 1;
            this.nud_zoom.Location = new System.Drawing.Point(50, 61);
            this.nud_zoom.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_zoom.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_zoom.Name = "nud_zoom";
            this.nud_zoom.Size = new System.Drawing.Size(64, 20);
            this.nud_zoom.TabIndex = 2;
            this.nud_zoom.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_zoom.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_zoom.ValueChanged += new System.EventHandler(this.nud_zoom_ValueChanged);
            // 
            // label_origin_scale
            // 
            this.label_origin_scale.AutoSize = true;
            this.label_origin_scale.Location = new System.Drawing.Point(3, 9);
            this.label_origin_scale.Name = "label_origin_scale";
            this.label_origin_scale.Size = new System.Drawing.Size(35, 13);
            this.label_origin_scale.TabIndex = 1;
            this.label_origin_scale.Text = "label1";
            // 
            // zc
            // 
            this.zc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zc.Location = new System.Drawing.Point(0, 0);
            this.zc.Name = "zc";
            this.zc.ScrollGrace = 0D;
            this.zc.ScrollMaxX = 0D;
            this.zc.ScrollMaxY = 0D;
            this.zc.ScrollMaxY2 = 0D;
            this.zc.ScrollMinX = 0D;
            this.zc.ScrollMinY = 0D;
            this.zc.ScrollMinY2 = 0D;
            this.zc.Size = new System.Drawing.Size(307, 455);
            this.zc.TabIndex = 0;
            // 
            // comboBox_palette
            // 
            this.comboBox_palette.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_palette.FormattingEnabled = true;
            this.comboBox_palette.Location = new System.Drawing.Point(188, 60);
            this.comboBox_palette.Name = "comboBox_palette";
            this.comboBox_palette.Size = new System.Drawing.Size(92, 21);
            this.comboBox_palette.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(129, 63);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "color type";
            // 
            // CoreLossVisualizer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(811, 455);
            this.Controls.Add(this.splitContainer1);
            this.Name = "CoreLossVisualizer";
            this.Text = "CoreLossVisualizer";
            this.Load += new System.EventHandler(this.CoreLossVisualizer_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.canvasTarget.ResumeLayout(false);
            this.canvasTarget.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_zoom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel canvasTarget;
        private System.Windows.Forms.Label label_origin_scale;
        private System.Windows.Forms.NumericUpDown nud_zoom;
        private System.Windows.Forms.Label label_coordinate;
        private ZedGraph.ZedGraphControl zc;
        private System.Windows.Forms.Button bt_apply_max_min;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tb_min;
        private System.Windows.Forms.TextBox tb_max;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bt_copypicture;
        private System.Windows.Forms.ComboBox comboBox_palette;
        private System.Windows.Forms.Label label4;
    }
}