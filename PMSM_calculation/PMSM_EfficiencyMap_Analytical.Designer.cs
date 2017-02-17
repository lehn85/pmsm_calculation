namespace calc_from_geometryOfMotor
{
    partial class PMSM_EfficiencyMap_Analytical
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
            this.plot = new OxyPlot.WindowsForms.PlotView();
            this.bt_showmap = new System.Windows.Forms.Button();
            this.tb_Umax = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tb_Imax = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.tb_maxSpeed = new System.Windows.Forms.TextBox();
            this.comboBox_maptype = new System.Windows.Forms.ComboBox();
            this.bt_show_maxtorque_capa_curve = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tb_Lq = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tb_Ld = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tb_psiM = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tb_R = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tb_pp = new System.Windows.Forms.TextBox();
            this.tb_rotor_Hloss_coeff = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tb_rotor_Eloss_coeff = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tb_stator_Hloss_coeff = new System.Windows.Forms.TextBox();
            this.tb_stator_Eloss_coeff = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // plot
            // 
            this.plot.Location = new System.Drawing.Point(235, 12);
            this.plot.Name = "plot";
            this.plot.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.plot.Size = new System.Drawing.Size(547, 442);
            this.plot.TabIndex = 0;
            this.plot.Text = "plotView1";
            this.plot.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.plot.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.plot.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // bt_showmap
            // 
            this.bt_showmap.Location = new System.Drawing.Point(7, 387);
            this.bt_showmap.Name = "bt_showmap";
            this.bt_showmap.Size = new System.Drawing.Size(75, 23);
            this.bt_showmap.TabIndex = 1;
            this.bt_showmap.Text = "Showmap";
            this.bt_showmap.UseVisualStyleBackColor = true;
            this.bt_showmap.Click += new System.EventHandler(this.bt_showmap_Click);
            // 
            // tb_Umax
            // 
            this.tb_Umax.Location = new System.Drawing.Point(108, 285);
            this.tb_Umax.Name = "tb_Umax";
            this.tb_Umax.Size = new System.Drawing.Size(100, 20);
            this.tb_Umax.TabIndex = 22;
            this.tb_Umax.Text = "140";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 288);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(50, 13);
            this.label8.TabIndex = 21;
            this.label8.Text = "Umax (V)";
            // 
            // tb_Imax
            // 
            this.tb_Imax.Location = new System.Drawing.Point(108, 259);
            this.tb_Imax.Name = "tb_Imax";
            this.tb_Imax.Size = new System.Drawing.Size(100, 20);
            this.tb_Imax.TabIndex = 20;
            this.tb_Imax.Text = "55";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 262);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Imax (A)";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 312);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(85, 13);
            this.label7.TabIndex = 19;
            this.label7.Text = "Max speed (rpm)";
            // 
            // tb_maxSpeed
            // 
            this.tb_maxSpeed.Location = new System.Drawing.Point(106, 309);
            this.tb_maxSpeed.Name = "tb_maxSpeed";
            this.tb_maxSpeed.Size = new System.Drawing.Size(100, 20);
            this.tb_maxSpeed.TabIndex = 17;
            this.tb_maxSpeed.Text = "6000";
            // 
            // comboBox_maptype
            // 
            this.comboBox_maptype.FormattingEnabled = true;
            this.comboBox_maptype.Location = new System.Drawing.Point(7, 360);
            this.comboBox_maptype.Name = "comboBox_maptype";
            this.comboBox_maptype.Size = new System.Drawing.Size(121, 21);
            this.comboBox_maptype.TabIndex = 23;
            // 
            // bt_show_maxtorque_capa_curve
            // 
            this.bt_show_maxtorque_capa_curve.Location = new System.Drawing.Point(7, 333);
            this.bt_show_maxtorque_capa_curve.Name = "bt_show_maxtorque_capa_curve";
            this.bt_show_maxtorque_capa_curve.Size = new System.Drawing.Size(165, 21);
            this.bt_show_maxtorque_capa_curve.TabIndex = 24;
            this.bt_show_maxtorque_capa_curve.Text = "Show max torque capa curve";
            this.bt_show_maxtorque_capa_curve.UseVisualStyleBackColor = true;
            this.bt_show_maxtorque_capa_curve.Click += new System.EventHandler(this.bt_show_maxtorque_capa_curve_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 431);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(168, 23);
            this.button1.TabIndex = 25;
            this.button1.Text = "TTTT";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tb_Lq
            // 
            this.tb_Lq.Location = new System.Drawing.Point(110, 35);
            this.tb_Lq.Name = "tb_Lq";
            this.tb_Lq.Size = new System.Drawing.Size(100, 20);
            this.tb_Lq.TabIndex = 31;
            this.tb_Lq.Text = "2.57";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 30;
            this.label1.Text = "Lq (mH)";
            // 
            // tb_Ld
            // 
            this.tb_Ld.Location = new System.Drawing.Point(110, 9);
            this.tb_Ld.Name = "tb_Ld";
            this.tb_Ld.Size = new System.Drawing.Size(100, 20);
            this.tb_Ld.TabIndex = 29;
            this.tb_Ld.Text = "1.51";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 27;
            this.label2.Text = "Ld (mH)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 28;
            this.label3.Text = "psiM (Wb)";
            // 
            // tb_psiM
            // 
            this.tb_psiM.Location = new System.Drawing.Point(108, 59);
            this.tb_psiM.Name = "tb_psiM";
            this.tb_psiM.Size = new System.Drawing.Size(100, 20);
            this.tb_psiM.TabIndex = 26;
            this.tb_psiM.Text = "0.115";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(15, 13);
            this.label4.TabIndex = 33;
            this.label4.Text = "R";
            // 
            // tb_R
            // 
            this.tb_R.Location = new System.Drawing.Point(110, 85);
            this.tb_R.Name = "tb_R";
            this.tb_R.Size = new System.Drawing.Size(100, 20);
            this.tb_R.TabIndex = 32;
            this.tb_R.Text = "0.0313";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 35;
            this.label5.Text = "pairs of pole";
            // 
            // tb_pp
            // 
            this.tb_pp.Location = new System.Drawing.Point(110, 111);
            this.tb_pp.Name = "tb_pp";
            this.tb_pp.Size = new System.Drawing.Size(100, 20);
            this.tb_pp.TabIndex = 34;
            this.tb_pp.Text = "4";
            // 
            // tb_rotor_Hloss_coeff
            // 
            this.tb_rotor_Hloss_coeff.Location = new System.Drawing.Point(70, 167);
            this.tb_rotor_Hloss_coeff.Name = "tb_rotor_Hloss_coeff";
            this.tb_rotor_Hloss_coeff.Size = new System.Drawing.Size(44, 20);
            this.tb_rotor_Hloss_coeff.TabIndex = 36;
            this.tb_rotor_Hloss_coeff.Text = "0.203";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 170);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(33, 13);
            this.label9.TabIndex = 35;
            this.label9.Text = "Rotor";
            // 
            // tb_rotor_Eloss_coeff
            // 
            this.tb_rotor_Eloss_coeff.Location = new System.Drawing.Point(128, 167);
            this.tb_rotor_Eloss_coeff.Name = "tb_rotor_Eloss_coeff";
            this.tb_rotor_Eloss_coeff.Size = new System.Drawing.Size(44, 20);
            this.tb_rotor_Eloss_coeff.TabIndex = 36;
            this.tb_rotor_Eloss_coeff.Text = "1.9";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(4, 197);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(35, 13);
            this.label10.TabIndex = 35;
            this.label10.Text = "Stator";
            // 
            // tb_stator_Hloss_coeff
            // 
            this.tb_stator_Hloss_coeff.Location = new System.Drawing.Point(70, 194);
            this.tb_stator_Hloss_coeff.Name = "tb_stator_Hloss_coeff";
            this.tb_stator_Hloss_coeff.Size = new System.Drawing.Size(44, 20);
            this.tb_stator_Hloss_coeff.TabIndex = 36;
            this.tb_stator_Hloss_coeff.Text = "19.46";
            // 
            // tb_stator_Eloss_coeff
            // 
            this.tb_stator_Eloss_coeff.Location = new System.Drawing.Point(128, 194);
            this.tb_stator_Eloss_coeff.Name = "tb_stator_Eloss_coeff";
            this.tb_stator_Eloss_coeff.Size = new System.Drawing.Size(44, 20);
            this.tb_stator_Eloss_coeff.TabIndex = 36;
            this.tb_stator_Eloss_coeff.Text = "15.52";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(67, 151);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(55, 13);
            this.label11.TabIndex = 35;
            this.label11.Text = "Hysteresis";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(128, 151);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(31, 13);
            this.label12.TabIndex = 35;
            this.label12.Text = "Eddy";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 134);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(199, 13);
            this.label13.TabIndex = 35;
            this.label13.Text = "Core loss at 50Hz, only induction magnet";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(7, 220);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(107, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Appy params";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.bt_apply_params_Click);
            // 
            // PMSM_EfficiencyMap_Analytical
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(794, 466);
            this.Controls.Add(this.tb_stator_Eloss_coeff);
            this.Controls.Add(this.tb_rotor_Eloss_coeff);
            this.Controls.Add(this.tb_stator_Hloss_coeff);
            this.Controls.Add(this.tb_rotor_Hloss_coeff);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tb_pp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tb_R);
            this.Controls.Add(this.tb_Lq);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tb_Ld);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_psiM);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.bt_show_maxtorque_capa_curve);
            this.Controls.Add(this.comboBox_maptype);
            this.Controls.Add(this.tb_Umax);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tb_Imax);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tb_maxSpeed);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.bt_showmap);
            this.Controls.Add(this.plot);
            this.Name = "PMSM_EfficiencyMap_Analytical";
            this.Text = "PMSM_EfficiencyMap_Analytical";
            this.Load += new System.EventHandler(this.PMSM_EfficiencyMap_Analytical_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private OxyPlot.WindowsForms.PlotView plot;
        private System.Windows.Forms.Button bt_showmap;
        private System.Windows.Forms.TextBox tb_Umax;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tb_Imax;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tb_maxSpeed;
        private System.Windows.Forms.ComboBox comboBox_maptype;
        private System.Windows.Forms.Button bt_show_maxtorque_capa_curve;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tb_Lq;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tb_Ld;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tb_psiM;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tb_R;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tb_pp;
        private System.Windows.Forms.TextBox tb_rotor_Hloss_coeff;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tb_rotor_Eloss_coeff;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tb_stator_Hloss_coeff;
        private System.Windows.Forms.TextBox tb_stator_Eloss_coeff;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button button2;
    }
}