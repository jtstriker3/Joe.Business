using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Report
{
    public interface IChartReport : IReport
    {
        ChartTypes ChartType { get; }
        Boolean ShowLabels { get; }
        int LabelAngle { get; }
        String LabelColor { get; }
        String LabelAlign { get; }
        int LabelX { get; }
        int LabelY { get; }
        bool LabelShadow { get; }
        string LabelStyle { get; }
        IEnumerable<PlotLine> YAxisPlotLines { get; }
        void SetYAxisPlotLines(Object filters);
        int? Height { get; }
        int? XRotation { get; }
        String YAxisText { get; }
    }
}
