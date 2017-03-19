/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PM_ACAnalyser : AbstractFEMAnalyser
    {
        /// <summary>
        /// Max value of current (A), not the rms
        /// </summary>
        public double Current { get; set; }

        /// <summary>
        /// Freq of current (Hz)
        /// </summary>
        public double Freq { get; set; }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            return (PM_ACAnalysisResults)AbstractFEMResults.loadResultsFromFile(typeof(PM_ACAnalysisResults), fromfile);
        }

        protected override AbstractFEMResults NewResults()
        {
            return new PM_ACAnalysisResults();
        }

        public override string Path_ResultsFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\ac_analysis.txt";
            }
        }

        private String WorkingFEMFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\ac_.fem";
            }
        }
        private String WorkingANSFile
        {
            get
            {
                return Path.GetDirectoryName(WorkingFEMFile) + "\\" + Path.GetFileNameWithoutExtension(WorkingFEMFile) + ".ans";
            }
        }

        protected override void analyze()
        {
            FEMM femm = FEMM.DefaultFEMM;

            // open original femm file
            femm.open(Path_OriginalFEMFile);

            // modify current, freq
            femm.mi_probdef(Freq, FEMM.UnitsType.millimeters, FEMM.ProblemType.planar, 1e-8, Motor.GeneralParams.MotorLength, 7, FEMM.ACSolverType.Succ_Approx);
            string ia = string.Format("{0}", Current);
            string ib = string.Format("{0}+I*{1}", Current * Math.Cos(-2 * Math.PI / 3), Current * Math.Sin(-2 * Math.PI / 3));
            string ic = string.Format("{0}+I*{1}", Current * Math.Cos(2 * Math.PI / 3), Current * Math.Sin(2 * Math.PI / 3));
            femm.mi_modifycircuitCurrent("A", ia);
            femm.mi_modifycircuitCurrent("B", ib);
            femm.mi_modifycircuitCurrent("C", ic);

            // save as new file and analyze            
            femm.mi_saveas(WorkingFEMFile);

            femm.mi_analyze(true);

            femm.mi_close();

            femm.open(WorkingANSFile);

            measureData(femm);

            femm.mo_close();
        }

        private void measureData(FEMM femm)
        {
            Stator3Phase stator = Motor.Stator as Stator3Phase;
            PM_ACAnalysisResults Results = this.Results as PM_ACAnalysisResults;

            //select block
            double r = (stator.xD + stator.xE) / 2;

            foreach (Stator3Phase.Coil sci in stator.coils)
            {
                int i = stator.coils.IndexOf(sci);

                //angle go clockwise from 3 o'clock (=0 degree in decarter), shift +pi/Q (to match rotor)
                int nn = stator.Q / (2 * Motor.Rotor.p);
                double aa = -2 * Math.PI * i / stator.Q + (nn % 2 == 0 ? Math.PI / stator.Q : 0);
                double x = r * Math.Cos(aa);
                double y = r * Math.Sin(aa);
                if (aa > -Motor.Rotor.alpha || aa < -2 * Math.PI + Motor.Rotor.alpha)
                    femm.mo_selectblock(x, y);
            }

            double r2 = (stator.xF2 + stator.RGap) / 2;
            femm.mo_selectblock(r2, 0);

            // measure data
            double lossMagnetic = femm.mo_blockintegral(FEMM.BlockIntegralType.Losses_Hysteresis);
            double lossResistive = femm.mo_blockintegral(FEMM.BlockIntegralType.Losses_Resistive);

            Results.coefficient_lossMagnetic = lossMagnetic / (Current * Current * Freq * Freq);
            Results.coefficient_lossResistive = lossResistive / (Current * Current);//=3/2*R

            Results.freq = Freq;
            Results.current = Current;
        }

        // create output folder
        protected override void PrepareDirectory()
        {
            Directory.CreateDirectory(Path_ToAnalysisVariant);
        }

        public static PM_ACAnalyser GetSampleMe(AbstractMotor motor)
        {
            PM_ACAnalyser aca = new PM_ACAnalyser();

            aca.AnalysisName = "ACAnalysis";
            aca.Motor = motor;
            aca.Current = 10;
            aca.Freq = 100;

            return aca;
        }
    }

    public class PM_ACAnalysisResults : AbstractFEMResults
    {
        //parameters measured
        public double coefficient_lossMagnetic;
        public double coefficient_lossResistive;

        public double current;
        public double freq;               

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            IDictionary<string, object> dict = base.BuildResultsForDisplay();

            //TODO: add variables

            return dict;
        }
    }
}
