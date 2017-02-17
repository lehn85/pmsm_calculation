using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using ZedGraph;
using calc_from_geometryOfMotor.motor;
using calc_from_geometryOfMotor.motor.PMMotor;
using System.IO;
using System.Diagnostics;

namespace calc_from_geometryOfMotor
{
    public partial class ResultsWindow : Form
    {
        public ResultsWindow()
        {
            InitializeComponent();
        }

        private void ResultsWindow_Load(object sender, EventArgs e)
        {
            refreshUI();
        }

        public void refreshUI()
        {
            ProjectManager pm = ProjectManager.GetInstance();

            treeView1.Nodes.Clear();
            results = new List<ResultItem>();

            treeView1.Nodes.Add("Motor");
            parseMotorDerivedParams("Motor");

            treeView1.Nodes.Add("Static");
            AbstractAnalyticalAnalyser aa = pm.analyticalAnalyser;
            AbstractStaticAnalyser sa = pm.staticAnalyser;
            parseComparisonResults("Static", aa == null ? null : aa.getResults(), sa == null ? null : sa.getResults());

            foreach (String name in pm.GetAnalysisResultsNames())
            {
                // add nodes to treeview of results
                String[] ss = name.Split('.', '\\');
                TreeNodeCollection nodes = treeView1.Nodes;//first lv collection nodes
                for (int i = 0; i < ss.Length; i++)
                {
                    if (nodes.ContainsKey(ss[i]))
                        nodes = treeView1.Nodes[ss[i]].Nodes;//get next level nodes collections
                    else
                        nodes = nodes.Add(ss[i], ss[i]).Nodes;//add ss[i] to itself and get its collection (of couse empty)
                }

                // now parse the results data
                object result = pm.GetAnalysisResults(name);

                if (result == null)
                    continue;

                if (result is AbstractFEMResults)
                    parseResults(name, (AbstractFEMResults)result);
            }

            treeView1.ExpandAll();

            showResultsInDGV(currentPath);
        }

        private void showResultsInDGV(String group)
        {
            dgv_results.DataSource = results.Where(r => r.group == group).ToList();
        }

        private class ResultItem
        {
            public String aName { get; set; }

            internal object aValue { get; set; }//internal to hide from datagridview
            public String aValueText { get { return (aValue == null) ? "" : aValue.ToString(); } }

            public String femName { get; set; }

            internal object femValue { get; set; }//internal to hide from datagridview, because array will not shown in dgv and cause exception
            public String femValueText { get { return (femValue == null) ? "" : femValue.ToString(); } }

            public String difference { get; set; }

            public String group { get; set; }//for grouping static/transient/..
        }

        private List<ResultItem> results;

        private String currentPath = "";

        private void parseMotorDerivedParams(string group)
        {
            ProjectManager pm = ProjectManager.GetInstance();
            var dict = pm.Motor.GetDerivedParameters();

            foreach (String name in dict.Keys)
            {
                object value = dict[name];
                ResultItem item = new ResultItem();
                item.aName = name;
                item.aValue = value;
                item.group = group;

                results.Add(item);
            }
        }

        private void parseComparisonResults(String groupname, AbstractAnalyticalResults ar, AbstractFEMResults fr)
        {
            ProjectManager pm = ProjectManager.GetInstance();

            var aResults = ar == null ? null : ar.BuildResultsForDisplay();
            var femResults = fr == null ? null : fr.BuildResultsForDisplay();

            // show all analytical results (gmc)
            if (aResults != null)
                foreach (String aName in aResults.Keys)
                {
                    ResultItem item = new ResultItem();
                    item.aName = aName;
                    item.aValue = aResults[aName];

                    //compare if has femresults
                    if (femResults != null)
                    {
                        bool hasfemvalue = false;
                        foreach (String fName in femResults.Keys)
                            if (item.aName == fName)
                            {
                                item.femName = fName;
                                item.femValue = femResults[fName];
                                if (item.aValue is double && item.femValue is double)
                                {
                                    double v1 = (double)item.aValue;
                                    double v2 = (double)item.femValue;
                                    item.difference = String.Format("{0:F3}%", (v2 - v1) / v2 * 100);
                                }
                                hasfemvalue = true;
                                break;
                            }

                        if (hasfemvalue)
                            femResults.Remove(item.femName);
                    }
                    //add item to list results
                    item.group = groupname;
                    results.Add(item);
                }

            // add remaining item of femresults to list
            if (femResults != null)
            {
                foreach (String fName in femResults.Keys)
                {
                    ResultItem item = new ResultItem();
                    item.femName = fName;

                    if (femResults[fName] is ListPointD)
                    {
                        var ps = femResults[fName] as ListPointD;
                        item.femValue = ps.ToZedGraphPointPairList();
                    }
                    else
                        item.femValue = femResults[fName];

                    item.group = groupname;
                    results.Add(item);
                }
            }
        }

        private void parseResults(String fullname, AbstractFEMResults femresults)
        {
            ProjectManager pm = ProjectManager.GetInstance();
            if (femresults == null)
                return;

            IDictionary<String, object> dicts = femresults.BuildResultsForDisplay();

            foreach (String name in dicts.Keys)
            {
                object value = dicts[name];
                ResultItem item = new ResultItem();
                item.femName = name;
                item.group = fullname;

                if (value is ListPointD)
                    item.femValue = ((ListPointD)value).ToZedGraphPointPairList();
                else
                    item.femValue = value;

                results.Add(item);
            }
        }

        #region GUI

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeView tv = (TreeView)sender;
            ProjectManager pm = ProjectManager.GetInstance();
            currentPath = e.Node.FullPath;

            showResultsInDGV(currentPath);

            this.Text = currentPath;
        }

        private void dgv_results_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 3)
                return;

            if (e.RowIndex < 0)
                return;

            DataGridView dgv = (DataGridView)sender;
            List<ResultItem> list = (List<ResultItem>)dgv.DataSource;

            ResultItem resultItem = list[e.RowIndex];
            object femValue = list[e.RowIndex].femValue;

            if (resultItem.femName == "OpenResults")
            {
                string path = resultItem.femValue as string;
                try
                {
                    Process.Start(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot open " + path + "\n" + ex.Message);
                }
            }
            // if click pointpairlist, open graph windows
            else if (femValue.GetType().Equals(typeof(PointPairList)))
            {
                GraphWindow tc = new GraphWindow();
                tc.MdiParent = this.MdiParent;
                foreach (ResultItem ri in list)
                {
                    if ((ri.femValue != null) &&
                        ri.femValue.GetType().Equals(typeof(PointPairList)))
                    {
                        PointPairList data = (PointPairList)ri.femValue;
                        tc.addData(ri.femName, data);
                    }
                }
                tc.Text = currentPath;
                tc.Show();
            }
            else if (femValue.GetType().Equals(typeof(List<CoreLossResults>)))
            {
                CoreLossVisualizer visualizer = new CoreLossVisualizer();
                visualizer.MdiParent = this.MdiParent;
                visualizer.Text = currentPath;
                visualizer.Show();

                var cl = femValue as List<CoreLossResults>;
                visualizer.SetListCorelossResults(cl);
            }
            else if (femValue.GetType().Equals(typeof(DQCurrentMap)))
            {
                var data = femValue as DQCurrentMap;
                EfficiencyMapViewer emv = new EfficiencyMapViewer();

                emv.Text = "Efficiency map viewer";
                emv.setData(data);                
                emv.Show();
            }
        }

        #endregion


    }
}
