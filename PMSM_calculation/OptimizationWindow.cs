/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using btl.generic;
using calc_from_geometryOfMotor.motor;
using fastJSON;
using calc_from_geometryOfMotor.motor.PMMotor;
using System.Diagnostics;
using NLua;
using System.Threading;
using System.IO;
using calc_from_geometryOfMotor.helper;
using System.Reflection;
using ZedGraph;

namespace calc_from_geometryOfMotor
{
    public partial class OptimizationWindow : Form
    {
        private Thread thread;

        private GA ga;
        private Dictionary<double, double[]> dict_fitness = new Dictionary<double, double[]>();

        [JsonIgnore]
        private AbstractMotor CurrentMotor { get { return ProjectManager.GetInstance().Motor; } }

        public String OutputFolder
        {
            get
            {
                return ProjectManager.GetInstance().CurrentOutputFolder + "\\Optimization_" + CurrentMotor.GetType().Name;
            }
        }

        public string OutputFolderFEM
        {
            get
            {
                return OutputFolder + "\\FEMM";
            }
        }

        public String optimizationConfigFile
        {
            get
            {
                return OutputFolder + "\\config.txt";
            }
        }

        public string CurrentConfigName
        {
            get
            {
                return comboBox_configs.SelectedItem.ToString();
            }
        }

        public OptimizationWindow()
        {
            InitializeComponent();

            loadOptConfigsFromDisk();

            initLua();
        }

        #region Buttons

        private void bt_run_optimization_click(object sender, EventArgs e)
        {
            saveDataFromUIToConfig(currentConfig);
            startOptimization();
        }

        private void bt_stop_Click(object sender, EventArgs e)
        {
            stopOptimization();
        }

        private void bt_open_result_folder_Click(object sender, EventArgs e)
        {
            Process.Start(OutputFolder);
        }

        private void bt_new_config_Click(object sender, EventArgs e)
        {
            string newname = Prompt.ShowDialog("Enter name:", "New config");
            OptimizationConfig config = new OptimizationConfig();
            opt_configs.Add(newname, config);
            comboBox_configs.Items.Add(newname);
            comboBox_configs.SelectedIndex = comboBox_configs.Items.Count - 1;
            selectAConfig(newname);
        }

        private void bt_save_config_Click(object sender, EventArgs e)
        {
            saveDataFromUIToConfig(currentConfig);

            saveOptConfigsToDisk();
        }

        private void bt_delete_config_Click(object sender, EventArgs e)
        {
            if (opt_configs.Count == 1)
            {
                MessageBox.Show("This is the last one, cannot delete");
                return;
            }

            int i = comboBox_configs.SelectedIndex;
            string name = comboBox_configs.SelectedItem as string;
            comboBox_configs.Items.RemoveAt(i);
            opt_configs.Remove(name);

            comboBox_configs.SelectedIndex = 0;
            selectAConfig(0);
        }

        private void bt_copy_config_Click(object sender, EventArgs e)
        {
            string newname = Prompt.ShowDialog("Enter name:", "New config");
            var config = JSON.DeepCopy(currentConfig);
            saveDataFromUIToConfig(config);//save current data from UI to newly created config
            opt_configs.Add(newname, config);
            comboBox_configs.Items.Add(newname);
            comboBox_configs.SelectedIndex = comboBox_configs.Items.Count - 1;
            selectAConfig(newname);
        }

        private void bt_rerun_finish_script_Click(object sender, EventArgs e)
        {
            saveDataFromUIToConfig(currentConfig);
            Ga_AllFinish(ga);
        }

        #endregion

        //#region Hardcode Optimization as first test        

        //private void init()
        //{
        //    //  Crossover		= 85%
        //    //  Mutation		=  5%
        //    //  Population size = 50
        //    //  Generations		= 500
        //    //  Genomes                        
        //    int nGen = 5;  //for now: Thick,Width,Rib,HRib,O2
        //    ga = new GA(0.85, 0.05, 100, 500, nGen);

        //    ga.FitnessFunction = new GAFunction(fitnessfunction);
        //    ga.FitnessFile = @"D:\fitness.csv";
        //    ga.Elitism = true;
        //}

