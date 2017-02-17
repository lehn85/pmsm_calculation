using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Interpolation;
using System.IO;

namespace calc_from_geometryOfMotor
{
    public class GeneralMagnetCircuit
    {
        private VectorBuilder<double> mybuilder = Vector<double>.Build;

        /**General magnet circuit is consisted of:
         *  Magnet
         *  Leak
         *  Airgap
         *  Stator: teeth and yoke
         * 
         * */
        // Magnet        
        public double PM { get; set; }//conductance
        public double Fc { get; set; }//coercive force
        public double phiR { get; set; }//redundant flux

        // Leak: bridge and barrier
        public double PFe { get; set; }
        public double phiHFe { get; set; }
        public double Pb { get; set; }
        public double Pa { get; set; }//leak through airgap to nearby pole

        // airgap
        public double Pd { get; set; }//delta

        // stator
        // characterictic steel
        public double[] Barray { get; set; }
        public double[] Harray { get; set; }
        public double Sz { get; set; }
        public double lz { get; set; }
        public double Sy { get; set; }
        public double ly { get; set; }
        public double Pslot { get; set; }

        // Other data for calculation of magnetic parameters like B, H (which not shown in magnetic circuit)
        public double wm { get; set; }
        public double lm { get; set; }
        public double L { get; set; }
        public double wd { get; set; }
        public double delta { get; set; }       

        //result which readonly from outside
        public double phiD { get; private set; }
        public double Bdelta { get { return phiD / (L * wd * 1e-6); } }//1e-6 since all length units are mm
        public double phiM { get; private set; }
        public double BM { get { return phiM / (L * wm * 1e-6); } }
        public double FM { get; private set; }
        public double phib { get; private set; }
        public double phiFe { get; private set; }
        public double Fz { get; private set; }
        public double Fy { get; private set; }

        public void calc()
        {
            int len = Barray.Length;
            // table phid-fz
            Vector<double> Fz_vector = mybuilder.Dense(len, i => Harray[i] * lz);
            Vector<double> phid_Fz_vector = mybuilder.Dense(len, i => Barray[i] * Sz + Fz_vector[i] * Pslot);

            // table phid-fy
            Vector<double> Fy_vector = mybuilder.Dense(len, i => Harray[i] * ly);
            Vector<double> phid_Fy_vector = mybuilder.Dense(len, i => Barray[i] * Sy);

            // reshape phid-fy, using interpolation to create new vector corresponding with phid_fy = phid_fz            
            Vector<double> phid_vector = mybuilder.DenseOfVector(phid_Fz_vector);//same phid table for all

            LinearSpline ls = LinearSpline.Interpolate(phid_Fy_vector.ToArray(), Fy_vector.ToArray());
            Vector<double> eqvFy_vector = mybuilder.Dense(len, i => ls.Interpolate(phid_vector[i]));

            // table f_phid
            Vector<double> f_phid = mybuilder.Dense(len, i => phid_vector[i] / Pd + Fz_vector[i] + eqvFy_vector[i] - (phiR - phiHFe - phid_vector[i]) / (PM + PFe + Pb));

            // calc phiD
            ls = LinearSpline.Interpolate(f_phid.ToArray(), phid_vector.ToArray());
            phiD = ls.Interpolate(0);
            FM = (phiR - phiHFe - phiD) / (PM + PFe + Pb);
            phib = FM * Pb;
            phiFe = phiHFe + FM * PFe;
            phiM = phiD + phib + phiFe;

            ls = LinearSpline.Interpolate(phid_vector.ToArray(), Fz_vector.ToArray());
            Fz = ls.Interpolate(phiD);
            ls = LinearSpline.Interpolate(phid_vector.ToArray(), eqvFy_vector.ToArray());
            Fy = ls.Interpolate(phiD);
        }

