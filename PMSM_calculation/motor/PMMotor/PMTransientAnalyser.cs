using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using fastJSON;
using MathNet.Numerics.LinearAlgebra;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PMTransientAnalyser : Transient3PhaseMotorAnalyser
    {
        protected override AbstractFEMResults NewResults()
        {
            return new PMTransientResults(this);
        }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            var r = (PMTransientResults)AbstractFEMResults.loadResultsFromFile(typeof(PMTransientResults), fromfile);

            return r;
        }

        new public static PMTransientAnalyser GetSampleMe(AbstractMotor motor)
        {
            PMTransientAnalyser ta = new PMTransientAnalyser();

            ta.AnalysisName = "Transient\\sample";
            ta.Motor = motor;
            ta.RotorSpeed = -3000;//rpm
            ta.EndTime = Math.Abs(120 / ta.RotorSpeedDegreeSecond);
            ta.StepCount = 120;
            ta.StartAngle = 40;
            ta.IA = "80";
            ta.IB = "-40";
            ta.IC = "-40";

            return ta;
        }

        private List<FEMM.Element> allElements;

        private CoreLoss statorLoss;
        private CoreLoss rotorLoss;

        // object to sync the creation of List of Elements
        // other thread will wait until this list created
        private object lock_first = new object();

        protected override void DoMeasureData(TransientStepArgs args, FEMM femm)
        {
            // measure base data
            base.DoMeasureData(args, femm);

            // measure only one, not all those for skew angle
            if (args.skewAngleAdded != 0)
                return;

            // measure loss
            // other processes need to wait for the first to finish this block of code
            lock (lock_first)
            {
                if (allElements == null)
                {
                    Stator3Phase stator = Motor.Stator as Stator3Phase;
                    int rotor_steel_group = -1;
                    int rotor_magnet_group = -1;
                    double rotor_Keddy = 0;
                    double rotor_Kh = 0;
                    double rotor_ro = 0;
                    if (Motor.Rotor is SPMRotor)
                    {
                        var rotor = Motor.Rotor as SPMRotor;
                        rotor_steel_group = rotor.Group_BlockLabel_Steel;
                        rotor_magnet_group = rotor.Group_BlockLabel_Magnet_Air;
                        rotor_Keddy = rotor.P_eddy_10_50;
                        rotor_Kh = rotor.P_hysteresis_10_50;
                        rotor_ro = rotor.Steel_ro;
                    }
                    else if (Motor.Rotor is VPMRotor)
                    {
                        var rotor = Motor.Rotor as VPMRotor;
                        rotor_steel_group = rotor.Group_BlockLabel_Steel;
                        rotor_magnet_group = rotor.Group_BlockLabel_Magnet_Air;
                        rotor_Keddy = rotor.P_eddy_10_50;
                        rotor_Kh = rotor.P_hysteresis_10_50;
                        rotor_ro = rotor.Steel_ro;
                    }

                    List<PointD> nodes = new List<PointD>();
                    int n = femm.mo_numnodes();
                    for (int i = 1; i <= n; i++)
                    {
                        var p = femm.mo_getnode(i);
                        nodes.Add(p);
                    }

                    allElements = new List<FEMM.Element>();
                    n = femm.mo_numelements();
                    for (int i = 1; i <= n; i++) // start from 1
                    {
                        var e = femm.mo_getelement(i);
                        for (int j = 0; j < e.nodes.Length; j++)
                            e.nodes[j]--;//convert from 1-base index to 0-base index
                        allElements.Add(e);
                    }

                    var statorElements = allElements.Where(e => e.group == stator.Group_BlockLabel_Steel).ToList();

                    statorLoss = new CoreLoss(this, nodes, statorElements);
                    statorLoss.name = "Stator_";
                    statorLoss.ro = stator.Steel_ro;
                    statorLoss.Keddy = stator.P_eddy_10_50;
                    statorLoss.Kh = stator.P_hysteresis_10_50;

                    var rotorElements = allElements.Where(e => e.group == rotor_steel_group || e.group == rotor_magnet_group).ToList();

                    // convert coordinates of elements back to 0 degree rotor angle
                    // hashset of node to mark rotated node
                    HashSet<int> rotatedNodes = new HashSet<int>();
                    // angle to rotate back
                    double a = Motor.GetNormalizedRotorAngle(args.RotorAngle) * Math.PI / 180;
                    // for each element
                    foreach (var e in rotorElements)
                    {
                        double xx = e.center.X;
                        double yy = e.center.Y;
                        e.center.X = xx * Math.Cos(-a) - yy * Math.Sin(-a);
                        e.center.Y = xx * Math.Sin(-a) + yy * Math.Cos(-a);

                        // rotate nodes of this element also
                        for (int i = 0; i < e.nodes.Length; i++)
                        {
                            int node_index = e.nodes[i];

                            // if already rotated
                            if (rotatedNodes.Contains(node_index))
                                continue;

                            xx = nodes[node_index].X;
                            yy = nodes[node_index].Y;
                            nodes[node_index] = new PointD()
                            {
                                X = xx * Math.Cos(-a) - yy * Math.Sin(-a),
                                Y = xx * Math.Sin(-a) + yy * Math.Cos(-a),
                            };

                            // mark as rotated
                            rotatedNodes.Add(node_index);
                        }
                    }

                    rotorLoss = new CoreLoss(this, nodes, rotorElements);
                    rotorLoss.name = "Rotor_";
                    rotorLoss.isRotor = true;
                    rotorLoss.Keddy = rotor_Keddy;
                    rotorLoss.Kh = rotor_Kh;
                    rotorLoss.ro = rotor_ro;
                }
            }// lock(..)

            statorLoss.GatherData(args, femm);
            rotorLoss.GatherData(args, femm);
        }

        protected override void OnFinishAnalysis()
        {
            //TODO: need a post-process method
            PMTransientResults Results = this.Results as PMTransientResults;

            // coreloss
            statorLoss.calculateCoreLoss();
            Results.StatorCoreLoss = statorLoss.Results.TotalCoreLoss;
            Results.StatorLossHysteresis = statorLoss.Results.LossHysteresis;
            Results.StatorLossEddy = statorLoss.Results.LossEddy;
            Results.StatorCoreLossResults = statorLoss.Results;

            rotorLoss.calculateCoreLoss();
            Results.RotorCoreLoss = rotorLoss.Results.TotalCoreLoss;
            Results.RotorLossHysteresis = rotorLoss.Results.LossHysteresis;
            Results.RotorLossEddy = rotorLoss.Results.LossEddy;
            Results.RotorCoreLossResults = rotorLoss.Results;

            allElements = null;//so that the next time run it is new 
        }

        protected override void SaveResultsToDisk()
        {
            statorLoss.Results.SaveDataAsBinary(Path_ToAnalysisVariant + "\\stator_coreloss.dat");
            rotorLoss.Results.SaveDataAsBinary(Path_ToAnalysisVariant + "\\rotor_coreloss.dat");

            base.SaveResultsToDisk();
        }
    }

    public class PMTransientResults : Transient3PhaseMotorResults
    {
        public PMTransientResults(AbstractTransientAnalyser analyser)
            : base(analyser)
        {
        }

        // for json import
        public PMTransientResults()
            : base()
        {
        }

        #region Stored data

        public double RotorCoreLoss { get; set; }
        public double RotorLossHysteresis { get; set; }
        public double RotorLossEddy { get; set; }
        public double StatorCoreLoss { get; set; }
        public double StatorLossHysteresis { get; set; }
        public double StatorLossEddy { get; set; }

        private CoreLossResults _stator_coreloss_results = null;
        [JsonIgnore]
        public CoreLossResults StatorCoreLossResults
        {
            get
            {
                if (_stator_coreloss_results == null)
                {
                    string fn = Analyser.Path_ToAnalysisVariant + "\\stator_coreloss.dat";
                    if (File.Exists(fn))
                        _stator_coreloss_results = CoreLossResults.LoadFromBinaryData(fn);
                }

                return _stator_coreloss_results;
            }
            set
            {
                _stator_coreloss_results = value;
            }
        }

        private CoreLossResults _rotor_coreloss_results = null;
        [JsonIgnore]
        public CoreLossResults RotorCoreLossResults
        {
            get
            {
                if (_rotor_coreloss_results == null)
                {
                    string fn = Analyser.Path_ToAnalysisVariant + "\\rotor_coreloss.dat";
                    if (File.Exists(fn))
                        _rotor_coreloss_results = CoreLossResults.LoadFromBinaryData(fn);
                }

                return _rotor_coreloss_results;
            }
            set
            {
                _rotor_coreloss_results = value;
            }
        }

        #endregion

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

        public void setFluxLinkageM(double psiM)
        {
            fluxlinkagem = psiM;
        }

        /// <summary>
        /// Generate results for display
        /// </summary>
        /// <returns></returns>
        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            IDictionary<string, object> dict = base.BuildResultsForDisplay();

            dict.Add("FluxLinkageM", FluxLinkageM);
            dict.Add("Ld(t)", new ListPointD(Times, Inductance_D()));
            dict.Add("Lq(t)", new ListPointD(Times, Inductance_Q()));

            double[] psid = FluxLinkage_D();
            double[] psiq = FluxLinkage_Q();
            double[] id = Current_D();
            double[] iq = Current_Q();
            double[] psid_ = new double[Count];
            double[] psiq_ = new double[Count];
            for (int i = 1; i < Count; i++)
            {
                psid_[i] = (psid[i] - psid[i - 1]) / (id[i] - id[i - 1]);

                psiq_[i] = (psiq[i] - psiq[i - 1]) / (iq[i] - iq[i - 1]);
            }
            dict.Add("PsiD'(id)", new ListPointD(id, psid_));
            dict.Add("PsiQ'(iq)", new ListPointD(iq, psiq_));

            // efficiency            
            double power = Torques.Average() * (RotorAngles[1] - RotorAngles[0]) / (Times[1] - Times[0]) * Math.PI / 180;
            dict.Add("AvgOutputPower", power);
            double loss = RotorCoreLoss + StatorCoreLoss;
            if (dict.ContainsKey("WindingLoss"))
                loss += (double)dict["WindingLoss"];
            double eff = 100.0 * power / (loss + power);
            dict.Add("Efficiency", eff);

            // coreloss
            dict.Add("RotorCoreLoss", RotorCoreLoss);
            dict.Add("RotorLossHysteresis", RotorLossHysteresis);
            dict.Add("RotorLossEddy", RotorLossEddy);

            dict.Add("StatorCoreLoss", StatorCoreLoss);
            dict.Add("StatorLossHysteresis", StatorLossHysteresis);
            dict.Add("StatorLossEddy", StatorLossEddy);

            dict.Add("DetailsCoreLoss", new List<CoreLossResults>(new CoreLossResults[] { StatorCoreLossResults, RotorCoreLossResults }));

            // L1,L2,psiM 
            //double[] psiAs = FluxLinkageOf("A");
            //double[] psiBs = FluxLinkageOf("B");
            //double[] psiCs = FluxLinkageOf("C");
            //double[] iAs = CurrentOf("A");
            //double[] iBs = CurrentOf("B");
            //double[] iCs = CurrentOf("C");
            //var stator = Analyser.Motor.Stator as Stator3Phase;

            //double pi = Math.PI;
            //Func<double, double> sin = x => Math.Sin(x);
            //Func<double, double> cos = x => Math.Cos(x);
            //List<Vector<double>> xs = new List<Vector<double>>();
            //for (int i = 0; i < Count; i++)
            //{
            //    double theta_e = (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;
            //    Matrix<double> A = Matrix<double>.Build.Dense(3, 3);
            //    Vector<double> v = Vector<double>.Build.Dense(3);
            //    A[0, 0] = 3.0 / 2 * iAs[i];
            //    A[0, 1] = -(iAs[i] * cos(2 * theta_e) + iBs[i] * cos(2 * (theta_e - pi / 3)) + iCs[i] * cos(2 * (theta_e + pi / 3)));
            //    A[0, 2] = cos(theta_e);
            //    A[1, 0] = 3.0 / 2 * iBs[i];
            //    A[1, 1] = -(iAs[i] * cos(2 * (theta_e - pi / 3)) + iBs[i] * cos(2 * (theta_e - 2 * pi / 3)) + iCs[i] * cos(2 * (theta_e + pi)));
            //    A[1, 2] = cos(theta_e - 2 * pi / 3);
            //    A[2, 0] = 3.0 / 2 * iCs[i];
            //    A[2, 1] = -(iAs[i] * cos(2 * (theta_e + pi / 3)) + iBs[i] * cos(2 * (theta_e + pi)) + iCs[i] * cos(2 * (theta_e + 2 * pi / 3)));
            //    A[2, 2] = cos(theta_e + 2 * pi / 3);
            //    //v[0] = 0.000947;//L1
            //    //v[1] = 0.000347;//L2
            //    //v[2] = 0.08504;//psiM
            //    v[0] = psiAs[i];
            //    v[1] = psiBs[i];
            //    v[2] = 1 - (psiAs[i] + psiBs[i]);
            //    try
            //    {
            //        Vector<double> x = A.Inverse().Multiply(v);
            //        xs.Add(x);
            //    }
            //    catch (Exception ex)
            //    {
            //        xs.Add(Vector<double>.Build.Dense(3));
            //    }
            //}

            double[] Ld = Inductance_D();
            double[] Lq = Inductance_Q();
            IEnumerable<int> seq = Enumerable.Range(0, Count);
            double[] L1 = seq.Select(i => (Ld[i] + Lq[i]) / 3.0).ToArray();
            double[] L2 = seq.Select(i => -(Ld[i] - Lq[i]) / 3.0).ToArray();
            var stator = Analyser.Motor.Stator as Stator3Phase;
            Func<int, double> theta_e = i => (ListResults[i].RotorAngle - stator.VectorMMFAngle) * Analyser.Motor.Rotor.p * Math.PI / 180;

            double[] LA = seq.Select(i => L1[i] - L2[i] * Math.Cos(2 * theta_e(i))).ToArray();
            double[] LB = seq.Select(i => L1[i] - L2[i] * Math.Cos(2 * (theta_e(i) - 2 * Math.PI / 3))).ToArray();
            double[] LC = seq.Select(i => L1[i] - L2[i] * Math.Cos(2 * (theta_e(i) + 2 * Math.PI / 3))).ToArray();
            double[] MAB = seq.Select(i => -0.5 * L1[i] - L2[i] * Math.Cos(2 * (theta_e(i) - Math.PI / 3))).ToArray();
            double[] MAC = seq.Select(i => -0.5 * L1[i] - L2[i] * Math.Cos(2 * (theta_e(i) + Math.PI / 3))).ToArray();
            double[] MBC = seq.Select(i => -0.5 * L1[i] - L2[i] * Math.Cos(2 * (theta_e(i) + Math.PI))).ToArray();

            double psiM = FluxLinkageM;
            double[] iAs = CurrentOf("A");
            double[] iBs = CurrentOf("B");
            double[] iCs = CurrentOf("C");
            double[] psiAs = seq.Select(i => LA[i] * iAs[i] + MAB[i] * iBs[i] + MAC[i] * iCs[i]).ToArray();// + psiM * Math.Cos(theta_e(i))).ToArray();
            double[] psiBs = seq.Select(i => MAB[i] * iAs[i] + LB[i] * iBs[i] + MBC[i] * iCs[i]).ToArray();//  + psiM * Math.Cos(theta_e(i) - 2 * Math.PI / 3)).ToArray();
            double[] psiCs = seq.Select(i => MAC[i] * iAs[i] + MBC[i] * iBs[i] + LC[i] * iCs[i]).ToArray();//  + psiM * Math.Cos(theta_e(i) + 2 * Math.PI / 3)).ToArray();

            dict.Add("L1", new ListPointD(Times, L1));
            dict.Add("L2", new ListPointD(Times, L2));
            //dict.Add("psiC (abc)", new ListPointD(Times, xs.Select(v => v[2]).ToArray()));
            dict.Add("LA", new ListPointD(Times, LA));
            dict.Add("LB", new ListPointD(Times, LB));
            dict.Add("LC", new ListPointD(Times, LC));

            dict.Add("MAB", new ListPointD(Times, MAB));
            dict.Add("MAC", new ListPointD(Times, MAC));
            dict.Add("MBC", new ListPointD(Times, MBC));

            dict.Add("psiA (xx)", new ListPointD(Times, psiAs));
            dict.Add("psiB (xx)", new ListPointD(Times, psiBs));
            dict.Add("psiC (xx)", new ListPointD(Times, psiCs));

            double mL1 = L1.Average();
            double mL2 = L2.Average();

            int Q = Analyser.Motor.Stator.Q;
            int p = Analyser.Motor.Rotor.p;
            int q = Q / 3 / p / 2;
            double kp = Math.Sin(Math.PI / (2 * 3)) / (q * Math.Sin(Math.PI / (2 * 3 * q)));
            double ns = 4 / Math.PI * kp * stator.NStrands * q * p;
            var Motor = Analyser.Motor;

            double a1 = mL1 / ((ns / 2 / p) * (ns / 2 / p) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * 4 * Math.PI * 1e-7);
            double a2 = mL2 / (0.5 * (ns / 2 / p) * (ns / 2 / p) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * 4 * Math.PI * 1e-7);

            var rotor = Motor.Rotor as VPMRotor;
            double gm = rotor.gammaMerad;
            double dmin = 1 / (a1 + a2 * gm / (2 * Math.Sin(gm)));
            double dmax = 1 / (a1 - a2 * (Math.PI - gm) / (2 * Math.Sin(gm)));

            dict.Add("dmin", dmin);
            dict.Add("dmax", dmax);

            return dict;
        }

        #region Inductance calculation (Fluxlinkage M involved)

        private Fdq[] inductanceDQ;

        public Fdq[] InductanceDQ()
        {
            if (inductanceDQ != null)
                return inductanceDQ;

            Fdq[] psidq = FluxLinkageDQ();
            Fdq[] Idq = CurrentDQ();
            if (psidq == null || Idq == null)
                return null;

            // calc now
            inductanceDQ = new Fdq[Idq.Length];
            double psiM = FluxLinkageM;
            for (int i = 0; i < Idq.Length; i++)
            {
                inductanceDQ[i].d = (psidq[i].d - psiM) / Idq[i].d;
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
