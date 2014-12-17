using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Report
{
    public class ChartReportAttribute : ReportAttribute
    {
        public ChartTypes ChartType { get; private set; }
        public Boolean ShowLabels { get; set; }
        public int LabelAngle { get; set; }
        public String LabelColor { get; set; }
        public String LabelAlign { get; set; }
        public int LabelX { get; set; }
        public int LabelY { get; set; }
        public bool LabelShadow { get; set; }
        public string LabelStyle { get; set; }
        public int Height { get; set; }
        public int XRotation { get; set; }
        public String YAxisText { get; set; }

        /// <summary>
        /// Single Item Report. Will Generate a DropDown List to Select From
        /// </summary>
        public ChartReportAttribute(String name, String description, Type repository, Type model, Type listView, String[] listViewDisplayProperties, ChartTypes chartType)
            : base(name, description, repository, model, listView, listViewDisplayProperties)
        {
            ChartType = chartType;
        }

        /// <summary>
        /// IEnumerable Result
        /// </summary>
        public ChartReportAttribute(String name, String description, Type repository, Type model, ChartTypes chartType)
            : base(name, description, repository, model)
        {
            ChartType = chartType;
        }

        /// <summary>
        /// Custom Repository to be Invoked that Returns the Report Result
        /// </summary>
        public ChartReportAttribute(String name, String description, Type repository, ChartTypes chartType)
            : base(name, description, repository)
        {
            ChartType = chartType;
        }

        public virtual IEnumerable<PlotLine> GetYAxisPlotLines(Object filters)
        {
            return null;
        }

    }
}
