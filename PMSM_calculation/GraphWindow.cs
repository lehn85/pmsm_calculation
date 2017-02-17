using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace calc_from_geometryOfMotor
{
    public partial class GraphWindow : Form
    {
        public GraphWindow()
        {
            InitializeComponent();
        }

        private void GraphWindow_Load(object sender, EventArgs e)
        {
            refreshListbox();
        }

        public void refreshListbox()
        {
            listBox1.Items.Clear();
            foreach (String name in Datas.Keys)
            {
                listBox1.Items.Add(name);
            }
        }

        IDictionary<String, IPointList> Datas = new Dictionary<String, IPointList>();

        Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Black, Color.YellowGreen, Color.Violet, Color.Orange,
                           Color.OrangeRed,Color.Olive,Color.Navy,Color.MistyRose,Color.PaleVioletRed,Color.Orchid
                         };

        IDictionary<String, PointPairList> FouriesAnalysis = new Dictionary<String, PointPairList>();

        public IDictionary<String, IPointList> getListDatas()
        {
            return Datas;
        }

        public void addData(String name, double[] x, double[] y)
        {
            PointPairList ppl = new PointPairList(x, y);
            Datas[name] = ppl;
        }

        public void addData(String name, IPointList ppl)
        {
            Datas[name] = ppl;
        }

        private PointPairList getFourierAnalysis(String name)
        {
            if (Datas[name] == null)
                return null;
            if (FouriesAnalysis.ContainsKey(name))
                return FouriesAnalysis[name];

            IPointList data = Datas[name];
            int n = data.Count;

            //fourier analysis
            Complex[] samples = new Complex[n];
            double[] x2 = new double[n];
            double[] y2 = new double[n];
            for (int i = 0; i < n; i++)
            {
                samples[i] = new Complex(data[i].Y, 0);
            }

            Fourier.Forward(samples, FourierOptions.NoScaling);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 13; i++)
                sb.AppendFormat("{0}:{1}<{2}\t", i, samples[i].Magnitude / n * 2, samples[i].Phase * 180 / Math.PI);
            Console.WriteLine("Fourier forward: " + sb.ToString());

            for (int i = 0; i < n / 2; i++)
            {
                x2[i] = i;
                //y2[i] = samples[i].Imaginary / n * 2;
                y2[i] = samples[i].Magnitude / n * 2;
            }
            y2[0] /= 2;

            FouriesAnalysis[name] = new PointPairList(x2, y2);

            return FouriesAnalysis[name];
        }

        /// <summary>
        /// Calculate THD of fourier
        /// </summary>
        /// <param name="name"></param>
        /// <returns>in %</returns>
        private double calcTHD(String name)
        {
            PointPairList fd = getFourierAnalysis(name);

            double sumOfSquare = 0;
            for (int i = 2; i < fd.Count; i++)
                sumOfSquare += fd[i].Y * fd[i].Y;

            return Math.Sqrt(sumOfSquare) / fd[1].Y * 100.0;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshGraphs();
            refreshDataGridView();
        }

        private void refreshGraphs()
        {
            zc1.IsShowPointValues = true;
            MasterPane masterPane = zc1.MasterPane;

            masterPane.PaneList.Clear();

            // first pane (primary)
            GraphPane pane1 = new GraphPane();
            int cc = 0;
            foreach (String name in listBox1.SelectedItems)
            {
                pane1.Title.Text = this.Text;
                pane1.AddCurve(name, Datas[name], colors[cc], SymbolType.None);
                cc++;
                if (cc >= colors.Length)
                    cc = 0;
            }
            masterPane.Add(pane1);

            // second pane
            if (checkBox_showFourierAnalysis.Checked)
            {
                GraphPane pane2 = new GraphPane();

                int c = 0;
                foreach (String name in listBox1.SelectedItems)
                {
                    pane2.Title.Text = "Fourier analysis";
                    pane2.AddBar(String.Format("{0} (THD={1:F2}%)", name, calcTHD(name)), getFourierAnalysis(name), colors[c]);
                    pane2.XAxis.Scale.Max = 30;
                    pane2.XAxis.Scale.Min = 0;
                    c++;
                    if (cc >= colors.Length)
                        cc = 0;
                }

                masterPane.Add(pane2);
            }

            // layout them
            using (Graphics g = CreateGraphics())
            {
                masterPane.SetLayout(g, PaneLayout.SingleColumn);
            }

            zc1.AxisChange();
            zc1.Invalidate();

            tb_minX.Text = pane1.XAxis.Scale.Min.ToString();
            tb_maxX.Text = pane1.XAxis.Scale.Max.ToString();
            tb_minY.Text = pane1.YAxis.Scale.Min.ToString();
            tb_maxY.Text = pane1.YAxis.Scale.Max.ToString();
        }

        private void refreshDataGridView()
        {
            DataTable tbl = new DataTable();

            int maxrow = 0;
            foreach (String name in listBox1.SelectedItems)
            {
                tbl.Columns.Add(name + ".X");
                tbl.Columns.Add(name + ".Y");
                if (maxrow < Datas[name].Count)
                    maxrow = Datas[name].Count;
            }

            // insert all rows
            for (int i = 0; i < maxrow; i++)
                tbl.Rows.Add(tbl.NewRow());


            // put data in to rows
            foreach (String name in listBox1.SelectedItems)
            {
                for (int i = 0; i < Datas[name].Count; i++)
                {
                    DataRow row = tbl.Rows[i];
                    row[name + ".X"] = Datas[name][i].X;
                    row[name + ".Y"] = Datas[name][i].Y;                    
                }
            }

            dataGridView1.DataSource = tbl;
        }

        private void checkBox_showFourierAnalysis_CheckedChanged(object sender, EventArgs e)
        {
            refreshGraphs();
        }

        private void bt_apply_Click(object sender, EventArgs e)
        {
            GraphPane pane = zc1.MasterPane.PaneList[0];
            pane.XAxis.Scale.Min = double.Parse(tb_minX.Text);
            pane.XAxis.Scale.Max = double.Parse(tb_maxX.Text);
            pane.YAxis.Scale.Min = double.Parse(tb_minY.Text);
            pane.YAxis.Scale.Max = double.Parse(tb_maxY.Text);
            zc1.Invalidate();
        }

        private void checkBox_showdata_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer3.Panel2Collapsed = !checkBox_showdata.Checked;
        }
    }
}
