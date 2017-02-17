using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using fastJSON;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractTransientAnalyser : AbstractFEMAnalyser
    {
        // end time for simulation (second)
        public double EndTime { get; set; }

        public int StepCount { get; set; }

        // rotor speed (rpm)
        public double RotorSpeed { get; set; }

        // rotor start angle (from o'clock - counterclockwise - degree)
        public double StartAngle { get; set; }

        public double SkewAngle { get; set; }
        public int NSkewSegment { get; set; }

        //readonly
        public double RotorSpeedRadSecond { get { return RotorSpeed * 2.0 * Math.PI / 60; } }
        public double RotorSpeedDegreeSecond { get { return RotorSpeed * 6; } }

        public override string Path_ToAnalysisVariant
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomOutputDir))
                    return CustomOutputDir;

                return Path_ToMotorVariant + "\\Transient\\" + Utils.CalculateMD5Hash(AnalysisName);
            }
        }
        public override string Path_ResultsFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\results.txt";
            }
        }

        public override string GetMD5String()
        {
            return Motor.GetMD5String() + "." + base.GetMD5String();
        }

        protected sealed override void analyze()
        {
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
                new Thread(delegate ()
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

            OnFinishAnalysis();
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

            if (SkewAngle <= 0 || NSkewSegment <= 0)
            {
                // prepare arguments for this step
                TransientStepArgs args = PrepareStepArgs(step);

                // before analyzing
                OnStartAnalyzeOneStep(this, args);

                // open femm file
                femm.open(Path_OriginalFEMFile);

                // modify rotor: rotate, or other things that we can think of
                DoModifyRotor(args, femm);

                // modify stator: change current, or something else?
                DoModifyStator(args, femm);

                // save as new file fem (temp only)
                String stepfileFEM = Path_ToAnalysisVariant + "\\" + String.Format("{0:D4}.FEM", step);
                femm.mi_saveas(stepfileFEM);

                // analyze
                femm.mi_analyze(true);

                // close
                femm.mi_close();

                // open ans file to measure ?
                String stepfileANS = Path.GetDirectoryName(stepfileFEM) + "\\" + Path.GetFileNameWithoutExtension(stepfileFEM) + ".ans";
                femm.open(stepfileANS);

                // do measure torque or anything else
                DoMeasureData(args, femm);

                // put in the list
                var Results = this.Results as Transient3PhaseMotorResults;
                Results[step] = args;

                //log.Info(String.Format("Time {0:F5}: {1}", t, torque));

                femm.mo_close();

                // after analyzing
                OnFinishAnalyzeOneStep(this, args);
            }
            else // has skewangle
            {
                // prepare arguments for this step
                TransientStepArgs total_args = PrepareStepArgs(step);

                // before analyzing
                OnStartAnalyzeOneStep(this, total_args);

                for (int k = 0; k < 2 * NSkewSegment + 1; k++)
                {
                    TransientStepArgs args = PrepareStepArgs(step);
                    double sk = (k - NSkewSegment) * SkewAngle / (2 * NSkewSegment);
                    args.skewAngleAdded = sk;
                    args.RotorAngle += sk;                    

                    // open femm file
                    femm.open(Path_OriginalFEMFile);

                    // modify rotor: rotate, or other things that we can think of
                    DoModifyRotor(args, femm);

                    // modify stator: change current, or something else?
                    DoModifyStator(args, femm);

                    // save as new file fem (temp only)
                    String stepfileFEM = Path_ToAnalysisVariant + "\\" + String.Format("{0:D4}_{1}.FEM", step, k);
                    femm.mi_saveas(stepfileFEM);

                    // analyze
                    femm.mi_analyze(true);

                    // close
                    femm.mi_close();

                    // open ans file to measure ?
                    String stepfileANS = Path.GetDirectoryName(stepfileFEM) + "\\" + Path.GetFileNameWithoutExtension(stepfileFEM) + ".ans";
                    femm.open(stepfileANS);

                    // do measure torque or anything else
                    DoMeasureData(args, femm);

                    // initialize data for total
                    if (k == 0)
                    {
                        total_args = JSON.DeepCopy(args);
                        total_args.Torque = 0;
                        foreach (string key in total_args.CircuitProperties.Keys)
                        {
                            total_args.CircuitProperties[key].fluxlinkage = 0;
                        }
                    }

                    femm.mo_close();

                    total_args.Torque += args.Torque / (2 * NSkewSegment + 1);
                    foreach (string key in total_args.CircuitProperties.Keys)
                    {
                        total_args.CircuitProperties[key].fluxlinkage += args.CircuitProperties[key].fluxlinkage / (2 * NSkewSegment + 1);
                    }
                }

                // put in the list
                var Results = this.Results as Transient3PhaseMotorResults;
                Results[step] = total_args;

                //log.Info(String.Format("Time {0:F5}: {1}", t, torque));                

                // after analyzing
                OnFinishAnalyzeOneStep(this, total_args);
            }
        }

        /// <summary>
        /// Prepare an arguments for a step
        /// This may be overriden to add other arguments as well.
        /// Derived class can override this, with new TransientStepArgs class pass as argument
        /// <param name="step">Step in which analysis is running</param>
        /// <param name="args">Arguments to be put data into, can be null.</param>
        /// </summary>
        protected virtual TransientStepArgs PrepareStepArgs(int step, TransientStepArgs args = null)
        {
            var Results = this.Results as Transient3PhaseMotorResults;
            double t = Results.Times[step];
            double rotorAngle = RotorSpeedDegreeSecond * t + StartAngle;

            // arguments for this step
            if (args == null)
                args = new TransientStepArgs();
            args.time = t;
            args.RotorAngle = rotorAngle;
            args.step = step;
            args.currents = new Dictionary<String, double>();

            return args;
        }

        protected virtual void DoModifyRotor(TransientStepArgs args, FEMM femm)
        {
            // clear the way (line-boundary conditional in airgap)
            Motor.Airgap.RemoveBoundary(femm);

            // normalize, make rotorAngle inside (-2alpha,2alpha)
            double xRotorAngle = Motor.GetNormalizedRotorAngle(args.RotorAngle);

            // rotate rotor
            Motor.Rotor.RotateRotorInFEMM(xRotorAngle, femm);

            // modify airgap (in case partial build)
            Motor.Airgap.AddBoundaryAtAngle(xRotorAngle, femm);
        }

        protected virtual void DoModifyStator(TransientStepArgs args, FEMM femm)
        {
            Motor.Stator.SetStatorCurrentsInFEMM(args.currents, femm);
        }

        protected virtual void DoMeasureData(TransientStepArgs args, FEMM femm)
        {
            Dictionary<String, FEMM.CircuitProperties> cps = Motor.Stator.getCircuitsPropertiesInAns(femm);
            double torque = Motor.Rotor.getTorqueInAns(femm);

            args.Torque = torque;
            args.RotorAngle = args.RotorAngle;
            args.CircuitProperties = cps;
        }

        public virtual void renameMe(String newname)
        {
            String newpath = Path_ToAnalysisVariant + "\\" + Utils.CalculateMD5Hash(newname);
            String oldpath = Path_ToAnalysisVariant + "\\" + Utils.CalculateMD5Hash(AnalysisName);

            if (Directory.Exists(oldpath))
            {
                // if newpath existed, delete it
                if (Directory.Exists(newpath))
                {
                    Directory.Delete(newpath, true);
                }
                Directory.Move(oldpath, newpath);
                AnalysisName = newname;
            }
        }

        public virtual void removeMe()
        {
            String path = Path_ToAnalysisVariant + "\\" + Utils.CalculateMD5Hash(AnalysisName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        protected override void PrepareDirectory()
        {
            Directory.CreateDirectory(Path_ToAnalysisVariant);

            String fn = Path_ToAnalysisVariant + "\\transientconfig.txt";
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(AnalysisName);
                sw.Write(JSON.Beautify(JSON.ToJSON(this)));
            }
        }

        public event EventHandler<TransientStepArgs> OnFinishAnalyzeOneStep = new EventHandler<TransientStepArgs>(delegate (object o, TransientStepArgs e) { });
        public event EventHandler<TransientStepArgs> OnStartAnalyzeOneStep = new EventHandler<TransientStepArgs>(delegate (object o, TransientStepArgs e) { });
    }

    /// <summary>
    /// Result of each step
    /// </summary>
    public class TransientStepArgs : EventArgs
    {
        public int step { get; set; }
        public double time { get; set; }
        public double Torque { get; set; }
        public double RotorAngle { get; set; }

        /// <summary>
        /// skew angle, that included in rotor angle
        /// </summary>
        [JsonIgnore]
        public double skewAngleAdded { get; set; }

        [JsonIgnore]
        public IDictionary<String, double> currents { get; internal set; }

        public Dictionary<string, FEMM.CircuitProperties> CircuitProperties { get; set; }
    }

    /// <summary>
    /// Results as array(list) of variable by time
    /// </summary>
    public class TransientResults : AbstractFEMResults
    {
        [JsonIgnore]
        new internal AbstractTransientAnalyser Analyser
        {
            get
            {
                return (AbstractTransientAnalyser)base.Analyser;
            }
            set
            {
                base.Analyser = value;
            }
        }

        #region Stored Data

        /// <summary>
        /// Times 0-StepCount 
        /// </summary>
        public double[] Times { get; set; }

        public TransientStepArgs[] ListResults { get; set; }

        #endregion

        public TransientResults(AbstractTransientAnalyser ta)
            : this()
        {
            Analyser = ta;

            Times = new double[Analyser.StepCount + 1];//0..stepCount
            ListResults = new TransientStepArgs[Analyser.StepCount + 1];
            for (int i = 0; i <= Analyser.StepCount; i++)
                Times[i] = Analyser.EndTime / Analyser.StepCount * i;
        }

        // for json export to string purpose
        public TransientResults()
            : base()
        {
        }

        /// <summary>
        /// Access to result at step
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public TransientStepArgs this[int step]
        {
            get
            {
                return ListResults[step];
            }

            internal set
            {
                ListResults[step] = value;
            }
        }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            IDictionary<String, object> dict = base.BuildResultsForDisplay();

            // to open fem file
            var pmstaticanalyser = Analyser as AbstractTransientAnalyser;
            if (pmstaticanalyser != null)
                dict.Add("OpenResults", pmstaticanalyser.Path_ToAnalysisVariant);

            dict.Add("Torque", new ListPointD(Times, Torques));

            // rotor angle
            dict.Add("RotorAngle", new ListPointD(Times, RotorAngles));

            return dict;
        }

        #region Output-array of double

        public int Count { get { return ListResults == null ? 0 : ListResults.Count(); } }

        public double[] Torques
        {
            get
            {
                return ListResults.Select(l => l.Torque).ToArray();
            }
        }

        public double[] RotorAngles
        {
            get
            {
                return ListResults.Select(l => l.RotorAngle).ToArray();
            }
        }

        public double[] FluxLinkageOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            return ListResults.Select(l => l.CircuitProperties[circuitName].fluxlinkage).ToArray();
        }

        public double[] CurrentOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            return ListResults.Select(l => l.CircuitProperties[circuitName].current).ToArray();
        }

        public double[] VoltageOf(String circuitName)
        {
            if (!ListResults[0].CircuitProperties.ContainsKey(circuitName))
                return null;

            return ListResults.Select(l => l.CircuitProperties[circuitName].volts).ToArray();
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
                tt[i] = (psi2 - psi1) / dt;
                psi1 = psi2;
            }
            tt[0] = tt[tt.Length - 1];
            return tt;
        }

        #endregion
    }
}
