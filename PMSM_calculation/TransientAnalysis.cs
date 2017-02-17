using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;
using System.Threading;
using Jayrock.Json.Conversion;
using Jayrock.Json;

namespace calc_from_geometryOfMotor
{
    public class TransientAnalysis
    {
        private static readonly ILog log = LogManager.GetLogger("TransientAnalysis");

        // motor
        [JsonIgnore]
        public PMMotor Motor { get; private set; }

        /// <summary>
        /// like this: Transient\\name
        /// </summary>        
        [JsonIgnore]
        public String AnalysisName { get; private set; }

        // end time for simulation (second)
        public double EndTime { get; set; }

        public int StepCount { get; set; }

        // rotor speed (rpm)
        public double RotorSpeed { get; set; }

        // rotor start angle (from o'clock - counterclockwise - degree)
        public double StartAngle { get; set; }

        // stator currents formular string (will be called on each step to update)
        public String IA { get; set; }
        public String IB { get; set; }
        public String IC { get; set; }

        //readonly
        public double RotorSpeedRadSecond { get { return RotorSpeed * 2.0 * Math.PI / 60; } }
        public double RotorSpeedDegreeSecond { get { return RotorSpeed * 6; } }

        // for private use
        private String Path_TransResultsFile;
        private String OutDir0;
        private String OutDir;

        // results store here
        [JsonIgnore]
        public TransientResults Results { get; private set; }

        public void setMotor(PMMotor motor)
        {
            this.Motor = motor;
        }

        public void setName(String name)
        {
            AnalysisName = name;
        }

        /// <summary>
        /// Setup path base on motor output fem file, motor params and analysis name
        /// </summary>
        private void generateOutputPath()
        {
            // directory store data for current motor (using md5 as folder) D:\..\..\m1\md5string-motor\Transient\ 
            OutDir0 = Path.GetDirectoryName(Motor.Path_FEMMFile) + "\\" + Path.GetFileNameWithoutExtension(Motor.Path_FEMMFile) + "\\" + Motor.GetMD5String() + "\\Transient";
            //using hash md5 instead of actual name because femm cannot open the path in unicode
            //<outdir0>\md5-transient-variant\
            OutDir = OutDir0 + "\\" + Utils.CalculateMD5Hash(AnalysisName);
            Path_TransResultsFile = OutDir + "\\results.txt";            
        }

        /// <summary>
        /// Hash MD5 of this transient analysis configuration
        /// </summary>
        /// <returns></returns>
        public String GetMD5String()
        {
            String str = JsonConvert.ExportToString(this);
            //log.Info(str);
            String md5 = Utils.CalculateMD5Hash(str);
            //log.Info(md5);
            return md5;
        }

        /// <summary>
        /// Start transient analysis. 
        /// Assuming existed a FEMM file was created. This method only rotates the rotor and do analyze.
        /// If file doesn't existed, we are doom
        /// </summary>
        /// <param name="config"></param>
        public void RunAnalysisSync()
        {
            if (Motor == null)
            {
                throw new InvalidOperationException("Motor hasn't been assigned");
            }

            if (!File.Exists(Motor.Path_FEMMFile))
            {
                log.Error("File doesn't exist: " + Motor.Path_FEMMFile);
                return;
            }

            // create path first
            generateOutputPath();

            // create output directory
            Directory.CreateDirectory(OutDir);

            // write config to disk (for explorer manually can read and understand what the hell are in this folder)
            writeConfigToDisk();

            // load results from disk            
            TransientResults existedResults = TransientResults.loadResultsFromFile(Path_TransResultsFile);
            log.Info("Output results file:" + Path_TransResultsFile);
            // if has
            if (existedResults != null)
            {
                if (GetMD5String() == existedResults.MD5String)
                {
                    log.Info("Transient analysis already been done.");
                    Results = existedResults;
                    Results.TransAnalysis = this;//this results is match with config, so link them
                    OnFinishedAnalysis(this, Results);
                    return;
                }
                log.Info("Transient analysis exists but different. New analysis will be done.");
            }

            // create new 
            Results = new TransientResults(this);
            Results.MD5String = GetMD5String();//assign md5 (signature of this)            

            // for multi-thread config
            int threadCount = 4;
            ManualResetEvent[] MREs = new ManualResetEvent[threadCount];
            Queue<int> steps = new Queue<int>();
            for (int i = 0; i < StepCount + 1; i++)
                steps.Enqueue(i);

            // start all threads
            for (int i = 0; i < threadCount; i++)
            {
                MREs[i] = new ManualResetEvent(false);
                ManualResetEvent mre = MREs[i];
                new Thread(delegate()
                {
                    FEMM femm = new FEMM();
                    while (steps.Count > 0)
                    {
                        int step = steps.Dequeue();
                        AnalyzeOne(step, femm);
                    }

                    mre.Set();
                }).Start();
            }

            // wait for all thread to finish
            for (int i = 0; i < threadCount; i++)
                MREs[i].WaitOne();

            // save results data to disk            
            Results.saveResultsToFile(Path_TransResultsFile);

            // finished event            
            OnFinishedAnalysis(this, Results);
        }

