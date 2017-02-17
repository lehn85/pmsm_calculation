namespace calc_from_geometryOfMotor
{
    partial class EfficiencyMapViewer
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
            this.tb_id = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tb_iq = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tb_speed = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tb_torque = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.bt_show_torque_speed_curve = new System.Windows.Forms.Button();
            this.tb_Imax = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tb_maxSpeed = new System.Windows.Forms.TextBox();
            this.tb_Umax = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.plot = new OxyPlot.WindowsForms.PlotView();
            this.bt_showmap = new System.Windows.Forms.Button();
            this.comboBox_maptype = new System.Windows.Forms.ComboBox();
            this.bt_show_curves = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.tb_skew = new System.Windows.Forms.TextBox();
            this.bt_export_advisor_Mfile = new System.Windows.Forms.Button();
            this.bt_export_effviewer_ansys = new System.Windows.Forms.Button();
            this.bt_open_effviewer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tb_id
            // 
            this.tb_id.Location = new System.Drawing.Point(56, 100);
            this.tb_id.Name = "tb_id";
            this.tb_id.Size = new System.Drawing.Size(100, 20);
            this.tb_id.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 103);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(16, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Id";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 129);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(16, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Iq";
            // 
            // tb_iq
            // 
            this.tb_iq.Location = new System.Drawing.Point(56, 126);
            this.tb_iq.Name = "tb_iq";
            this.tb_iq.Size = new System.Drawing.Size(100, 20);
            this.tb_iq.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Speed";
            // 
            // tb_speed
            // 
            this.tb_speed.Location = new System.Drawing.Point(55, 42);
            this.tb_speed.Name = "tb_speed";
            this.tb_speed.Size = new System.Drawing.Size(100, 20);
            this.tb_speed.TabIndex = 4;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 200);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(177, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Show transient analysis";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.bt_show_transient_analysis);
            // 
            // tb_torque
            // 
            this.tb_torque.Location = new System.Drawing.Point(55, 12);
            this.tb_torque.Name = "tb_torque";
            this.tb_torque.Size = new System.Drawing.Size(100, 20);
            this.tb_torque.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Torque";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(14, 71);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(103, 23);
            this.button2.TabIndex = 8;
            this.button2.Text = "Get best id,iq";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 156);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "label5";
            // 
            // bt_show_torque_speed_curve
            // 
            this.bt_show_torque_speed_curve.Location = new System.Drawing.Point(12, 319);
            this.bt_show_torque_speed_curve.Name = "bt_show_torque_speed_curve";
            this.bt_show_torque_speed_curve.Size = new System.Drawing.Size(132, 26);
            this.bt_show_torque_speed_curve.TabIndex = 10;
            this.bt_show_torque_speed_curve.Text = "Show max torque curve";
            this.bt_show_torque_speed_curve.UseVisualStyleBackColor = true;
            this.bt_show_torque_speed_curve.Click += new System.EventHandler(this.bt_show_max_torque_curve);
            // 
            // tb_Imax
            // 
            this.tb_Imax.Location = new System.Drawing.Point(127, 243);
            this.tb_Imax.Name = "tb_Imax";
            this.tb_Imax.Size = new System.Drawing.Size(84, 20);
            this.tb_Imax.TabIndex = 14;
            this.tb_Imax.Text = "55";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 246);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(110, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Imax (A) (phase peak)";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 296);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(85, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Max speed (rpm)";
            // 
            // tb_maxSpeed
            // 
            this.tb_maxSpeed.Location = new System.Drawing.Point(127, 293);
            this.tb_maxSpeed.Name = "tb_maxSpeed";
            this.tb_maxSpeed.Size = new System.Drawing.Size(82, 20);
            this.tb_maxSpeed.TabIndex = 11;
            this.tb_maxSpeed.Text = "6000";
            // 
            // tb_Umax
            // 
            this.tb_Umax.Location = new System.Drawing.Point(127, 269);
            this.tb_Umax.Name = "tb_Umax";
            this.tb_Umax.Size = new System.Drawing.Size(84, 20);
            this.tb_Umax.TabIndex = 16;
            this.tb_Umax.Text = "140";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 272);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(115, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Umax (V) (phase peak)";
            // 
            // plot
            // 
            this.plot.Location = new System.Drawing.Point(248, 2);
            this.plot.Name = "plot";
            this.plot.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plot.Size = new System.Drawing.Size(612, 493);
            this.plot.TabIndex = 17;
            this.plot.Text = "plotView1";
            this.plot.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plot.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plot.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // bt_showmap
            // 
            this.bt_showmap.Location = new System.Drawing.Point(12, 383);
            this.bt_showmap.Name = "bt_showmap";
            this.bt_showmap.Size = new System.Drawing.Size(168, 23);
            this.bt_showmap.TabIndex = 18;
            this.bt_showmap.Text = "Show map";
            this.bt_showmap.UseVisualStyleBackColor = true;
            this.bt_showmap.Click += new System.EventHandler(this.bt_show_eff_map);
            // 
            // comboBox_maptype
            // 
            this.comboBox_maptype.FormattingEnabled = true;
            this.comboBox_maptype.Location = new System.Drawing.Point(14, 356);
            this.comboBox_maptype.Name = "comboBox_maptype";
            this.comboBox_maptype.Size = new System.Drawing.Size(121, 21);
            this.comboBox_maptype.TabIndex = 20;
            // 
            // bt_show_curves
            // 
            this.bt_show_curves.Location = new System.Drawing.Point(150, 322);
            this.bt_show_curves.Name = "bt_show_curves";
            this.bt_show_curves.Size = new System.Drawing.Size(79, 23);
            this.bt_show_curves.TabIndex = 21;
            this.bt_show_curves.Text = "Test curves";
            this.bt_show_curves.UseVisualStyleBackColor = true;
            this.bt_show_curves.Click += new System.EventHandler(this.bt_show_curves_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 177);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(34, 13);
            this.label9.TabIndex = 23;
            this.label9.Text = "Skew";
            // 
            // tb_skew
            // 
            this.tb_skew.Location = new System.Drawing.Point(56, 174);
            this.tb_skew.Name = "tb_skew";
            this.tb_skew.Size = new System.Drawing.Size(100, 20);
            this.tb_skew.TabIndex = 22;
            // 
            // bt_export_advisor_Mfile
            // 
            this.bt_export_advisor_Mfile.Location = new System.Drawing.Point(15, 464);
            this.bt_export_advisor_Mfile.Name = "bt_export_advisor_Mfile";
            this.bt_export_advisor_Mfile.Size = new System.Drawing.Size(196, 23);
            this.bt_export_advisor_Mfile.TabIndex = 24;
            this.bt_export_advisor_Mfile.Text = "Export as Advisor M file";
            this.bt_export_advisor_Mfile.UseVisualStyleBackColor = true;
            this.bt_export_advisor_Mfile.Click += new System.EventHandler(this.bt_export_advisor_Mfile_Click);
            // 
            // bt_export_effviewer_ansys
            // 
            this.bt_export_effviewer_ansys.Location = new System.Drawing.Point(14, 412);
            this.bt_export_effviewer_ansys.Name = "bt_export_effviewer_ansys";
            this.bt_export_effviewer_ansys.Size = new System.Drawing.Size(195, 23);
            this.bt_export_effviewer_ansys.TabIndex = 25;
            this.bt_export_effviewer_ansys.Text = "Export for efficiency viewer (ANSYS)";
            this.bt_export_effviewer_ansys.UseVisualStyleBackColor = true;
            this.bt_export_effviewer_ansys.Click += new System.EventHandler(this.bt_export_effviewer_ansys_Click);
            // 
            // bt_open_effviewer
            // 
            this.bt_open_effviewer.Location = new System.Drawing.Point(82, 435);
            this.bt_open_effviewer.Name = "bt_open_effviewer";
            this.bt_open_effviewer.Size = new System.Drawing.Size(129, 23);
            this.bt_open_effviewer.TabIndex = 26;
            this.bt_open_effviewer.Text = "Open efficiency viewer";
            this.bt_open_effviewer.UseVisualStyleBackColor = true;
            this.bt_open_effviewer.Click += new System.EventHandler(this.bt_open_effviewer_Click);
            // 
            // EfficiencyMapViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(866, 499);
            this.Controls.Add(this.bt_open_effviewer);
            this.Controls.Add(this.bt_export_effviewer_ansys);
            this.Controls.Add(this.bt_export_advisor_Mfile);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.tb_skew);
            this.Controls.Add(this.bt_show_curves);
            this.Controls.Add(this.comboBox_maptype);
            this.Controls.Add(this.bt_showmap);
            this.Controls.Add(this.plot);
            this.Controls.Add(this.tb_Umax);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tb_Imax);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tb_maxSpeed);
            this.Controls.Add(this.bt_show_torque_speed_curve);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.tb_torque);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_speed);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tb_iq);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tb_id);
            this.Name = "EfficiencyMapViewer";
            this.Text = "EfficiencyMapViewer";
            this.Load += new System.EventHandler(this.EfficiencyMapViewer_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tb_id;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tb_iq;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tb_speed;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tb_torque;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button bt_show_torque_speed_curve;
        private System.Windows.Forms.TextBox tb_Imax;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tb_maxSpeed;
        private System.Windows.Forms.TextBox tb_Umax;
        private System.Windows.Forms.Label label8;
        private OxyPlot.WindowsForms.PlotView plot;
        private System.Windows.Forms.Button bt_showmap;
        private System.Windows.Forms.ComboBox comboBox_maptype;
        private System.Windows.Forms.Button bt_show_curves;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tb_skew;
        private System.Windows.Forms.Button bt_export_advisor_Mfile;
        private System.Windows.Forms.Button bt_export_effviewer_ansys;
        private System.Windows.Forms.Button bt_open_effviewer;
    }
}