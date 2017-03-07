using btl.generic;
using calc_from_geometryOfMotor.motor;
using calc_from_geometryOfMotor.motor.PMMotor;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using calc_from_geometryOfMotor.RootFinding;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Numerics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using MathNet.Numerics.Optimization;
using System.IO;

namespace calc_from_geometryOfMotor
{
    public partial class EfficiencyMapViewer : Form
    {
        private DQCurrentMap map;
        private PM_DQCurrentAnalyser analyser;
        private double psiM;
        private double resistance;
        private double Ld_approx;//rough
        private double Lq_approx;
        private AbstractTransientAnalyser dummyTransientAnalyser;

        private const int SPEED_POINT_COUNT = 20;
        private const int TORQUE_POINT_COUNT = 20;

        private MTPA mtpa;

        public EfficiencyMapViewer()
        {
            InitializeComponent();
        }

        private void EfficiencyMapViewer_Load(object sender, EventArgs e)
        {
            comboBox_maptype.Items.AddRange(typeof(MapType).GetEnumNames());
        }

        #region Public methods (input)

        public void setData(DQCurrentMap data)
        {
            this.map = data;
            analyser = map.Analyser as PM_DQCurrentAnalyser;

            // get psiM for calculation
            AbstractMotor motor = analyser.Motor;
            if (motor != null)
            {
                var sa = motor.GetStaticAnalyser();
                if (sa != null)
                {
                    var results = sa.Results as PMStaticResults;
                    if (results != null)
                    {
                        psiM = results.psiM;
                    }
                }

                var stator = motor.Stator as Stator3Phase;
                if (stator != null)
                {
                    resistance = stator.resistancePhase;
                }
            }

            ProjectManager pm = ProjectManager.GetInstance();
            dummyTransientAnalyser = pm.GetDefaultTransientAnalyser();

            mtpa = buildTableMaxtorquePerAmple();

            var d = interpolateData(analyser.MaxCurrent * Math.Cos(2 * Math.PI / 3), analyser.MaxCurrent * Math.Sin(2 * Math.PI / 3), 3000);
            Ld_approx = d.Ld;
            Lq_approx = d.Lq;
        }

        #endregion

        #region Processing DQ Current Data

        private class APointData
        {
            public double id;
            public double iq;
            public double speed;
            public double torque;//0-
            public double cogging;
            public double rotorloss;
            public double statorloss;
            public double copperloss;
            public PMTransientResults result;

            public double ed;//1-harmonic
            public double eq;//1-harmonic     
            public double ud;
            public double uq;

            public double Ld;
            public double Lq;

            public double power
            {
                get
                {
                    //return 1.5 * (ed * id + eq * iq);
                    var p = torque * speed / 60 * 2 * Math.PI;
                    return p < 0 ? 0 : p;
                }
            }

            public double voltage
            {
                get
                {
                    return Math.Sqrt(ud * ud + uq * uq);
                }
            }
            public double current
            {
                get
                {
                    return Math.Sqrt(id * id + iq * iq);
                }
            }
            public double beta
            {
                get
                {
                    if (id == 0)
                        return iq > 0 ? 90 : (iq == 0 ? 0 : -90);

                    double b = Math.Atan(iq / id) * 180 / Math.PI;
                    if (b < 0)
                        b += 180;
                    return b;
                }
            }

            public double efficiency
            {
                get
                {
                    return 100.0 * power / (power + rotorloss + statorloss + copperloss);
                }
            }
        }

