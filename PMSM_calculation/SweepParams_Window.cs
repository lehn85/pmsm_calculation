/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using log4net;

namespace calc_from_geometryOfMotor
{
    public partial class SweepParams_Window : Form
    {
        private static readonly ILog log = LogManager.GetLogger("SweepWindow");

        public SweepParams_Window()
        {
            InitializeComponent();
        }

        private void SweepParams_Window_Load(object sender, EventArgs e)
        {
            initialize();
        }

        private void initialize()
        {
            ProjectManager pm = ProjectManager.GetInstance();
            sweeper = pm.pSweeper;//sweeper get from project manager
            comboBox_paramsName.Items.AddRange(sweeper.getParameterNames());
            
            showParamsInDGV(sweeper.ParamsTable);
            showResultsInDGV(sweeper.ResultsTable);

            checkBox_enableFem.Checked = sweeper.EnableFEMAnalysis;
        }

        ParamSweeper sweeper;

        private void bt_addParam_Click(object sender, EventArgs e)
        {
            if (comboBox_paramsName.Text == "")
                return;

            String[] ss = tb_value.Text.Split(',', ' ');
            List<double> list = new List<double>();
            foreach (String s in ss)
            {
                double d = double.NaN;
                if (double.TryParse(s, out d))
                    list.Add(d);
            }

            sweeper.addParamValues(comboBox_paramsName.Text, list.ToArray());

            showParamsInDGV(sweeper.ParamsTable);
        }

        private void bt_sweep_Click(object sender, EventArgs e)
        {
            sweeper.OnFinishSweep -= new EventHandler<ParamSweeperResults>(sweeper_OnFinishSweep);
            sweeper.OnFinishSweep += new EventHandler<ParamSweeperResults>(sweeper_OnFinishSweep);

            FEMM.CloseFemm();

            new Thread(new ThreadStart(sweeper.doSweep)).Start();
        }

        void sweeper_OnFinishSweep(object sender, ParamSweeperResults e)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)delegate() { sweeper_OnFinishSweep(sender, e); });
                return;
            }

            FEMM.CloseFemm();

            showResultsInDGV(e.ResultsTable);
        }

        private List<String> columnsToHide;

        private void showResultsInDGV(DataTable resultsTbl)
        {
            if (resultsTbl == null)
                return;

            if (columnsToHide == null)
                columnsToHide = new List<string>();

            // set data
            dgv_results.DataSource = null;
            dgv_results.DataSource = resultsTbl;

            // setup column width and header text
            foreach (DataGridViewColumn column in dgv_results.Columns)
            {
                int pp = column.HeaderText.LastIndexOf('\\');
                if (pp >= 0)
                    column.HeaderText = column.HeaderText.Substring(pp + 1);

                column.Width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, false);
            }

            // hightlight the current set of params row
            ProjectManager pm = ProjectManager.GetInstance();
            String md5current = pm.Motor.GetMD5String();
            foreach (DataGridViewRow row in dgv_results.Rows)
            {
                if (row.Cells[ParamSweeper.COL_MD5].Value.ToString() == md5current)
                    row.DefaultCellStyle.BackColor = Color.Aqua;
                if (row.Cells[ParamSweeper.COL_STATUS].Value.ToString() != "OK")
                    row.DefaultCellStyle.ForeColor = Color.Red;
            }
        }

        private void showParamsInDGV(DataTable paramsTbl)
        {
            dgv_params.DataSource = null;
            dgv_params.DataSource = paramsTbl;

            // sort the columns
            dgv_params.Columns[ParamSweeper.COL_ID].DisplayIndex = 0;
            dgv_params.Columns[ParamSweeper.COL_MD5].DisplayIndex = 1;
            dgv_params.Columns[ParamSweeper.COL_STATUS].DisplayIndex = 2;

            // setup columns width and header text
            foreach (DataGridViewColumn column in dgv_params.Columns)
            {
                int pp = column.HeaderText.LastIndexOf('\\');
                if (pp >= 0)
                    column.HeaderText = column.HeaderText.Substring(pp + 1);

                column.Width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, false);
            }

            // hightlight the current set of params row
            // hightlight set of params that are not valid
            ProjectManager pm = ProjectManager.GetInstance();
            String md5current = pm.Motor.GetMD5String();
            foreach (DataGridViewRow row in dgv_params.Rows)
            {
                if (row.Cells[ParamSweeper.COL_MD5].Value.ToString() == md5current)
                    row.DefaultCellStyle.BackColor = Color.Aqua;
                if (row.Cells[ParamSweeper.COL_STATUS].Value.ToString() != "OK")
                    row.DefaultCellStyle.ForeColor = Color.Red;
            }
        }

        private void checkBox_enableFem_CheckedChanged(object sender, EventArgs e)
        {
            sweeper.EnableFEMAnalysis = checkBox_enableFem.Checked;
        }

        private void comboBox_paramsName_SelectedIndexChanged(object sender, EventArgs e)
        {
            String item = (String)comboBox_paramsName.SelectedItem;
            ProjectManager pm = ProjectManager.GetInstance();
            foreach (Parameter p in pm.MotorParams)
                if (p.fullname == item)
                {
                    tb_value.Text = p.value.ToString();
                    break;
                }
        }

        private void dgv_params_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                dgv_params.ClearSelection();
                dgv_params.Rows[e.RowIndex].Selected = true;

                var cc = dgv_params.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                int x = cc.X + e.X;
                int y = cc.Y + e.Y;
                contextMenu_params.Show((Control)sender, x, y);
            }
        }

        private void setThisAsCurrentSetOfParamsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show("Set this as current motor?", "Ask", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                ProjectManager pm = ProjectManager.GetInstance();
                var row = dgv_params.SelectedRows[0];
                String md5 = row.Cells[ParamSweeper.COL_MD5].Value.ToString();
                sweeper.applyParametersCollectionVariant(md5, pm.MotorParams);
                pm.InvalidateParams();
            }
        }
    }
}
