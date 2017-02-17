using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Interpolation;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PMAnalyticalAnalyser : AbstractAnalyticalAnalyser
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
        public double gammaM { get; set; }
        public int p { get; set; }
        public int Q { get; set; }
        public int Nstrand { get; set; }

        public double wm2 { get; set; }

        // dq
        public double PMd;
        public double PMq;

        public override void RunAnalysis()
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

            var Results = new PMAnalyticalResults();
            Results.Analyser = this;
            this.Results = Results;

            // calc phiD
            ls = LinearSpline.Interpolate(f_phid.ToArray(), phid_vector.ToArray());
            double phiD = ls.Interpolate(0);
            double FM = (phiR - phiHFe - phiD) / (PM + PFe + Pb);
            double phib = FM * Pb;
            double phiFe = phiHFe + FM * PFe;
            double phiM = phiD + phib + phiFe;

            ls = LinearSpline.Interpolate(phid_vector.ToArray(), Fz_vector.ToArray());
            double Fz = ls.Interpolate(phiD);
            ls = LinearSpline.Interpolate(phid_vector.ToArray(), eqvFy_vector.ToArray());
            double Fy = ls.Interpolate(phiD);

            double Bdelta = phiD / (L * wd * 1e-6);
            double BM = phiM / (L * wm * 1e-6);
            double HM = FM / (lm * 1e-3);
            double pc = phiM / (PM * FM);

            double Bz = phiD / Sz;
            double By = phiD / Sy;

            double Vz = Sz * lz;
            double Vy = Sy * ly;
            double ro_ = 7800;//kg/m^3 - specific weight
            double kPMz = 1.3 * Bz * Bz * ro_ * Vz;
            double kPMy = 1.3 * By * By * ro_ * Vy;
            Results.kPMz = kPMz;
            Results.kPMy = kPMy;

            // assign results
            Results.phiD = phiD;
            Results.Bdelta = Bdelta;
            Results.phiM = phiM;
            Results.BM = BM;
            Results.FM = FM;
            Results.HM = HM;
            Results.pc = pc;
            Results.phib = phib;
            Results.phiFe = phiFe;
            Results.Fz = Fz;
            Results.Fy = Fy;
            Results.Bz = Bz;
            Results.By = By;
            Results.wd = wd;
            Results.gammaM = gammaM;

            int q = Q / 3 / p / 2;
            double kp = Math.Sin(Math.PI / (2 * 3)) / (q * Math.Sin(Math.PI / (2 * 3 * q)));

            //Results.psiM = phiD * Nstrand * p * q * kp /*4 / Math.PI * Math.Sin(gammaM / 2 * Math.PI / 180)*/;            

            // Ld, Lq

            //double In = 3;//Ampe whatever            
            //double Fmm = q * kp * Nstrand * In;
            //double phidelta = (Fmm) / (1 / PMd + Fz / phiD + Fy / phiD) / 2;//2time,PMd=PM+Pdelta
            //double psi = phidelta * q * kp * Nstrand;
            //double LL = p * psi / In;

            //Results.LL = LL;
            //Results.Ld = 1.5 * LL;//M=-1/2L            

            //phidelta = (Fmm) / (1 / PMq + Fz / phiD + Fy / phiD) / 2;//
            //psi = phidelta * q * kp * Nstrand;
            //LL = p * psi / In;
            //Results.Lq = 1.5 * LL;

            // Resistant
            Stator3Phase stator = Motor.Stator as Stator3Phase;
            Results.R = stator.resistancePhase;

            double delta2 = delta * 1.1;
            double ns = 4 / Math.PI * kp * stator.NStrands * q;
            double dmin = delta2;
            double alphaM = Motor.Rotor is VPMRotor ? (Motor.Rotor as VPMRotor).alphaM : 180;
            //double dmax = delta2 + lm / Math.Sin(alphaM);            
            double dmax = delta2 + Constants.mu_0 * L * wd * 1e-3 * (1 / (PM + PFe + Pb) + Fy / phiD + Fz / phiD);//* wd / wm2 * ((VPMRotor)Motor.Rotor).mu_M;
            //double dmax = L * wd * 1e-3 * Constants.mu_0 / PMd;
            //double dmin2 = 1 / Math.PI * ((180 - gammaM) * dmin + gammaM * dmax) * Math.PI / 180 - 2 / Math.PI * Math.Sin(gammaM * Math.PI / 180) * (dmax - dmin);
            //double dmax2 = 1 / Math.PI * ((180 - gammaM) * dmin + gammaM * dmax) * Math.PI / 180 + 2 / Math.PI * Math.Sin(gammaM * Math.PI / 180) * (dmax - dmin);
            //double a1 = 0.5 * (1 / dmin2 + 1 / dmax2) * 1e3;
            //double a2 = 0.5 * (1 / dmin2 - 1 / dmax2) * 1e3;
            //double a1 = 0.5 * (dmin + dmax) / (dmin * dmax) * 1e3;
            //double a2 = 0.5 * (dmax - dmin) / (dmin * dmax) * 1e3;

            // test override           

            double gm = gammaM * Math.PI / 180;
            double a1 = 1 / Math.PI * (gm / dmax + (Math.PI - gm) / dmin) * 1e3;
            double a2 = -2 / Math.PI * Math.Sin(gm) * (1 / dmax - 1 / dmin) * 1e3;
            double L1 = (ns / 2) * (ns / 2) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * a1 * 4 * Math.PI * 1e-7;
            double L2 = 0.5 * (ns / 2) * (ns / 2) * Math.PI * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6 * a2 * 4 * Math.PI * 1e-7;

            Results.dmin = dmin;
            Results.dmax = dmax;
            Results.L1 = L1;
            Results.L2 = L2;
            Results.Ld = 1.5 * (L1 - L2);
            Results.Lq = 1.5 * (L1 + L2);

            double psim = 2 * Math.Sin(gammaM * Math.PI / 180 / 2) * ns * Bdelta * Motor.Rotor.RGap * Motor.GeneralParams.MotorLength * 1e-6;

            Results.psiM = psim;

            // current demagnetizing            
            //Results.Ikm = 2 * phiR / PM / (kp * Nstrand * q); //old
            double hck = Motor.Rotor is VPMRotor ? (Motor.Rotor as VPMRotor).Hck : 0;
            Results.Ikm = Math.PI * (hck - HM) * lm * 1e-3 / (2 * q * Nstrand * kp);

            dataValid = (Results.Ld > 0 && Results.Lq > 0 && Results.psiM >= 0 && gm > 0);
        }

        private bool dataValid = false;
        public override bool isDataValid()
        {
            return dataValid;
        }
    }

    public class PMAnalyticalResults : AbstractAnalyticalResults
    {
        //result which readonly from outside
        public double phiD { get; set; }
        public double Bdelta { get; set; }//1e-6 since all length units are mm
        public double phiM { get; set; }
        public double BM { get; set; }
        public double FM { get; set; }
        public double HM { get; set; }
        public double pc { get; set; }
        public double phib { get; set; }
        public double phiFe { get; set; }
        public double Fz { get; set; }
        public double Fy { get; set; }
        public double Bz { get; set; }
        public double By { get; set; }
        public double kPMz { get; set; }
        public double kPMy { get; set; }
        public double wd { get; set; }
        public double gammaM { get; set; }
        public double psiM { get; set; }
        public double Ld { get; set; }
        public double Lq { get; set; }
        public double LL { get; set; }
        public double R { get; set; }
        public double Ikm { get; set; }

        public double L1 { get; set; }
        public double L2 { get; set; }
        public double dmin { get; set; }
        public double dmax { get; set; }

        //public double psiM2 { get; set; }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            var dict = base.BuildResultsForDisplay();

            dict.Add("Bdelta", Bdelta);
            dict.Add("phiD", phiD);
            dict.Add("gammaM", gammaM);
            dict.Add("phiM", phiM);
            dict.Add("phib", phib);
            dict.Add("phiFe", phiFe);
            dict.Add("FM", FM);
            dict.Add("BM", BM);
            dict.Add("HM", HM);
            dict.Add("PC", pc);
            dict.Add("Fz", Fz);
            dict.Add("Fy", Fy);
            dict.Add("Bz", Bz);
            dict.Add("By", By);
            dict.Add("kPMz", kPMz);
            dict.Add("kPMy", kPMy);
            dict.Add("wd", wd);
            dict.Add("psiM (max)", psiM);
            //dict.Add("psiM2", psiM2);
            dict.Add("Ld", Ld);
            dict.Add("Lq", Lq);
            dict.Add("LL", LL);
            dict.Add("R", R);
            dict.Add("psiM/Ld", psiM / Ld);//maximum
            Stator3Phase stator = Analyser.Motor.Stator as Stator3Phase;
            if (stator != null)
            {
                double Jrequired = psiM / Ld / (stator.WireDiameter * stator.WireDiameter / 4 * Math.PI) / Math.Sqrt(2);
                dict.Add("psiM/Ld/Swire (rms)", Jrequired);
                dict.Add("length_one_turn", stator.wirelength_oneturn);
            }
            dict.Add("Ikm", Ikm);

            dict.Add("L1", L1);
            dict.Add("L2", L2);

            dict.Add("dmin", dmin);
            dict.Add("dmax", dmax);

            double b1 = Bdelta * 4 / Math.PI * Math.Sin(gammaM * Math.PI / 180 / 2);
            double b3 = Bdelta * 4 / (Math.PI * 3) * Math.Sin(3 * gammaM * Math.PI / 180 / 2);
            dict.Add("Bd_1", b1);
            dict.Add("Bd_3", b3);

            return dict;
        }
    }
}
