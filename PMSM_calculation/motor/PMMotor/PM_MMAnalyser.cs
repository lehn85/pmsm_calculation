using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using MathNet.Numerics.Interpolation;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PM_MMAnalyser : AbstractMMAnalyser
    {
        public double MaxCurrent { get; set; }
        public int StepCount { get; set; }
        public double Beta { get; set; }//angle between rotor/stator general current
        public double RotorAngle { get; set; }//rotor angle at which study        

        public bool Only2ndQuarter { get; set; }
        public bool NotIncludeCurrentZero { get; set; }

        private int ResultCount
        {
            get
            {
                int count = StepCount;
                if (!Only2ndQuarter)
                    count *= 2;
                if (!NotIncludeCurrentZero)
                    count += 1;

                return count;
            }
        }

        protected override AbstractFEMResults NewResults()
        {
            return new PM_MMAnalysisResults(ResultCount);
        }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            return (PM_MMAnalysisResults)AbstractFEMResults.loadResultsFromFile(typeof(PM_MMAnalysisResults), fromfile);
        }

        protected override void analyze()
        {
            // steps numbering are put in queue for multi-thread
            Queue<int> steps = new Queue<int>();
            Queue<double> currents = new Queue<double>();
            for (int i = 0; i < ResultCount; i++)
                steps.Enqueue(i);

            // for multi-thread config            
            int threadCount = 4;
            if (FEMMToUse != null && FEMMToUse.Count > 0)
            {
                threadCount = FEMMToUse.Count;
            }
            if (threadCount > steps.Count)
                threadCount = steps.Count;

            ManualResetEvent[] MREs = new ManualResetEvent[threadCount];
            FEMM[] femms = new FEMM[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                MREs[i] = new ManualResetEvent(false);
                if (FEMMToUse != null && FEMMToUse.Count > 0)
                    femms[i] = FEMMToUse[i];
                else
                    femms[i] = new FEMM();
            }

            // start all threads
            for (int i = 0; i < threadCount; i++)
            {
                ManualResetEvent mre = MREs[i];
                FEMM femm = femms[i];
                new Thread(delegate ()
                {
                    while (steps.Count > 0)
                        AnalyzeOne(steps.Dequeue(), femm);

                    mre.Set();
                }).Start();
            }

            // wait for all thread to finish
            for (int i = 0; i < threadCount; i++)
                MREs[i].WaitOne();
        }

        private void AnalyzeOne(int step, FEMM femm = null)
        {
            if (femm == null)
                femm = FEMM.DefaultFEMM;

            // open femm file
            femm.open(Path_OriginalFEMFile);

            ///// Rotate rotor to an angle 
            // clear the way (line-boundary conditional in airgap)
            Motor.Airgap.RemoveBoundary(femm);

            // normalize, make rotorAngle inside (-2alpha,2alpha) to build FEMM model        
            double femmRotorAngle = Motor.GetNormalizedRotorAngle(RotorAngle);

            // rotate rotor
            Motor.Rotor.RotateRotorInFEMM(femmRotorAngle, femm);

            // modify airgap (in case partial build)
            Motor.Airgap.AddBoundaryAtAngle(femmRotorAngle, femm);

            // modify stator currents               
            // testing iq=const, id change to see how inductance d,q change 
            //double id = 80.0 / StepCount * (step - StepCount);
            //double iq = 40.0;
            //double current = Math.Sqrt(id * id + iq * iq);
            //double Beta = Math.Abs(id) > 1e-8 ? Math.Atan(iq / id) * 180 / Math.PI : 90.0;
            //if (Beta < 0)
            //    Beta += 180;

            // Test change beta, not current
            //double beta = (this.Beta - 100) / StepCount * step + 100;
            //double current = MaxCurrent;

            var stator = Motor.Stator as Stator3Phase;

            // Change current
            double current;

            if (Only2ndQuarter)
            {
                int c = NotIncludeCurrentZero ? 1 : 0;
                current = MaxCurrent * (step + c) / StepCount;
            }
            else
            {
                int c = NotIncludeCurrentZero ? (step < StepCount ? 0 : 1) : 0;
                current = MaxCurrent / StepCount * ((step + c) - StepCount);
            }

            // correction beta with current rotor angle and original angle (0)
            double beta = this.Beta + (RotorAngle - stator.VectorMMFAngle) * Motor.Rotor.p;//in degree

            double IA = current * Math.Cos(beta * Math.PI / 180);
            double IB = current * Math.Cos(beta * Math.PI / 180 - 2 * Math.PI / 3);
            double IC = current * Math.Cos(beta * Math.PI / 180 + 2 * Math.PI / 3);
            Dictionary<String, double> currents = new Dictionary<string, double>()
            {
                {"A",IA},{"B",IB},{"C",IC}
            };
            Motor.Stator.SetStatorCurrentsInFEMM(currents, femm);

            // save as new file fem (temp only)
            String stepfileFEM = Path_ToAnalysisVariant + "\\" + String.Format("{0:D4}.FEM", step);
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
            double t = Motor.Rotor.getTorqueInAns(femm);

            PMMMAnalysisOneStepResult result = new PMMMAnalysisOneStepResult();
            result.Iabc = new Fabc { a = cps["A"].current, b = cps["B"].current, c = cps["C"].current };
            result.FluxLinkage_abc = new Fabc { a = cps["A"].fluxlinkage, b = cps["B"].fluxlinkage, c = cps["C"].fluxlinkage };
            result.Vabc = new Fabc { a = cps["A"].volts, b = cps["B"].volts, c = cps["C"].volts };

            // no currents, then save the fluxlinkageM, which is importance to calculate Ld
            var Results = this.Results as PM_MMAnalysisResults;

            result.torque = t;
            result.FluxLinkage_M = Results.FluxLinkageM;
            result.theta_e = (RotorAngle - stator.VectorMMFAngle) * Motor.Rotor.p * Math.PI / 180;//in radian

            Results[step] = result;

            femm.mo_close();
        }

        public static PM_MMAnalyser GetSampleMe(AbstractMotor motor)
        {
            PM_MMAnalyser mma = new PM_MMAnalyser();

            mma.AnalysisName = "MMAnalysis";
            mma.Motor = motor;
            mma.Beta = 135;
            mma.MaxCurrent = 160;
            mma.RotorAngle = 40;
            mma.StepCount = 10;

            return mma;
        }

        private String DefaultOriginalSimulinkModelFile = @"E:\MatlabProject\zAspirant\pm\m1_test.mdl";

        public override void MakeMatlabSimulinkModelFile(string outputfile, string original = "")
        {
            if (original == "")
                original = DefaultOriginalSimulinkModelFile;

            String id_values = "[";
            String psid_values = "[";
            var results = Results as PM_MMAnalysisResults;
            var sortedResultsById = results.ListResults.OrderBy(r => r.FluxLinkage_dq.d).ToArray();
            foreach (var item in sortedResultsById)
            {
                id_values += item.Idq.d + " ";
                psid_values += item.FluxLinkage_dq.d + " ";
            }

            id_values += "]";
            psid_values += "]";

            String iq_values = "[";
            String psiq_values = "[";
            var sortedResultsByIq = results.ListResults.OrderBy(r => r.FluxLinkage_dq.q).ToArray();
            foreach (var item in sortedResultsByIq)
            {
                iq_values += item.Idq.q + " ";
                psiq_values += item.FluxLinkage_dq.q + " ";
            }

            iq_values += "]";
            psiq_values += "]";

            MATLAB ml = MATLAB.DefaultInstance;
            String pathtomodel = Path.GetDirectoryName(original);
            String modelname = Path.GetFileNameWithoutExtension(original);
            String pathtoblock = modelname + "/PMSM";
            ml.ChangeWorkingFolder(pathtomodel);
            ml.load_system(modelname);

            // set values
            ml.set_param(pathtoblock, "id_values", id_values);
            ml.set_param(pathtoblock, "iq_values", iq_values);
            ml.set_param(pathtoblock, "psid_values", psid_values);
            ml.set_param(pathtoblock, "psiq_values", psiq_values);

            ml.set_param(pathtoblock, "R", results.PhaseResistance.ToString());
            ml.set_param(pathtoblock, "pp", results.Analyser.Motor.Rotor.p.ToString());

            String outputpath = Path.GetDirectoryName(outputfile);
            String outputname = Path.GetFileNameWithoutExtension(outputfile);
            ml.ChangeWorkingFolder(outputpath);
            ml.save_system(modelname, outputname);//save as new name
            ml.close_system(outputname);//close it
        }

    }//end of class MMAnalysis

    public class PMMMAnalysisOneStepResult
    {
        internal double theta_e { get; set; }

        internal double FluxLinkage_M { get; set; }

        public double torque { get; set; }
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

    public class PM_MMAnalysisResults : AbstractFEMResults
    {
        //Stored data
        public PMMMAnalysisOneStepResult[] ListResults { get; set; }

        private double fluxlinkagem = double.NaN;
        public double FluxLinkageM
        {
            get
            {
                // if available, then return
                if (!double.IsNaN(fluxlinkagem))
                    return fluxlinkagem;
                if (Analyser == null)
                    return 0;

                fluxlinkagem = 0;//first value                
                AbstractMotor motor = Analyser.Motor;
                if (motor != null)
                {
                    var sa = motor.GetStaticAnalyser();
                    if (sa != null)
                    {
                        var results = sa.Results as PMStaticResults;
                        if (results != null)
                            fluxlinkagem = results.psiM;
                    }
                }

                return fluxlinkagem;
            }
        }

        public double PhaseResistance
        {
            get
            {
                // consider also part of wire outside of slot
                AbstractMotor motor = Analyser.Motor;
                if (motor != null)
                {
                    Stator3Phase stator = motor.Stator as Stator3Phase;
                    if (stator != null)
                        return stator.resistancePhase;
                    else
                    {
                        foreach (var item in ListResults)
                        {
                            if (item.Iabc.a != 0)
                                return item.Vabc.a / item.Iabc.a;
                        }
                    }
                }
                return 0;
            }
        }

        public int Count { get { return ListResults.Length; } }

        public PM_MMAnalysisResults(int count)
        {
            ListResults = new PMMMAnalysisOneStepResult[count];
        }

        // for json import
        public PM_MMAnalysisResults()
        {
        }

        public PMMMAnalysisOneStepResult this[int i]
        {
            get
            {
                return ListResults[i];
            }

            internal set
            {
                ListResults[i] = value;
            }
        }

        /// <summary>
        /// Process data after load
        /// </summary>
        public override void ProcessDataAfterLoad()
        {
            PM_MMAnalyser analyser = this.Analyser as PM_MMAnalyser;
            if (analyser == null)
                return;

            // assure fluxlinkage M, and theta_e            
            double VectorMMFAngle = (Analyser.Motor.Stator as Stator3Phase).VectorMMFAngle;
            foreach (PMMMAnalysisOneStepResult oneresult in ListResults)
            {
                // angle between rotor and vector mms A
                oneresult.theta_e = (analyser.RotorAngle - VectorMMFAngle) * analyser.Motor.Rotor.p * Math.PI / 180;//in radian
                oneresult.FluxLinkage_M = FluxLinkageM;
            }
        }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            IDictionary<string, object> dict = base.BuildResultsForDisplay();

            PM_MMAnalyser analyser = this.Analyser as PM_MMAnalyser;
            int pp = analyser.Motor.Rotor.p;

            // to open fem file            
            dict.Add("OpenResults", analyser.Path_ToAnalysisVariant);


            dict.Add("psiD(Id)", new ListPointD(ListResults.Select(p => p.Idq.d).ToArray(), ListResults.Select(p => p.FluxLinkage_dq.d).ToArray()));
            dict.Add("psiQ(Iq)", new ListPointD(ListResults.Select(p => p.Idq.q).ToArray(), ListResults.Select(p => p.FluxLinkage_dq.q).ToArray()));
            dict.Add("Ld(I)", new ListPointD(ListResults.Where(p => p.Idq.d < 0).Select(p => p.Idq.Magnitude).ToArray(),
                                              ListResults.Where(p => p.Idq.d < 0).Select(p => p.Ldq.d).ToArray()));
            dict.Add("Lq(I)", new ListPointD(ListResults.Where(p => p.Idq.d < 0).Select(p => p.Idq.Magnitude).ToArray(),
                                              ListResults.Where(p => p.Idq.d < 0).Select(p => p.Ldq.q).ToArray()));
            dict.Add("psiM/Ld/Imax", new ListPointD(ListResults.Select(p => p.Idq.Magnitude).ToArray(), ListResults.Select(p => p.FluxLinkage_M / p.Ldq.d / p.Idq.Magnitude).ToArray()));
            dict.Add("FluxLinkage(M)-mm", FluxLinkageM);
            dict.Add("PhaseResistance", PhaseResistance);

            double[] psid_ = new double[Count];
            double[] psiq_ = new double[Count];
            for (int i = 1; i < Count; i++)
            {
                psid_[i] = (ListResults[i].FluxLinkage_dq.d - ListResults[i - 1].FluxLinkage_dq.d) /
                    (ListResults[i].Idq.d - ListResults[i - 1].Idq.d);

                psiq_[i] = (ListResults[i].FluxLinkage_dq.q - ListResults[i - 1].FluxLinkage_dq.q) /
                    (ListResults[i].Idq.q - ListResults[i - 1].Idq.q);
            }
            dict.Add("PsiD'(id)", new ListPointD(ListResults.Select(p => p.Idq.d).ToArray(), psid_));
            dict.Add("PsiQ'(iq)", new ListPointD(ListResults.Select(p => p.Idq.q).ToArray(), psiq_));


            //LinearSpline ls_psi_d = LinearSpline.Interpolate(ListResults.Select(p => p.Idq.d), ListResults.Select(p => p.FluxLinkage_dq.d));
            //LinearSpline ls_psi_q = LinearSpline.Interpolate(ListResults.Select(p => p.Idq.q), ListResults.Select(p => p.FluxLinkage_dq.q));
            // Build torque by angle for each
            for (int i = 0; i < ListResults.Length; i++)
            {
                var one_step_result = ListResults[i];

                // only quarter where id<0,iq>0
                if (one_step_result.Idq.d >= 0 || one_step_result.Idq.q <= 0)
                    continue;

                double Ld = one_step_result.Ldq.d;
                double Lq = one_step_result.Ldq.q;
                double II = one_step_result.Idq.Magnitude;
                double Beta0 = one_step_result.Idq.Phase;
                double psiM = one_step_result.FluxLinkage_M;

                double d_wire = (Analyser.Motor.Stator as Stator3Phase).WireDiameter;
                double S_wire = d_wire * d_wire / 4 * Math.PI;

                String name = String.Format("Torque (Imax={0:F2}, J={1:F2})", II, II / S_wire / Math.Sqrt(2));
                if (dict.ContainsKey(name))
                    continue;

                int n = 360;
                double[] x = Enumerable.Range(0, n).Select(kk => 1.0 * kk).ToArray();
                double[] tt = new double[n];
                for (int k = 0; k < n; k++)
                {
                    double beta = 2.0 * Math.PI * k / n;
                    double id = II * Math.Cos(beta);
                    double iq = II * Math.Sin(beta);

                    tt[k] = 1.5 * pp * (psiM * iq + (Ld - Lq) * id * iq);

                    //double psid = ls_psi_d.Interpolate(id);
                    //double psiq = ls_psi_q.Interpolate(iq);
                    //tt[k] = 1.5 * pp * (psid * iq - psiq * id);
                }

                object value = new ListPointD(x, tt);
                dict.Add(name, value);
            }

            IEnumerable<int> seq = Enumerable.Range(0, Count);
            var arrI = ListResults.Where(p => p.Idq.d < 0).Select(p => p.Idq.Magnitude).ToArray();
            var arrL1 = seq.Where(i => ListResults[i].Idq.d < 0).Select(i => (ListResults[i].Ldq.d + ListResults[i].Ldq.q) / 3).ToArray();
            var arrL2 = seq.Where(i => ListResults[i].Idq.d < 0).Select(i => -(ListResults[i].Ldq.d - ListResults[i].Ldq.q) / 3).ToArray();
            dict.Add("L1(I)", new ListPointD(arrI, arrL1));
            dict.Add("L2(I)", new ListPointD(arrI, arrL2));


            int Q = Analyser.Motor.Stator.Q;
            int q = Q / 3 / pp / 2;
            double kp = Math.Sin(Math.PI / (2 * 3)) / (q * Math.Sin(Math.PI / (2 * 3 * q)));
            var stator = Analyser.Motor.Stator as Stator3Phase;
            double ns = 4 / Math.PI * kp * stator.NStrands * q * pp;
            var Motor = Analyser.Motor;
            var rotor = Motor.Rotor as VPMRotor;

            double gm = rotor.gammaMerad;
            double[] arr_dmin = new double[Count];
            double[] arr_dmax = new double[Count];
            for (int i = 0; i < arrL1.Length; i++)
            {
                double mL1 = arrL1[i];
                double mL2 = arrL2[i];
                double a1 = mL1 / ((ns / 2 / pp) * (ns / 2 / pp) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * 4 * Math.PI * 1e-7);
                double a2 = mL2 / (0.5 * (ns / 2 / pp) * (ns / 2 / pp) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * 4 * Math.PI * 1e-7);

                arr_dmin[i] = 1 / (a1 + a2 * gm / (2 * Math.Sin(gm)));
                arr_dmax[i] = 1 / (a1 - a2 * (Math.PI - gm) / (2 * Math.Sin(gm)));
            }

            dict.Add("dmin(I)", new ListPointD(arrI, arr_dmin));
            dict.Add("dmax(I)", new ListPointD(arrI, arr_dmax));

            return dict;
        }
    }
}
