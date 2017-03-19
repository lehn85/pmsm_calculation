/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using btl.generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace calc_from_geometryOfMotor
{
    public partial class OptimizationResultsWindow : Form
    {
        public event Action<Individual> OnIndividualSelected;
        public event Action<Individual> OnApplyIndividualClick;

        public OptimizationResultsWindow()
        {
            InitializeComponent();
        }

        private void OptimizationResultsWindow_Load(object sender, EventArgs e)
        {
            dgv.CellClick += Dgv_CellClick;
            //zc1.MouseClick += Zc1_MouseClick;
        }

        private void Zc1_MouseClick(object sender, MouseEventArgs e)
        {
            CurveItem curve = null;
            int index = -1;
            Point mouseScreenPoint = PointToScreen(e.Location);
            zc1.GraphPane.FindNearestPoint(mouseScreenPoint, out curve, out index);
            if (curve != null && index >= 0)
            {
                var ind = GetNearestIndividual(new double[] { curve[index].X, curve[index].Y });
                var clone = new Individual(ind.Genes, ind.Fitness);
                if (OnIndividualSelected != null)
                    OnIndividualSelected.Invoke(clone);
            }
        }

        private Individual GetNearestIndividual(double[] fitness)
        {
            int k = -1;
            var list = history[history.Count - 1];
            double nearest = double.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var ind = list[i];
                double d = Math.Sqrt(Math.Pow(ind.Fitness[0] - fitness[0], 2) + Math.Pow(ind.Fitness[1] - fitness[1], 2));
                if (d < nearest)
                {
                    nearest = d;
                    k = i;
                }
            }

            if (k >= 0)
                return list[k];

            return null;
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var rIndex = e.RowIndex;
            if (rIndex < 0)
                return;

            double[] fitness = new double[dgv.ColumnCount];
            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] = (double)dgv.Rows[rIndex].Cells[i].Value;
            }

            var ind = GetNearestIndividual(fitness);
            var clone = new Individual(ind.Genes, ind.Fitness);
            if (OnIndividualSelected != null)
                OnIndividualSelected.Invoke(clone);
        }

        Color[] colors = { Color.Red, Color.Green, Color.Blue };

        private readonly List<List<Individual>> history = new List<List<Individual>>();

        private readonly List<Individual> allpop = new List<Individual>();

        public void visualizeNonDominatedSet(List<Individual> list)
        {
            if (list == null)
                return;
            if (list.Count == 0)
                return;
            if (list[0].FitnessCount != 2)
                return;

            if (history.Count < 2)
                history.Add(list);
            else history[1] = list;            

            zc1.IsShowPointValues = true;
            var pane = zc1.GraphPane;

            pane.CurveList.Clear();

            for (int j = 0; j < history.Count; j++)
            {
                var lll = history[j];
                var ppl = lll.Select(i => new PointPair(i.Fitness[0], i.Fitness[1]))
                              .OrderBy(p => p.X)
                              .ToList();
                var real_ppl = new PointPairList(ppl.Select(p => p.X).ToArray(), ppl.Select(p => p.Y).ToArray());

                pane.AddCurve("Pareto-frontier (" + (j == 0 ? "First gen" : "Last gen") + ")", real_ppl, colors[j]);
            }            

            var data = history[history.Count - 1].Select(i => new SolutionItem()
            {
                individual = i,
                f1 = i.Fitness[0],
                f2 = i.Fitness[1],
            })
            .OrderBy(si => si.f1)
            .ToList();

            dgv.DataSource = new BindingList<SolutionItem>(data);

            zc1.AxisChange();
            zc1.Invalidate();
        }        

        public void clearHistory()
        {
            history.Clear();
        }

        public class SolutionItem
        {
            internal Individual individual;
            public double f1 { get; set; }
            public double f2 { get; set; }
        }

        private void bt_apply_selected_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0 || OnApplyIndividualClick == null)
                return;

            var row = dgv.SelectedRows[0];

            double[] fitness = new double[dgv.ColumnCount];
            for (int i = 0; i < fitness.Length; i++)
            {
                fitness[i] = (double)row.Cells[i].Value;
            }

            var ind = GetNearestIndividual(fitness);
            var clone = new Individual(ind.Genes, ind.Fitness);

            OnApplyIndividualClick(clone);
        }
    }
}