        /// <summary>
        /// Run analysis in new thread (Async)
        /// </summary>
        public void StartAnalysis()
        {
            Thread thread = new Thread(RunAnalysisSync);
            thread.Start();
        }

        /// <summary>
        /// Analyze 1 step, using femm window
        /// </summary>
        /// <param name="data"></param>
        /// <param name="step"></param>
        /// <param name="femm"></param>
        private void AnalyzeOne(int step, FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            double t = Results.Times[step];
            double rotorAngle = RotorSpeedDegreeSecond * t + StartAngle;

            // run script to update IA,IB,IC
            NLua.Lua lua_state = LuaHelper.GetLuaState();
            String[] currentFormulas = { IA, IB, IC };
            String[] circuitNames = { "A", "B", "C" };
            Dictionary<String, double> currents = new Dictionary<string, double>();
            try
            {
                lua_state["step"] = step;
                lua_state["time"] = t;
                lua_state["omega"] = RotorSpeedRadSecond;

                for (int i = 0; i < currentFormulas.Length; i++)
                {
                    double Ix = (double)lua_state.DoString("return " + currentFormulas[i])[0];
                    currents[circuitNames[i]] = Ix;
                }
            }
            catch (Exception ex)
            {
                log.Error("Lua error :" + ex.Message);
                //TODO: maybe halt analysis since there maybe big problem
            }

            // open femm file
            femm.open(Motor.Path_FEMMFile);

            // clear the way (line-boundary conditional in airgap)
            Motor.Airgap.MakeWayInAirgapBeforeRotationInFEMM(femm);

            // normalize, make rotorAngle inside (-2alpha,2alpha)
            double xRotorAngle = rotorAngle;
            if (!Motor.GeneralParams.FullBuildFEMModel)
            {
                double alphax2 = 2 * Motor.Rotor.alphaDegree;
                if (xRotorAngle > alphax2 || xRotorAngle < -alphax2)
                    xRotorAngle = xRotorAngle - Math.Round(xRotorAngle / (2 * alphax2)) * 2 * alphax2;
            }

            // rotate rotor
            Motor.Rotor.RotateRotorInFEMM(xRotorAngle, femm);

            // modify airgap (in case partial build)
            Motor.Airgap.ModifyAirgapAfterRotationInFEMM(xRotorAngle, femm);

            // modify stator currents using args passed from OnBeginAnalyzeOne           
            Motor.Stator.SetStatorCurrentsInFEMM(currents, femm);

            // save as new file fem (temp only)
            String stepfileFEM = OutDir + "\\" + String.Format("{0:D4}.FEM", step);
            femm.mi_saveas(stepfileFEM);

            // analyze
            femm.mi_analyze(true);

            // close
            femm.mi_close();

            // delete temp file
            //File.Delete(stepfileFEM);

            // open ans file to measure ?
            String stepfileANS = Path.GetDirectoryName(stepfileFEM) + "\\" + Path.GetFileNameWithoutExtension(stepfileFEM) + ".ans";
            femm.open(stepfileANS);

            //String bmpFile = config.OutDir + "\\bmp\\" + Path.GetFileNameWithoutExtension(stepfileFEM) + ".bmp";
            //femm.mo_savebitmap(bmpFile);

            // get circuits properties from stator
            TransientStepResult result = new TransientStepResult();

            Dictionary<String, FEMM.CircuitProperties> cps = Motor.Stator.getCircuitsPropertiesInAns(femm);
            double torque = Motor.Rotor.getTorqueInAns(femm);

            result.Torque = torque;
            result.RotorAngle = rotorAngle;
            result.CircuitProperties = cps;

            Results[step] = result;

            OnFinishAnalyzeOneStep(this, result);

            log.Info(String.Format("Time {0:F5}: {1}", t, torque));

            femm.mo_close();
        }

