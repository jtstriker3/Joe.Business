﻿using System;
namespace Joe.Business.Report
{
    public interface IReportAttribute
    {
        Type Repository { get; }
        string Description { get; }
        Type ListView { get; set; }
        System.Collections.Generic.IEnumerable<string> ListViewDisplayProperties { get; }
        Type Model { get; }
        string Name { get; }
        bool Single { get; }
        String Group { get; }
        ChartTypes ChartType { get; }
    }
}
