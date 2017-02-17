namespace calc_from_geometryOfMotor
{
    partial class PreviewWindow
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
            this.label_infotext = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "x,y,s";
            // 
            // label_infotext
            // 
            this.label_infotext.AutoSize = true;
            this.label_infotext.BackColor = System.Drawing.Color.Transparent;
            this.label_infotext.Enabled = false;
            this.label_infotext.Location = new System.Drawing.Point(12, 22);
            this.label_infotext.Name = "label_infotext";
            this.label_infotext.Size = new System.Drawing.Size(28, 13);
            this.label_infotext.TabIndex = 1;
            this.label_infotext.Text = "x,y,s";
            // 
            // PreviewWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(483, 292);
            this.Controls.Add(this.label_infotext);
            this.Controls.Add(this.label1);
            this.DoubleBuffered = true;
            this.Name = "PreviewWindow";
            this.Text = "PreviewWindow";
            this.Load += new System.EventHandler(this.PreviewWindow_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PreviewWindow_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PreviewWindow_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_infotext;
    }
}