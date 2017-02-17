using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PMStaticAnalyser : AbstractStaticAnalyser
    {
        protected override AbstractFEMResults NewResults()
        {
            return new PMStaticResults();
        }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            return (PMStaticResults)AbstractFEMResults.loadResultsFromFile(typeof(PMStaticResults), fromfile);
        }
    }

    public class PMStaticResults : AbstractFEMResults
    {
        /// <summary>
        /// Distribution of B in airgap. Will be positive to negative. Odd function.
        /// So if use fourier analysis, -Im(Xk) is the magnitude
        /// </summary>
        public PointD[] Bairgap { get; set; }
        public double Bdelta_max { get; set; }
        public double Bdelta { get; set; }
        public double wd { get; set; }
        public double phiD { get; set; }
        public double phiM { get; set; }
        public double phiSigma { get; set; }
        public double Fy { get; set; }
        public double Fz { get; set; }
        public double FM { get; set; }
        public double phib { get; set; }
        public double phiFe { get; set; }
        public double phisigmaS { get; set; }
        public double psiM { get; set; }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            var dict = base.BuildResultsForDisplay();

            // to open fem file
            var pmstaticanalyser = Analyser as PMStaticAnalyser;
            dict.Add("OpenResults", pmstaticanalyser.WorkingFEMFile);

            dict.Add("B-Airgap", new ListPointD(Bairgap));
            dict.Add("Bdelta", Bdelta);
            dict.Add("Bdelta_max", Bdelta_max);
            dict.Add("wd", wd);
            dict.Add("phiD", phiD);
            dict.Add("phiM", phiM);
            dict.Add("phiSigma", phiSigma);
            dict.Add("FM", FM);
            dict.Add("Fz", Fz);
            dict.Add("Fy", Fy);
            dict.Add("phib", phib);
            dict.Add("phiFe", phiFe);
            dict.Add("phisigmaS", phisigmaS);
            dict.Add("psiM", psiM);            
            return dict;
        }
    }
}
