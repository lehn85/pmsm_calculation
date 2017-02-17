using fastJSON;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace calc_from_geometryOfMotor.motor
{
    public class CoreLoss
    {
        // first input data (in constructor)
        private AbstractTransientAnalyser analyser;
        private List<FEMM.Element> elements;
        private List<PointD> nodes;

        // input data
        public string name;
        public bool isRotor = false;
        public double ro;
        public double Keddy;//eddy loss (W/kg) at 1T 50Hz
        public double Kh;//P1,0/50 (W/kg)   

        // measured/gathered data     
        private List<double[]> Bx;//List of element, each is array double by step
        private List<double[]> By;

        private List<double[]> Hx;
        private List<double[]> Hy;

        /// <summary>
        /// Create coreloss model from transient analyser and list of elements
        /// </summary>
        /// <param name="analyser"></param>
        /// <param name="es"></param>
        public CoreLoss(AbstractTransientAnalyser analyser, List<PointD> nodes, List<FEMM.Element> es)
        {
            this.analyser = analyser;
            elements = es;
            this.nodes = nodes;

            Bx = new List<double[]>();
            By = new List<double[]>();

            for (int i = 0; i < elements.Count; i++)
            {
                Bx.Add(new double[analyser.StepCount + 1]);
                By.Add(new double[analyser.StepCount + 1]);
            }

            Hx = new List<double[]>();
            Hy = new List<double[]>();

            for (int i = 0; i < elements.Count; i++)
            {
                Hx.Add(new double[analyser.StepCount + 1]);
                Hy.Add(new double[analyser.StepCount + 1]);
            }
        }

        /// <summary>
        /// Call this function on each step analysis to gather Bx,By data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="femm"></param>
        public void GatherData(TransientStepArgs args, FEMM femm)
        {
            //rotate                 
            double xRotorAngle = args.RotorAngle;
            if (isRotor)
            {
                // normalize, make rotorAngle inside (-2alpha,2alpha)                    
                xRotorAngle = analyser.Motor.GetNormalizedRotorAngle(args.RotorAngle);

                //convert to radian
                xRotorAngle *= Math.PI / 180;
            }

            // get flux density of all elements in stator for 1 step            
            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];

                double x = e.center.X;
                double y = e.center.Y;

                //rotate
                if (isRotor)
                {
                    double xx = x * Math.Cos(xRotorAngle) - y * Math.Sin(xRotorAngle);
                    double yy = x * Math.Sin(xRotorAngle) + y * Math.Cos(xRotorAngle);
                    x = xx;
                    y = yy;
                }

                var pv = femm.mo_getpointvalues(x, y);

                if (isRotor)
                {
                    double xx = pv.B1 * Math.Cos(-xRotorAngle) - pv.B2 * Math.Sin(-xRotorAngle);
                    double yy = pv.B1 * Math.Sin(-xRotorAngle) + pv.B2 * Math.Cos(-xRotorAngle);
                    pv.B1 = xx;
                    pv.B2 = yy;

                    xx = pv.H1 * Math.Cos(-xRotorAngle) - pv.H2 * Math.Sin(-xRotorAngle);
                    yy = pv.H1 * Math.Sin(-xRotorAngle) + pv.H2 * Math.Cos(-xRotorAngle);
                    pv.H1 = xx;
                    pv.H2 = yy;
                }

                Bx[i][args.step] = pv.B1;
                By[i][args.step] = pv.B2;

                Hx[i][args.step] = pv.H1;
                Hy[i][args.step] = pv.H2;
            }
        }

        #region Calculation derived data

        public CoreLossResults Results { get; private set; }

        public void calculateCoreLoss()
        {
            if (elements == null || elements.Count == 0)
                return;

            if (Bx == null || Bx.Count == 0)
                return;
            if (By == null || By.Count == 0)
                return;
            if (Bx.Count != By.Count && Bx.Count != elements.Count)
                return;

            Results = new CoreLossResults()
            {
                name = name,
                ro = ro,
                Keddy = Keddy,
                Kh = Kh,
                basefreq = 1 / analyser.EndTime,
                length = analyser.Motor.GeneralParams.MotorLength,
                elements = elements,
                nodes = nodes,
                Bx = Bx,
                By = By,
                multiply = analyser.Motor.GeneralParams.FullBuildFEMModel ? 1 : analyser.Motor.Rotor.p * 2,
            };

            Results.calculateCoreLoss();
        }

        #endregion
    }

    [Serializable]
    /// <summary>
    /// Class for save,load,calculate derived results for coreloss
    /// </summary>
    public class CoreLossResults
    {
        #region Source Data

        public string name;

        // input data from motor model
        public double ro;//specific weight kg/m3
        public double Keddy;//eddy loss (W/kg) at 1T 50Hz
        public double Kh;//P1,0/50 (W/kg)

        // input data from motor model
        public double basefreq; //base frequency for this coreloss analysis, 
        public double length;//length motor to calculate volume and mass
        public double multiply;//multiply if model is partial

        // measured data (stored on disk or just measure during transient analysis)
        public List<FEMM.Element> elements;
        public List<PointD> nodes;
        public List<double[]> Bx;//List of element, each is array double by step
        public List<double[]> By;

        public List<double[]> Hx;
        public List<double[]> Hy;

        #endregion

        #region Save/load measured data        

        ///// <summary>
        ///// Save data as JSON
        ///// </summary>
        //public void SaveDataAsJSON()
        //{
        //    // Bxy by times
        //    using (StreamWriter sw = new StreamWriter(analyser.Path_ToAnalysisVariant + "\\" + name + "Bxy_.txt"))
        //    {
        //        // X block
        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            // 1 line is 1 element
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                // value of Bx of that element in step j
        //                sb.Append(Bx[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }

        //        sw.WriteLine("\r\n");

        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                sb.Append(By[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }
        //    }

        //    using (StreamWriter sw = new StreamWriter(analyser.Path_ToAnalysisVariant + "\\" + name + "Bxy_fft.txt"))
        //    {
        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                sb.Append(Results.Bfftx[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }

        //        sw.WriteLine("\r\n");

        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                sb.Append(Results.Bffty[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }
        //    }

        //    using (StreamWriter sw = new StreamWriter(analyser.Path_ToAnalysisVariant + "\\" + name + "elements.txt"))
        //    {
        //        sw.Write(JSON.Beautify(JSON.ToJSON(elements)));
        //    }

        //    // H
        //    using (StreamWriter sw = new StreamWriter(analyser.Path_ToAnalysisVariant + "\\" + name + "Hxy_.txt"))
        //    {
        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                sb.Append(Hx[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }

        //        sw.WriteLine("\r\n");

        //        for (int i = 0; i < elements.Count; i++)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            for (int j = 0; j < analyser.StepCount + 1; j++)
        //            {
        //                sb.Append(Hy[i][j] + "\t");
        //            }

        //            sw.WriteLine(sb.ToString());
        //        }
        //    }

        //}

        /// <summary>
        /// Save measured data as binary
        /// </summary>
        public void SaveDataAsBinary(string fn)
        {
            try
            {
                using (Stream file = File.Open(fn, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(file, this);
                    //bin.Serialize(file, elements);
                    //bin.Serialize(file, Bx);
                    //bin.Serialize(file, By);
                    //bin.Serialize(file, Hx);
                    //bin.Serialize(file, Hy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Save data to " + fn + " failed:" + ex.Message);
            }
        }

        public static CoreLossResults LoadFromBinaryData(string fn)
        {
            try
            {
                using (Stream file = File.Open(fn, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    CoreLossResults results = (CoreLossResults)bin.Deserialize(file);
                    return results;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load data from " + fn + " failed:" + ex.Message);
                return null;
            }
        }

        #endregion

        #region Derived data

        // FFT of Bx, By
        public List<double[]> Bfftx { get; private set; }
        public List<double[]> Bffty { get; private set; }

        /// <summary>
        /// Loss of each Element on hysteresis
        /// </summary>
        public List<double> elementHysLosses { get; private set; }

        /// <summary>
        /// Loss of each element on eddy
        /// </summary>
        public List<double> elementEddyLosses { get; private set; }

        // hysteresis
        public double LossHysteresis { get; private set; }
        // eddy
        public double LossEddy { get; private set; }

        public double TotalCoreLoss { get { return LossHysteresis + LossEddy; } }

        /// <summary>
        /// Calculate derived data from source data
        /// </summary>
        public void calculateCoreLoss()
        {
            //double ro = 7800;//kg/m3
            //double Keddy = 2.5;//GOST 2311 (0.65mm lamination)//0.663 / ro;//0.53 is watt/(m^3*T^2*Hz^2) -> convert to watt/kg
            ////double Keddy = 0.663 * 2500 / ro;//M-19 29Ga (0.18mm lamination) America (Watt/(kg*T2*(50Hz)^2) ~ 0.21 W/kg = P,1.0/50
            //double Kh = 200 * 50 / ro;//=1,3W/kg 

            if (Bx == null || Bx.Count == 0)
                return;
            if (By == null || By.Count == 0)
                return;
            if (Bx.Count != By.Count)
                return;

            Bfftx = new List<double[]>();
            foreach (var bx in Bx)
            {
                Complex[] samples = bx.Select(d => new Complex(d, 0)).ToArray();

                Fourier.Forward(samples, FourierOptions.NoScaling);

                //double[] bfftx = samples.Select(c => c.Real / bx.Length * 2).ToArray();
                double[] bfftx = samples.Select(c => c.Magnitude / bx.Length * 2).ToArray();
                bfftx[0] /= 2;

                Bfftx.Add(bfftx);
            }

            Bffty = new List<double[]>();
            foreach (var by in By)
            {
                Complex[] samples = by.Select(d => new Complex(d, 0)).ToArray();

                Fourier.Forward(samples, FourierOptions.NoScaling);

                //double[] bffty = samples.Select(c => c.Real / by.Length * 2).ToArray();
                double[] bffty = samples.Select(c => c.Magnitude / by.Length * 2).ToArray();
                bffty[0] /= 2;

                Bffty.Add(bffty);
            }

            double f = basefreq;//base freq = 1/Tcycle
            f /= 50;//use Russian GOST

            double loss_hysteresis = 0;
            double loss_eddy = 0;
            double k_ob = 1.5;//k обработки (копылов) зависит от обработки стали
            double beta = 1.5;//показатель степени, зависящий от марки стали (2212)

            elementHysLosses = new List<double>();
            elementEddyLosses = new List<double>();

            // sweep elements
            for (int i = 0; i < Bfftx.Count; i++)
            {
                double[] bfftx = Bfftx[i];
                double[] bffty = Bffty[i];

                // mass of element
                double me = elements[i].area * 1e-6 * length * 1e-3 * ro;

                // loss of element
                double pe_h = 0;
                double pe_e = 0;

                // sweep all frequencies, but half since it is mirrored
                for (int j = 0; j < bfftx.Length / 2; j++)
                {
                    // skip DC part
                    if (j == 0)
                        continue;

                    double bsq = bfftx[j] * bfftx[j] + bffty[j] * bffty[j];

                    pe_h += Kh * j * f * bsq * me;
                    //pe_e += Keddy * (j * f) * (j * f) * bsq * me;
                    pe_e += k_ob * Keddy * Math.Pow(j * f, beta) * bsq * me;//копылов
                }

                elementHysLosses.Add(pe_h);
                elementEddyLosses.Add(pe_e);

                loss_hysteresis += pe_h;
                loss_eddy += pe_e;
            }

            LossHysteresis = loss_hysteresis * multiply;
            if (double.IsNaN(LossHysteresis) || double.IsInfinity(LossHysteresis))
                LossHysteresis = -1;

            LossEddy = loss_eddy * multiply;
            if (double.IsNaN(LossEddy) || double.IsInfinity(LossEddy))
                LossEddy = -1;
        }

        #endregion
    }
}
