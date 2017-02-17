using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using log4net;
using System.IO;
using calc_from_geometryOfMotor.motor;

namespace calc_from_geometryOfMotor
{
    public partial class Form_MotorParams : Form
    {
        private static readonly ILog log = LogManager.GetLogger("FormMotorParam");

        public Form_MotorParams()
        {
            InitializeComponent();
        }

        private void Form_MotorParams_Load(object sender, EventArgs e)
        {
            refreshUI();
        }

        public void refreshUI()
        {
            treeView1.Nodes.Clear();

            ProjectManager pm = ProjectManager.GetInstance();
            foreach (String path in pm.Components.Keys)
            {
                String[] ss = path.Split('.', '\\');
                TreeNodeCollection nodes = treeView1.Nodes;//first lv collection nodes
                for (int i = 0; i < ss.Length; i++)
                {
                    if (nodes.ContainsKey(ss[i]))
                        nodes = treeView1.Nodes[ss[i]].Nodes;//get next level nodes collections
                    else
                        nodes = nodes.Add(ss[i], ss[i]).Nodes;//add ss[i] to itself and get its collection (of couse empty)
                }
            }

            treeView1.ExpandAll();
            if (treeView1.Nodes.Count > 0)
                treeView1.SelectedNode = treeView1.Nodes[0];//select the first
        }

        private void dgv_motorParams_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            List<Parameter> list = (List<Parameter>)dgv.DataSource;
            Parameter param = list[e.RowIndex];

            // try evaluate value
            param.EvaluateValue();

            // redraw (because maybe param changed)
            dgv.Invalidate();

            // inform project manager that params have changed
            ProjectManager pm = ProjectManager.GetInstance();
            // begin invoke to make it run in next UI loop
            // to avoid infinity loop (celleditend-refresh dgv)
            pm.InvalidateParams();
        }

        private void dgv_motorParams_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            // only work with value
            if (e.ColumnIndex != Parameter.INDEX_TEXT)
                return;

            DataGridView dgv = (DataGridView)sender;
            List<Parameter> listPI = (List<Parameter>)dgv.DataSource;
            Parameter pi = listPI[e.RowIndex];

