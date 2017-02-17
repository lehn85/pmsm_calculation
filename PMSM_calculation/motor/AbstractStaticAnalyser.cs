using fastJSON;
using System;
using System.IO;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractStaticAnalyser : AbstractFEMAnalyser
    {
        /// <summary>
        /// Make 2 options for static analyser:
        /// + On original file 
        /// + Make a copy in the output folder
        /// </summary>
        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        public bool DoAnalysisOnOriginalFEMFile { get; set; }

        /// <summary>
        /// Get results as FEMResults
        /// </summary>
        /// <returns></returns>
        public AbstractFEMResults getResults()
        {
            return (AbstractFEMResults)Results;
        }

        public override string GetMD5String()
        {
            // return motor md5 because this analysis actually has no config
            return Motor.GetMD5String();
        }

        public override String Path_ToAnalysisVariant
        {
            get
            {
                if (DoAnalysisOnOriginalFEMFile)
                    return Path.GetDirectoryName(Path_OriginalFEMFile);
                else return Path_ToMotorVariant;
            }
        }
        public override string Path_ResultsFile
        {
            get
            {
                if (DoAnalysisOnOriginalFEMFile)
                    return Path_ToAnalysisVariant + "\\" + Path.GetFileNameWithoutExtension(Path_OriginalFEMFile) + ".txt";
                return Path_ToAnalysisVariant + "\\static.txt";
            }
        }

        public string WorkingFEMFile
        {
            get
            {
                if (DoAnalysisOnOriginalFEMFile)
                    return Path_OriginalFEMFile;
                else return Path_ToAnalysisVariant + "\\static.fem";
            }
        }
        public string WorkingANSFile
        {
            get
            {
                return Path.GetDirectoryName(WorkingFEMFile) + "\\" + Path.GetFileNameWithoutExtension(WorkingFEMFile) + ".ans";
            }
        }

        protected override void PrepareDirectory()
        {
            if (DoAnalysisOnOriginalFEMFile)
                return;

            Directory.CreateDirectory(Path_ToAnalysisVariant);

            String fn = Path_ToAnalysisVariant + "\\motor_params.txt";
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.Write(JSON.Beautify(JSON.ToJSON(Motor)));
            }
        }

        /// <summary>
        /// analyze will be sealed here, the child class can only do the measure action
        /// </summary>
        protected sealed override void analyze()
        {
            FEMM femm = null;
            if (FEMMToUse != null && FEMMToUse.Count > 0)
                femm = FEMMToUse[0];
            else
                femm = FEMM.DefaultFEMM;

            // open original femm file
            femm.open(Path_OriginalFEMFile);
            // no modification (if other like transient will need to rotate rotor)
            // save as new file and analyze
            if (!DoAnalysisOnOriginalFEMFile)
                femm.mi_saveas(WorkingFEMFile);

            femm.mi_analyze(true);
            femm.mi_close();

            if (!File.Exists(WorkingANSFile))
            {
                log.Error(WorkingANSFile + " not exists, there may be something wrong with Femm file " + WorkingFEMFile);
                return;
            }

            femm.open(WorkingANSFile);

            try
            {
                measureInOpenedFem(femm);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            femm.mo_close();
        }

        protected virtual void measureInOpenedFem(FEMM femm)
        {
        }
    }
}
