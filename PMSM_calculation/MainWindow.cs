/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using log4net;
using NLua;
using calc_from_geometryOfMotor.motor.PMMotor;
using calc_from_geometryOfMotor.motor;

namespace calc_from_geometryOfMotor
{
    public partial class MainWindow : Form
    {
        private static readonly ILog log = LogManager.GetLogger("MAIN");

        private LogWindow logwindow;
        private PreviewWindow previewWindow;
        private Form_MotorParams form_motorParams;
        private ResultsWindow resultsWindow;
        private SweepParams_Window sweepParamsWindow;
        private Lua lua_state;
        private ProjectManager projectManager;

        private OptimizationWindow optimizationWindow;

        public MainWindow()
        {
            InitializeComponent();

            lua_state = LuaHelper.GetLuaState();

            projectManager = ProjectManager.GetInstance();
            projectManager.OnMotorAnalysisResultsUpdate += new EventHandler(projectManager_RequestRefreshUI);
            projectManager.loadStartupProject();

            form_motorParams = new Form_MotorParams();
            form_motorParams.MdiParent = this;

            resultsWindow = new ResultsWindow();
            resultsWindow.MdiParent = this;

            logwindow = new LogWindow();
            logwindow.MdiParent = this;

            previewWindow = new PreviewWindow();
            previewWindow.MdiParent = this;

            initMenu();
        }

        private void initMenu()
        {
            var pm = ProjectManager.GetInstance();
            Type[] listmotorTypes = pm.ListMotorType;
            newToolStripMenuItem.DropDownItems.Clear();
            foreach (Type motortype in listmotorTypes)
            {
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Name = motortype.Name;
                item.Text = motortype.Name;
                item.Tag = motortype;
                item.Click += newToolStripMenuItem_ChildItem_Click;
                newToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            previewWindow.Show();
            resultsWindow.Show();
            logwindow.Show();
            form_motorParams.Show();

            refreshUI();

            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void projectManager_RequestRefreshUI(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Delegate d = (Action)delegate () { projectManager_RequestRefreshUI(sender, e); };
                BeginInvoke(d);
                return;
            }

            if (previewWindow != null && previewWindow.Visible)
                previewWindow.refreshPreview();

            if (resultsWindow != null && resultsWindow.Visible)
                resultsWindow.refreshUI();

            refreshUI();
        }

        private void refreshUI()
        {
            string md5str = projectManager.Motor != null ? projectManager.Motor.GetMD5String() : "NULL";
            this.Text = String.Format("{0} ({1})", projectManager.CurrentProjectFile, md5str);
        }

        #region File menu

        private void newToolStripMenuItem_ChildItem_Click(object sender, EventArgs e)
        {
            var itemClicked = sender as ToolStripItem;
            Type motorType = itemClicked.Tag as Type;
            projectManager.createNewProject(motorType);

            //refresh params (need to be done here because nowhere it is called)            
            if (form_motorParams != null && form_motorParams.Visible)
                form_motorParams.refreshUI();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.saveProject();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "*.prm|*.prm";
            dialog.FilterIndex = 0;
            dialog.RestoreDirectory = true;
            DialogResult result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            projectManager.saveProject(dialog.FileName);

            refreshUI();//update project name in window title
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "*.prm|*.prm";
            dialog.FilterIndex = 0;
            dialog.RestoreDirectory = true;
            DialogResult result = dialog.ShowDialog();

            if (result != DialogResult.OK)
                return;

            String fn = dialog.FileName;

            bool b = ProjectManager.GetInstance().openProject(dialog.FileName);
            if (!b)
                MessageBox.Show("Error when open " + dialog.FileName);

            //refresh params (need to be done here because nowhere it is called)            
            if (form_motorParams != null && form_motorParams.Visible)
                form_motorParams.refreshUI();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region View menu

        private void logToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logwindow.Show();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (previewWindow == null || previewWindow.IsDisposed)
            {
                previewWindow = new PreviewWindow();
                previewWindow.Visible = true;
            }
        }

        #endregion

        #region Analysis menu

        private void buildFEMMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // create motor instance using params in listview
            // also validate  
            projectManager.buildFEMM();
        }

        private void measureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.runStaticAnalysis();
        }

        private void transientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.runAllTransientAnalysis();
        }

        private void mMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.runMMAnalysis();
        }

        private void dQToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.runDQCurrentAnalysis();
        }

        private void sweepParametersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sweepParamsWindow == null || sweepParamsWindow.IsDisposed)
                sweepParamsWindow = new SweepParams_Window();

            sweepParamsWindow.Show();
        }

        private void optimizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (optimizationWindow == null || optimizationWindow.IsDisposed)
                optimizationWindow = new OptimizationWindow();

            optimizationWindow.Show();
        }

        #endregion

        #region Window menu

        private void tileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void tileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void cascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void arrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        #endregion

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            var r = MessageBox.Show("Save project?", "Exit", MessageBoxButtons.YesNoCancel);

            if (r == DialogResult.Yes)
                projectManager.saveProject();
            else if (r == DialogResult.Cancel)
                e.Cancel = true;
        }

        private void test1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PMSM_EfficiencyMap_Analytical w = new PMSM_EfficiencyMap_Analytical();
            w.Show();
        }

        void mma_OnFinishedAnalysis(object sender, AbstractResults e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)delegate () { mma_OnFinishedAnalysis(sender, e); });
            }

            FEMM.CloseFemm();

            var mmr = e as PM_MMAnalysisResults;
            GraphWindow gw = new GraphWindow();
            IDictionary<string, object> dict = mmr.BuildResultsForDisplay();
            foreach (String name in dict.Keys)
            {
                var graphdata = dict[name] as ListPointD;
                if (graphdata != null)
                    gw.addData(name, graphdata.ToZedGraphPointPairList());
            }
            gw.Show();
        }

        void ta_OnFinishedAnalysis(object sender, AbstractResults e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)delegate () { ta_OnFinishedAnalysis(sender, e); });
            }

            FEMM.CloseFemm();

            var tr = e as Transient3PhaseMotorResults;
            GraphWindow gw = new GraphWindow();
            IDictionary<string, object> dict = tr.BuildResultsForDisplay();
            foreach (String name in dict.Keys)
            {
                var graphdata = dict[name] as ListPointD;
                if (graphdata != null)
                    gw.addData(name, graphdata.ToZedGraphPointPairList());
            }
            gw.Show();
        }

        private void makeSimulinkModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "*.mdl|*.mdl";
            dialog.FilterIndex = 0;
            dialog.RestoreDirectory = true;
            DialogResult result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            projectManager.makeSimulinkModel(dialog.FileName);
        }
    }
}