        //private void run()
        //{
        //    Stopwatch sw = new Stopwatch();
        //    sw.Start();

        //    ga.Go();

        //    sw.Stop();

        //    //results:
        //    double[] values;
        //    double fitness;
        //    ga.GetBest(out values, out fitness);

        //    var motor = constructMotorFromGens(values);

        //    Console.WriteLine(JSON.ToJSON(motor));

        //    MessageBox.Show("Took " + (sw.ElapsedMilliseconds / 1000) + " secs. Dict size = " + dict_fitness.Count);
        //}

        //double[] failed = null;

        //private Dictionary<double, double[]> dict_fitness = new Dictionary<double, double[]>();

        ///// <summary>
        ///// Input all gens, each one is double [0..1].
        ///// We need to convert them to actual parameters:
        ///// for double: d*(max-min)+min
        ///// for natural 0,1,2,3: round(d*Nmax)
        ///// </summary>
        ///// <param name="gens"></param>
        ///// <returns></returns>
        //private double[] fitnessfunction(double[] gens)
        //{
        //    double d = 0;
        //    for (int i = 0; i < gens.Length; i++)
        //        d += gens[i] * Math.Pow(10, i);

        //    if (dict_fitness.ContainsKey(d))
        //        return dict_fitness[d];

        //    var motor = constructMotorFromGens(gens);

        //    // motor calculation
        //    motor.CalcPointsCoordinates();

        //    // if params invalid, just move on
        //    if (!motor.IsParamsValid())
        //    {
        //        foreach (ParamValidationInfo pvi in motor.ListParamsValidation)
        //        {
        //            if (pvi.msgType == ParamValidationInfo.MessageType.Error)
        //                Console.WriteLine("Error: \t+ " + pvi.message);
        //        }

        //        return dict_fitness[d] = failed;
        //    }

        //    // calc gmc
        //    AbstractAnalyticalAnalyser aa = motor.GetAnalyticalAnalyser();
        //    aa.RunAnalysis();

        //    var results = aa.Results as PMAnalyticalResults;

        //    //real fitness function


        //    // boundary conditions
        //    if (results.gammaM < 130 || results.gammaM > 135
        //        || results.psiM <= 0 || results.Ld <= 0)
        //        return dict_fitness[d] = failed;

        //    if (results.pc < 5)
        //        return dict_fitness[d] = failed;

        //    if (results.psiM / results.Ld > 50)
        //        return dict_fitness[d] = failed;

        //    double iii = results.psiM / results.Ld * 0.9;
        //    // fitness function
        //    double f1 = 10000 - iii * iii * results.R;
        //    //double f2 = (20 - Math.Abs(results.psiM / results.Ld - 40));//as near 40 as possible?
        //    double f = f1;

        //    dict_fitness[d] = f;

        //    return f;
        //}

        //private double convertFromGen(double d, double min, double max)
        //{
        //    return d * (max - min) + min;
        //}

        //private VPMMotor constructMotorFromGens(double[] gens)
        //{
        //    var pm = ProjectManager.GetInstance();
        //    var pc = pm.MotorToParams(CurrentMotor);
        //    var motor = pm.ParamsToMotor(pc) as VPMMotor;

        //    // test1, assume that VPMMotor                        
        //    motor.Rotor.O2 = convertFromGen(gens[0], 20, 28);
        //    motor.Rotor.ThickMag = convertFromGen(gens[1], 3, 8);
        //    motor.Rotor.B1 = motor.Rotor.ThickMag - 0.5;
        //    motor.Rotor.WidthMag = convertFromGen(gens[2], 20, 28);
        //    motor.Rotor.Rib = convertFromGen(gens[3], 2, 14);
        //    motor.Rotor.HRib = convertFromGen(gens[4], 1, 5);

        //    return motor;
        //}

        //#endregion

        #region OptimizationConfig

        public class OptimizationConfig
        {
            public int genome_size = 1;//genomes count
            public int fitness_size = 1; //fitness size
            public int population_size = 50;//population
            public int generation_size = 100;//generation 
            public double cross_rate = 0.85;
            public double mutation_rate = 0.05;