        public event EventHandler<TransientStepResult> OnFinishAnalyzeOneStep = new EventHandler<TransientStepResult>(delegate(object o, TransientStepResult e) { });

        public event EventHandler<TransientResults> OnFinishedAnalysis = new EventHandler<TransientResults>(delegate(object o, TransientResults e) { });

        public void LoadResultsFromDisk()
        {
            if (Motor == null)
            {
                throw new InvalidOperationException("Motor hasn't been assigned");
            }

            generateOutputPath();

            Results = TransientResults.loadResultsFromFile(Path_TransResultsFile);

            // if load success, link it to current transient analysis 
            // even they aren't match, no problem
            if (Results != null)
                Results.TransAnalysis = this;
        }

        public void renameMe(String newname)
        {
            String md5newname = Utils.CalculateMD5Hash(newname);
            String md5oldname = Utils.CalculateMD5Hash(AnalysisName);

            if (Directory.Exists(OutDir0 + "\\" + md5newname))
            {
                Directory.Delete(OutDir0 + "\\" + md5newname, true);
            }

            if (Directory.Exists(OutDir0 + "\\" + md5oldname))
                Directory.Move(OutDir0 + "\\" + md5oldname, OutDir0 + "\\" + md5newname);

            AnalysisName = newname;

            // regenerate output path, since name is changed
            generateOutputPath();
        }

        public void removeMe()
        {
            if (Directory.Exists(OutDir))
                Directory.Delete(OutDir, true);
        }

        private void writeConfigToDisk()
        {
            String fn = OutDir + "\\transientconfig.txt";
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(AnalysisName);
                sw.Write(JsonConvert.ExportToString(this));
            }                        
        }

        #region Sample Config

        public static TransientAnalysis GetSampleMe(PMMotor motor)
        {
            TransientAnalysis ta = new TransientAnalysis();

            ta.AnalysisName = "Transient\\sample";
            ta.Motor = motor;
            ta.RotorSpeed = -3000;//rpm
            ta.EndTime = Math.Abs(120 / ta.RotorSpeedDegreeSecond);
            ta.StepCount = 120;
            ta.StartAngle = 40;

            return ta;
        }

