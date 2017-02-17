using calc_from_geometryOfMotor.motor;
using fastJSON;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace calc_from_geometryOfMotor.motor.PMMotor
{
    public class PM_DQCurrentAnalyser : AbstractFEMAnalyser
    {
        // Parameter to input
        [ParamInfo(HelpText = "Max current to be analyzed")]
        public double MaxCurrent { get; set; }
        [ParamInfo(HelpText = "Step count of current")]
        public int CurrentSampleCount { get; set; }

        [ParamInfo(HelpText = "Step count of transient analysis")]
        public int StepCount { get; set; }
        [ParamInfo(HelpText = "BaseSpeed (rpm)")]
        public double BaseSpeed { get; set; }
        //[ParamInfo(HelpText = "Start angle (degree,mech)")]
        //public double StartAngle { get; set; }//start angle        

        protected override AbstractFEMResults NewResults()
        {
            return new DQCurrentMap();
        }

        protected override AbstractFEMResults LoadResults(string fromfile)
        {
            return (DQCurrentMap)AbstractFEMResults.loadResultsFromFile(typeof(DQCurrentMap), fromfile);
        }

        // where to save 
        public override string Path_ToAnalysisVariant
        {
            get
            {
                return Path_ToMotorVariant + "\\EfficiencyMap";
            }
        }

        public override string Path_ResultsFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\results.txt";
            }
        }

        // prepare place
        protected override void PrepareDirectory()
        {
            Directory.CreateDirectory(Path_ToAnalysisVariant);

            // save config
            String fn = Path_ToAnalysisVariant + "\\efficiencymap_config.txt";
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(AnalysisName);
                sw.Write(JSON.Beautify(JSON.ToJSON(this)));
            }
        }

        protected override void analyze()
        {
            DQCurrentMap map = Results as DQCurrentMap;

            generateListCurrents(map);

            int n = map.results.Count;
            int k = 0;

            foreach (var p in map.results)
            {
                k++;
                log.DebugFormat("Transient Analyzing {0}/{1}: id={2},iq={3}", k, n, p.idq.d, p.idq.q);
                analyzeOne(p);

                FEMM.CloseFemm();
            }
        }

        private void generateListCurrents(DQCurrentMap dqmap)
        {
            for (int i = 0; i < CurrentSampleCount + 1; i++)
                for (int j = 0; j < CurrentSampleCount + 1; j++)
                    dqmap.results.Add(new DQCurrentPointData()
                    {
                        index_d = i,
                        index_q = j,
                        idq = new Fdq()
                        {
                            d = -MaxCurrent * i / CurrentSampleCount,
                            q = MaxCurrent * j / CurrentSampleCount,
                        },
                    });
        }

        private void analyzeOne(DQCurrentPointData p)
        {
            PMTransientAnalyser analyser = new PMTransientAnalyser();
            Stator3Phase stator = this.Motor.Stator as Stator3Phase;
            double id = p.idq.d;
            double iq = p.idq.q;

            analyser.Motor = this.Motor;
            analyser.OutputPath = this.Path_ToAnalysisVariant;
            analyser.Path_OriginalFEMFile = this.Path_OriginalFEMFile;
            analyser.AnalysisName = string.Format("{0},{1}", p.index_d, p.index_q);

            analyser.StepCount = StepCount;
            double endtime = 1 / (BaseSpeed / 60 * Motor.Rotor.p);
            analyser.EndTime = endtime;
            analyser.RotorSpeed = BaseSpeed;
            double beta = (id != 0) ? Math.Atan(iq / id) : Math.PI / 2;
            if (beta <= 0)
                beta = beta + Math.PI;
            double omega = BaseSpeed / 60 * 2 * Math.PI * Motor.Rotor.p;
            double II = Math.Sqrt(id * id + iq * iq);
            analyser.StartAngle = stator.VectorMMFAngle - 360.0 / (Motor.Rotor.p * 2.0) / 2.0;
            analyser.IA = string.Format("{0}*sin({1}*time+{2})", II, omega, beta);
            analyser.IB = string.Format("{0}*sin({1}*time+{2}-2*pi/3)", II, omega, beta);
            analyser.IC = string.Format("{0}*sin({1}*time+{2}+2*pi/3)", II, omega, beta);

            analyser.RunAnalysis();

            p.transientResult = analyser.Results as PMTransientResults;

            // release memory ocupied by coreloss
            p.transientResult.RotorCoreLossResults = null;
            p.transientResult.StatorCoreLossResults = null;
            p.transientResult.Analyser = null;                      

            GC.Collect();
        }

        public static PM_DQCurrentAnalyser GetSampleMe(AbstractMotor motor)
        {
            PM_DQCurrentAnalyser dqa = new PM_DQCurrentAnalyser();

            dqa.AnalysisName = "DQCAnalysis";
            dqa.Motor = motor;

            dqa.BaseSpeed = 3000;
            dqa.MaxCurrent = 100;
            dqa.CurrentSampleCount = 10;
            dqa.StepCount = 60;

            return dqa;
        }
    }

    public class DQCurrentPointData
    {
        public int index_d;
        public int index_q;
        public Fdq idq;
        public PMTransientResults transientResult;
    }

    public class DQCurrentMap : AbstractFEMResults
    {
        public List<DQCurrentPointData> results;

        public DQCurrentMap()
        {
            results = new List<DQCurrentPointData>();
        }

        public override IDictionary<string, object> BuildResultsForDisplay()
        {
            var dict = base.BuildResultsForDisplay();

            dict.Add("OpenResults", (Analyser as PM_DQCurrentAnalyser).Path_ToAnalysisVariant);
            dict.Add("Details", this);

            return dict;
        }
    }
}
