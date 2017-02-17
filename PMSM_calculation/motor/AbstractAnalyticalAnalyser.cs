using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractAnalyticalAnalyser : AbstractAnalyser
    {
        /// <summary>
        /// Get results
        /// </summary>
        /// <returns></returns>
        public AbstractAnalyticalResults getResults()
        {
            return (AbstractAnalyticalResults)Results;
        }

        public virtual bool isDataValid()
        {
            return true;
        }
    }

    public abstract class AbstractAnalyticalResults : AbstractResults
    {
        
    }
}