        #endregion
    }

    /// <summary>
    /// Result of each step
    /// </summary>
    public class TransientStepResult : EventArgs
    {
        public double Torque { get; set; }
        public double RotorAngle { get; set; }

        public IDictionary<String, FEMM.CircuitProperties> CircuitProperties;
    }

    /// <summary>
    /// Results as array(list) of variable by time
    /// </summary>
    public class TransientResults : EventArgs
    {
        private static readonly ILog log = LogManager.GetLogger("TransientResults");

        //back link
        [JsonIgnore]
        public TransientAnalysis TransAnalysis { get; internal set; }

        /// <summary>
        /// MD5 string as hash value for this whole transient config
        /// </summary>
        public String MD5String { get; internal set; }

        /// <summary>
        /// Flux linkage by magnet
        /// </summary>
        public double FluxLinkageM
        {
            get
            {
                if (TransAnalysis != null && TransAnalysis.Motor != null && TransAnalysis.Motor.StaticResults != null)
                    return TransAnalysis.Motor.StaticResults.psiM;
                else return double.NaN;
            }
        }

        /// <summary>
        /// Times 0-StepCount 
        /// </summary>
        public double[] Times { get; internal set; }

        public TransientStepResult[] ListResults { get; internal set; }

        public TransientResults(TransientAnalysis ta)
        {
            TransAnalysis = ta;

            Times = new double[TransAnalysis.StepCount + 1];//0..stepCount
            ListResults = new TransientStepResult[TransAnalysis.StepCount + 1];
            for (int i = 0; i <= TransAnalysis.StepCount; i++)
                Times[i] = TransAnalysis.EndTime / TransAnalysis.StepCount * i;
        }

        // for json export to string purpose
        public TransientResults()
        {
        }

        /// <summary>
        /// Access to result at step
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public TransientStepResult this[int step]
        {
            get
            {
                return ListResults[step];
            }
            set
            {
                ListResults[step] = value;
            }
        }

        #region save/load results to disk

        public void saveResultsToFile(String fn)
        {
            using (StreamWriter sw = new StreamWriter(fn))
            {
                string str = JsonConvert.ExportToString(this);
                sw.Write(str);
            }
        }

        public static TransientResults loadResultsFromFile(String fn)
        {
            if (!File.Exists(fn))
                return null;

            using (StreamReader sr = new StreamReader(fn))
            {
                try
                {
                    TransientResults results = JsonConvert.Import<TransientResults>(sr);
                    return results;
                }
                catch (Exception e)
                {
                    log.Error("Fail open " + fn + ".Error: " + e.Message);
                    return null;
                }
            }
        }

        #endregion

        #region Output-array of double

        public int Count { get { return ListResults == null ? 0 : ListResults.Count(); } }

        public double[] Torques
        {
            get
            {
                double[] tt = new double[ListResults.Length];
                for (int i = 0; i < ListResults.Length; i++)
                    tt[i] = ListResults[i].Torque;

                return tt;
            }
        }

        public double[] RotorAngles
        {
            get
            {
                double[] tt = new double[ListResults.Length];
                for (int i = 0; i < ListResults.Length; i++)
                    tt[i] = ListResults[i].RotorAngle;

                return tt;
            }
        }

        public double[] FluxLinkageOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            double[] tt = new double[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
                tt[i] = ListResults[i].CircuitProperties[circuitName].fluxlinkage;

            return tt;
        }

        public double[] CurrentOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            double[] tt = new double[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
                tt[i] = ListResults[i].CircuitProperties[circuitName].current;

            return tt;
        }

        public double[] VoltageOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            double[] tt = new double[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
                tt[i] = ListResults[i].CircuitProperties[circuitName].volts;

            return tt;
        }

        public double[] InducedVoltageOf(String circuitName)
        {
            if (ListResults[0].CircuitProperties.ContainsKey(circuitName) == false)
                return null;

            double[] tt = new double[ListResults.Length];
            double dt = Times[1] - Times[0];
            double psi1 = ListResults[0].CircuitProperties[circuitName].fluxlinkage;
            for (int i = 1; i < ListResults.Length; i++)
            {
                double psi2 = ListResults[i].CircuitProperties[circuitName].fluxlinkage;
                tt[i] = -(psi2 - psi1) / dt;
                psi1 = psi2;
            }
            tt[0] = tt[tt.Length - 1];
            return tt;
        }

        #endregion

        #region Further conversion (abc)->(dq)

        private Fdq[] fluxlinkageDQ;

        public Fdq[] FluxLinkageDQ()
        {
            if (fluxlinkageDQ != null)
                return fluxlinkageDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;

            fluxlinkageDQ = new Fdq[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
            {
                double fA = ListResults[i].CircuitProperties["A"].fluxlinkage;
                double fB = ListResults[i].CircuitProperties["B"].fluxlinkage;
                double fC = ListResults[i].CircuitProperties["C"].fluxlinkage;
                double theta_e = (ListResults[i].RotorAngle - ListResults[0].RotorAngle) * TransAnalysis.Motor.Rotor.p * Math.PI / 180;

                fluxlinkageDQ[i] = ParkTransform.abc_dq(fA, fB, fC, theta_e);
            }

            return fluxlinkageDQ;
        }

        public double[] FluxLinkage_D()
        {
            Fdq[] psidq = FluxLinkageDQ();
            if (psidq == null)
                return null;

            return psidq.Select(p => p.d).ToArray();
        }

        public double[] FluxLinkage_Q()
        {
            Fdq[] psidq = FluxLinkageDQ();
            if (psidq == null)
                return null;

            return psidq.Select(p => p.q).ToArray();
        }


        private Fdq[] currentDQ;

        public Fdq[] CurrentDQ()
        {
            if (currentDQ != null)
                return currentDQ;

            if (!ListResults[0].CircuitProperties.ContainsKey("A") ||
                !ListResults[0].CircuitProperties.ContainsKey("B") ||
                !ListResults[0].CircuitProperties.ContainsKey("C"))
                return null;

            currentDQ = new Fdq[ListResults.Length];
            for (int i = 0; i < ListResults.Length; i++)
            {
                double fA = ListResults[i].CircuitProperties["A"].current;
                double fB = ListResults[i].CircuitProperties["B"].current;
                double fC = ListResults[i].CircuitProperties["C"].current;
                double theta_e = (ListResults[i].RotorAngle - ListResults[0].RotorAngle) * TransAnalysis.Motor.Rotor.p * Math.PI / 180;

                currentDQ[i] = ParkTransform.abc_dq(fA, fB, fC, theta_e);
            }
            return currentDQ;
        }

        public double[] Current_D()
        {
            Fdq[] Idq = CurrentDQ();
            if (Idq == null)
                return null;

            return Idq.Select(p => p.d).ToArray();
        }

        public double[] Current_Q()
        {
            Fdq[] Idq = CurrentDQ();
            if (Idq == null)
                return null;

            return Idq.Select(p => p.q).ToArray();
        }

        private Fdq[] inductanceDQ;

        public Fdq[] InductanceDQ()
        {
            if (inductanceDQ != null)
                return inductanceDQ;

            Fdq[] psidq = FluxLinkageDQ();
            Fdq[] Idq = CurrentDQ();
            if (psidq == null || Idq == null)
                return null;

            // look for psiM (rough) (at Id=0)
            //double psiM = 0;
            //int indexM = -1;
            //double Imin = double.MaxValue;
            //for (int i = 0; i < Idq.Length; i++)
            //    if (Imin > Math.Abs(Idq[i].d))
            //    {
            //        Imin = Math.Abs(Idq[i].d);
            //        indexM = i;
            //    }
            //psiM = psidq[indexM].d;
            //FluxLinkageM = psiM;

            // calc now
            inductanceDQ = new Fdq[Idq.Length];

            for (int i = 0; i < Idq.Length; i++)
            {
                inductanceDQ[i].d = (psidq[i].d - FluxLinkageM) / Idq[i].d;
                inductanceDQ[i].q = psidq[i].q / Idq[i].q;
            }

            return inductanceDQ;
        }

        public double[] Inductance_D()
        {
            if (InductanceDQ() == null)
                return null;

            return inductanceDQ.Select(p => p.d).ToArray();
        }

        public double[] Inductance_Q()
        {
            if (InductanceDQ() == null)
                return null;

            return inductanceDQ.Select(p => p.q).ToArray();
        }

        #endregion
    }
}
