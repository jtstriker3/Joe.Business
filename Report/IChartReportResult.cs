using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Report
{
    public interface IChartReportResult
    {

        Object Series { get; }
        /// <summary>
        /// Must Be Castable to IPoint
        /// </summary>
        IEnumerable Data { get; }
        //Optional Property To set
        IEnumerable<String> XAxis { get; }
        /// <summary>
        /// Optional
        /// </summary>
        IEnumerable<String> YAxis { get; }
    }
}