        /// <summary>
        /// Calc flux in airgap
        /// Assuming all teeth is in the pole
        /// </summary>
        /// <param name="Fstator">Mmf from stator in each tooth of stator</param>
        /// <param name="outputFile">Output result will be written into outputFile</param>
        /// <returns>Return flux in airgap that will enter each tooth</returns>
        public double[] calc_with_mmfStator(double[] Fstator, String outputFile)
        {
            double Sz1 = Sz / Fstator.Length; //each teeth area
            double Pslot1 = Pslot / Fstator.Length; //each slot permeability
            double Pdelta1 = Pd / Fstator.Length; //each slot-airgap permeability

            int lenBH = Barray.Length;
            int len = 100;
            // airgap phid, fd            
            Vector<double> phid = mybuilder.Dense(len, i => i * 0.00012);

            // table phid_Fy-Fy
            Vector<double> Fy = mybuilder.Dense(lenBH, i => Harray[i] * ly);
            Vector<double> phid_Fy = mybuilder.Dense(lenBH, i => Barray[i] * Sy);
            // normalize follow airgap
            LinearSpline ls = LinearSpline.Interpolate(phid_Fy.ToArray(), Fy.ToArray());
            Vector<double> eqvFy = mybuilder.Dense(len, i => ls.Interpolate(phid[i]));
            Fy = eqvFy;//now length = len

            // calc the FM on the right
            Vector<double> fRight = mybuilder.Dense(len, i => (phiR - phiHFe - phid[i]) / (PM + PFe + Pb) - Fy[i]);

            // calc the FMleft

            List<Vector<double>> phid_1t_list = new List<Vector<double>>();

            for (int k = 0; k < Fstator.Length; k++)
            {
                Vector<double> Fz = mybuilder.Dense(lenBH, i => Harray[i] * lz);
                Vector<double> phid_1t = mybuilder.Dense(lenBH, i => Barray[i] * Sz1 + Fz[i] * Pslot1);

                Vector<double> fLeft = mybuilder.Dense(lenBH, i => phid_1t[i] / Pdelta1 + Fz[i] - Fstator[k]);

                ls = LinearSpline.Interpolate(fLeft.ToArray(), phid_1t.ToArray());
                Vector<double> tempPhid_1t = mybuilder.Dense(len, i => ls.Interpolate(fRight[i]));

                phid_1t_list.Add(tempPhid_1t);
            }

            double[] ff = new double[len];
            for (int j = 0; j < len; j++)
            {
                ff[j] = 0;
                for (int k = 0; k < Fstator.Length; k++)
                {
                    ff[j] += phid_1t_list[k][j];
                }
            }
            Vector<double> f = mybuilder.Dense(len, i => ff[i] - phid[i]);

            ls = LinearSpline.Interpolate(f.ToArray(), phid.ToArray());

            phiD = ls.Interpolate(0);


            StreamWriter writer;
            if (outputFile != "")
                writer = new StreamWriter(outputFile);
            else writer = new StreamWriter(Console.OpenStandardOutput());

            writer.WriteLine("PhiD\tTotalPhid_k(ff)\tFinal eval(f)\tfRight\tFy");
            for (int j = 0; j < len; j++)
            {
                writer.WriteLine(phid[j] + "\t" + ff[j] + "\t" + f[j] + "\t" + fRight[j] + "\t" + Fy[j]);
            }

            writer.WriteLine();
            double result_phid = ls.Interpolate(0);
            writer.WriteLine("Result phid:" + result_phid);

            double[] phiD_1t = new double[Fstator.Length];
            for (int k = 0; k < Fstator.Length; k++)
            {
                ls = LinearSpline.Interpolate(phid.ToArray(), phid_1t_list[k].ToArray());
                phiD_1t[k] = ls.Interpolate(result_phid);
                writer.WriteLine(k + "\t" + phiD_1t[k]);
                //Console.WriteLine(k + "\t" + ls.Interpolate(result_phid));
            }

            for (int j = 0; j < len; j++)
            {
                for (int k = 0; k < Fstator.Length; k++)
                {
                    writer.Write(phid_1t_list[k][j] + "\t");
                }
                writer.WriteLine();
            }

            writer.Close();

            return phiD_1t;
        }

