using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using log4net;
using System.IO;
using calc_from_geometryOfMotor.motor;
using fastJSON;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;

namespace calc_from_geometryOfMotor
{
    public class ParamSweeper
    {
        private static readonly ILog log = LogManager.GetLogger("SweepWindow");

        private readonly string PARAMS_TABLE = "ParamsTable";
        private readonly string RESULTS_TABLE = "ResultsTable";

        public ParamSweeper()
        {
            EnableFEMAnalysis = false;

            loadDataFromDisk();

            if (ParamsTable == null)
            {
                ParamsTable = new DataTable();
                ParamsTable.TableName = PARAMS_TABLE;
            }

            if (!ParamsTable.Columns.Contains(COL_ID))
                ParamsTable.Columns.Add(COL_ID, typeof(int));
            if (!ParamsTable.Columns.Contains(COL_MD5))
                ParamsTable.Columns.Add(COL_MD5, typeof(string));
            if (!ParamsTable.Columns.Contains(COL_STATUS))
                ParamsTable.Columns.Add(COL_STATUS, typeof(string));

            //DataColumn[] keycolumns = { ParamsTable.Columns[COL_MD5] };
            //ParamsTable.PrimaryKey = keycolumns;
        }

        [JsonIgnore]
        private AbstractMotor CurrentMotor { get { return ProjectManager.GetInstance().Motor; } }

        public bool EnableFEMAnalysis { get; set; }

        public String OutputFolder
        {
            get
            {
                return ProjectManager.GetInstance().CurrentOutputFolder + "\\sweep_" + CurrentMotor.GetType().Name;
            }
        }

        public String sweepDataFile
        {
            get
            {
                return OutputFolder + "\\sweepdata.txt";
            }
        }

        public static readonly String COL_ID = "ID";
        public static readonly String COL_MD5 = "MD5";
        public static readonly String COL_STATUS = "Status";

        /// <summary>
        /// Table that contains all variants of params, each column is a param, each row is a variant
        /// </summary>        
        public DataTable ParamsTable { get; private set; }

        /// <summary>
        /// result tables
        /// </summary>
        public DataTable ResultsTable { get; private set; }

        public void addParamValues(String name, double[] listD)
        {
            DataTable oldTbl = ParamsTable;
            List<double> list = new List<double>(listD);
            //make a shadow copy
            ParamsTable = ParamsTable.Copy();
            ParamsTable.Clear();
            // if this is new column
            if (!ParamsTable.Columns.Contains(name))
            {
                ParamsTable.Columns.Add(name, typeof(double));
            }

            // get list of distinc rows contain only other columns
            Dictionary<String, DataRow> RowsOtherColumns = new Dictionary<String, DataRow>();
            foreach (DataRow row in oldTbl.Rows)
            {
                String hc = "";
                for (int i = 0; i < oldTbl.Columns.Count; i++)
                    if (oldTbl.Columns[i].ColumnName != name &&
                        oldTbl.Columns[i].ColumnName != COL_ID &&
                        oldTbl.Columns[i].ColumnName != COL_MD5)
                        hc = hc + row[i] + "_";

                if (!RowsOtherColumns.ContainsKey(hc))
                    RowsOtherColumns.Add(hc, row);

                // if columns name already existed, try fill the already existed number in the column to list
                if (oldTbl.Columns.Contains(name))
                {
                    double d;
                    if (double.TryParse(row[name].ToString(), out d))
                        if (!list.Contains(d))
                            list.Add(d);
                }
            }

            // insert the listD
            foreach (double d in list)
            {
                // table empty to begin with
                if (oldTbl.Rows.Count == 0)
                {
                    DataRow newRow = ParamsTable.NewRow();
                    newRow[name] = d;
                    ParamsTable.Rows.Add(newRow);
                }
                else
                {
                    foreach (DataRow oldrow in RowsOtherColumns.Values)
                    {
                        DataRow newRow = ParamsTable.NewRow();
                        // copy old to new
                        for (int i = 0; i < oldTbl.Columns.Count; i++)
                            newRow[i] = oldrow[i];
                        // new columns
                        newRow[name] = d;
                        ParamsTable.Rows.Add(newRow);
                    }
                }
            }

            // update ID and MD5 in params table
            updateID_MD5_inParamsTable();
        }

