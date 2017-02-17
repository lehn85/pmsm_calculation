using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MathNet.Numerics.Interpolation;
using calc_from_geometryOfMotor.RootFinding;
using OxyPlot.Axes;
using OxyPlot.Series;
using ZedGraph;
using MathNet.Numerics.Optimization;

namespace calc_from_geometryOfMotor
{
    public partial class PMSM_EfficiencyMap_Analytical : Form
    {
        public PMSM_EfficiencyMap_Analytical()
        {
            InitializeComponent();
        }

        private void PMSM_EfficiencyMap_Analytical_Load(object sender, EventArgs e)
        {
            comboBox_maptype.Items.AddRange(typeof(MapType).GetEnumNames());

            bt_apply_params_Click(null, null);
        }

        #region Motor Parameters

        private double Ld;
        private double Lq;
        private double R = 0.04;
        private double p = 4;
        private double psiM = 0.146;

        private double rotor_Hloss_coeff;
        private double rotor_Eloss_coeff;
        private double stator_Hloss_coeff;
        private double stator_Eloss_coeff;

        private void bt_apply_params_Click(object sender, EventArgs e)
        {
            bool success = true;
            success = success && double.TryParse(tb_Ld.Text, out Ld);
            success = success && double.TryParse(tb_Lq.Text, out Lq);
            success = success && double.TryParse(tb_R.Text, out R);
            success = success && double.TryParse(tb_pp.Text, out p);
            success = success && double.TryParse(tb_psiM.Text, out psiM);

            success = success && double.TryParse(tb_rotor_Hloss_coeff.Text, out rotor_Hloss_coeff);
            success = success && double.TryParse(tb_rotor_Eloss_coeff.Text, out rotor_Eloss_coeff);
            success = success && double.TryParse(tb_stator_Hloss_coeff.Text, out stator_Hloss_coeff);
            success = success && double.TryParse(tb_stator_Eloss_coeff.Text, out stator_Eloss_coeff);

            Ld *= 1e-3;//convert back from mH to H
            Lq *= 1e-3;

            if (!success)
                MessageBox.Show("Some paramters are invalid");

        }

        #endregion

        #region Point data (id,iq,speed)

        private class APointData
        {
            public double id;
            public double iq;
            public double speed;
            public double torque;//0-
            public double rotorloss;
            public double statorloss;
            public double copperloss;

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
                    return p;
                    //return p < 0 ? 0 : p;
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

        private APointData calculatePointdata(double id, double iq, double speed)
        {
            double omega_e = speed / 60 * 2 * Math.PI * p;
            double freq_e = speed / 60 * p;
            APointData data = new APointData()
            {
                id = id,
                iq = iq,
                speed = speed,

                torque = 1.5 * p * (psiM * iq + (Ld - Lq) * id * iq),
                ed = -Lq * iq * omega_e,
                eq = (psiM + Ld * id) * omega_e,
                copperloss = 1.5 * R * (id * id + iq * iq),
                rotorloss = rotor_Hloss_coeff * freq_e / 50 + rotor_Eloss_coeff * (freq_e / 50) * (freq_e / 50),
                statorloss = stator_Hloss_coeff * freq_e / 50 + stator_Eloss_coeff * (freq_e / 50) * (freq_e / 50),
            };

            data.ud = R * id + data.ed;
            data.uq = R * iq + data.eq;

            return data;
        }

        #endregion

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

        private MTPA _mtpa;

        private string _param_config = "";

        private MTPA buildTableMaxtorquePerAmple()
        {
            string config = string.Format("{0},{1},{2},{3},{4}", R, p, Ld, Lq, psiM);
            if (_param_config == config)
            {
                return _mtpa;
            }

            int stepcount = 100;
            //int beta_count = 90;
            var idqs = new List<Fdq>();
            var max_torques = new List<double>();

            int k = 0;

            for (int i = 0; i < stepcount + 1; i++)
            {
                double II = 1000 * i / stepcount;

                var minimizer = new GoldenSectionMinimizer();
                var objfnc = new SimpleObjectiveFunction1D(beta_rad =>
                {
                    double id = II * Math.Cos(beta_rad);
                    double iq = II * Math.Sin(beta_rad);
                    var data = calculatePointdata(id, iq, 3000);
                    k++;
                    return -data.torque;
                });
                var min = minimizer.FindMinimum(objfnc, Math.PI / 2, Math.PI);
                if (min != null)
                {
                    double beta_rad = min.MinimizingPoint;
                    double max_t = -min.FunctionInfoAtMinimum.Value;
                    max_torques.Add(max_t);
                    idqs.Add(new Fdq
                    {
                        d = II * Math.Cos(beta_rad),
                        q = II * Math.Sin(beta_rad),
                    });
                }
            }

            Console.WriteLine("Call calc count = " + k);            

            MTPA mtpa = new MTPA()
            {
                idqs = idqs,
                max_torques = max_torques
            };

            _mtpa = mtpa;
            _param_config = config;

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
                    var d = calculatePointdata(id, iq_, 3000);

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
                var data = calculatePointdata(id, iq, 3000);
                c.Add(new PointD { X = beta, Y = data.torque });
            }