            public string luascript_constructMotor = "";//construct motor from gens
            public string luascript_fitness_function = "";//return fitness
            public string luascript_init = "";//script when init
            public string luascript_finish = "";//script when finish
        }

        private OptimizationConfig defaultConfig = new OptimizationConfig()
        {
            population_size = 50,
            generation_size = 100,
            genome_size = 5,
            luascript_init =
@"function convertFromGen(d, min, max)        
    return d * (max - min) + min
end        
",
            luascript_constructMotor =
@"motor.Rotor.O2 = convertFromGen(gens[0], 20, 28)
motor.Rotor.ThickMag = convertFromGen(gens[1], 3, 8)
motor.Rotor.B1 = motor.Rotor.ThickMag - 0.5
motor.Rotor.WidthMag = convertFromGen(gens[2], 20, 28)
motor.Rotor.Rib = convertFromGen(gens[3], 2, 14)
motor.Rotor.HRib = convertFromGen(gens[4], 1, 5)
",
            luascript_fitness_function =
@"local f = 0
f = results.psiM*10
if results.gammaM < 130 or results.gammaM > 135 or results.psiM <= 0 or results.Ld <= 0 then
      f = f*0.01
end
if results.pc < 5 then f=f*0.02 end
if results.psiM / results.Ld > 50 then f=f*0.03 end
return f
"
        };

        #endregion

        #region Config management        

        private Dictionary<string, OptimizationConfig> opt_configs;
        private OptimizationConfig currentConfig;

        private void initDefaultConfigs()
        {
            opt_configs = new Dictionary<string, OptimizationConfig>();
            currentConfig = JSON.DeepCopy(defaultConfig);
            opt_configs.Add("Sample", currentConfig);
        }