        private void updateID_MD5_inParamsTable()
        {
            // copy params from original            
            ParametersCollection swParams = MotorToParams(CurrentMotor);

            // sweep variants, setting ID, MD5
            foreach (DataRow paramRow in ParamsTable.Rows)
            {
                int variantNum = ParamsTable.Rows.IndexOf(paramRow);
                paramRow[COL_ID] = variantNum;

                // update params from this row (variant)
                foreach (DataColumn dc in ParamsTable.Columns)
                {
                    String colname = dc.ColumnName;
                    foreach (Parameter p in swParams)
                        if (p.fullname == colname)
                        {
                            p.text = paramRow[colname].ToString();
                            p.EvaluateValue();
                            break;
                        }
                }

                // create motor from parameters
                AbstractMotor motor = ParamsToMotor(swParams);

                paramRow[COL_MD5] = motor.GetMD5String();
            }
        }

        /// <summary>
        /// Apply a variant (with md5) to a parameters collection
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="pc"></param>
        public void applyParametersCollectionVariant(String md5, ParametersCollection pc)
        {
            // find a row with id            
            DataRow paramRow = null;
            foreach (DataRow row in ParamsTable.Rows)
                if (row[COL_MD5].ToString() == md5)
                {
                    paramRow = row;
                    break;
                }

            if (paramRow == null)
                return;

            // update params from this row (variant)
            foreach (DataColumn dc in ParamsTable.Columns)
            {
                String colname = dc.ColumnName;
                foreach (Parameter p in pc)
                    if (p.fullname == colname)
                    {
                        p.text = paramRow[colname].ToString();
                        p.EvaluateValue();
                        break;
                    }
            }
        }

