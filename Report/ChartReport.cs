using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Report
{
    public class ChartReport : Report, IChartReport
    {
        public ChartTypes ChartType { get; private set; }
        public Boolean ShowLabels { get; private set; }
        public int LabelAngle { get; private set; }
        public String LabelColor { get; private set; }
        public String LabelAlign { get; private set; }
        public int LabelX { get; private set; }
        public int LabelY { get; private set; }
        public bool LabelShadow { get; private set; }
        public string LabelStyle { get; private set; }
        public IEnumerable<PlotLine> YAxisPlotLines { get; private set; }
        private ChartReportAttribute _reportAttribute { get; set; }
        public int? Height { get; private set; }
        public int? XRotation { get; private set; }
        public String YAxisText { get; private set; }

        public ChartReport()
        {

        }

        public ChartReport(Type reportView)
            : base(reportView)
        {
            var reportAttribute = reportView.GetCustomAttribute<ChartReportAttribute>();
            _reportAttribute = reportAttribute;
            ChartType = reportAttribute.ChartType;
            ShowLabels = reportAttribute.ShowLabels;
            LabelAngle = reportAttribute.LabelAngle;
            LabelAlign = reportAttribute.LabelAlign;
            LabelColor = reportAttribute.LabelColor;
            LabelX = reportAttribute.LabelX;
            LabelY = reportAttribute.LabelY;
            LabelShadow = reportAttribute.LabelShadow;
            LabelStyle = reportAttribute.LabelStyle;
            Height = reportAttribute.Height == default(int) ? default(int?) : reportAttribute.Height;
            XRotation = reportAttribute.XRotation == default(int) ? default(int?) : reportAttribute.XRotation;
            YAxisText = reportAttribute.YAxisText;
        }

        public void SetYAxisPlotLines(Object filters)
        {
            YAxisPlotLines = _reportAttribute.GetYAxisPlotLines(filters);
        }
    }
}
