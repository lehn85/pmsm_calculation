namespace calc_from_geometryOfMotor
{
    partial class OptimizationWindow
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
            this.bt_run_optimization = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.udn_genome_size = new System.Windows.Forms.NumericUpDown();
            this.udn_generation_size = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.udn_population_size = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.rtb_construct_motor = new System.Windows.Forms.RichTextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.checkBox_useFEA = new System.Windows.Forms.CheckBox();
            this.udn_fitness_size = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.udn_mutation_rate = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.udn_cross_rate = new System.Windows.Forms.NumericUpDown();
            this.rtb_init_script = new System.Windows.Forms.RichTextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.rtb_fitness_function = new System.Windows.Forms.RichTextBox();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.bt_rerun_finish_script = new System.Windows.Forms.Button();
            this.rtb_finish_script = new System.Windows.Forms.RichTextBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.rtb_results = new System.Windows.Forms.RichTextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label_progress = new System.Windows.Forms.Label();
            this.bt_stop = new System.Windows.Forms.Button();
            this.bt_open_result_folder = new System.Windows.Forms.Button();
            this.bt_save_config = new System.Windows.Forms.Button();
            this.bt_new_config = new System.Windows.Forms.Button();
            this.comboBox_configs = new System.Windows.Forms.ComboBox();
            this.bt_delete_config = new System.Windows.Forms.Button();
            this.bt_copy_config = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.udn_genome_size)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_generation_size)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_population_size)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udn_fitness_size)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_mutation_rate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_cross_rate)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // bt_run_optimization
            // 
            this.bt_run_optimization.Location = new System.Drawing.Point(348, 310);
            this.bt_run_optimization.Name = "bt_run_optimization";
            this.bt_run_optimization.Size = new System.Drawing.Size(84, 36);
            this.bt_run_optimization.TabIndex = 0;
            this.bt_run_optimization.Text = "Run optimizing";
            this.bt_run_optimization.UseVisualStyleBackColor = true;
            this.bt_run_optimization.Click += new System.EventHandler(this.bt_run_optimization_click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Genomes";
            // 
            // udn_genome_size
            // 
            this.udn_genome_size.Location = new System.Drawing.Point(76, 80);
            this.udn_genome_size.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udn_genome_size.Name = "udn_genome_size";
            this.udn_genome_size.Size = new System.Drawing.Size(70, 20);
            this.udn_genome_size.TabIndex = 3;
            this.udn_genome_size.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // udn_generation_size
            // 
            this.udn_generation_size.Location = new System.Drawing.Point(76, 44);
            this.udn_generation_size.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.udn_generation_size.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udn_generation_size.Name = "udn_generation_size";
            this.udn_generation_size.Size = new System.Drawing.Size(70, 20);
            this.udn_generation_size.TabIndex = 5;
            this.udn_generation_size.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Generations";
            // 
            // udn_population_size
            // 
            this.udn_population_size.Location = new System.Drawing.Point(76, 10);
            this.udn_population_size.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.udn_population_size.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.udn_population_size.Name = "udn_population_size";
            this.udn_population_size.Size = new System.Drawing.Size(70, 20);
            this.udn_population_size.TabIndex = 7;
            this.udn_population_size.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Population";
            // 
            // rtb_construct_motor
            // 
            this.rtb_construct_motor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtb_construct_motor.Font = new System.Drawing.Font("Courier New", 10F);
            this.rtb_construct_motor.Location = new System.Drawing.Point(0, 0);
            this.rtb_construct_motor.Name = "rtb_construct_motor";
            this.rtb_construct_motor.Size = new System.Drawing.Size(599, 278);
            this.rtb_construct_motor.TabIndex = 9;
            this.rtb_construct_motor.Text = "";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(607, 304);
            this.tabControl1.TabIndex = 10;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.checkBox_useFEA);
            this.tabPage1.Controls.Add(this.udn_fitness_size);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.udn_mutation_rate);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.udn_cross_rate);
            this.tabPage1.Controls.Add(this.rtb_init_script);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.udn_genome_size);
            this.tabPage1.Controls.Add(this.udn_population_size);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.udn_generation_size);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(599, 278);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // checkBox_useFEA
            // 
            this.checkBox_useFEA.AutoSize = true;
            this.checkBox_useFEA.Location = new System.Drawing.Point(9, 238);
            this.checkBox_useFEA.Name = "checkBox_useFEA";
            this.checkBox_useFEA.Size = new System.Drawing.Size(68, 17);
            this.checkBox_useFEA.TabIndex = 17;
            this.checkBox_useFEA.Text = "Use FEA";
            this.checkBox_useFEA.UseVisualStyleBackColor = true;
            this.checkBox_useFEA.Visible = false;
            // 
            // udn_fitness_size
            // 
            this.udn_fitness_size.Location = new System.Drawing.Point(76, 109);
            this.udn_fitness_size.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udn_fitness_size.Name = "udn_fitness_size";
            this.udn_fitness_size.Size = new System.Drawing.Size(70, 20);
            this.udn_fitness_size.TabIndex = 13;
            this.udn_fitness_size.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 181);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Mutation rate";
            // 
            // udn_mutation_rate
            // 
            this.udn_mutation_rate.DecimalPlaces = 2;
            this.udn_mutation_rate.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.udn_mutation_rate.Location = new System.Drawing.Point(76, 179);
            this.udn_mutation_rate.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udn_mutation_rate.Name = "udn_mutation_rate";
            this.udn_mutation_rate.Size = new System.Drawing.Size(70, 20);
            this.udn_mutation_rate.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 150);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Cross rate";
            // 
            // udn_cross_rate
            // 
            this.udn_cross_rate.DecimalPlaces = 2;
            this.udn_cross_rate.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.udn_cross_rate.Location = new System.Drawing.Point(76, 148);
            this.udn_cross_rate.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udn_cross_rate.Name = "udn_cross_rate";
            this.udn_cross_rate.Size = new System.Drawing.Size(70, 20);
            this.udn_cross_rate.TabIndex = 9;
            // 
            // rtb_init_script
            // 
            this.rtb_init_script.Font = new System.Drawing.Font("Courier New", 10F);
            this.rtb_init_script.Location = new System.Drawing.Point(154, 12);
            this.rtb_init_script.Name = "rtb_init_script";
            this.rtb_init_script.Size = new System.Drawing.Size(434, 259);
            this.rtb_init_script.TabIndex = 8;
            this.rtb_init_script.Text = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 111);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(39, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Criteria";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.rtb_construct_motor);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(599, 278);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Construct motor from gens";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.rtb_fitness_function);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(599, 278);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Fitness function";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // rtb_fitness_function
            // 
            this.rtb_fitness_function.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtb_fitness_function.Font = new System.Drawing.Font("Courier New", 10F);
            this.rtb_fitness_function.Location = new System.Drawing.Point(0, 0);
            this.rtb_fitness_function.Name = "rtb_fitness_function";
            this.rtb_fitness_function.Size = new System.Drawing.Size(599, 278);
            this.rtb_fitness_function.TabIndex = 0;
            this.rtb_fitness_function.Text = "";
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.bt_rerun_finish_script);
            this.tabPage5.Controls.Add(this.rtb_finish_script);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(599, 278);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Output script";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // bt_rerun_finish_script
            // 
            this.bt_rerun_finish_script.Location = new System.Drawing.Point(513, 252);
            this.bt_rerun_finish_script.Name = "bt_rerun_finish_script";
            this.bt_rerun_finish_script.Size = new System.Drawing.Size(75, 23);
            this.bt_rerun_finish_script.TabIndex = 1;
            this.bt_rerun_finish_script.Text = "Execute script";
            this.bt_rerun_finish_script.UseVisualStyleBackColor = true;
            this.bt_rerun_finish_script.Click += new System.EventHandler(this.bt_rerun_finish_script_Click);
            // 
            // rtb_finish_script
            // 
            this.rtb_finish_script.Dock = System.Windows.Forms.DockStyle.Top;
            this.rtb_finish_script.Font = new System.Drawing.Font("Courier New", 10F);
            this.rtb_finish_script.Location = new System.Drawing.Point(0, 0);
            this.rtb_finish_script.Name = "rtb_finish_script";
            this.rtb_finish_script.Size = new System.Drawing.Size(599, 251);
            this.rtb_finish_script.TabIndex = 0;
            this.rtb_finish_script.Text = "";
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.rtb_results);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(599, 278);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Results";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // rtb_results
            // 
            this.rtb_results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtb_results.Location = new System.Drawing.Point(0, 0);
            this.rtb_results.Name = "rtb_results";
            this.rtb_results.ReadOnly = true;
            this.rtb_results.Size = new System.Drawing.Size(599, 278);
            this.rtb_results.TabIndex = 0;
            this.rtb_results.Text = "";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(348, 365);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(249, 18);
            this.progressBar1.TabIndex = 11;
            // 
            // label_progress
            // 
            this.label_progress.AutoSize = true;
            this.label_progress.Location = new System.Drawing.Point(345, 349);
            this.label_progress.Name = "label_progress";
            this.label_progress.Size = new System.Drawing.Size(75, 13);
            this.label_progress.TabIndex = 12;
            this.label_progress.Text = "label_progress";
            // 
            // bt_stop
            // 
            this.bt_stop.Location = new System.Drawing.Point(438, 310);
            this.bt_stop.Name = "bt_stop";
            this.bt_stop.Size = new System.Drawing.Size(82, 36);
            this.bt_stop.TabIndex = 13;
            this.bt_stop.Text = "Stop";
            this.bt_stop.UseVisualStyleBackColor = true;
            this.bt_stop.Click += new System.EventHandler(this.bt_stop_Click);
            // 
            // bt_open_result_folder
            // 
            this.bt_open_result_folder.Location = new System.Drawing.Point(526, 310);
            this.bt_open_result_folder.Name = "bt_open_result_folder";
            this.bt_open_result_folder.Size = new System.Drawing.Size(74, 36);
            this.bt_open_result_folder.TabIndex = 14;
            this.bt_open_result_folder.Text = "Open result folder";
            this.bt_open_result_folder.UseVisualStyleBackColor = true;
            this.bt_open_result_folder.Click += new System.EventHandler(this.bt_open_result_folder_Click);
            // 
            // bt_save_config
            // 
            this.bt_save_config.Location = new System.Drawing.Point(102, 343);
            this.bt_save_config.Name = "bt_save_config";
            this.bt_save_config.Size = new System.Drawing.Size(83, 25);
            this.bt_save_config.TabIndex = 9;
            this.bt_save_config.Text = "Save config";
            this.bt_save_config.UseVisualStyleBackColor = true;
            this.bt_save_config.Click += new System.EventHandler(this.bt_save_config_Click);
            // 
            // bt_new_config
            // 
            this.bt_new_config.Location = new System.Drawing.Point(13, 343);
            this.bt_new_config.Name = "bt_new_config";
            this.bt_new_config.Size = new System.Drawing.Size(83, 25);
            this.bt_new_config.TabIndex = 9;
            this.bt_new_config.Text = "New config";
            this.bt_new_config.UseVisualStyleBackColor = true;
            this.bt_new_config.Click += new System.EventHandler(this.bt_new_config_Click);
            // 
            // comboBox_configs
            // 
            this.comboBox_configs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_configs.FormattingEnabled = true;
            this.comboBox_configs.Location = new System.Drawing.Point(13, 311);
            this.comboBox_configs.Name = "comboBox_configs";
            this.comboBox_configs.Size = new System.Drawing.Size(137, 21);
            this.comboBox_configs.TabIndex = 15;
            this.comboBox_configs.SelectedIndexChanged += new System.EventHandler(this.comboBox_configs_SelectedIndexChanged);
            // 
            // bt_delete_config
            // 
            this.bt_delete_config.Location = new System.Drawing.Point(191, 343);
            this.bt_delete_config.Name = "bt_delete_config";
            this.bt_delete_config.Size = new System.Drawing.Size(83, 25);
            this.bt_delete_config.TabIndex = 9;
            this.bt_delete_config.Text = "Delete";
            this.bt_delete_config.UseVisualStyleBackColor = true;
            this.bt_delete_config.Click += new System.EventHandler(this.bt_delete_config_Click);
            // 
            // bt_copy_config
            // 
            this.bt_copy_config.Location = new System.Drawing.Point(13, 374);
            this.bt_copy_config.Name = "bt_copy_config";
            this.bt_copy_config.Size = new System.Drawing.Size(83, 25);
            this.bt_copy_config.TabIndex = 16;
            this.bt_copy_config.Text = "Copy config";
            this.bt_copy_config.UseVisualStyleBackColor = true;
            this.bt_copy_config.Click += new System.EventHandler(this.bt_copy_config_Click);
            // 
            // OptimizationWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 406);
            this.Controls.Add(this.bt_copy_config);
            this.Controls.Add(this.comboBox_configs);
            this.Controls.Add(this.bt_new_config);
            this.Controls.Add(this.bt_delete_config);
            this.Controls.Add(this.bt_save_config);
            this.Controls.Add(this.bt_open_result_folder);
            this.Controls.Add(this.bt_stop);
            this.Controls.Add(this.label_progress);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.bt_run_optimization);
            this.Name = "OptimizationWindow";
            this.Text = "OptimizationTool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OptimizationWindow_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.udn_genome_size)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_generation_size)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_population_size)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udn_fitness_size)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_mutation_rate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udn_cross_rate)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bt_run_optimization;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown udn_genome_size;
        private System.Windows.Forms.NumericUpDown udn_generation_size;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown udn_population_size;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox rtb_construct_motor;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.RichTextBox rtb_fitness_function;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label_progress;
        private System.Windows.Forms.RichTextBox rtb_init_script;
        private System.Windows.Forms.RichTextBox rtb_results;
        private System.Windows.Forms.Button bt_stop;
        private System.Windows.Forms.Button bt_open_result_folder;
        private System.Windows.Forms.Button bt_save_config;
        private System.Windows.Forms.Button bt_new_config;
        private System.Windows.Forms.ComboBox comboBox_configs;
        private System.Windows.Forms.Button bt_delete_config;
        private System.Windows.Forms.Button bt_copy_config;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.RichTextBox rtb_finish_script;
        private System.Windows.Forms.Button bt_rerun_finish_script;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown udn_mutation_rate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown udn_cross_rate;
        private System.Windows.Forms.NumericUpDown udn_fitness_size;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox checkBox_useFEA;
    }
}