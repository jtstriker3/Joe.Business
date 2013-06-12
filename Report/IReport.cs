using System;
namespace Joe.Business.Report
{
    public interface IReport
    {
        string Description { get; }
        System.Collections.Generic.IEnumerable<ReportFilter> Filters { get; set; }
        Type ListView { get; }
        System.Collections.Generic.IEnumerable<string> ListViewDisplayProperties { get; }
        Type Model { get; }
        string Name { get; set; }
        Type ReportBO { get; }
        Type ReportView { get; }
        bool Single { get; }
        System.Collections.IEnumerable SingleChoices { get; set; }
        string SingleID { get; set; }
    }
}