            // B, H
            if (pi.name == "BH")
            {
                //MessageBox.Show("Now do something to input B,H");
                OpenFileDialog fod = new OpenFileDialog()
                {
                    Filter = "Two columns text file (B-H)|*.txt",
                };
                if (fod.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var points = readBHCurveFromFile(fod.FileName);
                        pi.value = points;
                        dgv_motorParams_CellEndEdit(sender, e);//request validating data
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while import B-H\n" + ex.Message, "Error");
                    }
                }
            }
        }

        private PointBH[] readBHCurveFromFile(string fn)
        {
            List<PointBH> list = new List<PointBH>();
            using (StreamReader sr = new StreamReader(fn))
            {
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine();
                    string[] ss = s.Split(new char[] { ' ', '\t' });
                    if (ss.Length >= 2)
                    {
                        double b = 0, h = 0;
                        if (double.TryParse(ss[0], out b) && double.TryParse(ss[1], out h))
                        {
                            // check duplication
                            if (list.Where(pbh => pbh.b == b).Count() == 0)
                            {
                                list.Add(new PointBH() { b = b, h = h });
                                // if not zero, add 
                                if (b != 0 && h != 0)
                                    list.Add(new PointBH() { b = -b, h = -h });
                            }
                        }
                    }
                }
            }

            // order by b
            return list.OrderBy(pbh => pbh.b).ToArray();
        }
        /// <summary>
        /// Show input params
        /// </summary>
        /// <param name="dgv"></param>
        /// <param name="pc"></param>
        private void showInputParamsInDGV(DataGridView dgv, ParametersCollection pc)
        {
            dgv.DataSource = pc;

            // if null, then leave
            if (pc == null)
                return;

            dgv.Columns[Parameter.INDEX_GROUP].Visible = false;
            dgv.Columns[Parameter.INDEX_VALUETYPE].Visible = false;
            dgv.Columns[Parameter.INDEX_NAME].HeaderText = "Parameter";
            dgv.Columns[Parameter.INDEX_TEXT].HeaderText = "Value";
            dgv.Columns[Parameter.INDEX_VALUE].HeaderText = "Eval";
            dgv.Columns[Parameter.INDEX_DESC].HeaderText = "Description";
            dgv.Columns[Parameter.INDEX_STATUS].HeaderText = "Status";

            // customize the cell that valuetype enum
            foreach (Parameter pi in pc)
            {
                if (pi.valueType.IsEnum || pi.valueType.Equals(typeof(bool)))
                {
                    int index = pc.IndexOf(pi);
                    DataGridViewComboBoxCell cell = new DataGridViewComboBoxCell();
                    dgv.Rows[index].Cells[Parameter.INDEX_TEXT] = cell;
                    if (pi.valueType.IsEnum)
                        cell.DataSource = Enum.GetNames(pi.valueType);
                    else if (pi.valueType.Equals(typeof(bool)))
                        cell.DataSource = new String[] { true.ToString(), false.ToString() };
                }
            }
        }

        private String currentPath = "";
        private TreeNode currentNode = null;

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeView tv = (TreeView)sender;
            ProjectManager pm = ProjectManager.GetInstance();
            String item = e.Node.FullPath;

            ParametersCollection pc = new ParametersCollection();
            pc.AddRange(pm.MotorParams.Where(pi => pi.group == item));
            showInputParamsInDGV(dgv_motorParams, pc);

            currentPath = e.Node.FullPath;
            currentNode = e.Node;

            this.Text = currentPath;
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (e.Node.FullPath.StartsWith("Transient"))
                {
                    if (e.Node.Level == 0)//parent
                    {
                        runToolStripMenuItem.Visible = false;
                        removeToolStripMenuItem.Visible = false;
                        contextMenu_transient.Show(this, e.Location, ToolStripDropDownDirection.Default);
                    }
                    else if (e.Node.Level == 1)//child
                    {
                        runToolStripMenuItem.Visible = true;
                        removeToolStripMenuItem.Visible = true;
                        contextMenu_transient.Show(this, e.Location, ToolStripDropDownDirection.Default);
                    }
                }

                else if (e.Node.FullPath.StartsWith("Motor"))//motor
                {
                    if (e.Node.Level == 0)//parent
                    {
                        var pm = ProjectManager.GetInstance();
                        Type[] listmotorTypes = pm.ListMotorType;
                        changeMotorTypeToolStripMenuItem.DropDownItems.Clear();
                        foreach (Type motortype in listmotorTypes)
                        {
                            ToolStripMenuItem item = new ToolStripMenuItem();
                            item.Name = motortype.Name;
                            item.Text = motortype.Name;
                            item.Tag = motortype;
                            if (motortype.Equals(pm.CurrentMotorType))
                                item.Checked = true;
                            item.Click += contextMenu_ChangeMotorType_ChildItemClicked;
                            changeMotorTypeToolStripMenuItem.DropDownItems.Add(item);
                        }
                        contextMenu_Motor.Show(this, e.Location, ToolStripDropDownDirection.Default);
                    }
                }
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectManager.GetInstance().addTransientAnalysis();

            refreshUI();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectManager.GetInstance().runTransientAnalysis(currentPath);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectManager.GetInstance().removeTransientAnalysis(currentPath);
            currentPath = "";
            refreshUI();
        }

        private void contextMenu_ChangeMotorType_ChildItemClicked(object sender, EventArgs e)
        {
            var clickedItem = sender as ToolStripItem;
            var pm = ProjectManager.GetInstance();
            var motorType = clickedItem.Tag as Type;
            pm.changeMotorType(motorType);
            refreshUI();
        }

        private void treeView1_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (!e.Node.FullPath.Contains("Transient"))
                e.CancelEdit = true;
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null)
                return;

            String newname = e.Node.Parent.FullPath + "\\" + e.Label;

            bool b = ProjectManager.GetInstance().renameTransientAnalysis(currentPath, newname);

            if (!b)
                e.CancelEdit = true;
            else
            {
                //update UI after rename
                //refreshUI();
            }
        }
    }
}