        /// <summary>
        /// Calc flux in airgap
        /// 1 Tooth is in between 2 pole
        /// </summary>
        /// <param name="Fstator">Mmf from stator in each tooth of stator. 0-is the one in the middle. 1->(Nz-1) the other teeth</param>
        /// <param name="outputFile">Output result will be written into outputFile</param>
        /// <returns>Return flux in airgap that will enter each tooth</returns>
        public double[] calc_with_mmfStator_v2(double[] Fstator, String outputFile)
        {
            double Sz1 = Sz / Fstator.Length;
            double Pslot1 = Pslot / Fstator.Length;
            double Pdelta1 = Pd / Fstator.Length;

            int lenBH = Barray.Length;
            int len = 100;
            // airgap phid, fd            
            Vector<double> phid = mybuilder.Dense(len, i => i * 0.00012);

            // table phid_Fy-Fy
            Vector<double> Fy = mybuilder.Dense(lenBH, i => Harray[i] * ly);
            Vector<double> phid_Fy = mybuilder.Dense(lenBH, i => Barray[i] * Sy);
            // normalize follow airgap
            LinearSpline ls = LinearSpline.Interpolate(phid_Fy.ToArray(), Fy.ToArray());
            Vector<double> eqvFy = mybuilder.Dense(len, i => ls.Interpolate(phid[i]));
            Fy = eqvFy;//now length = len

            // calc the FM on the right
            Vector<double> fRight = mybuilder.Dense(len, i => (phiR - phiHFe - phid[i]) / (PM + PFe + Pb + Pa) - Fy[i]);

            // calc the FMleft

            List<Vector<double>> phid_1t_list = new List<Vector<double>>();

            // for F0, phid0:
            Vector<double> Fz0 = mybuilder.Dense(lenBH, i => Harray[i] * lz);
            Vector<double> phid0 = mybuilder.Dense(lenBH, i => Barray[i] * Sz1 + Fz0[i] * Pslot1);//a table Fz0-phid0
            Vector<double> func = mybuilder.Dense(lenBH, i => phid0[i] / (Pdelta1) + Fz0[i] - Fstator[0]);//function to solve = 0
            ls = LinearSpline.Interpolate(func.ToArray(), phid0.ToArray());
            Vector<double> tempphid0 = mybuilder.Dense(len, i => ls.Interpolate(0));
            phid_1t_list.Add(tempphid0);

            // for the rest teeth
            for (int k = 1; k < Fstator.Length; k++)
            {
                Vector<double> Fz = mybuilder.Dense(lenBH, i => Harray[i] * lz);
                Vector<double> phid_1t = mybuilder.Dense(lenBH, i => Barray[i] * Sz1 + Fz[i] * Pslot1);

                Vector<double> fLeft = mybuilder.Dense(lenBH, i => phid_1t[i] / Pdelta1 + Fz[i] - Fstator[k]);

                ls = LinearSpline.Interpolate(fLeft.ToArray(), phid_1t.ToArray());
                Vector<double> tempPhid_1t = mybuilder.Dense(len, i => ls.Interpolate(fRight[i]));

                phid_1t_list.Add(tempPhid_1t);
            }


            // a table represent sum of all phidelta1t
            double[] ff = new double[len];
            for (int j = 0; j < len; j++)
            {
                ff[j] = 0;
                for (int k = 0; k < Fstator.Length; k++)
                {
                    ff[j] += phid_1t_list[k][j];
                }
            }

            Vector<double> f = mybuilder.Dense(len, i => ff[i] - phid[i]);//a function to solve = 0

            ls = LinearSpline.Interpolate(f.ToArray(), phid.ToArray());

            phiD = ls.Interpolate(0);

            StreamWriter writer = new StreamWriter(outputFile);
            writer.WriteLine("PhiD\tTotalPhid_k(ff)\tFinal eval(f)\tfRight\tFy");
            for (int j = 0; j < len; j++)
            {
                writer.WriteLine(phid[j] + "\t" + ff[j] + "\t" + f[j] + "\t" + fRight[j] + "\t" + Fy[j]);
            }

            writer.WriteLine();
            double result_phid = ls.Interpolate(0);
            writer.WriteLine("Result phid:" + result_phid);

            double[] phiD_1t = new double[Fstator.Length];
            for (int k = 0; k < Fstator.Length; k++)
            {
                ls = LinearSpline.Interpolate(phid.ToArray(), phid_1t_list[k].ToArray());
                phiD_1t[k] = ls.Interpolate(result_phid);//get each phid for each teeth
                writer.WriteLine(k + "\t" + phiD_1t[k]);
                //Console.WriteLine(k + "\t" + ls.Interpolate(result_phid));
            }

            for (int j = 0; j < len; j++)
            {
                for (int k = 0; k < Fstator.Length; k++)
                {
                    writer.Write(phid_1t_list[k][j] + "\t");
                }
                writer.WriteLine();
            }

            writer.Close();

            return phiD_1t;
        }
    }
}
