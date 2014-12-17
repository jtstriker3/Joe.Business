using System;
namespace Joe.Business.Report
{
    public interface IReportFilterAttribute
    {
        System.Collections.Generic.IEnumerable<string> DisplayProperties { get; }
        string FilterPropertyName { get; }
        bool IsListFilter { get; }
        bool IsValueFilter { get; }
        Type ListView { get; }
        Type ListViewRepo { get; }
        Type Model { get; }
        string Operator { get; set; }
        string ValueProperty { get; set; }
        string RepoListFilter { get; set; }
        bool GetDisplayFromContext { get; set; }
    }
}