        public void doSweep()
        {
            // update ID and MD5 in params table before do sweep
            updateID_MD5_inParamsTable();

            // copy params from original            
            ParametersCollection swParams = MotorToParams(CurrentMotor);

            // new result table
            ResultsTable = new DataTable();
            ResultsTable.TableName = RESULTS_TABLE;

            ResultsTable.Columns.Add(COL_ID, typeof(int));
            ResultsTable.Columns.Add(COL_MD5, typeof(string));
            ResultsTable.Columns.Add(COL_STATUS, typeof(string));
            bool newlyTable = true;

            // apply sweeping param from table
            // each variant
            foreach (DataRow paramRow in ParamsTable.Rows)
            {
                int variantNum = ParamsTable.Rows.IndexOf(paramRow);
                // update params from this row (variant)
                foreach (DataColumn dc in ParamsTable.Columns)
                {
                    String colname = dc.ColumnName;
                    foreach (Parameter p in swParams)
                        if (p.fullname == colname)
                        {
                            p.text = paramRow[colname].ToString();
                            p.EvaluateValue();
                            break;
                        }
                }

                // create new motor from parameters
                AbstractMotor motor = ParamsToMotor(swParams);

                // motor calculation
                motor.CalcPointsCoordinates();

                // if params invalid, just move on
                if (!motor.IsParamsValid())
                {
                    log.Error(String.Format("Variant {0} invalid:", variantNum));
                    paramRow[COL_STATUS] = "Error: ";
                    foreach (ParamValidationInfo pvi in motor.ListParamsValidation)
                    {
                        if (pvi.msgType == ParamValidationInfo.MessageType.Error)
                            log.Error("\t+ " + pvi.message);

                        //update error in param row
                        paramRow[COL_STATUS] += pvi.message + "; ";
                    }

                    //resultRow also
                    DataRow rr = ResultsTable.NewRow();
                    rr[COL_ID] = paramRow[COL_ID];
                    rr[COL_MD5] = paramRow[COL_MD5];
                    rr[COL_STATUS] = paramRow[COL_STATUS];
                    ResultsTable.Rows.Add(rr);

                    continue;
                }
                else paramRow[COL_STATUS] = "OK";

                //// organize results
                ParametersCollection pc = new ParametersCollection();

                // calc gmc
                AbstractAnalyticalAnalyser aa = motor.GetAnalyticalAnalyser();
                aa.RunAnalysis();
                pc.AddRange(ParametersCollection.FromObject("analytic", aa.Results));

                // femm 
                if (EnableFEMAnalysis)
                {
                    String outdir = OutputFolder;
                    String femFile = outdir + "\\" + motor.GetMD5String() + ".FEM";
                    Directory.CreateDirectory(outdir);
                    if (!motor.isFEMModelReady(femFile))
                    {
                        motor.BuildFEMModel(femFile);
                    }

                    AbstractStaticAnalyser staticAnalyser = motor.GetStaticAnalyser();
                    staticAnalyser.Path_OriginalFEMFile = femFile;
                    staticAnalyser.OutputPath = outdir;
                    staticAnalyser.DoAnalysisOnOriginalFEMFile = true;
                    staticAnalyser.RunAnalysis();
                    pc.AddRange(ParametersCollection.FromObject("fem", staticAnalyser.Results));
                }
                Dictionary<String, int> countsParam = new Dictionary<string, int>();

                // find params match by name between analytic and fem
                foreach (Parameter p in pc)
                {
                    if (countsParam.ContainsKey(p.name))
                        countsParam[p.name]++;
                    else countsParam[p.name] = 1;
                }
                // add comparison columns
                foreach (String name in countsParam.Keys)
                    if (countsParam[name] == 2)
                    {
                        Parameter p = new Parameter();
                        p.group = "xCompare";
                        p.name = name;
                        double d1 = (double)pc.FindParameter("analytic", name).value;
                        double d2 = (double)pc.FindParameter("fem", name).value;
                        p.value = String.Format("{0:F3}%", (d1 - d2) / d2 * 100.0);
                        p.valueType = typeof(String);
                        pc.Add(p);
                    }

                // sort columns
                pc = new ParametersCollection(pc.OrderBy(param => param.name).ThenBy(param => param.group).ToList());

                // 1 times create 
                if (newlyTable)
                {
                    foreach (Parameter p in pc)
                        if (!p.valueType.IsArray)//don't care about array (airgap flux-density distribution) for now
                            ResultsTable.Columns.Add(p.fullname, p.valueType);

                    newlyTable = false;
                }

                // assign this variant results to row of results table
                DataRow resultRow = ResultsTable.NewRow();
                foreach (Parameter p in pc)
                    if (resultRow.Table.Columns.Contains(p.fullname))
                        resultRow[p.fullname] = p.value;
                resultRow[COL_ID] = paramRow[COL_ID];
                resultRow[COL_MD5] = paramRow[COL_MD5];
                resultRow[COL_STATUS] = paramRow[COL_STATUS];
                ResultsTable.Rows.Add(resultRow);
            }// for each variant

            // raise event
            ParamSweeperResults r = new ParamSweeperResults();
            r.ResultsTable = ResultsTable;
            OnFinishSweep(this, r);

            saveDataToDisk();
        }

        private ParametersCollection MotorToParams(AbstractMotor motor)
        {
            // Parameters from motor            
            Dictionary<String, object> objs = new Dictionary<string, object>();
            objs.Add("Motor\\General", motor.GeneralParams);
            objs.Add("Motor\\Rotor", motor.Rotor);
            objs.Add("Motor\\Stator", motor.Stator);
            //objs.Add("Motor\\Materials", motor.MaterialParams);
            objs.Add("Motor\\Airgap", motor.Airgap);

            ParametersCollection motorParams = new ParametersCollection();
            foreach (String objname in objs.Keys)
            {
                motorParams.AddRange(ParametersCollection.FromObject(objname, objs[objname]));
            }

            return motorParams;
        }

        private AbstractMotor ParamsToMotor(ParametersCollection pc)
        {
            ProjectManager pm = ProjectManager.GetInstance();
            AbstractMotor motor = pm.GetSampleMotor();//new default
            Dictionary<String, object> objs = new Dictionary<string, object>();
            objs.Add("Motor\\General", motor.GeneralParams);
            objs.Add("Motor\\Rotor", motor.Rotor);
            objs.Add("Motor\\Stator", motor.Stator);
            //objs.Add("Motor\\Materials", motor.MaterialParams);
            objs.Add("Motor\\Airgap", motor.Airgap);
            foreach (String objname in objs.Keys)
            {
                pc.putValuesToObject(objname, objs[objname]);
            }

            return motor;
        }

