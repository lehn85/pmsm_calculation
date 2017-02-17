using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using fastJSON;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractAnalyser
    {
        protected static readonly ILog log = LogManager.GetLogger("Analyser");   

        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        /// <summary>
        /// Name of analyser
        /// </summary>
        public String AnalysisName { get; set; }

        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        /// <summary>
        /// Motor attached to analyser
        /// </summary>
        public AbstractMotor Motor { get; set; }

        [ExcludeFromMD5Calculation, ExcludeFromParams, JsonIgnore]
        /// <summary>
        /// Results of analysis
        /// </summary>
        public AbstractResults Results { get; protected set; }

        /// <summary>
        /// Hash MD5 of this analysis configuration
        /// </summary>
        /// <returns></returns>
        public virtual String GetMD5String()
        {
            return Utils.CalculateObjectMD5Hash(this);
        }

        /// <summary>
        /// Run analysis synchronously
        /// </summary>
        public virtual void RunAnalysis()
        {
            //Do nothing
        }

        /// <summary>
        /// Run analysis asynchronously
        /// </summary>
        public void StartAnalysis()
        {
            (new Thread(RunAnalysis)).Start();
        }

        protected void raiseFinishAnalysisEvent()
        {
            OnFinishedAnalysis(this, Results);
        }

        /// <summary>
        /// Event fire when finish analysis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="results"></param>
        public delegate void MyEventHandler(object sender, AbstractResults results);
        public event MyEventHandler OnFinishedAnalysis = new MyEventHandler(delegate(object sender, AbstractResults results) { });
    }

    public abstract class AbstractResults
    {
        protected static readonly ILog log = LogManager.GetLogger("ResultsLogger");

        [JsonIgnore, ExcludeFromParams, ExcludeFromMD5Calculation]
        internal AbstractAnalyser Analyser { get; set; }

        /// <summary>
        /// MD5 string as hash value for this whole transient config
        /// </summary>
        [ExcludeFromParams]
        public String MD5String { get; set; }

        /// <summary>
        /// Build a set of results that can be displayed. UI can call this to get data it needs.        
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<String, object> BuildResultsForDisplay()
        {
            return new Dictionary<String, object>();
        }
    }
}