            return c;
        }

        #endregion

        #region Maxtorque-capability-curve

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
                var data_ = calculatePointdata(idq.d, idq.q, s);
                var u_ = Math.Sqrt(data_.ud * data_.ud + data_.uq * data_.uq);
                return u_ - Umax;
            }, 100, max_speed, 1e-3);

            // speed by speed            
            int count1 = (int)(speed1 / max_speed * count) + 1;
            int count2 = count - count1;

            for (int i = 1; i < count1 + 1; i++)
            {
                double speed = speed1 * i / count1;

                var data = calculatePointdata(idq.d, idq.q, speed);

                mtcc.speeds.Add(speed);
                mtcc.maxtorques.Add(data.torque);
                mtcc.currents.Add(idq);
                mtcc.voltages.Add(new Fdq() { d = data.ud, q = data.uq });
                mtcc.power.Add(data.power);
                mtcc.effs.Add(data.efficiency);
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

                var vle = buildVoltageLimitEllipse(speed, 1000, Imax, Umax);

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
                else
                {
                    var range = Enumerable.Range(0, vle.maxtorque_point);

                    LinearSpline spline = LinearSpline.Interpolate(range.Select(k => vle.curve[k].Magnitude), range.Select(k => vle.curve[k].d));
                    double id = spline.Interpolate(Imax);
                    double iq = Math.Sqrt(Imax * Imax - id * id);
                    idq2 = new Fdq { d = id, q = iq };
                    found = true;
                }

                if (found)
                {
                    //double id = idq2.d;//Imax * Math.Cos(beta * Math.PI / 180);
                    //double iq = idq2.q;//Imax * Math.Sin(beta * Math.PI / 180);
                    var data = calculatePointdata(idq2.d, idq2.q, speed);

                    mtcc.speeds.Add(speed);
                    mtcc.maxtorques.Add(data.torque);
                    mtcc.currents.Add(idq2);
                    mtcc.voltages.Add(new Fdq() { d = data.ud, q = data.uq });
                    mtcc.power.Add(data.power);
                    mtcc.effs.Add(data.efficiency);
                }
            }

            return mtcc;
        }

        #endregion

        #region VoltageLimitCurve

        /// <summary>
        /// Data of the curve speed(torque), where speed cannot increase without increase beta
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
            var mtpa = buildTableMaxtorquePerAmple();

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

                // look for speed, that is suitable for given current (in max torque condition) and u=Umax
                double ss = 0;
                bool success = Brent.TryFindRoot(s =>
                {
                    var data_ = calculatePointdata(idq.d, idq.q, s);
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

            for (int j = 0; j < count + 1; j++)
            {
                double id = -Imax * j / count;
                double iq;
                double t = 0;
                bool bb = Brent.TryFindRoot(iq_ =>
                {
                    var d = calculatePointdata(id, iq_, speed);
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

            if (vle.curve[0].d < 0)
            {
                Fdq idq1 = vle.curve[0];
                Fdq idq2 = vle.curve[1];
                Fdq idq0 = new Fdq
                {
                    d = idq1.d + (Imax / count) * (idq1.q / idq2.q) / (1 - idq1.q / idq2.q),
                    q = 0,
                };
                vle.curve.Insert(0, idq0);
                vle.torques.Insert(0, 0);
                vle.maxtorque_point++;
            }

            return vle;
        }

        #endregion

        #region EfficiencyMap

        private enum MapType
        {
            power,
            windingloss,
            rotor_coreloss,
            stator_coreloss,
            coreloss,
            total_loss,
            efficiency,
            voltage,
            current,
            beta,
            Ld,
            Lq,
        }

        private class EfficiencyMap
        {
            public double Imax;
            public double Umax;
            public double MaxSpeed;

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

            // loss
            public double[,] windingloss;
            public double[,] rotor_coreloss;
            public double[,] stator_coreloss;
            public double[,] coreloss;
            public double[,] totalloss;

            // efficiency
            public double[,] efficiency;

            // other
            public double[,] voltage;
            public double[,] current;
            public double[,] beta;
            public double[,] Ld;
            public double[,] Lq;
        }

        private EfficiencyMap buildEfficiencyMap(int torque_count, int speed_count, double Imax, double Umax, double max_speed)
        {
            MaxtorqueCapabilityCurve mtcc = buildMaxtorqueCapabilityCurve(speed_count, Imax, Umax, max_speed);
            VoltageLimitCurve vlc = buildVoltageLimitCurve(speed_count, Imax, Umax, max_speed);

            var em = new EfficiencyMap()
            {
                // input
                Imax = Imax,
                Umax = Umax,
                MaxSpeed = max_speed,

                // axis
                speed_points = new double[speed_count + 1],
                torque_points = new double[torque_count + 1],

                // value
                power = new double[speed_count + 1, torque_count + 1],
                windingloss = new double[speed_count + 1, torque_count + 1],
                rotor_coreloss = new double[speed_count + 1, torque_count + 1],
                stator_coreloss = new double[speed_count + 1, torque_count + 1],
                coreloss = new double[speed_count + 1, torque_count + 1],
                totalloss = new double[speed_count + 1, torque_count + 1],
                efficiency = new double[speed_count + 1, torque_count + 1],

                voltage = new double[speed_count + 1, torque_count + 1],
                current = new double[speed_count + 1, torque_count + 1],
                beta = new double[speed_count + 1, torque_count + 1],
                Ld = new double[speed_count + 1, torque_count + 1],
                Lq = new double[speed_count + 1, torque_count + 1],
            };
            em.power.Fill2D(double.NaN);
            em.windingloss.Fill2D(double.NaN);
            em.rotor_coreloss.Fill2D(double.NaN);
            em.stator_coreloss.Fill2D(double.NaN);
            em.coreloss.Fill2D(double.NaN);
            em.totalloss.Fill2D(double.NaN);
            em.efficiency.Fill2D(double.NaN);

            em.voltage.Fill2D(double.NaN);
            em.current.Fill2D(double.NaN);
            em.beta.Fill2D(double.NaN);
            em.Ld.Fill2D(double.NaN);
            em.Lq.Fill2D(double.NaN);

            var mtpa = buildTableMaxtorquePerAmple();
            double max_t = mtpa.GetMaxTorqueWithCurrentMagnitude(Imax);

            LinearSpline torqueCapabilityLimit = LinearSpline.Interpolate(mtcc.speeds, mtcc.maxtorques);

            for (int i = 0; i < speed_count + 1; i++)
                for (int j = 0; j < torque_count + 1; j++)
                {
                    // speed
                    double speed = max_speed * i / speed_count;
                    em.speed_points[i] = speed;

                    // torque
                    double t = max_t * j / torque_count;
                    em.torque_points[j] = t;
                    double max_possible_torque = torqueCapabilityLimit.Interpolate(speed);
                    if (t > max_possible_torque)
                        continue;

                    // speed limit 1
                    double speed1 = vlc.GetMaxSpeedForTorque(t);

                    APointData data = null;

                    if (t == 0)
                    {
                        data = calculatePointdata(0, 0, speed);
                    }
                    // zone 1
                    else if (speed <= speed1)
                    {
                        Fdq idq = mtpa.GetCurrentForTorque(t);
                        if (double.IsNaN(idq.d) || double.IsNaN(idq.q))
                            data = null;
                        else
                            data = calculatePointdata(idq.d, idq.q, speed);
                    }
                    else
                    {
                        Fdq idq = getCurrentForZone2(t, speed, Imax, Umax);
                        if (double.IsNaN(idq.d) || double.IsNaN(idq.q))
                            data = null;
                        else
                            data = calculatePointdata(idq.d, idq.q, speed);
                    }

                    if (data != null)
                    {
                        em.rotor_coreloss[i, j] = data.rotorloss;
                        em.stator_coreloss[i, j] = data.statorloss;
                        em.coreloss[i, j] = data.rotorloss + data.statorloss;
                        em.windingloss[i, j] = data.copperloss;
                        em.totalloss[i, j] = data.copperloss + data.rotorloss + data.statorloss;
                        em.power[i, j] = data.power;
                        em.efficiency[i, j] = 100 * data.power / (data.power + em.totalloss[i, j]);

                        em.voltage[i, j] = data.voltage;
                        em.current[i, j] = data.current;
                        em.beta[i, j] = data.beta;
                        em.Ld[i, j] = double.IsNaN(data.Ld) || data.Ld < 0 ? -0.0001 : data.Ld * 1000;//to mH
                        em.Lq[i, j] = double.IsNaN(data.Ld) || data.Ld < 0 ? -0.0001 : data.Lq * 1000;//to mH
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
                vle = buildVoltageLimitEllipse(speed, 100, Imax, Umax);
                dict_vle[key] = vle;
            }

            var range = Enumerable.Range(0, vle.maxtorque_point);

            // find point on Voltage limit ellipse that bring given torque
            LinearSpline spline = LinearSpline.Interpolate(range.Select(i => vle.torques[i]), range.Select(i => vle.curve[i].d));
            double id = spline.Interpolate(torque);

            // get iq from this id
            var maxiq_point = vle.curve[vle.maxtorque_point];
            var minid_point = vle.curve[vle.minid_point];
            if (id < maxiq_point.d || minid_point.d < id)
                return default(Fdq);

            LinearSpline spline2 = LinearSpline.Interpolate(vle.curve.Select(f => f.d), vle.curve.Select(f => f.q));
            double iq = spline2.Interpolate(id);
            return new Fdq
            {
                d = id,
                q = iq,
            };
        }

        #endregion

        private EfficiencyMap effMap;
        private string last_config = "";

        private void bt_showmap_Click(object sender, EventArgs e)
        {
            bool success = true;
            double Imax = 0;
            success = success && double.TryParse(tb_Imax.Text, out Imax);
            double Umax = 0;
            success = success && double.TryParse(tb_Umax.Text, out Umax);
            double max_speed = 0;
            success = success && double.TryParse(tb_maxSpeed.Text, out max_speed);

            string config = string.Format("{0},{1},{2}", Imax, Umax, max_speed);

            MapType maptype = MapType.power;

            Enum.TryParse<MapType>(comboBox_maptype.Text, true, out maptype);

            if (!success)
            {
                MessageBox.Show("Error parse text to double");
                return;
            }

            var mtpa = buildTableMaxtorquePerAmple();

            int n = 100;
            double x0 = 0;
            double x1 = max_speed;
            double y0 = 0;
            double y1 = mtpa.GetMaxTorqueWithCurrentMagnitude(Imax);

            if (last_config != config)
            {
                effMap = buildEfficiencyMap(50, 100, Imax, Umax, max_speed);
                last_config = config;
            }

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
                    title = "Ld (mH)";
                    break;
                default:
                    break;
            }

            double fmin = double.MaxValue;
            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (data[i, j] >= 0 && fmin > data[i, j])
                        fmin = data[i, j];
                }

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
                Minimum = fmin,
                //Maximum = 100,
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

            var hms = new HeatMapSeries
            {
                X0 = x0,
                X1 = x1,
                Y0 = y0,
                Y1 = y1,
                Data = data,
                Interpolate = true,
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
                cs.ContourLevels = new double[] { 0, 20, 40, 60, 80, 90, 91, 92, 93, 94, 95, 95.2, 95.4, 95.6, 95.8, 96, 96.2, 97, 98, 99 };
                model.Series.Add(cs);
            }

            // max-torque capability curve
            MaxtorqueCapabilityCurve mtcc = buildMaxtorqueCapabilityCurve(100, Imax, Umax, max_speed);
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
            VoltageLimitCurve vlc = buildVoltageLimitCurve(100, Imax, Umax, max_speed);
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

        private void bt_show_maxtorque_capa_curve_Click(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
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
                    List<Fdq> idqs = curveSameTorque(t, 300);

                    gw.addData("maxt=" + t + ",Imag=" + I, new PointPairList(idqs.Select(f => f.d).ToArray(), idqs.Select(f => f.q).ToArray()));
                }

                for (int i = 1; i < count + 1; i++)
                {
                    double speed = max_speed * i / count;

                    var vle = buildVoltageLimitEllipse(speed, 300, 300, Umax);
                    List<Fdq> idqs = vle.curve;

                    gw.addData("speed=" + speed, new PointPairList(idqs.Select(f => f.d).ToArray(), idqs.Select(f => f.q).ToArray()));

                    gw.addData("Moment-id-speed=" + speed, new PointPairList(idqs.Select(f => f.d).ToArray(), vle.torques.ToArray()));
                }                

                gw.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error " + ex.Message);
            }
        }


    }
}
