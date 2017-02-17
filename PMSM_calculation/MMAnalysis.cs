using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Jayrock.Json.Conversion;
using System.IO;
using log4net;

namespace calc_from_geometryOfMotor
{
    // a full analysis all characteristics to build Matlab-simulink model
    public class MMAnalysis
    {
        private static readonly ILog log = LogManager.GetLogger("MMAnalysis");

        internal PMMotor Motor { get; set; }

        public double MaxCurrent { get; set; }
        public int StepCount { get; set; }
        public double Beta { get; set; }//angle between rotor/stator general current
        public double RotorAngle { get; set; }//rotor angle        

        private String OutDir;
        private String OutDir0;
        private String Path_resultsFile;

        [JsonIgnore]
        public MMAnalysisResults Results;

        public String GetMD5String()
        {
            String str = JsonConvert.ExportToString(this);
            //log.Info(str);
            String md5 = Utils.CalculateMD5Hash(str);
            //log.Info(md5);
            return md5;
        }

        public void setMotor(PMMotor motor)
        {
            Motor = motor;
        }

        private void generateOutputPath()
        {
            OutDir0 = Path.GetDirectoryName(Motor.Path_FEMMFile) + "\\" + Path.GetFileNameWithoutExtension(Motor.Path_FEMMFile) + "\\" + Motor.GetMD5String();
            OutDir = OutDir0 + "\\" + "MMAnalysis";
            Path_resultsFile = OutDir + "\\results.txt";            
        }

        public MMAnalysisResults LoadResultsFromDisk()
        {
            generateOutputPath();
            Results = MMAnalysisResults.loadResultsFromFile(Path_resultsFile);
            //if (Results != null && Results.MD5String != GetMD5String())
            //    return null;
            return Results;
        }

        public void RunAnalysis()
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

            // gen output path
            generateOutputPath();

            // create directory
            Directory.CreateDirectory(OutDir);

            // load results from disk
            MMAnalysisResults existedResults = MMAnalysisResults.loadResultsFromFile(Path_resultsFile);
            log.Info("Output results file:" + Path_resultsFile);
            // if has
            if (existedResults != null)
            {
                if (GetMD5String() == existedResults.MD5String)
                {
                    log.Info("M analysis already been done.");
                    Results = existedResults;
                    //OnFinishedAnalysis(this, Results);
                    return;
                }
                log.Info("M analysis exists but different. New analysis will be done.");
            }

            // create new 
            Results = new MMAnalysisResults(2 * StepCount + 1);
            Results.MD5String = GetMD5String();//assign md5 (signature of this)

            // steps numbering are put in queue for multi-thread
            Queue<int> steps = new Queue<int>();
            Queue<double> currents = new Queue<double>();
            for (int i = 0; i <= 2 * StepCount; i++)
            {
                if (i != StepCount)// meaning I=0
                    steps.Enqueue(i);
            }

            // for multi-thread config
            int threadCount = 4;
            ManualResetEvent[] MREs = new ManualResetEvent[threadCount];
            FEMM[] femms = new FEMM[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                MREs[i] = new ManualResetEvent(false);
                femms[i] = new FEMM();
            }

            // calculate the first (I=0), to get FluxLinkageM
            AnalyzeOne(StepCount, femms[0]);

            // start all threads
            for (int i = 0; i < threadCount; i++)
            {
                ManualResetEvent mre = MREs[i];
                FEMM femm = femms[i];
                new Thread(delegate()
                {
                    while (steps.Count > 0)
                        AnalyzeOne(steps.Dequeue(), femm);

                    mre.Set();
                }).Start();
            }

            // wait for all thread to finish
            for (int i = 0; i < threadCount; i++)
                MREs[i].WaitOne();

            //create folder if needed
            Directory.CreateDirectory(OutDir);
            // save results data to disk            
            Results.saveResultsToFile(Path_resultsFile);

            // finished event            
            OnFinishedAnalysis(this, Results);
        }

        public void StartAnalysisAsync()
        {
            Thread thread = new Thread(delegate()
            {
                RunAnalysis();
            });

            thread.Start();
        }

