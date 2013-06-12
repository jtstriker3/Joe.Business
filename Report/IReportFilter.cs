using System;
namespace Joe.Business.Report
{
    public interface IReportFilter
    {
        System.Linq.IQueryable<T> ApplyFilterToList<T>(System.Linq.IQueryable<T> list);
        System.ComponentModel.DataAnnotations.DisplayAttribute DisplayAttribute { get; }
        System.Collections.Generic.IEnumerable<string> DisplayProperties { get; }
        Type FilterType { get; }
        object GetFilterValue(object reportView);
        bool IsListFilter { get; }
        bool IsValueFilter { get; }
        System.Collections.IEnumerable ListValues { get; set; }
        Type ListView { get; }
        Type ListViewBO { get; }
        Type Model { get; }
        string PropertyName { get; set; }
        void SetFilterValue(object reportView);
        string Value { get; set; }
        string ValueProperty { get; set; }
    }
}