        private void loadOptConfigsFromDisk()
        {
            try
            {
                using (StreamReader sr = new StreamReader(optimizationConfigFile))
                {
                    string str = sr.ReadToEnd();
                    var o = JSON.Parse(str) as Dictionary<string, object>;
                    opt_configs = new Dictionary<string, OptimizationConfig>();
                    foreach (string name in o.Keys)
                    {
                        opt_configs[name] = JSON.ToObject<OptimizationConfig>(JSON.ToJSON(o[name]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't load optimization config", ex.Message);
                initDefaultConfigs();
            }

            //add to combo box
            comboBox_configs.Items.Clear();
            foreach (string name in opt_configs.Keys)
            {
                comboBox_configs.Items.Add(name);
            }
            comboBox_configs.SelectedIndex = 0;
            selectAConfig(0);//select first one as current config
        }

        private void saveOptConfigsToDisk()
        {
            Directory.CreateDirectory(OutputFolder);
            using (StreamWriter sw = new StreamWriter(optimizationConfigFile))
            {
                string str = JSON.Beautify(JSON.ToJSON(opt_configs));
                sw.Write(str);
            }
        }

        private void selectAConfig(int index)
        {
            if (index >= 0 && index < opt_configs.Count)
            {
                selectAConfig(opt_configs.Keys.ElementAt(index));
            }
        }

        private void selectAConfig(string name)
        {
            currentConfig = opt_configs[name];

            udn_cross_rate.Value = Convert.ToDecimal(currentConfig.cross_rate);
            udn_mutation_rate.Value = Convert.ToDecimal(currentConfig.mutation_rate);

            udn_population_size.Value = currentConfig.population_size;
            udn_generation_size.Value = currentConfig.generation_size;
            udn_genome_size.Value = currentConfig.genome_size;
            udn_fitness_size.Value = currentConfig.fitness_size;

            rtb_init_script.Text = currentConfig.luascript_init;
            rtb_construct_motor.Text = currentConfig.luascript_constructMotor;
            rtb_fitness_function.Text = currentConfig.luascript_fitness_function;
            rtb_finish_script.Text = currentConfig.luascript_finish;
        }

        private void saveDataFromUIToConfig(OptimizationConfig config)
        {
            config.cross_rate = Convert.ToDouble(udn_cross_rate.Value);
            config.mutation_rate = Convert.ToDouble(udn_mutation_rate.Value);

            config.population_size = Convert.ToInt32(udn_population_size.Value);
            config.generation_size = Convert.ToInt32(udn_generation_size.Value);
            config.genome_size = Convert.ToInt32(udn_genome_size.Value);
            config.fitness_size = Convert.ToInt32(udn_fitness_size.Value);

            config.luascript_init = rtb_init_script.Text;
            config.luascript_constructMotor = rtb_construct_motor.Text;
            config.luascript_fitness_function = rtb_fitness_function.Text;
            config.luascript_finish = rtb_finish_script.Text;
        }

        #endregion

        #region GA with Lua                

        #region Lua only

        private Lua lua;
        private object lock_dostring = new object();

        private void initLua()
        {
            lua = new Lua();
        }

        private void closeLua()
        {
            if (lua != null)
            {
                lua.Close();
                lua = null;
            }
        }

        /// <summary>
        /// Init evolution simulation params: genomes count, population count, generation count
        /// </summary>
        /// <param name="initString"></param>
        private void lua_initGA(string lua_script)
        {
            lua["genome_size"] = currentConfig.genome_size;
            lua["fitness_size"] = currentConfig.fitness_size;
            lua["population_size"] = currentConfig.population_size;
            lua["generation_size"] = currentConfig.generation_size;

            // init string
            lua.DoString(lua_script);
        }

        /// <summary>
        /// Execute lua_script that should modify a motor using given gens
        /// </summary>
        /// <param name="gens">Genomes</param>
        /// <param name="lua_script">lua_script</param>
        /// <returns></returns>
        private void lua_constructMotorfromGens(double[] gens, string lua_script, out AbstractMotor motor, out AbstractMMAnalyser mma)
        {
            // deep copy a motor from project manager
            var pm = ProjectManager.GetInstance();
            var pc = originParams;
            motor = pm.ParamsToMotor(pc);
            mma = motor.GetMMAnalyser();

            lock (lock_dostring)
            {
                lua["gens"] = gens;
                lua["motor"] = motor;
                lua["mma"] = mma;

                lua.DoString(lua_script);
            }
        }

        private double[] lua_fitnessFunction(object analysis_results, object fem_results, string lua_script)
        {
            object[] o = null;
            lock (lock_dostring)
            {
                lua["results"] = analysis_results;
                lua["femresults"] = fem_results;

                o = lua.DoString(lua_script);
            }

            if (o != null && o.Length > 0)
            {
                if (currentConfig.fitness_size == 1 && o[0] is double)
                    return new double[1] { (double)o[0] };

                if (o[0] is LuaTable)
                {
                    var tbl = o[0] as LuaTable;
                    var values = tbl.Values.Cast<double>().ToArray();
                    return values;
                }
            }

            throw new ArgumentException("Luascript must return double[] as fitness");
        }

        #endregion

        #region Lua+GA        

        private Stopwatch sw;

        private ParametersCollection originParams;

        private OptimizationResultsWindow optResultsWindow = null;

        private bool optimizationIsRunning = false;

        private List<FEMM> FEMMToUse;

        private int individualCount = 0;

        /// <summary>
        /// Init GA, and start new thread to run optimization
        /// </summary>
        private void startOptimization()
        {
            if (optimizationIsRunning)
                return;

            // reset results window
            if (optResultsWindow != null && !optResultsWindow.IsDisposed)
                optResultsWindow.clearHistory();

            optimizationIsRunning = true;

            originParams = ProjectManager.GetInstance().MotorToParams(CurrentMotor);

            // restart Lua (like clean-up)
            closeLua();
            initLua();

            // init datalog
            init_datalog();

            try
            {
                // init script
                lua_initGA(currentConfig.luascript_init);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init script failed: " + ex.Message);
                return;
            }

            ga = new GA(currentConfig.cross_rate, currentConfig.mutation_rate, currentConfig.population_size, currentConfig.generation_size, currentConfig.genome_size, currentConfig.fitness_size);

            ga.FitnessFunction = new GAFunction(fitnessFunction);
            ga.FitnessFile = OutputFolder + "\\fitness_" + CurrentConfigName + ".csv";
            ga.Elitism = true;
            ga.StepFinish += Ga_StepFinish;

            thread = new Thread(actualRunOptimization);
            thread.IsBackground = true;
            thread.Start();
        }

        private void stopOptimization()
        {
            if (ga != null)
                ga.Cancelled = true;
        }

        private void applyOptimalResult(AbstractMotor optimalMotor)
        {
            ProjectManager pm = ProjectManager.GetInstance();

            var pc = pm.MotorToParams(optimalMotor);

            foreach (var p_dest in pm.MotorParams)
            {
                foreach (var p_src in pc)
                    if (p_src.fullname == p_dest.fullname && p_src.valueType == p_dest.valueType)
                    {
                        p_dest.text = p_src.text;
                        p_dest.value = p_src.value;
                    }
            }

            // update motor
            pm.InvalidateParams();
        }

        /// <summary>
        /// Actual run, all hard works here
        /// </summary>
        private void actualRunOptimization()
        {
            sw = new Stopwatch();
            sw.Start();

            individualCount = 0;

            // new FEMM windows
            if (lua["useFEM"] != null)
            {
                FEMMToUse = new List<FEMM>();
                // 1 windows for now
                for (int i = 0; i < 1; i++)
                    FEMMToUse.Add(new FEMM());
            }

            try
            {
                ga.Go();

                //results:            
                Ga_AllFinish(ga);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Genetic algorithm simulation failed: " + ex.Message);
                ga.Cancelled = true;
            }

            sw.Stop();

            // close FEMM
            if (FEMMToUse != null)
            {
                FEMMToUse.Clear();
                FEMMToUse = null;
                FEMM.CloseFemm();
            }

            optimizationIsRunning = false;
        }

        /// <summary>
        /// Calculate fitness from gens, return value is nullable
        /// </summary>
        /// <param name="gens"></param>
        /// <returns></returns>
        private double[] fitnessFunction(double[] gens)
        {
            double d = 0;
            for (int i = 0; i < gens.Length; i++)
                d += gens[i] * Math.Pow(10, i);

            if (dict_fitness.ContainsKey(d))
                return dict_fitness[d];

            // construct motor from gens, using Lua script
            AbstractMotor motor = null;
            AbstractMMAnalyser mma = null;
            lua_constructMotorfromGens(gens, currentConfig.luascript_constructMotor, out motor, out mma);

            // motor calculate points coordinates
            motor.CalcPointsCoordinates();

            // if params invalid, return fitness as null
            if (!motor.IsParamsValid())
            {
                foreach (ParamValidationInfo pvi in motor.ListParamsValidation)
                {
                    if (pvi.msgType == ParamValidationInfo.MessageType.Error)
                        Console.WriteLine("Error: \t+ " + pvi.message);
                }

                return dict_fitness[d] = null;
            }

            // analytical analysis
            var a = motor.GetAnalyticalAnalyser();
            a.RunAnalysis();
            if (!a.isDataValid())
            {
                return dict_fitness[d] = null;
            }

            // finite elements analysis if available
            object fem_results = null;
            if (lua["useFEM"] != null && mma != null)
            {
                // static analysis
                String outdir = OutputFolderFEM + "\\" + motor.GetMD5String();
                Directory.CreateDirectory(outdir);
                String femFile = outdir + "\\static.FEM";
                if (!motor.isFEMModelReady(femFile))
                {
                    motor.BuildFEMModel(femFile, FEMMToUse[0]);
                }

                AbstractStaticAnalyser staticAnalyser = motor.GetStaticAnalyser();
                staticAnalyser.Path_OriginalFEMFile = femFile;
                staticAnalyser.OutputPath = outdir;
                staticAnalyser.DoAnalysisOnOriginalFEMFile = true;
                staticAnalyser.FEMMToUse = FEMMToUse;
                staticAnalyser.RunAnalysis();

                // for some reason, analysis was not succesful?
                if (!File.Exists(staticAnalyser.WorkingANSFile))
                    return dict_fitness[d] = null;

                // mma analysis
                mma.Path_OriginalFEMFile = femFile;
                mma.CustomOutputDir = outdir;
                mma.FEMMToUse = FEMMToUse;
                mma.RunAnalysis();
                fem_results = mma.Results;

                // to take pictures of progress 
                //FEMMToUse[0].open(femFile);

                //Invoke((Action)delegate ()
                //{
                //    individualCount++;
                //    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                //    Graphics gr = Graphics.FromImage(bmp);
                //    gr.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                //    bmp.Save("D:\\captures\\" + string.Format("{0:D8}", individualCount) + ".png", System.Drawing.Imaging.ImageFormat.Png);
                //});

                //FEMMToUse[0].mi_close();
            }

            var f = lua_fitnessFunction(a.Results, fem_results, currentConfig.luascript_fitness_function);
            dict_fitness[d] = f;

            return f;
        }

        #region Datalog

        private Dictionary<string, List<double>> datalog = new Dictionary<string, List<double>>();

        /// <summary>
        /// Add value of 'var' in a log data
        /// </summary>
        /// <param name="var"></param>
        /// <param name="value"></param>
        private void lua_datalog(string var, double value)
        {
            List<double> list = null;
            if (datalog.ContainsKey(var))
            {
                list = datalog[var];
            }
            else
            {
                list = new List<double>();
                datalog[var] = list;
            }

            list.Add(value);
        }

        private void init_datalog()
        {
            // init datalog
            datalog = new Dictionary<string, List<double>>();

            //register function datalog
            if (lua.GetFunction("data_log") == null)
                lua.RegisterFunction("data_log", this, this.GetType().GetMethod("lua_datalog", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        #endregion

        /// <summary>
        /// Call each time a step finished
        /// </summary>
        /// <param name="ga"></param>
        /// <param name="step"></param>
        private void Ga_StepFinish(GA ga, int step)
        {
            if (IsDisposed)
                return;

            //Console.WriteLine("Step " + step);
            Invoke((Action)delegate ()
            {
                progressBar1.Value = 100 * step / ga.Generations;
                label_progress.Text = string.Format("{0}/{1} {2}%", step, ga.Generations, progressBar1.Value);

                // open a preview window show result
                if (optResultsWindow == null || optResultsWindow.IsDisposed)
                {
                    optResultsWindow = new OptimizationResultsWindow();
                    optResultsWindow.OnIndividualSelected += OptResultsWindow_OnIndividualSelected;
                    optResultsWindow.OnApplyIndividualClick += OptResultsWindow_OnApplyIndividualClick;
                    optResultsWindow.Show();
                }

                optResultsWindow.visualizeNonDominatedSet(ga.NonDominatedSet);
            });
        }

        private void OptResultsWindow_OnApplyIndividualClick(Individual ind)
        {
            if (optimizationIsRunning)
                return;

            AbstractMotor motor = null;
            string infotext = "";
            string log = "";

            analyzeAnIndividual(ind, out infotext, out log, out motor);
            if (motor == null)
                return;

            applyOptimalResult(motor);
        }

        private PreviewWindow previewWindow;
        private void OptResultsWindow_OnIndividualSelected(Individual ind)
        {
            AbstractMotor motor = null;
            string infotext = "";
            string log = "";

            analyzeAnIndividual(ind, out infotext, out log, out motor);
            if (motor == null)
                return;

            rtb_results.Clear();
            rtb_results.AppendText(log);

            if (previewWindow == null || previewWindow.IsDisposed)
            {
                previewWindow = new PreviewWindow();
                previewWindow.Show();
            }

            previewWindow.SetMotor(motor);
            previewWindow.SetInfoText(infotext);
            previewWindow.Text = "Preview window";
            previewWindow.refreshPreview();
        }

        private void analyzeAnIndividual(Individual ind, out string infotext, out string log, out AbstractMotor motor)
        {
            double[] gens = ind.Genes;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Took " + (sw.ElapsedMilliseconds / 1000) + " secs. Dict size = " + dict_fitness.Count);

            infotext = "";
            AbstractMMAnalyser mma = null;

            try
            {
                lua_constructMotorfromGens(gens, currentConfig.luascript_constructMotor, out motor, out mma);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Motor constructing script failed: " + ex.Message);
                motor = null;
            }

            // finish script
            if (motor != null && currentConfig.luascript_finish != null && currentConfig.luascript_finish != "")
            {
                try
                {
                    //lua["fitness"] = fitness;
                    var a = motor.GetAnalyticalAnalyser();
                    a.RunAnalysis();

                    // finite elements analysis if available
                    object fem_results = null;
                    if (lua["useFEM"] != null && a.isDataValid() && mma != null)
                    {
                        // static analysis
                        String outdir = OutputFolderFEM + "\\" + motor.GetMD5String();
                        Directory.CreateDirectory(outdir);
                        String femFile = outdir + "\\static.FEM";
                        if (!motor.isFEMModelReady(femFile))
                        {
                            motor.BuildFEMModel(femFile);
                        }

                        AbstractStaticAnalyser staticAnalyser = motor.GetStaticAnalyser();
                        staticAnalyser.Path_OriginalFEMFile = femFile;
                        staticAnalyser.OutputPath = outdir;
                        staticAnalyser.DoAnalysisOnOriginalFEMFile = true;
                        staticAnalyser.RunAnalysis();

                        // mma analysis
                        mma.Path_OriginalFEMFile = femFile;
                        mma.CustomOutputDir = outdir;
                        mma.RunAnalysis();
                        fem_results = mma.Results;

                        FEMM.CloseFemm();
                    }


                    object[] o = null;
                    lock (lock_dostring)
                    {
                        lua["motor"] = motor;
                        lua["gens"] = gens;
                        lua["results"] = a.Results;
                        lua["femresults"] = fem_results;

                        o = lua.DoString(currentConfig.luascript_finish, "FinishScript");
                    }

                    if (o != null && o.Length > 0 && o[0] is string)
                    {
                        infotext = o[0].ToString();
                        sb.AppendLine("FinishScript:\r\n" + (o[0] as string));
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine("Finish script failed: " + ex.Message);
                }

                // 
                sb.AppendLine("Motor details:");
                sb.AppendLine(JSON.Beautify(JSON.ToJSON(motor)));
            }

            log = sb.ToString();
        }

        /// <summary>
        /// When finished all
        /// </summary>
        /// <param name="ga">GA</param>
        /// <param name="sw">Stopwatch for benchmarking</param>
        private void Ga_AllFinish(GA ga)
        {
            if (ga == null || ga.Running)
                return;

            if (IsDisposed)
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Took " + (sw.ElapsedMilliseconds / 1000) + " secs. Dict size = " + dict_fitness.Count);

            Invoke((Action)delegate ()
            {
                rtb_results.Text = sb.ToString();
                progressBar1.Value = 0;
                label_progress.Text = (ga.Cancelled ? "Cancelled after " : "Finished in ") + (sw.ElapsedMilliseconds / 1000) + " secs.";



                //if (datalog.Count > 0)
                //{
                //    GraphWindow tc = new GraphWindow();

                //    var x = Enumerable.Range(1, currentConfig.generation_size).Select(i => (double)i).ToArray();

                //    foreach (string var in datalog.Keys)
                //    {
                //        double[] y = new double[currentConfig.generation_size];
                //        List<double> datalog_y = datalog[var];
                //        int n = y.Length < datalog_y.Count ? y.Length : datalog_y.Count;//get min
                //        for (int i = 0; i < n; i++)
                //            y[i] = datalog_y[i];
                //        PointPairList data = new PointPairList(x, y);
                //        tc.addData(var, data);

                //    }

                //    tc.Text = "Optimization progress";
                //    tc.Show();
                //}

                if (optResultsWindow != null)
                {
                    optResultsWindow.visualizeNonDominatedSet(ga.GetFinalNonDominatedSet());
                }
            });
        }

        #endregion

        #endregion

        private void OptimizationWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // if ga exist, running, cancel it and wait for it to finish
            if (ga != null && ga.Running)
            {
                new Thread(delegate ()
                {

                    ga.Cancelled = true;
                    while (ga.Running)
                    {
                        Thread.Sleep(100);
                    }
                    closeLua();
                }).Start();
            }
            else
            {
                closeLua();
            }
        }

        private void comboBox_configs_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = comboBox_configs.SelectedIndex;
            selectAConfig(i);
        }
    }
}