        private void AnalyzeOne(int step, FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            // open femm file
            femm.open(Motor.Path_FEMMFile);

            ///// Rotate rotor to an angle 
            // clear the way (line-boundary conditional in airgap)
            Motor.Airgap.MakeWayInAirgapBeforeRotationInFEMM(femm);

            // normalize, make rotorAngle inside (-2alpha,2alpha)
            double xRotorAngle = RotorAngle;
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

            // modify stator currents               
            double current = MaxCurrent / StepCount * (step - StepCount);//step can be -StepCount to +StepCount (total 2*StepCount+1), current=-Max->+Max

            double IA = current * Math.Cos(Beta * Math.PI / 180);
            double IB = current * Math.Cos(Beta * Math.PI / 180 - 2 * Math.PI / 3);
            double IC = current * Math.Cos(Beta * Math.PI / 180 + 2 * Math.PI / 3);
            Dictionary<String, double> currents = new Dictionary<string, double>()
            {
                {"A",IA},{"B",IB},{"C",IC}
            };
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

            Dictionary<String, FEMM.CircuitProperties> cps = Motor.Stator.getCircuitsPropertiesInAns(femm);

            MMAnalysisOneStepResult result = new MMAnalysisOneStepResult();
            result.Iabc = new Fabc { a = cps["A"].current, b = cps["B"].current, c = cps["C"].current };
            result.FluxLinkage_abc = new Fabc { a = cps["A"].fluxlinkage, b = cps["B"].fluxlinkage, c = cps["C"].fluxlinkage };
            result.Vabc = new Fabc { a = cps["A"].volts, b = cps["B"].volts, c = cps["C"].volts };

            // no currents, then save the fluxlinkageM, which is importance to calculate Ld
            if (current == 0)
                Results.FluxLinkageM = result.FluxLinkage_dq.d;

            result.FluxLinkage_M = Results.FluxLinkageM;

            Results[step] = result;

            femm.mo_close();

            log.Info("Step " + step + " done.");
        }

        public event EventHandler<MMAnalysisResults> OnFinishedAnalysis = new EventHandler<MMAnalysisResults>(delegate(object o, MMAnalysisResults e) { });

        public static MMAnalysis GetSample(PMMotor motor)
        {
            MMAnalysis mma = new MMAnalysis();

            mma.Motor = motor;
            mma.Beta = 45;
            mma.MaxCurrent = 160;
            mma.RotorAngle = 40;
            mma.StepCount = 10;

            return mma;
        }

    }//end of class MMAnalysis

    public class MMAnalysisOneStepResult
    {
        private static double theta_e = 0;//by default for now

        public double FluxLinkage_M { get; set; }

        public Fabc Iabc { get; set; }
        public Fabc FluxLinkage_abc { get; set; }
        public Fabc Vabc { get; set; }

        public Fdq Idq { get { return ParkTransform.abc_dq(Iabc, theta_e); } }
        public Fdq FluxLinkage_dq { get { return ParkTransform.abc_dq(FluxLinkage_abc, theta_e); } }
        public Fdq Vdq { get { return ParkTransform.abc_dq(Vabc, theta_e); } }

        public Fdq Ldq
        {
            get
            {
                Fdq ldq = new Fdq();
                ldq.d = (FluxLinkage_dq.d - FluxLinkage_M) / Idq.d;
                ldq.q = FluxLinkage_dq.q / Idq.q;

                return ldq;
            }
        }
    }

    public class MMAnalysisResults : EventArgs
    {
        private static readonly ILog log = LogManager.GetLogger("MMAnalysis");

        public int Count { get { return ListResults.Length; } }

        public String MD5String { get; internal set; }

        public double FluxLinkageM { get; set; }

        public MMAnalysisOneStepResult[] ListResults { get; internal set; }

        public MMAnalysisResults(int count)
        {
            ListResults = new MMAnalysisOneStepResult[count];
        }

        // for json import
        public MMAnalysisResults()
        {
        }

        public MMAnalysisOneStepResult this[int i]
        {
            get
            {
                return ListResults[i];
            }

            set
            {
                ListResults[i] = value;
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

        public static MMAnalysisResults loadResultsFromFile(String fn)
        {
            if (!File.Exists(fn))
                return null;

            using (StreamReader sr = new StreamReader(fn))
            {
                try
                {
                    MMAnalysisResults results = JsonConvert.Import<MMAnalysisResults>(sr);
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
    }
}
