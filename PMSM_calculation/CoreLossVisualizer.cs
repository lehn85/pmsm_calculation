using calc_from_geometryOfMotor.motor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace calc_from_geometryOfMotor
{
    public partial class CoreLossVisualizer : Form
    {
        private List<CoreLossResults> resultList;

        private float Ox = 300.0F;
        private float Oy = 300.0F;
        private float scale = 5.0F;

        public CoreLossVisualizer()
        {
            InitializeComponent();

            nud_zoom.Value = Convert.ToDecimal(scale);
        }

        private void CoreLossVisualizer_Load(object sender, EventArgs e)
        {
            buildBrushTable();

            List<PaletteType> list = Enum.GetValues(typeof(PaletteType)).Cast<PaletteType>().ToList();
            comboBox_palette.DataSource = list;
            comboBox_palette.SelectedIndex = 0;
            comboBox_palette.SelectedIndexChanged +=
                (s, ee) =>
                {
                    setPalette(list[comboBox_palette.SelectedIndex]);
                };
        }

        #region Public

        public void SetListCorelossResults(List<CoreLossResults> list)
        {
            resultList = list;

            detectMinMax();

            canvasTarget.Invalidate();
        }

        #endregion

        #region Few calculations

        private double default_max, default_min;
        private double min;// heat map min
        private double max;// heat map max        

        private void detectMinMax()
        {

            if (resultList == null)
                return;

            min = double.MaxValue;
            max = double.MinValue;

            foreach (var clResults in resultList)
            {
                for (int i = 0; i < clResults.elements.Count; i++)
                {
                    double v = getElementValue(clResults, i);
                    min = (min > v) ? v : min;
                    max = (max < v) ? v : max;
                }
            }

            default_max = max;
            default_min = min;

            tb_max.Text = max.ToString();
            tb_min.Text = min.ToString();
        }

        private void detectBoundary(out double x1, out double y1, out double x2, out double y2)
        {
            x1 = double.MaxValue;
            y1 = double.MaxValue;
            x2 = double.MinValue;
            y2 = double.MinValue;
            foreach (var clResults in resultList)
            {
                for (int i = 0; i < clResults.nodes.Count; i++)
                {
                    var n = clResults.nodes[i];
                    if (n.X < x1)
                        x1 = n.X;
                    if (n.Y < y1)
                        y1 = n.Y;
                    if (n.X > x2)
                        x2 = n.X;
                    if (n.Y > y2)
                        y2 = n.Y;
                }
            }
        }

        private double getElementValue(CoreLossResults part, int i)
        {
            return (part.elementEddyLosses[i] + part.elementHysLosses[i])
                        / (part.elements[i].area * part.length * 1e-9) / part.ro; //W/kg
        }

        #endregion

        #region Control: buttons, ...

        // when button apply max,min clicked
        private void bt_apply_max_min_Click(object sender, EventArgs e)
        {
            try
            {
                max = double.Parse(tb_max.Text);
                min = double.Parse(tb_min.Text);
                if (max < min || max < 0 || min < 0)
                {
                    max = default_max;
                    min = default_min;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Max, Min entered invalid " + ex.Message);
                max = default_max;
                min = default_min;
            }

            tb_max.Text = max.ToString();
            tb_min.Text = min.ToString();

            canvasTarget.Invalidate();
        }

        private void bt_copypicture_Click(object sender, EventArgs e)
        {
            double x1, y1, x2, y2;
            detectBoundary(out x1, out y1, out x2, out y2);

            int sw = 120;// for scale presentation to the right
            int w = (int)(scale * (x2 - x1)) + sw;
            int h = (int)(scale * (y2 - y1));
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, w, h));

                drawAll(g, 0, h / 2, scale);

                drawIntensivenessScale(g, w - sw + 5, 5, 40, 16);
            }
            Clipboard.SetImage(bmp);
        }

        #endregion

        #region Canvas events        

        // this is canvas target paint method
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            label_origin_scale.Text = String.Format("{0:F1} : {1:F1} @ {2:F1}", Ox, Oy, scale);
            drawAll(g, Ox, Oy, scale);
            drawIntensivenessScale(g, 300, 0, 40, 16);
        }

        private MouseEventArgs lastMouseDown;
        private PointF lastO;

        private void canvasTarget_MouseDown(object sender, MouseEventArgs e)
        {
            lastMouseDown = e;
            lastO = new PointF(Ox, Oy);
        }

        private void canvasTarget_MouseMove(object sender, MouseEventArgs e)
        {
            //PointF coord = MouseLocationToCoordinate(e.Location);
            //label_coordinate.Text = string.Format("{0:F2} , {1:F2}", coord.X, coord.Y);
        }

        private void canvasTarget_MouseUp(object sender, MouseEventArgs e)
        {
            if (lastMouseDown == null)
                return;
            // movement is too small
            if (Math.Sqrt(Math.Pow(e.X - lastMouseDown.X, 2) + Math.Pow(e.Y - lastMouseDown.Y, 2)) < 5)
            {
                showDataForMouseLocation(MouseLocationToCoordinate(e.Location));
                return;
            }

            Ox = lastO.X + (e.X - lastMouseDown.X);
            Oy = lastO.Y + (e.Y - lastMouseDown.Y);
            canvasTarget.Invalidate();

            lastMouseDown = null;
        }

        private void canvasTarget_MouseWheel(object sender, MouseEventArgs e)
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

            canvasTarget.Invalidate();
        }

        private void nud_zoom_ValueChanged(object sender, EventArgs e)
        {
            float oldScale = scale;
            scale = (float)Convert.ToDouble(nud_zoom.Value);

            if (scale < 1.0)
                scale = 1.0F;
            else if (scale > 1000.0)
                scale = 1000.0F;

            float deltaScale = scale - oldScale;
            Ox = canvasTarget.Width / 2 - (canvasTarget.Width / 2 - Ox) * (scale / oldScale);
            Oy = canvasTarget.Height / 2 - (canvasTarget.Height / 2 - Oy) * (scale / oldScale);

            canvasTarget.Invalidate();
        }

        private PointF MouseLocationToCoordinate(Point location)
        {
            // convert mouse location to coordinate, corresponding to element
            return new PointF((location.X - Ox) / scale, -(location.Y - Oy) / scale);
        }

        #endregion

        #region Draw method

        private const int nTotalBrush = 20;
        private const int nBrush = nTotalBrush / 2;
        private Color color1 = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
        private Color color2 = Color.FromArgb(0xff, 0x80, 0x80, 0x80);
        private Color color3 = Color.FromArgb(0xff, 0, 0, 0);
        private List<Brush> brushes;

        public enum PaletteType
        {
            Grayscale,
            GreenOrangeRed,
        }

        private PaletteType _palette = PaletteType.Grayscale;

        private void setPalette(PaletteType p)
        {
            _palette = p;
            if (_palette == PaletteType.Grayscale)
            {
                color1 = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
                color2 = Color.FromArgb(0xff, 0x80, 0x80, 0x80);
                color3 = Color.FromArgb(0xff, 0, 0, 0);
            }
            else if (_palette == PaletteType.GreenOrangeRed)
            {
                color1 = Color.Green;
                color2 = Color.Orange;
                color3 = Color.Red;
            }
            buildBrushTable();
            canvasTarget.Invalidate();
        }

        private void buildBrushTable()
        {
            brushes = new List<Brush>();

            addBrushWithColorTransient(color1, color2, nBrush);
            addBrushWithColorTransient(color2, color3, nBrush);
        }

        private void addBrushWithColorTransient(Color color1, Color color2, int count)
        {
            int d_r = color2.R - color1.R;
            int d_g = color2.G - color1.G;
            int d_b = color2.B - color1.B;

            for (int i = 0; i < count; i++)
            {
                double p = 1.0 * i / count;
                Color c = Color.FromArgb((int)(color1.R + p * d_r),
                                         (int)(color1.G + p * d_g),
                                         (int)(color1.B + p * d_b));

                brushes.Add(new SolidBrush(c));
            }
        }

        private void drawAll(Graphics g, float Ox, float Oy, float scale)
        {
            if (resultList == null)
                return;

            // transform system coordinate
            g.ResetTransform();
            g.TranslateTransform(Ox, Oy);
            g.ScaleTransform(scale, -scale);

            //var pen_colors = new Color[] { Color.DarkBlue, Color.DarkGreen };

            GraphicsPath path = new GraphicsPath();

            for (int i = 0; i < resultList.Count; i++)
            {
                var clResults = resultList[i];
                //var pen_color = pen_colors[i];
                //using (Pen pen = new Pen(pen_color, 1.0F / scale))
                //{
                for (int j = 0; j < clResults.elements.Count; j++)
                {
                    var e = clResults.elements[j];
                    PointF[] nodes = e.nodes.Select(index => clResults.nodes[index].ToPointF()).ToArray();

                    double p = (getElementValue(clResults, j) - min) / (max - min);
                    int k = (int)(p * nTotalBrush);
                    if (k < 0) k = 0;
                    if (k > nTotalBrush - 1)
                        k = nTotalBrush - 1;

                    Brush b = brushes[k];

                    g.FillPolygon(b, nodes);

                    if (_palette == PaletteType.Grayscale)
                        path.AddPolygon(nodes);
                }
                //}
            }

            if (path.PointCount > 0)
                using (Pen pen = new Pen(Color.DimGray, 1.0F / scale))
                {
                    g.DrawPath(pen, path);
                }

            // draw intensiveness scale            
        }

        private void drawIntensivenessScale(Graphics g, int x, int y, int w, int h)
        {
            if (brushes == null)
                return;

            g.ResetTransform();

            int y2 = y + h * brushes.Count;
            using (Font f = new Font(FontFamily.GenericSansSerif, h * 2 / 3, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Brush brush_text = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < brushes.Count; i++)
                {
                    Brush b = brushes[i];
                    g.FillRectangle(b, x, y2 - h - i * h, w, h);
                    g.DrawString(string.Format("{0:F1}", min + (max - min) * i / (brushes.Count - 1)), f, brush_text, x + w + 5, y2 - h - i * h);
                }

                g.DrawString("Specific coreloss (W/kg)", f, brush_text, x, y2 + 5);
            }
        }

        #endregion

        #region Show data of element

        private void showDataForMouseLocation(PointF coord)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            CoreLossResults part = null;
            int iE = -1;
            double min_d = double.MaxValue;
            double d = 0;
            foreach (var clResults in resultList)
            {
                for (int i = 0; i < clResults.elements.Count; i++)
                {
                    d = Math.Pow(coord.X - clResults.elements[i].center.X, 2) + Math.Pow(coord.Y - clResults.elements[i].center.Y, 2);
                    if (d < min_d)
                    {
                        min_d = d;
                        part = clResults;
                        iE = i;
                    }
                }
            }

            sw.Stop();

            if (part != null && iE >= 0)
            {
                showGraphDataForElement(part, iE);
            }
        }

        private void showGraphDataForElement(CoreLossResults part, int index)
        {
            label_coordinate.Text = string.Format("{0}, Element {1}, coreloss = {2} (W/kg)",
                part.name, index, getElementValue(part, index));

            zc.IsShowPointValues = true;
            MasterPane masterPane = zc.MasterPane;

            // clear pane
            masterPane.PaneList.Clear();

            // first pane: Bx,By
            GraphPane pane1 = new GraphPane();
            pane1.Title.Text = "Vector B";
            pane1.AddCurve("VectorB", part.Bx[index], part.By[index], Color.Blue, SymbolType.None);
            //pane1.XAxis.Scale.Max = 2;
            //pane1.XAxis.Scale.Min = -2;
            //pane1.YAxis.Scale.Max = 2;
            //pane1.YAxis.Scale.Min = -2;

            masterPane.Add(pane1);

            // second pane            
            GraphPane pane2 = new GraphPane();
            pane2.Title.Text = "Bx, By by time";
            double T = 1.0 / part.basefreq;
            int nSample = part.Bx[index].Length;
            double[] time = Enumerable.Range(0, nSample).Select(i => T * i / nSample).ToArray();
            pane2.AddCurve("Bx", time, part.Bx[index], Color.Blue, SymbolType.None);
            pane2.AddCurve("By", time, part.By[index], Color.Red, SymbolType.None);
            pane2.YAxis.Scale.Max = 2;
            pane2.YAxis.Scale.Min = -2;

            masterPane.Add(pane2);

            // 3rd pane            
            GraphPane pane3 = new GraphPane();
            pane3.Title.Text = "Fourier analysis of Bx, By";
            nSample = part.Bfftx[index].Length / 2;
            double[] freq = Enumerable.Range(0, nSample).Select(i => (double)i * part.basefreq).ToArray();
            double[] bfftx = Enumerable.Range(0, nSample).Select(i => part.Bfftx[index][i]).ToArray();
            double[] bffty = Enumerable.Range(0, nSample).Select(i => part.Bffty[index][i]).ToArray();

            pane3.AddBar(string.Format("FFT Bx (THD = {0:F2}%)", calcTHD(bfftx)), freq, bfftx, Color.Blue);
            pane3.AddBar(string.Format("FFT By (THD = {0:F2}%)", calcTHD(bffty)), freq, bffty, Color.Red);
            //pane3.XAxis.Scale.Max = 30 * part.basefreq;
            //pane3.XAxis.Scale.Min = 0;

            masterPane.Add(pane3);

            // layout them
            using (Graphics g = CreateGraphics())
            {
                masterPane.SetLayout(g, PaneLayout.SingleColumn);
            }

            zc.AxisChange();
            zc.Invalidate();
        }

        private double calcTHD(double[] fft)
        {
            double sumOfSquare = 0;
            for (int i = 2; i < fft.Length; i++)
                sumOfSquare += fft[i] * fft[i];

            return Math.Sqrt(sumOfSquare) / fft[1] * 100.0;
        }

        #endregion        
    }
}
