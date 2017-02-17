using fastJSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractFEMAnalyser : AbstractAnalyser
    {
        /// <summary>
        /// Path to output folder
        /// </summary>
        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        public virtual String OutputPath { get; set; }

        /// <summary>
        /// Path to the original FEM file
        /// </summary>
        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        public virtual String Path_OriginalFEMFile { get; set; }

        /// <summary>
        /// Path to motor variant (
        /// </summary>       
        public virtual String Path_ToMotorVariant
        {
            get
            {
                return OutputPath + "\\" + Motor.GetMD5String();
            }
        }

        /// <summary>
        /// Path to analysis variants under motor variant
        /// </summary>
        public virtual String Path_ToAnalysisVariant
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomOutputDir))
                    return CustomOutputDir;

                return Path_ToMotorVariant;
            }
        }

        /// <summary>
        /// Path to result file (txt) of this current variant
        /// </summary>
        public virtual String Path_ResultsFile
        {
            get
            {
                return Path_ToAnalysisVariant + "\\results.txt";
            }
        }

        [ExcludeFromParams]
        [ExcludeFromMD5Calculation]
        public string CustomOutputDir { get; set; }

        /// <summary>
        /// FEMM to reuse. Assign before analyze to reuse FEMM windows.
        /// </summary>   
        [ExcludeFromParams]
        [ExcludeFromMD5Calculation]
        public List<FEMM> FEMMToUse { get; set; }

        /// <summary>
        /// Must implement in derived class to create instance of results that match analyser 
        /// </summary>
        /// <returns></returns>
        protected abstract AbstractFEMResults NewResults();

        /// <summary>
        /// Must implement in derived class to load data saved on disk
        /// </summary>
        /// <param name="fromfile"></param>
        /// <returns></returns>
        protected abstract AbstractFEMResults LoadResults(String fromfile);

        /// <summary>
        /// Start FEMM analysis        
        /// </summary>
        /// <param name="config"></param>
        public override void RunAnalysis()
        {
            // check input if ok or not
            if (!CheckInput())
                return;

            // prepare directory to store data
            PrepareDirectory();

            // load results from disk  
            Results = LoadResults(Path_ResultsFile);
            // if results ok, just return
            if (Results != null && GetMD5String() == Results.MD5String)
            {
                Results.Analyser = this;
                ((AbstractFEMResults)Results).ProcessDataAfterLoad();
                raiseFinishAnalysisEvent();
                return;
            }

            // make new results
            Results = NewResults();

            // call the worker
            analyze();

            // save the md5
            if (Results != null)
            {
                Results.MD5String = GetMD5String();
                Results.Analyser = this;
                ((AbstractFEMResults)Results).ProcessDataAfterLoad();
            }

            OnFinishAnalysis();

            SaveResultsToDisk();

            // finished event            
            raiseFinishAnalysisEvent();
        }

        /// <summary>
        /// Main work here, must be implemented in descendant class
        /// </summary>
        protected virtual void analyze()
        {

        }

        /// <summary>
        /// Check input like Motor, Path_ToOriginalFEMFile
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckInput()
        {
            if (Motor == null)
                throw new InvalidOperationException("Motor hasn't been assigned");

            if (!File.Exists(Path_OriginalFEMFile))
                throw new FileNotFoundException("Original FEM file hasn't been created or path is incorrect (" + Path_OriginalFEMFile + ")");

            return true;
        }

        /// <summary>
        /// Create output directory
        /// Analysis that doesn't need to create directory should override this one
        /// </summary>
        protected virtual void PrepareDirectory()
        {
        }

        /// <summary>
        /// When analysis finished
        /// </summary>
        protected virtual void OnFinishAnalysis()
        {

        }

        /// <summary>
        /// Method to load results from disk
        /// </summary>
        public virtual void LoadResultsFromDisk()
        {
            if (Motor == null)
                throw new InvalidOperationException("Motor hasn't been assigned");

            String md5 = GetMD5String();

            // if already loaded, just return
            if (Results != null && Results.MD5String == md5)
                return;

            // call load result (abstract method will be implemented in child class)
            Results = LoadResults(Path_ResultsFile);

            // if has results            
            if (Results != null)
            {
                // check md5, if invalid then result is null
                if (Results.MD5String != md5)
                    Results = null;

            }

            // results ok
            if (Results != null)
            {
                Results.Analyser = this;
                ((AbstractFEMResults)Results).ProcessDataAfterLoad();
            }
        }

        /// <summary>
        /// Save to disk
        /// Analysis that doesn't need to save results should override this one
        /// </summary>
        protected virtual void SaveResultsToDisk()
        {
            if (Results != null)
                ((AbstractFEMResults)Results).saveResultsToFile(Path_ResultsFile);
        }        
    }

    public abstract class AbstractFEMResults : AbstractResults
    {
        // for json export to string purpose
        public AbstractFEMResults()
        {
        }

        #region save/load results to disk

        public virtual void saveResultsToFile(String fn)
        {
            using (StreamWriter sw = new StreamWriter(fn))
            {
                string str = JSON.ToJSON(this);
                sw.Write(str);
            }
        }

        public static object loadResultsFromFile(Type type, String fn)
        {
            if (!File.Exists(fn))
                return null;

            using (StreamReader sr = new StreamReader(fn))
            {
                try
                {
                    string str = sr.ReadToEnd();
                    object results = JSON.ToObject(str, type);
                    return results;
                }
                catch (Exception ex)
                {
                    log.Error("While import json: " + ex.Message);
                    return null;
                }
            }
        }

        #endregion

        public virtual void ProcessDataAfterLoad()
        {

        }
    }
}
