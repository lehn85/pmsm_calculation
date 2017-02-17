using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using calc_from_geometryOfMotor.motor;

namespace calc_from_geometryOfMotor
{
    public partial class PreviewWindow : Form
    {
        public PreviewWindow()
        {
            InitializeComponent();

            MouseWheel += new MouseEventHandler(PreviewWindow_MouseWheel);            
        }

        private void PreviewWindow_Load(object sender, EventArgs e)
        {

        }
        
        private float Ox = 300.0F;
        private float Oy = 300.0F;
        private float scale = 3.0F;
        private AbstractMotor Motor;

        public void refreshPreview()
        {            
            Invalidate();
        }

        // default is project motor
        private bool IsProjectMotor = true;

        public void SetMotor(AbstractMotor motor)
        {
            Motor = motor;
            IsProjectMotor = false;
        }

        public void SetInfoText(string text)
        {
            label_infotext.Text = text;
        }

        private void PreviewWindow_Paint(object sender, PaintEventArgs e)
        {
            if (IsProjectMotor)
                Motor = ProjectManager.GetInstance().Motor;

            label1.Text = String.Format("{0:F1} : {1:F1} @ {2:F1}", Ox, Oy, scale);
            Motor.DrawPreview(e.Graphics, Ox, Oy, scale);
        }

        private MouseEventArgs lastMouseDown;
        private PointF lastO;

        private void PreviewWindow_MouseDown(object sender, MouseEventArgs e)
        {
            lastMouseDown = e;
            lastO = new PointF(Ox, Oy);
        }

        private void PreviewWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (lastMouseDown == null)
                return;

            Ox = lastO.X + (e.X - lastMouseDown.X);
            Oy = lastO.Y + (e.Y - lastMouseDown.Y);
            Invalidate();
        }

        private void PreviewWindow_MouseUp(object sender, MouseEventArgs e)
        {
            lastMouseDown = null;
        }

        private void PreviewWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldScale = scale;
            if (e.Delta > 0)
                scale += 1.0F;
            else if (e.Delta < 0)
                scale -= 1.0F;
            else return;

            if (scale < 1.0)
                scale = 1.0F;
            else if (scale > 1000.0)
                scale = 1000.0F;

            float deltaScale = scale - oldScale;
            Ox = e.X - (e.X - Ox) * (scale / oldScale);
            Oy = e.Y - (e.Y - Oy) * (scale / oldScale);

            Invalidate();
        }
    }
}