        private APointData interpolateData(double id, double iq, double speed, double Kh = 1, double Ke = 1)
        {
            PMTransientResults result = new PMTransientResults();

            double endtime = 1 / (speed / 60 * analyser.Motor.Rotor.p);
            double[] times = Enumerable.Range(0, analyser.StepCount + 1).Select(i => endtime * i / analyser.StepCount).ToArray();
            double startAngle = map.results[0].transientResult.RotorAngles[0];
            double omega = speed / 60 * 2 * Math.PI * analyser.Motor.Rotor.p;
            double beta = (id != 0) ? Math.Atan(iq / id) : Math.PI / 2;
            if (beta < 0)
                beta = beta + Math.PI;

            result.Times = times;
            result.Analyser = dummyTransientAnalyser;
            dummyTransientAnalyser.EndTime = endtime;
            dummyTransientAnalyser.StartAngle = startAngle;
            dummyTransientAnalyser.StepCount = analyser.StepCount;
            dummyTransientAnalyser.RotorSpeed = speed;
            result.setFluxLinkageM(psiM);//fluxlinkage M for calculation                              

            double II = Math.Sqrt(id * id + iq * iq);
            // for interpolation
            int index_d = (int)Math.Floor(Math.Abs(id) * analyser.CurrentSampleCount / analyser.MaxCurrent);
            int index_q = (int)Math.Floor(Math.Abs(iq) * analyser.CurrentSampleCount / analyser.MaxCurrent);
            double id1 = -analyser.MaxCurrent * index_d / analyser.CurrentSampleCount;
            double id2 = -analyser.MaxCurrent * (index_d + 1) / analyser.CurrentSampleCount;
            double iq1 = analyser.MaxCurrent * index_q / analyser.CurrentSampleCount;
            double iq2 = analyser.MaxCurrent * (index_q + 1) / analyser.CurrentSampleCount;
            double s = analyser.MaxCurrent * analyser.MaxCurrent / (analyser.CurrentSampleCount * analyser.CurrentSampleCount);
            // weights
            double w11 = Math.Abs((id - id1) * (iq - iq1) / s);
            double w12 = Math.Abs((id - id1) * (iq - iq2) / s);
            double w21 = Math.Abs((id - id2) * (iq - iq1) / s);
            double w22 = Math.Abs((id - id2) * (iq - iq2) / s);
            // nearest points
            DQCurrentPointData p11 = null, p12 = null, p21 = null, p22 = null;

            // find nearest points
            for (int i = 0; i < map.results.Count; i++)
            {
                if (map.results[i].index_d == index_d && map.results[i].index_q == index_q)
                    p11 = map.results[i];
                if (map.results[i].index_d == index_d + 1 && map.results[i].index_q == index_q)
                    p21 = map.results[i];
                if (map.results[i].index_d == index_d && map.results[i].index_q == index_q + 1)
                    p12 = map.results[i];
                if (map.results[i].index_d == index_d + 1 && map.results[i].index_q == index_q + 1)
                    p22 = map.results[i];
            }

            if (p22 == null)
                p22 = p11;
            if (p21 == null)
                p21 = p11;
            if (p12 == null)
                p12 = p11;

            if (p11 == null)
                return null;

            // create interpolated results of torques, psiA,psiB,psiC
            List<TransientStepArgs> steps = new List<TransientStepArgs>();
            for (int i = 0; i < map.results[0].transientResult.Count; i++)
            {
                var currents = new Dictionary<string, double>()
                    {
                        {"A", II*Math.Sin(omega*times[i]+beta)},
                        {"B", II*Math.Sin(omega*times[i]+beta-2*Math.PI/3)},
                        {"C", II*Math.Sin(omega*times[i]+beta+2*Math.PI/3)}
                    };

                TransientStepArgs arg = new TransientStepArgs()
                {
                    step = i,
                    time = times[i],
                    RotorAngle = startAngle + times[i] * speed * 6,
                    currents = currents,
                    Torque = w11 * p22.transientResult.ListResults[i].Torque
                            + w12 * p21.transientResult.ListResults[i].Torque
                            + w21 * p12.transientResult.ListResults[i].Torque
                            + w22 * p11.transientResult.ListResults[i].Torque,
                    CircuitProperties = new Dictionary<string, FEMM.CircuitProperties>()
                    {
                        {"A",new FEMM.CircuitProperties()
                        {
                            name = "A",
                            current = currents["A"],
                            volts = resistance*currents["A"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                        } },
                        {"B",new FEMM.CircuitProperties()
                        {
                            name = "B",
                            current = currents["B"],
                            volts = resistance*currents["B"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                        } },
                        {"C",new FEMM.CircuitProperties()
                        {
                            name = "C",
                            current = currents["C"],
                            volts = resistance*currents["C"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                        } },
                    },
                };

                steps.Add(arg);
            }

            result.ListResults = steps.ToArray();

            // coreloss
            result.RotorLossHysteresis = w11 * p22.transientResult.RotorLossHysteresis
                                       + w12 * p21.transientResult.RotorLossHysteresis
                                       + w21 * p12.transientResult.RotorLossHysteresis
                                       + w22 * p11.transientResult.RotorLossHysteresis;
            result.RotorLossEddy = w11 * p22.transientResult.RotorLossEddy
                                       + w12 * p21.transientResult.RotorLossEddy
                                       + w21 * p12.transientResult.RotorLossEddy
                                       + w22 * p11.transientResult.RotorLossEddy;

            result.StatorLossHysteresis = w11 * p22.transientResult.StatorLossHysteresis
                                       + w12 * p21.transientResult.StatorLossHysteresis
                                       + w21 * p12.transientResult.StatorLossHysteresis
                                       + w22 * p11.transientResult.StatorLossHysteresis;
            result.StatorLossEddy = w11 * p22.transientResult.StatorLossEddy
                                       + w12 * p21.transientResult.StatorLossEddy
                                       + w21 * p12.transientResult.StatorLossEddy
                                       + w22 * p11.transientResult.StatorLossEddy;

            //            

            // coreloss + speed correction
            result.RotorLossHysteresis *= Kh * speed / analyser.BaseSpeed;
            result.RotorLossEddy *= Ke * Math.Pow(speed / analyser.BaseSpeed, 2);
            result.StatorLossHysteresis *= Kh * speed / analyser.BaseSpeed;
            result.StatorLossEddy *= Ke * Math.Pow(speed / analyser.BaseSpeed, 2);

            // coreloss total
            result.RotorCoreLoss = result.RotorLossHysteresis + result.RotorLossEddy;
            result.StatorCoreLoss = result.StatorLossHysteresis + result.StatorLossEddy;

            var data = new APointData()
            {
                id = id,
                iq = iq,
                speed = speed,
                torque = result.Torques.Sum() / result.Count,
                cogging = (result.Torques.Max() - result.Torques.Min()) / 2,
                rotorloss = result.RotorCoreLoss,
                statorloss = result.StatorCoreLoss,
                copperloss = 3.0 / 2 * (id * id + iq * iq) * resistance,
                result = result,
                ed = result.InducedVoltage_D().Sum() / result.Count,
                eq = result.InducedVoltage_Q().Sum() / result.Count,
                Ld = result.Inductance_D().Sum() / result.Count,
                Lq = result.Inductance_Q().Sum() / result.Count,
            };

            data.ud = resistance * id + data.ed;
            data.uq = resistance * iq + data.eq;

            p11.transientResult.Analyser = dummyTransientAnalyser;
            var tt = p11.transientResult.InducedVoltageDQ();

            return data;
        }

        private APointData interpolateDataWithSkew2(double id, double iq, double speed, double skewAngle, int Nseg = 5)
        {
            double beta0 = Math.Atan(iq / id);//radian
            if (beta0 <= 0)
                beta0 += Math.PI;
            double II = Math.Sqrt(id * id + iq * iq);
            List<APointData> datas = new List<APointData>();
            double skewAngle_e = skewAngle * analyser.Motor.Rotor.p * Math.PI / 180;//radian
            for (int i = 0; i < 2 * Nseg + 1; i++)
            {
                double theta_skew_k = (i - Nseg) * skewAngle_e / (2 * Nseg);
                double beta = beta0 + theta_skew_k;
                double newid = II * Math.Cos(beta);
                double newiq = II * Math.Sin(beta);
                var d = interpolateData(newid, newiq, speed);

                datas.Add(d);
            }

            var data = interpolateData(id, iq, speed);

            double totalRotatedAngle_degree = 360 / analyser.Motor.Rotor.p;

            for (int j = 0; j < analyser.StepCount + 1; j++)
            {
                // torque in consideration of skew
                double t = 0;
                for (int k = 0; k < 2 * Nseg + 1; k++)
                {
                    double theta_skew = (k - Nseg) * skewAngle / (2 * Nseg);
                    double xk = j + theta_skew / (totalRotatedAngle_degree / analyser.StepCount);
                    double tk = interpolateArrayOfPeriodicData(datas[k].result.Torques, xk);
                    t += tk;
                }

                data.result[j].Torque = t / (2 * Nseg + 1);
            }

            return data;
        }

        private APointData interpolateDataWithSkew(double id, double iq, double speed, double skewangle, int Nseg = 10)
        {
            PMTransientResults result = new PMTransientResults();

            double endtime = 1 / (speed / 60 * analyser.Motor.Rotor.p);
            double[] times = Enumerable.Range(0, analyser.StepCount + 1).Select(i => endtime * i / analyser.StepCount).ToArray();
            double startAngle = map.results[0].transientResult.RotorAngles[0];
            double totalRotatedAngle_degree = 360 / analyser.Motor.Rotor.p;
            double omega = speed / 60 * 2 * Math.PI * analyser.Motor.Rotor.p;
            double beta = (id != 0) ? Math.Atan(iq / id) : Math.PI / 2;
            if (beta < 0)
                beta = beta + Math.PI;

            result.Times = times;
            result.Analyser = dummyTransientAnalyser;
            dummyTransientAnalyser.EndTime = endtime;
            dummyTransientAnalyser.StartAngle = startAngle;
            dummyTransientAnalyser.StepCount = analyser.StepCount;
            dummyTransientAnalyser.RotorSpeed = speed;
            result.setFluxLinkageM(psiM);//fluxlinkage M for calculation                              

            double II = Math.Sqrt(id * id + iq * iq);
            // for interpolation
            int index_d = (int)Math.Floor(Math.Abs(id) * analyser.CurrentSampleCount / analyser.MaxCurrent);
            int index_q = (int)Math.Floor(Math.Abs(iq) * analyser.CurrentSampleCount / analyser.MaxCurrent);
            double id1 = -analyser.MaxCurrent * index_d / analyser.CurrentSampleCount;
            double id2 = -analyser.MaxCurrent * (index_d + 1) / analyser.CurrentSampleCount;
            double iq1 = analyser.MaxCurrent * index_q / analyser.CurrentSampleCount;
            double iq2 = analyser.MaxCurrent * (index_q + 1) / analyser.CurrentSampleCount;
            double s = analyser.MaxCurrent * analyser.MaxCurrent / (analyser.CurrentSampleCount * analyser.CurrentSampleCount);
            // weights
            double w11 = Math.Abs((id - id1) * (iq - iq1) / s);
            double w12 = Math.Abs((id - id1) * (iq - iq2) / s);
            double w21 = Math.Abs((id - id2) * (iq - iq1) / s);
            double w22 = Math.Abs((id - id2) * (iq - iq2) / s);
            // nearest points
            DQCurrentPointData p11 = null, p12 = null, p21 = null, p22 = null;

            // find nearest points
            for (int i = 0; i < map.results.Count; i++)
            {
                if (map.results[i].index_d == index_d && map.results[i].index_q == index_q)
                    p11 = map.results[i];
                if (map.results[i].index_d == index_d + 1 && map.results[i].index_q == index_q)
                    p21 = map.results[i];
                if (map.results[i].index_d == index_d && map.results[i].index_q == index_q + 1)
                    p12 = map.results[i];
                if (map.results[i].index_d == index_d + 1 && map.results[i].index_q == index_q + 1)
                    p22 = map.results[i];
            }

            if (p22 == null)
                p22 = p11;
            if (p21 == null)
                p21 = p11;
            if (p12 == null)
                p12 = p11;

            if (p11 == null)
                return null;

            var t11array = p11.transientResult.ListResults.Select(r => r.Torque);
            var t12array = p12.transientResult.ListResults.Select(r => r.Torque);
            var t21array = p21.transientResult.ListResults.Select(r => r.Torque);
            var t22array = p22.transientResult.ListResults.Select(r => r.Torque);

            // create interpolated results of torques, psiA,psiB,psiC
            List<TransientStepArgs> steps = new List<TransientStepArgs>();
            for (int i = 0; i < map.results[0].transientResult.Count; i++)
            {
                var currents = new Dictionary<string, double>()
                    {
                        {"A", II*Math.Sin(omega*times[i]+beta)},
                        {"B", II*Math.Sin(omega*times[i]+beta-2*Math.PI/3)},
                        {"C", II*Math.Sin(omega*times[i]+beta+2*Math.PI/3)}
                    };

                TransientStepArgs arg = new TransientStepArgs()
                {
                    step = i,
                    time = times[i],
                    RotorAngle = startAngle + times[i] * speed * 6,
                    currents = currents,
                    //Torque = w11 * p22.transientResult.ListResults[i].Torque
                    //        + w12 * p21.transientResult.ListResults[i].Torque
                    //        + w21 * p12.transientResult.ListResults[i].Torque
                    //        + w22 * p11.transientResult.ListResults[i].Torque,                    
                    CircuitProperties = new Dictionary<string, FEMM.CircuitProperties>()
                    {
                        {"A",new FEMM.CircuitProperties()
                        {
                            name = "A",
                            current = currents["A"],
                            volts = resistance*currents["A"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["A"].fluxlinkage
                        } },
                        {"B",new FEMM.CircuitProperties()
                        {
                            name = "B",
                            current = currents["B"],
                            volts = resistance*currents["B"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["B"].fluxlinkage
                        } },
                        {"C",new FEMM.CircuitProperties()
                        {
                            name = "C",
                            current = currents["C"],
                            volts = resistance*currents["C"],
                            fluxlinkage = w11 * p22.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w12 * p21.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w21 * p12.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                                        + w22 * p11.transientResult.ListResults[i].CircuitProperties["C"].fluxlinkage
                        } },
                    },
                };

                // torque in consideration of skew
                double t = 0;
                for (int k = 0; k < 2 * Nseg + 1; k++)
                {
                    double theta_skew_k = (k - Nseg) * skewangle / (2 * Nseg);
                    double xk = i + theta_skew_k / (totalRotatedAngle_degree / analyser.StepCount);
                    double tk = w11 * interpolateArrayOfPeriodicData(t22array, xk)
                               + w12 * interpolateArrayOfPeriodicData(t21array, xk)
                               + w21 * interpolateArrayOfPeriodicData(t12array, xk)
                               + w22 * interpolateArrayOfPeriodicData(t11array, xk);
                    t += tk;
                }

                arg.Torque = t / (2 * Nseg + 1);

                steps.Add(arg);
            }

            result.ListResults = steps.ToArray();

            // coreloss
            result.RotorLossHysteresis = w11 * p22.transientResult.RotorLossHysteresis
                                       + w12 * p21.transientResult.RotorLossHysteresis
                                       + w21 * p12.transientResult.RotorLossHysteresis
                                       + w22 * p11.transientResult.RotorLossHysteresis;
            result.RotorLossEddy = w11 * p22.transientResult.RotorLossEddy
                                       + w12 * p21.transientResult.RotorLossEddy
                                       + w21 * p12.transientResult.RotorLossEddy
                                       + w22 * p11.transientResult.RotorLossEddy;

            result.StatorLossHysteresis = w11 * p22.transientResult.StatorLossHysteresis
                                       + w12 * p21.transientResult.StatorLossHysteresis
                                       + w21 * p12.transientResult.StatorLossHysteresis
                                       + w22 * p11.transientResult.StatorLossHysteresis;
            result.StatorLossEddy = w11 * p22.transientResult.StatorLossEddy
                                       + w12 * p21.transientResult.StatorLossEddy
                                       + w21 * p12.transientResult.StatorLossEddy
                                       + w22 * p11.transientResult.StatorLossEddy;

            //            

            // coreloss + speed correction
            result.RotorLossHysteresis *= speed / analyser.BaseSpeed;
            result.RotorLossEddy *= Math.Pow(speed / analyser.BaseSpeed, 2);
            result.StatorLossHysteresis *= speed / analyser.BaseSpeed;
            result.StatorLossEddy *= Math.Pow(speed / analyser.BaseSpeed, 2);

            // coreloss total
            result.RotorCoreLoss = result.RotorLossHysteresis + result.RotorLossEddy;
            result.StatorCoreLoss = result.StatorLossHysteresis + result.StatorLossEddy;

            var data = new APointData()
            {
                id = id,
                iq = iq,
                speed = speed,
                torque = result.Torques.Sum() / result.Count,
                cogging = (result.Torques.Max() - result.Torques.Min()) / 2,
                rotorloss = result.RotorCoreLoss,
                statorloss = result.StatorCoreLoss,
                copperloss = 3.0 / 2 * (id * id + iq * iq) * resistance,
                result = result,
                ed = result.InducedVoltage_D().Sum() / result.Count,
                eq = result.InducedVoltage_Q().Sum() / result.Count,
                Ld = result.Inductance_D().Sum() / result.Count,
                Lq = result.Inductance_Q().Sum() / result.Count,
            };

            data.ud = resistance * id + data.ed;
            data.uq = resistance * iq + data.eq;

            p11.transientResult.Analyser = dummyTransientAnalyser;
            var tt = p11.transientResult.InducedVoltageDQ();

            return data;
        }

        private double interpolateArrayOfPeriodicData(IEnumerable<double> array, double x, bool is0andNequal = true)
        {
            int n = array.Count();
            // if last value is same as first, don't use it
            if (is0andNequal)
                n--;
            if (x < 0 || x > n)
                x = x - n * Math.Floor(x / n);//normalize

            if (x == 0 || x == n)
                return array.ElementAt(0);

            int i = (int)Math.Floor(x);
            double v1 = array.ElementAt(i);
            double v2 = i < n - 1 ? array.ElementAt(i + 1) : array.ElementAt(0);
            return v1 * (i + 1 - x) + v2 * (x - i);
        }

        private Complex[] fourierAnalysis(double[] ss)
        {
            int n = ss.Length;
            //fourier analysis
            Complex[] samples = ss.Select(d => new Complex(d, 0)).ToArray();

            Fourier.Forward(samples, FourierOptions.NoScaling);
            Complex[] returns = samples.Select(c => c / n * 2).ToArray();
            returns[0] /= 2;

            return samples;
        }

        #endregion

        private void bt_show_transient_analysis(object sender, EventArgs e)
        {
            double id = 0;
            double iq = 0;
            double speed = 3000;
            double skew = 10;
            if (double.TryParse(tb_id.Text, out id)
                && double.TryParse(tb_iq.Text, out iq)
                && double.TryParse(tb_speed.Text, out speed)
                && double.TryParse(tb_skew.Text, out skew))
            {
                try
                {
                    var data = interpolateData(id, iq, speed);
                    var dicts = data.result.BuildResultsForDisplay();

                    GraphWindow tc = new GraphWindow();

                    //int sc = 10;
                    //double max_skew = 30;
                    //for (int i = 0; i < sc; i++)
                    //{
                    //    double skewAngle = i * max_skew / sc;                    
                    var data2 = interpolateDataWithSkew(id, iq, speed, skew, 20);
                    tc.addData("Torque(Skew=" + skew + ") (1)", new PointPairList(data2.result.Times, data2.result.Torques));

                    data2 = interpolateDataWithSkew2(id, iq, speed, skew, 20);
                    tc.addData("Torque(Skew=" + skew + ") (2)", new PointPairList(data2.result.Times, data2.result.Torques));
                    //}

                    foreach (String name in dicts.Keys)
                    {
                        object value = dicts[name];

                        if (value is ListPointD)
                        {
                            var pointlist = ((ListPointD)value).ToZedGraphPointPairList();
                            tc.addData(name, pointlist);
                        }
                    }

                    tc.Text = string.Format("id={0},iq={1},speed={2},torque={3},rotorloss={4},statorloss={5}", id, iq, speed, data.torque, data.rotorloss, data.statorloss);
                    tc.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Input invalid");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int speed_count = 100;
            int torque_count = 50;
            double Imax = 55;
            double Umax = 140;
            double max_speed = 6000;

            double speed = double.Parse(tb_speed.Text);
            double torque = double.Parse(tb_torque.Text);

            MaxtorqueCapabilityCurve mtcc = buildMaxtorqueCapabilityCurve(speed_count, Imax, Umax, max_speed);
            VoltageLimitCurve vlc = buildVoltageLimitCurve(speed_count, Imax, Umax, max_speed);

            double speed1 = vlc.GetMaxSpeedForTorque(torque);

            Fdq idq = default(Fdq);

            // zone 1
            if (speed <= speed1)
            {
                idq = mtpa.GetCurrentForTorque(torque);
            }
            else
            {
                // Curve of same torque current
                List<Fdq> cst = curveSameTorque(torque, Imax);
                if (cst.Count > 0)
                {

                    LinearSpline spline = LinearSpline.Interpolate(cst.Select(f => f.d), cst.Select(f => f.q));

                    double iq = 0;
                    double id = 0;

                    // find root id
                    bool b_id = Brent.TryFindRoot(id_ =>
                    {
                        iq = spline.Interpolate(id_);
                        var d = interpolateData(id_, iq, speed);
                        if (d == null)
                            return 999;

                        double u = Math.Sqrt(d.ud * d.ud + d.uq * d.uq);

                        return u - Umax;
                    }, -Imax, 0, 1e-8, 500, out id);


                    if (b_id)
                        idq = new Fdq()
                        {
                            d = id,
                            q = iq,
                        };
                }
            }

            tb_id.Text = idq.d.ToString();
            tb_iq.Text = idq.q.ToString();
            label5.Text = string.Format("{0:F3} # {1:F3}", idq.Magnitude, idq.Phase);
        }

        #region MTPA table

        private class MTPA
        {
            public List<Fdq> idqs;
            public List<double> max_torques;

            private LinearSpline id_spline;
            private LinearSpline iq_spline;
            private LinearSpline maxTorque_spline;

            public Fdq GetCurrentForTorque(double torque)
            {
                checkAndCreateSpline();

                double id = id_spline.Interpolate(torque);
                double iq = iq_spline.Interpolate(torque);
                return new Fdq() { d = id, q = iq };
            }

            public double GetMaxTorqueWithCurrentMagnitude(double current)
            {
                checkAndCreateSpline();

                return maxTorque_spline.Interpolate(current);
            }

            private void checkAndCreateSpline()
            {
                if (id_spline == null)
                {
                    id_spline = LinearSpline.Interpolate(max_torques, idqs.Select(f => f.d));
                    iq_spline = LinearSpline.Interpolate(max_torques, idqs.Select(f => f.q));
                    maxTorque_spline = LinearSpline.Interpolate(idqs.Select(f => f.Magnitude), max_torques);
                }
            }
        }

        private MTPA _mtpa = null;

        private MTPA buildTableMaxtorquePerAmple()
        {
            if (_mtpa != null)
                return _mtpa;

            int stepcount = 20;
            //int beta_count = 90;
            var idqs = new List<Fdq>();
            var max_torques = new List<double>();

            double Imax = analyser.MaxCurrent;

            int k = 0;

            idqs.Add(new Fdq { d = 0, q = 0 });
            max_torques.Add(0);

            // cannot start from 0.
            for (int i = 1; i < stepcount + 1; i++)
            {
                double II = Imax * i / stepcount;

                var minimizer = new GoldenSectionMinimizer(1e-5, 1000, 0);
                var objfnc = new SimpleObjectiveFunction1D(b =>
                {
                    double id = II * Math.Cos(b);
                    double iq = II * Math.Sin(b);
                    var data = interpolateData(id, iq, 3000);
                    k++;
                    return -data.torque;
                });

                MinimizationOutput1D min = null;
                double beta_rad = Math.PI / 2;
                double max_t = 0;
                Fdq idq = default(Fdq);
                try
                {
                    min = minimizer.FindMinimum(objfnc, Math.PI / 2, Math.PI);

                    // minimum found
                    beta_rad = min.MinimizingPoint;
                    max_t = -min.FunctionInfoAtMinimum.Value;
                    idq = new Fdq
                    {
                        d = II * Math.Cos(beta_rad),
                        q = II * Math.Sin(beta_rad),
                    };
                }
                catch (OptimizationException)
                {
                    // minimum not found, get value at pi/2
                    beta_rad = Math.PI / 2;
                    idq = new Fdq { d = 0, q = II };
                    var data = interpolateData(idq.d, idq.q, 3000);
                    max_t = data.torque;
                }

                max_torques.Add(max_t);
                idqs.Add(idq);
            }

            Console.WriteLine("Call calc count = " + k);

            MTPA mtpa = new MTPA()
            {
                idqs = idqs,
                max_torques = max_torques
            };

            _mtpa = mtpa;

            return mtpa;
        }

        #endregion

        #region Torque-id-iq table

        private List<Fdq> curveSameTorque(double t, double Imax = 55)
        {
            //Fdq Imin = mtpa.GetCurrentForTorque(t);
            //double iq_maxbeta;
            //bool b_iq = Brent.TryFindRoot(iq_ =>
            //{
            //    var d = interpolateData(-Imax, iq_, 3000);

            //    return d.torque - t;
            //}, 0, Imax, 1e-8, 500, out iq_maxbeta);

            //if (!b_iq)
            //    return new List<Fdq>();//nothing

            int count = 50;
            List<Fdq> idqs = new List<Fdq>();
            for (int i = 0; i < count + 1; i++)
            {
                double id = -Imax * i / count;
                double iq = 0;
                bool bI = Brent.TryFindRoot(iq_ =>
                {
                    var d = interpolateData(id, iq_, 3000);

                    return d.torque - t;
                }, 0, Imax, 1e-5, 100, out iq);

                if (bI)
                {
                    idqs.Add(new Fdq()
                    {
                        d = id,
                        q = iq,
                    });
                }
            }

            return idqs;
        }

        private List<PointD> curveTorqueBetaAtCurrent(double II)
        {
            List<PointD> c = new List<PointD>();
            int beta_count = 90;
            for (int j = 0; j < beta_count + 1; j++)
            {
                double beta = (90.0 + 90.0 * j / beta_count);
                double beta_rad = beta * Math.PI / 180;
                double id = II * Math.Cos(beta_rad);
                double iq = II * Math.Sin(beta_rad);
                var data = interpolateData(id, iq, 3000);
                c.Add(new PointD { X = beta, Y = data.torque });
            }

            return c;
        }

        #endregion

        #region Voltage limit ellipse (id,iq)

        private class VoltageLimitEllipse
        {
            public double speed;
            public double Imax;
            public double Umax;

            public int maxtorque_point;
            public int minid_point;
            public List<Fdq> curve;
            public List<double> torques;
        }

        /// <summary>
        /// Build a 'min id(w)' curve for faster build voltage limit ellipse
        /// </summary>
        /// <returns></returns>
        private double findMinId(double speed, double Imax, double Umax)
        {
            double idmax_search = -psiM / Ld_approx;
            if (idmax_search < -Imax)
                idmax_search = -Imax;
            double id = 0;

            bool bb = Brent.TryFindRoot(id_ =>
            {
                var d = interpolateData(id_, 0, speed);
                double uu = Math.Sqrt(d.ud * d.ud + d.uq * d.uq);
                return uu - Umax;
            }, idmax_search, 0, 1e-5, 100, out id);

            if (bb)
                return id;

            // check speed if it is too low or too high
            var dd = interpolateData(0, 0, speed);
            double u = Math.Sqrt(dd.ud * dd.ud + dd.uq * dd.uq);

            if (u < Umax)
                return 0;
            else
                return double.NaN;
        }

        private VoltageLimitEllipse buildVoltageLimitEllipse(double speed, int count = 50, double Imax = 55, double Umax = 140)
        {
            var vle = new VoltageLimitEllipse
            {
                speed = speed,
                Imax = Imax,
                Umax = Umax,
                curve = new List<Fdq>(),
                torques = new List<double>(),
            };

            double maxt = 0;
            bool minfound = false;

            int index = 0;

            double minId = findMinId(speed, Imax, Umax);
            if (double.IsNaN(minId))
                return vle;

            // first point
            var dd = interpolateData(minId, 0, speed);
            vle.curve.Add(new Fdq { d = minId, q = 0 });
            vle.torques.Add(dd.torque);

            for (int j = 1; j < count + 1; j++)
            {
                double id = minId - (Imax + minId) * j / count;
                double iq;
                double t = 0;
                bool bb = Brent.TryFindRoot(iq_ =>
                {
                    var d = interpolateData(id, iq_, speed);
                    double u = Math.Sqrt(d.ud * d.ud + d.uq * d.uq);
                    t = d.torque;
                    return u - Umax;
                }, 0, Imax, 1e-5, 100, out iq);

                if (bb)
                {
                    var idq = new Fdq { d = id, q = iq };
                    if (maxt < t)
                    {
                        maxt = t;
                        vle.maxtorque_point = index;
                    }
                    if (!minfound)
                    {
                        vle.minid_point = index;
                        minfound = true;
                    }

                    vle.curve.Add(idq);
                    vle.torques.Add(t);
                    index++;
                }
            }

            //if (vle.curve.Count > 1 && vle.curve[0].d < 0)
            //{
            //    Fdq idq1 = vle.curve[0];
            //    Fdq idq2 = vle.curve[1];
            //    Fdq idq0 = new Fdq
            //    {
            //        d = idq1.d + (Imax / count) * (idq1.q / idq2.q) / (1 - idq1.q / idq2.q),
            //        q = 0,
            //    };
            //    vle.curve.Insert(0, idq0);
            //    vle.torques.Insert(0, 0);
            //    vle.maxtorque_point++;
            //}

            return vle;
        }

        #endregion

        #region Maxtorque-capability-curve

        // this is artifical mechanical characteristic
        private class MaxtorqueCapabilityCurve
        {
            public double Imax;
            public double Umax;
            public double MaxSpeed;

            public readonly List<double> speeds = new List<double>();
            public readonly List<double> maxtorques = new List<double>();
            public readonly List<Fdq> currents = new List<Fdq>();
            public readonly List<Fdq> voltages = new List<Fdq>();
            public readonly List<double> power = new List<double>();
            public readonly List<double> phi = new List<double>();
            public readonly List<double> effs = new List<double>();
            public readonly List<double> coreloss = new List<double>();
        }

        private MaxtorqueCapabilityCurve buildMaxtorqueCapabilityCurve(int count = 50, double Imax = 55, double Umax = 140, double max_speed = 6000)
        {
            var mtpa = buildTableMaxtorquePerAmple();

            MaxtorqueCapabilityCurve mtcc = new MaxtorqueCapabilityCurve()
            {
                Imax = Imax,
                Umax = Umax,
                MaxSpeed = max_speed,
            };

            // Look for beta to get max torque

            double max_t = mtpa.GetMaxTorqueWithCurrentMagnitude(Imax);
            Fdq idq = mtpa.GetCurrentForTorque(max_t);

            // Create PointPairList
            // look for speed1
            var speed1 = Brent.FindRoot(s =>
            {
                var data_ = interpolateData(idq.d, idq.q, s);
                var u_ = Math.Sqrt(data_.ud * data_.ud + data_.uq * data_.uq);
                return u_ - Umax;
            }, 100, max_speed, 1e-3);

            // up to speed1
            int count1 = (int)(speed1 / max_speed * count) + 1;
            int count2 = count - count1;

            for (int i = 0; i < count1 + 1; i++)
            {
                double speed = speed1 * i / count1;

                var data = interpolateData(idq.d, idq.q, speed);

                mtcc.speeds.Add(speed);
                mtcc.maxtorques.Add(data.torque);
                mtcc.currents.Add(idq);
                mtcc.voltages.Add(new Fdq() { d = data.ud, q = data.uq });
                mtcc.power.Add(data.power);
                mtcc.effs.Add(data.efficiency);
                mtcc.coreloss.Add(data.rotorloss + data.statorloss);
            }

            double t = max_t;

            for (int i = 1; i < count2 + 1; i++)
            {
                double speed = speed1 + (max_speed - speed1) * i / count2;

                // find beta which make U=Umax                
                //double beta = 0;
                //bool success = Brent.TryFindRoot(b =>
                //{
                //    var data_ = calculatePointdata(Imax * Math.Cos(b * Math.PI / 180), Imax * Math.Sin(b * Math.PI / 180), speed);
                //    var u_ = Math.Sqrt(data_.ud * data_.ud + data_.uq * data_.uq);
                //    return u_ - Umax;
                //}, 100, 180, 1e-5, 100, out beta);

                // Here:
                // Find Id,Iq that bring max torque at speed
                // it is intersect of circle Imax and ellipse Umax/w if Imax < ellipse center (-psiM/Ld)
                // or ellipse center itself if Imax > ellipse center

                Fdq idq2 = default(Fdq);
                bool found = false;

                var vle = buildVoltageLimitEllipse(speed, 20, Imax, Umax);

                if (vle.curve.Count == 0)
                    continue;

                // check max torque point on voltage limit ellipse 
                // and minimum Id point 
                var maxtorque_point = vle.curve[vle.maxtorque_point];
                var minid_point = vle.curve[vle.minid_point];

                if (maxtorque_point.Magnitude <= Imax)
                {
                    idq2 = new Fdq { d = maxtorque_point.d, q = maxtorque_point.q };
                    found = true;
                }
                else if (minid_point.Magnitude > Imax) //Imax out of ellipse
                {
                    found = false;
                }
                else //find point is an intersection of vle and current limit circle
                {
                    var range = Enumerable.Range(0, vle.maxtorque_point);
                    // convert voltage limit ellipse to a function: Id from current magnitude
                    LinearSpline spline = LinearSpline.Interpolate(range.Select(k => vle.curve[k].Magnitude), range.Select(k => vle.curve[k].d));
                    // get id = f(Imax)
                    double id = spline.Interpolate(Imax);
                    // calculate iq
                    double iq = Math.Sqrt(Imax * Imax - id * id);
                    idq2 = new Fdq { d = id, q = iq };
                    found = true;
                }

                if (found)
                {
                    //double id = idq2.d;//Imax * Math.Cos(beta * Math.PI / 180);
                    //double iq = idq2.q;//Imax * Math.Sin(beta * Math.PI / 180);
                    var data = interpolateData(idq2.d, idq2.q, speed);

                    mtcc.speeds.Add(speed);
                    mtcc.maxtorques.Add(data.torque);
                    mtcc.currents.Add(idq2);
                    mtcc.voltages.Add(new Fdq() { d = data.ud, q = data.uq });
                    mtcc.power.Add(data.power);
                    mtcc.effs.Add(data.efficiency);
                    mtcc.coreloss.Add(data.rotorloss + data.statorloss);
                }
            }

            return mtcc;
        }

        #endregion

        #region VoltageLimitCurve (M,w)        

        /// <summary>
        /// Data of the curve speed(torque), where speed cannot increase without increase beta. Or limit of zone1.
        /// wb(M)
        /// </summary>
        private class VoltageLimitCurve
        {
            public double Imax;
            public double Umax;
            public double MaxSpeed;

            public readonly List<double> speeds = new List<double>();
            public readonly List<double> torques = new List<double>();
            public readonly List<Fdq> currents = new List<Fdq>();

            private LinearSpline speed_spline;

            /// <summary>
            /// Get max speed possible for given torque. Condition: Umax, Imax and max-torque-per-ample
            /// </summary>
            /// <param name="t"></param>
            /// <returns></returns>
            public double GetMaxSpeedForTorque(double t)
            {
                if (speed_spline == null)
                {
                    speed_spline = LinearSpline.Interpolate(torques, speeds);
                }

                return speed_spline.Interpolate(t);
            }
        }

        private VoltageLimitCurve buildVoltageLimitCurve(int count = 50, double Imax = 55, double Umax = 140, double max_speed = 6000)
        {
            VoltageLimitCurve vlc = new VoltageLimitCurve()
            {
                Imax = Imax,
                Umax = Umax,
                MaxSpeed = max_speed,
            };

            for (int i = 0; i < count + 1; i++)
            {
                double t = mtpa.GetMaxTorqueWithCurrentMagnitude(i * Imax / count);
                var idq = mtpa.GetCurrentForTorque(t);

                double ss = 0;
                bool success = Brent.TryFindRoot(s =>
                {
                    var data_ = interpolateData(idq.d, idq.q, s);
                    var u_ = Math.Sqrt(data_.ud * data_.ud + data_.uq * data_.uq);
                    return u_ - Umax;
                }, 100, max_speed, 1e-3, 100, out ss);

                if (success)
                {
                    vlc.speeds.Add(ss);
                    vlc.torques.Add(t);
                    vlc.currents.Add(idq);
                }
            }

            return vlc;
        }

        #endregion        

        #region EfficiencyMap

        private enum MapType
        {
            power,
            torque,
            cogging,
            windingloss,
            rotor_coreloss,
            stator_coreloss,
            coreloss,
            total_loss,
            coreloss_percentage,
            efficiency,
            voltage,
            current,
            beta,
            Id,
            Iq,
            Ld,
            Lq,
        }

        private class EfficiencyMap
        {
            public double Imax;
            public double Umax;
            public double MaxSpeed;

            public MaxtorqueCapabilityCurve mtcc;
            public VoltageLimitCurve vlc;

            /// <summary>
            /// Speed points on Axis X
            /// </summary>
            public double[] speed_points;

            /// <summary>
            /// Torque points on Axis Y
            /// </summary>
            public double[] torque_points;

            //---- DATA
            // power
            public double[,] power;
            // efficiency
            /// <summary>
            /// %
            /// </summary>
            public double[,] efficiency;

            public double[,] torque;
            public double[,] cogging;

            // loss
            public double[,] windingloss;
            public double[,] rotor_coreloss;
            public double[,] stator_coreloss;
            public double[,] coreloss;
            public double[,] totalloss;
            public double[,] coreloss_percentage;

            // id, iq
            public double[,] id;
            public double[,] iq;

            // other
            /// <summary>
            /// V
            /// </summary>
            public double[,] voltage;
            /// <summary>
            /// A
            /// </summary>
            public double[,] current;
            /// <summary>
            /// degree
            /// </summary>
            public double[,] beta;
            /// <summary>
            /// mH
            /// </summary>
            public double[,] Ld;
            /// <summary>
            /// mH
            /// </summary>
            public double[,] Lq;
        }

        private EfficiencyMap buildEfficiencyMap(int speed_count, int torque_count, double Imax, double Umax, double max_speed)
        {
            // max torque curve
            MaxtorqueCapabilityCurve mtcc = buildMaxtorqueCapabilityCurve(speed_count, Imax, Umax, max_speed);

            // voltage limit curve (zone1 limit)
            VoltageLimitCurve vlc = buildVoltageLimitCurve(speed_count, Imax, Umax, max_speed);

            var em = new EfficiencyMap()
            {
                // input
                Imax = Imax,
                Umax = Umax,
                MaxSpeed = max_speed,
                mtcc = mtcc,
                vlc = vlc,

                // axis
                speed_points = new double[speed_count],
                torque_points = new double[torque_count],

                // value
                power = new double[speed_count, torque_count],
                efficiency = new double[speed_count, torque_count],

                torque = new double[speed_count, torque_count],
                cogging = new double[speed_count, torque_count],

                windingloss = new double[speed_count, torque_count],
                rotor_coreloss = new double[speed_count, torque_count],
                stator_coreloss = new double[speed_count, torque_count],
                coreloss = new double[speed_count, torque_count],
                totalloss = new double[speed_count, torque_count],
                coreloss_percentage = new double[speed_count, torque_count],

                id = new double[speed_count, torque_count],
                iq = new double[speed_count, torque_count],

                voltage = new double[speed_count, torque_count],
                current = new double[speed_count, torque_count],
                beta = new double[speed_count, torque_count],
                Ld = new double[speed_count, torque_count],
                Lq = new double[speed_count, torque_count],
            };
            em.power.Fill2D(double.NaN);
            em.efficiency.Fill2D(double.NaN);

            em.torque.Fill2D(double.NaN);
            em.cogging.Fill2D(double.NaN);

            em.windingloss.Fill2D(double.NaN);
            em.rotor_coreloss.Fill2D(double.NaN);
            em.stator_coreloss.Fill2D(double.NaN);
            em.coreloss.Fill2D(double.NaN);
            em.coreloss_percentage.Fill2D(double.NaN);
            em.totalloss.Fill2D(double.NaN);

            em.id.Fill2D(double.NaN);
            em.iq.Fill2D(double.NaN);

            em.voltage.Fill2D(double.NaN);
            em.current.Fill2D(double.NaN);
            em.beta.Fill2D(double.NaN);
            em.Ld.Fill2D(double.NaN);
            em.Lq.Fill2D(double.NaN);

            var mtpa = buildTableMaxtorquePerAmple();
            double max_t = mtpa.GetMaxTorqueWithCurrentMagnitude(Imax);

            LinearSpline torqueCapabilityLimit = LinearSpline.Interpolate(mtcc.speeds, mtcc.maxtorques);

            for (int i = 0; i < speed_count; i++)
            {
                // speed
                double speed = max_speed * (i + 1) / speed_count;
                em.speed_points[i] = speed;
                if (speed == 0)
                    speed = 1;

                double max_torque_for_this_speed = torqueCapabilityLimit.Interpolate(speed);

                for (int j = 0; j < torque_count; j++)
                {
                    // torque
                    double t = max_t * (j + 1) / torque_count;
                    em.torque_points[j] = t;
                    if (t == 0)
                        t = 0.1;
                    if (t > max_torque_for_this_speed * 1.02)//allow 2%
                        continue;

                    // speed limit 1
                    double speed1 = vlc.GetMaxSpeedForTorque(t);

                    APointData data = null;

                    if (t == 0)
                    {
                        data = interpolateData(0, 0, speed);
                    }
                    // zone 1
                    else if (speed <= speed1)
                    {
                        Fdq idq = mtpa.GetCurrentForTorque(t);
                        if (double.IsNaN(idq.d) || double.IsNaN(idq.q))
                            data = null;
                        else
                            data = interpolateData(idq.d, idq.q, speed);
                    }
                    else
                    {
                        Fdq idq = getCurrentForZone2(t, speed, Imax, Umax);
                        if (double.IsNaN(idq.d) || double.IsNaN(idq.q))
                            data = null;
                        else
                            data = interpolateData(idq.d, idq.q, speed);
                    }

                    if (data != null)
                    {
                        em.rotor_coreloss[i, j] = data.rotorloss;
                        em.stator_coreloss[i, j] = data.statorloss;
                        em.coreloss[i, j] = data.rotorloss + data.statorloss;
                        em.windingloss[i, j] = data.copperloss;
                        em.totalloss[i, j] = data.copperloss + data.rotorloss + data.statorloss;
                        em.coreloss_percentage[i, j] = 100 * em.coreloss[i, j] / em.totalloss[i, j];
                        em.power[i, j] = data.power;

                        em.efficiency[i, j] = 100 * data.power / (data.power + em.totalloss[i, j]);

                        em.torque[i, j] = data.torque;
                        em.cogging[i, j] = data.cogging;

                        em.id[i, j] = data.id;
                        em.iq[i, j] = data.iq;

                        em.voltage[i, j] = data.voltage;
                        em.current[i, j] = data.current;
                        em.beta[i, j] = data.beta;
                        em.Ld[i, j] = double.IsNaN(data.Ld) || data.Ld < 0 ? -0.0001 : data.Ld * 1000;//to mH
                        em.Lq[i, j] = double.IsNaN(data.Ld) || data.Ld < 0 ? -0.0001 : data.Lq * 1000;//to mH
                    }
                }
            }

            return em;
        }

        Dictionary<string, VoltageLimitEllipse> dict_vle = new Dictionary<string, VoltageLimitEllipse>();

        private Fdq getCurrentForZone2(double torque, double speed, double Imax, double Umax)
        {
            VoltageLimitEllipse vle = null;
            string key = string.Format("{0},{1},{2}", Imax, Umax, speed);
            if (dict_vle.ContainsKey(key))
            {
                vle = dict_vle[key];
            }
            else
            {
                vle = buildVoltageLimitEllipse(speed, 20, Imax, Umax);
                dict_vle[key] = vle;
            }

            if (vle.torques.Count == 0)
                return default(Fdq);

            var range = Enumerable.Range(0, vle.maxtorque_point);

            // find point on Voltage limit ellipse that bring given torque
            LinearSpline spline = LinearSpline.Interpolate(range.Select(i => vle.torques[i]), range.Select(i => vle.curve[i].d));
            double id = spline.Interpolate(torque);

            // get iq from this id
            var maxiq_point = vle.curve[vle.maxtorque_point];
            var minid_point = vle.curve[vle.minid_point];
            if (id < maxiq_point.d || minid_point.d < id)
                return new Fdq { d = double.NaN, q = double.NaN };

            LinearSpline spline2 = LinearSpline.Interpolate(vle.curve.Select(f => f.d), vle.curve.Select(f => f.q));
            double iq = spline2.Interpolate(id);
            return new Fdq
            {
                d = id,
                q = iq,
            };
        }

        #endregion

        #region Export for other programs

        private void saveAsAdvisorMFile(string fn, EfficiencyMap effmap)
        {
            ProjectManager pm = ProjectManager.GetInstance();
            var motor = pm.Motor;

            string text = Properties.Resources.advisor_MC;

            text = text.Replace("{MOTOR_NAME}", Path.GetFileNameWithoutExtension(pm.CurrentProjectFile));
            text = text.Replace("{CREATED_DATE}", DateTime.Now.ToString());

            // MAP_TORQUE
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < effmap.torque_points.Length; i++)
            {
                sb.Append(effmap.torque_points[i]);
                sb.Append(" ");
            }
            sb.Append("]");
            text = text.Replace("{MAP_TORQUE}", sb.ToString());

            // MAP_SPEED
            sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < effmap.speed_points.Length; i++)
            {
                sb.Append(effmap.speed_points[i]);
                sb.Append(" ");
            }
            sb.Append("]*(2*pi)/60");//convert to rad/s
            text = text.Replace("{MAP_SPEED}", sb.ToString());

            // MAP_EFF
            sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < effmap.speed_points.Length; i++)
            {
                double lastvalue = 0;
                for (int j = 0; j < effmap.torque_points.Length; j++)
                {
                    if (double.IsNaN(effMap.efficiency[i, j]))
                        sb.Append(lastvalue);
                    else
                    {
                        lastvalue = effmap.efficiency[i, j] / 100;
                        sb.Append(lastvalue);
                    }
                    sb.Append(" ");
                }
                sb.AppendLine();
            }
            sb.Append("]");//convert to rad/s
            text = text.Replace("{MAP_EFF}", sb.ToString());

            // {MAP_MAX_TORQUE}
            var mtcc = effmap.mtcc;
            sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < mtcc.maxtorques.Count; i++)
            {
                sb.Append(mtcc.maxtorques[i]);
                sb.Append(" ");
            }
            sb.Append("]");
            text = text.Replace("{MAP_MAX_TORQUE}", sb.ToString());

            // {MAP_SPEED_MAX_TORQUE}                
            sb = new StringBuilder();
            sb.Append("[");           
            for (int i = 0; i < mtcc.speeds.Count; i++)
            {
                sb.Append(mtcc.speeds[i]);
                sb.Append(" ");
            }
            sb.Append("]*(2*pi)/60");
            text = text.Replace("{MAP_SPEED_MAX_TORQUE}", sb.ToString());

            // {INERTIA}
            text = text.Replace("{INERTIA}", motor.Rotor.Inertia.ToString());

            // {MASS}
            text = text.Replace("{MASS}", motor.Mass.ToString());

            // write to file
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(text);
            }
        }

        private void saveAsDatFileMachineDesignToolkitAnsys(string fn, EfficiencyMap effmap)
        {
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine("Efficiency map calculated on:    ");
                sw.WriteLine("{0}    {1}    ", effmap.speed_points.Length, effmap.torque_points.Length);

                sw.WriteLine("Speed    ");
                sw.WriteLine();
                for (int i = 0; i < effmap.speed_points.Length; i++)
                    sw.Write(effmap.speed_points[i] + "    ");
                sw.WriteLine();
                sw.WriteLine();

                sw.WriteLine("Torque    ");
                sw.WriteLine();
                for (int i = effmap.torque_points.Length - 1; i >= 0; i--)
                    sw.Write(effmap.torque_points[i] + "    ");
                sw.WriteLine();
                sw.WriteLine();

                LinearSpline torqueCapabilityLimit = LinearSpline.Interpolate(effmap.mtcc.speeds, effmap.mtcc.maxtorques);
                sw.WriteLine("TSC    ");
                sw.WriteLine();
                for (int i = 0; i < effmap.speed_points.Length; i++)
                    sw.Write(torqueCapabilityLimit.Interpolate(effmap.speed_points[i]) + "    ");
                sw.WriteLine();
                sw.WriteLine();

                sw.WriteLine("Efficiency map Percent    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effmap.efficiency[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Speed map rpm    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effmap.speed_points[i] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Torque map NewtonMeter    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effmap.torque_points[j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Current map A    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effmap.current[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Voltage map V    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effmap.voltage[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Gamma map deg    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.beta[i, j] - 90.0) + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("TotalLoss map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effMap.totalloss[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("WindingLoss map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effMap.windingloss[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("OutputPower map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write(effMap.power[i, j] + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("InputPower map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effMap.power[i, j] + effmap.totalloss[i, j]) + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("CoreLoss map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effMap.coreloss[i, j]) + "    ");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("SolidLoss map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("MechanicalLoss map W    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("TorqueRipple map NewtonMeter    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.cogging[i, j]) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("PowerFactor map None    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Phid map Wb    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Phiq map Wb    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Vd map V    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Vq map V    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Id map A    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.id[i, j]) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Iq map A    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.iq[i, j]) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Ld map H    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.Ld[i, j] / 1000.0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("Lq map H    ");
                sw.WriteLine();
                for (int j = effmap.torque_points.Length - 1; j >= 0; j--)
                {
                    for (int i = 0; i < effmap.speed_points.Length; i++)
                    {
                        if (double.IsNaN(effmap.efficiency[i, j]))
                            sw.Write("nan    ");
                        else
                            sw.Write((effmap.Lq[i, j] / 1000.0) + "    ");//unknown
                    }
                    sw.WriteLine();
                }
                sw.WriteLine();
            }
        }

        #endregion

        //private void useGAShowTorqueCurve()
        //{
        //    int stepcount = 40;
        //    double max_speed = 6000;
        //    double Imax = 55;
        //    double Umax = 160;
        //    PointPairList ppl_torque = new PointPairList();
        //    PointPairList ppl_current = new PointPairList();
        //    PointPairList ppl_voltage = new PointPairList();
        //    PointPairList ppl_power = new PointPairList();
        //    PointPairList ppl_beta = new PointPairList();

        //    for (int i = 1; i < stepcount + 1; i++)
        //    {
        //        Console.WriteLine("Step = " + i);

        //        double speed = max_speed * i / stepcount;

        //        // GA optimization
        //        // find max torque with given speed considering current and voltage is limited
        //        GA ga = new GA(0.8, 0.4, 50, 300, 2, 1);
        //        ga.FitnessFunction =
        //            gens =>
        //            {
        //                double id = -gens[0] * analyser.MaxCurrent;
        //                double iq = gens[1] * analyser.MaxCurrent;
        //                var d = interpolateData(id, iq, speed);

        //                double II = Math.Sqrt(id * id + iq * iq);
        //                double ud = resistance * id + d.ed;
        //                double uq = resistance * iq + d.eq;
        //                double UU = Math.Sqrt(ud * ud + uq * uq);

        //                double f = d.torque;
        //                if (II > Imax * 1.0001 || UU > Umax * 1.0001)
        //                    f = f * 0.00001;

        //                return new double[1] { f };
        //            };

        //        ga.Elitism = true;
        //        ga.Go();
        //        double[] gg;
        //        double[] ff;
        //        ga.GetBest(out gg, out ff);

        //        Fdq idq = new Fdq()
        //        {
        //            d = -gg[0] * analyser.MaxCurrent,
        //            q = gg[1] * analyser.MaxCurrent
        //        };

        //        var data = interpolateData(idq.d, idq.q, speed);

        //        Fdq udq = new Fdq()
        //        {
        //            d = resistance * idq.d + data.ed,
        //            q = resistance * idq.q + data.eq
        //        };

        //        if (ff[0] > 0.1)
        //        {
        //            ppl_torque.Add(speed, data.torque);
        //            ppl_current.Add(speed, idq.Magnitude);
        //            ppl_voltage.Add(speed, udq.Magnitude);
        //            ppl_power.Add(speed, 1.5 * (data.ed * idq.d + data.eq * idq.q));
        //            ppl_beta.Add(speed, idq.Phase);
        //        }
        //        else
        //        {
        //            ppl_torque.Add(speed, 0);
        //            ppl_current.Add(speed, 0);
        //            ppl_voltage.Add(speed, 0);
        //            ppl_power.Add(speed, 0);
        //            ppl_beta.Add(speed, 0);
        //        }
        //    }

        //    GraphWindow tc = new GraphWindow();

        //    tc.addData("Torque-speed", ppl_torque);
        //    tc.addData("Current-speed", ppl_current);
        //    tc.addData("CurrentPhase-speed", ppl_beta);
        //    tc.addData("Voltage-speed", ppl_voltage);
        //    tc.addData("Power-speed", ppl_power);

        //    tc.Show();
        //}

        private void bt_show_max_torque_curve(object sender, EventArgs e)
        {
            try
            {
                double Imax = double.Parse(tb_Imax.Text);
                double Umax = double.Parse(tb_Umax.Text);
                double max_speed = double.Parse(tb_maxSpeed.Text);
                MaxtorqueCapabilityCurve mtcc = buildMaxtorqueCapabilityCurve(50, Imax, Umax, max_speed);
                VoltageLimitCurve vlc = buildVoltageLimitCurve(50, Imax, Umax, max_speed);

                GraphWindow tc = new GraphWindow();
                tc.addData("Torque-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.maxtorques.ToArray()));
                tc.addData("Current-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.currents.Select(f => f.Magnitude).ToArray()));
                tc.addData("CurrentPhase-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.currents.Select(f => f.Phase).ToArray()));
                tc.addData("Voltage-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.voltages.Select(f => f.Magnitude).ToArray()));
                tc.addData("Power-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.power.ToArray()));
                tc.addData("Efficiency", new PointPairList(mtcc.speeds.ToArray(), mtcc.effs.ToArray()));
                tc.addData("Coreloss-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.coreloss.ToArray()));
                tc.addData("Boundary-Torque-speed1", new PointPairList(vlc.speeds.ToArray(), vlc.torques.ToArray()));
                tc.addData("(iq,id)", new PointPairList(mtcc.currents.Select(f => f.d).ToArray(), mtcc.currents.Select(f => f.q).ToArray()));
                tc.addData("id-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.currents.Select(f => f.d).ToArray()));
                tc.addData("iq-speed", new PointPairList(mtcc.speeds.ToArray(), mtcc.currents.Select(f => f.q).ToArray()));

                tc.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private EfficiencyMap effMap;
        private string last_config = "";

        /// <summary>
        /// Get input Imax, Umax, ... from textbox
        /// Build effmap
        /// </summary>
        /// <returns></returns>
        private bool doBuildEffmap()
        {
            bool success = true;
            double Imax = 0;
            success = success && double.TryParse(tb_Imax.Text, out Imax);
            double Umax = 0;
            success = success && double.TryParse(tb_Umax.Text, out Umax);
            double max_speed = 0;
            success = success && double.TryParse(tb_maxSpeed.Text, out max_speed);

            string config = string.Format("{0},{1},{2}", Imax, Umax, max_speed);

            if (!success)
            {
                MessageBox.Show("Error parse text to double");
                return false;
            }

            if (last_config != config)
            {
                effMap = buildEfficiencyMap(SPEED_POINT_COUNT, TORQUE_POINT_COUNT, Imax, Umax, max_speed);
                last_config = config;
            }

            return true;
        }

        /// <summary>
        /// Event handler when click bt_show_eff_map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_show_eff_map(object sender, EventArgs e)
        {
            if (!doBuildEffmap())
                return;

            MapType maptype = MapType.power;

            Enum.TryParse<MapType>(comboBox_maptype.Text, true, out maptype);

            var data = effMap.power;
            string title = "Power (W)";

            switch (maptype)
            {
                case MapType.power:
                    data = effMap.power;
                    title = "Power (W)";
                    break;
                case MapType.efficiency:
                    data = effMap.efficiency;
                    title = "Efficiency (%)";
                    break;
                case MapType.torque:
                    data = effMap.torque;
                    title = "Torque (N.m)";
                    break;
                case MapType.cogging:
                    data = effMap.cogging;
                    title = "Cogging (N.m)";
                    break;
                case MapType.windingloss:
                    data = effMap.windingloss;
                    title = "Winding loss (W)";
                    break;
                case MapType.rotor_coreloss:
                    data = effMap.rotor_coreloss;
                    title = "Rotor core loss (W)";
                    break;
                case MapType.stator_coreloss:
                    data = effMap.stator_coreloss;
                    title = "Stator loss (W)";
                    break;
                case MapType.coreloss:
                    data = effMap.coreloss;
                    title = "Core loss (W)";
                    break;
                case MapType.total_loss:
                    data = effMap.totalloss;
                    title = "Total loss (W)";
                    break;
                case MapType.coreloss_percentage:
                    data = effMap.coreloss_percentage;
                    title = "Coreloss percentage (%)";
                    break;
                case MapType.Id:
                    data = effMap.id;
                    title = "Id (A)";
                    break;
                case MapType.Iq:
                    data = effMap.iq;
                    title = "Iq (A)";
                    break;
                case MapType.voltage:
                    data = effMap.voltage;
                    title = "Voltage (V)";
                    break;
                case MapType.current:
                    data = effMap.current;
                    title = "Current (A)";
                    break;
                case MapType.beta:
                    data = effMap.beta;
                    title = "Beta (degree)";
                    break;
                case MapType.Ld:
                    data = effMap.Ld;
                    title = "Ld (mH)";
                    break;
                case MapType.Lq:
                    data = effMap.Lq;
                    title = "Lq (mH)";
                    break;
                default:
                    break;
            }

            //double fmin = double.MaxValue;
            //for (int i = 0; i < data.GetLength(0); i++)
            //    for (int j = 0; j < data.GetLength(1); j++)
            //    {
            //        if (data[i, j] >= 0 && fmin > data[i, j])
            //            fmin = data[i, j];
            //    }

            var model = new PlotModel
            {
                Title = title,
            };

            model.Axes.Add(new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(500),
                HighColor = OxyColors.Gray,
                LowColor = OxyColors.White,
                Minimum = 60,
                Maximum = 100,
                Title = title
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Speed",
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c)
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Torque",
                //AbsoluteMinimum = 0,
                //Minimum = 0,
                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Solid,
                //MajorGridlineColor = OxyColor.FromAColor(40, c),
                //MinorGridlineColor = OxyColor.FromAColor(20, c)
            });

            var mtpa = buildTableMaxtorquePerAmple();

            var hms = new HeatMapSeries
            {
                X0 = 0,
                X1 = effMap.MaxSpeed,
                Y0 = 0,
                Y1 = mtpa.GetMaxTorqueWithCurrentMagnitude(effMap.Imax),
                Data = data,
                Interpolate = false,
            };
            model.Series.Add(hms);

            // contour
            var cs = new ContourSeries
            {
                //Color = OxyColors.Gray,
                //FontSize = 12,
                //ContourLevelStep = double.NaN,                
                //LabelBackground = OxyColors.White,
                ColumnCoordinates = effMap.speed_points,
                RowCoordinates = effMap.torque_points,
                Data = data,

            };
            if (cs.Data == effMap.efficiency)
            {
                cs.ContourLevels = new double[] { 60, 70, 80, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 };
                model.Series.Add(cs);
            }

            // max-torque capability curve
            MaxtorqueCapabilityCurve mtcc = effMap.mtcc;
            var mtcc_series = new LineSeries()
            {
                LineStyle = LineStyle.Solid,
                Color = OxyColor.FromArgb(255, 0, 0, 0),
                StrokeThickness = 3,
            };
            for (int i = 0; i < mtcc.speeds.Count; i++)
            {
                mtcc_series.Points.Add(new OxyPlot.DataPoint(mtcc.speeds[i], mtcc.maxtorques[i]));
            }
            model.Series.Add(mtcc_series);

            // voltage limit curve 1
            VoltageLimitCurve vlc = effMap.vlc;
            var vlc_series = new LineSeries()
            {
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
            };
            for (int i = 0; i < vlc.speeds.Count; i++)
            {
                vlc_series.Points.Add(new OxyPlot.DataPoint(vlc.speeds[i], vlc.torques[i]));
            }
            model.Series.Add(vlc_series);

            plot.Model = model;
        }

        /// <summary>
        /// Event handler when click bt_show_curves (U, M/I=max,...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_show_curves_Click(object sender, EventArgs e)
        {
            var gw = new GraphWindow();

            var mtpa = buildTableMaxtorquePerAmple();

            int count = 10;
            try
            {
                double Imax = double.Parse(tb_Imax.Text);
                double Umax = double.Parse(tb_Umax.Text);
                double max_speed = double.Parse(tb_maxSpeed.Text);

                gw.addData("mtpa: iq(id)", new PointPairList(mtpa.idqs.Select(f => f.d).ToArray(), mtpa.idqs.Select(f => f.q).ToArray()));

                for (int i = 1; i < count + 1; i++)
                {
                    double I = Imax * i / count;
                    double t = mtpa.GetMaxTorqueWithCurrentMagnitude(I);
                    List<Fdq> idqs = curveSameTorque(t, analyser.MaxCurrent);

                    gw.addData("maxt=" + t + ",Imag=" + I, new PointPairList(idqs.Select(f => f.d).ToArray(), idqs.Select(f => f.q).ToArray()));
                }

                for (int i = 0; i < count + 1; i++)
                {
                    double II = i * 10 / count;
                    var pp = curveTorqueBetaAtCurrent(II);
                    gw.addData("II=" + II, new PointPairList(pp.Select(p => p.X).ToArray(), pp.Select(p => p.Y).ToArray()));
                }

                for (int i = 1; i < count + 1; i++)
                {
                    double speed = max_speed * i / count;

                    var vle = buildVoltageLimitEllipse(speed, 100, analyser.MaxCurrent, Umax);
                    List<Fdq> idqs = vle.curve;

                    gw.addData("speed=" + speed, new PointPairList(idqs.Select(f => f.d).ToArray(), idqs.Select(f => f.q).ToArray()));

                    gw.addData("Moment-id-speed=" + speed, new PointPairList(idqs.Select(f => f.d).ToArray(), vle.torques.ToArray()));
                }

                // show curve Lq(id,iq)
                for (int i = 0; i < analyser.CurrentSampleCount; i++)
                {
                    double id = -(i * analyser.MaxCurrent / analyser.CurrentSampleCount);
                    var dd = Enumerable.Range(0, analyser.CurrentSampleCount)
                            .Select(j => interpolateData(id, j * analyser.MaxCurrent / analyser.CurrentSampleCount, analyser.BaseSpeed));
                    gw.addData("Lq(id" + i + ",iq)", new PointPairList(dd.Select(d => d.iq).ToArray(), dd.Select(d => d.Lq).ToArray()));
                }

                gw.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler when click export as Mfile        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_export_advisor_Mfile_Click(object sender, EventArgs e)
        {
            if (!doBuildEffmap())
                return;

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Matlab M file|*.m",
                RestoreDirectory = true,
                FilterIndex = 0,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                saveAsAdvisorMFile(dialog.FileName, effMap);
            }
        }

        private void bt_export_effviewer_ansys_Click(object sender, EventArgs e)
        {
            if (!doBuildEffmap())
                return;

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Data file|*.data",
                RestoreDirectory = true,
                FilterIndex = 0,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                saveAsDatFileMachineDesignToolkitAnsys(dialog.FileName, effMap);
            }
        }

        private void bt_open_effviewer_Click(object sender, EventArgs e)
        {
            Process.Start(@"E:\zAspirant\Efficiency Map Displayer\Efficiency Map Displayer.exe");
        }
    }
}
