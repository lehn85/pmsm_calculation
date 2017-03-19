/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fastJSON;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractMMAnalyser : AbstractFEMAnalyser
    {
        // where to save 
        public override string Path_ToAnalysisVariant
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomOutputDir))
                    return CustomOutputDir;

                return Path_ToMotorVariant + "\\MMAnalysis";
            }
        }

        public override string Path_ResultsFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\results_" + GetMD5String() + ".txt";
            }
        }

        // prepare place
        protected override void PrepareDirectory()
        {
            Directory.CreateDirectory(Path_ToAnalysisVariant);

            // save config
            String fn = Path_ToAnalysisVariant + "\\mmconfig_" + GetMD5String() + ".txt";
            using (StreamWriter sw = new StreamWriter(fn))
            {
                sw.WriteLine(AnalysisName);
                sw.Write(JSON.Beautify(JSON.ToJSON(this)));
            }
        }

        /// <summary>
        /// Make matlab m file function, or simulink model 
        /// </summary>
        public virtual void MakeMatlabSimulinkModelFile(String outputfile, String original = "")
        {

        }        
    }
}