        public String[] getParameterNames()
        {
            ParametersCollection pc = MotorToParams(CurrentMotor);
            return pc.Select(p => p.fullname).ToArray();
        }

        public event EventHandler<ParamSweeperResults> OnFinishSweep = new EventHandler<ParamSweeperResults>(delegate (object s, ParamSweeperResults r) { });

        #region save-load        

        public void saveDataToDisk()
        {
            Directory.CreateDirectory(OutputFolder);
            using (StreamWriter sw = new StreamWriter(sweepDataFile))
            {
                JsonObject jo = new JsonObject();
                jo["EnableFEMAnalysis"] = EnableFEMAnalysis;
                writeDataTableToJsonObject(jo, ParamsTable, PARAMS_TABLE);
                writeDataTableToJsonObject(jo, ResultsTable, RESULTS_TABLE);

                sw.Write(JSON.Beautify(JSON.ToJSON(jo)));
            }
        }

        public void loadDataFromDisk()
        {
            if (!File.Exists(sweepDataFile))
                return;

            using (StreamReader sr = new StreamReader(sweepDataFile))
            {
                string str = sr.ReadToEnd();
                JsonObject jo = JSON.Parse(str) as JsonObject;

                // read bool
                EnableFEMAnalysis = (bool)jo["EnableFEMAnalysis"];

                // params table
                ParamsTable = readDataTableFromJsonObject(jo, PARAMS_TABLE);

                // results table                
                ResultsTable = readDataTableFromJsonObject(jo, RESULTS_TABLE);
            }

        }

        /// <summary>
        /// write a datatable, including datatype of each column to a jsonobject
        /// </summary>
        /// <param name="jo"></param>
        /// <param name="dt"></param>
        private void writeDataTableToJsonObject(JsonObject jo, DataTable dt, String tblName)
        {
            String[] colnames = Enumerable.Range(0, dt.Columns.Count).Select(i => dt.Columns[i].ColumnName).ToArray();
            String[] col_datatypes = Enumerable.Range(0, dt.Columns.Count).Select(i => dt.Columns[i].DataType.FullName).ToArray();
            JsonObject listDatatype = new JsonObject();
            for (int i = 0; i < colnames.Length; i++)
                listDatatype[colnames[i]] = col_datatypes[i];
            jo[tblName + "_datatype"] = listDatatype;
            jo[tblName] = dt;
        }

        private DataTable readDataTableFromJsonObject(JsonObject jo, String tblName)
        {
            try
            {
                DataTable dt = new DataTable();
                dt.TableName = tblName;

                JsonObject listDatatype = (JsonObject)jo[tblName + "_datatype"];
                JsonObject data = jo[tblName] as JsonObject;
                List<object> tblrows = data[tblName] as List<object>;//list [tblname] inside of object [tblname]

                int col_count = listDatatype.Keys.Count;
                for (int i = 0; i < col_count; i++)
                {
                    Type type = Type.GetType(listDatatype.Values.ElementAt(i).ToString());
                    dt.Columns.Add(listDatatype.Keys.ElementAt(i), type);
                }

                // for each row
                foreach (object rowobj in tblrows)
                {
                    // list of object in a row
                    List<object> inrow = rowobj as List<object>;

                    // put values
                    DataRow row = dt.NewRow();
                    for (int i = 0; i < col_count; i++) //each column
                    {
                        object value = inrow[i];
                        if (dt.Columns[i].DataType.IsArray)
                            row[i] = JSON.ToObject(value.ToString(), dt.Columns[i].DataType);
                        else if (value == null)
                            row[i] = DBNull.Value;
                        else row[i] = value;
                    }
                    dt.Rows.Add(row);
                }

                return dt;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        #endregion
    }

    public class ParamSweeperResults : EventArgs
    {
        public DataTable ResultsTable { get; internal set; }
    }
}
