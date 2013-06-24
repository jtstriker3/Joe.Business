using Joe.Map;
using System;
namespace Joe.Business.Report
{
    public interface IReportFilter
    {
        System.Linq.IQueryable<T> ApplyFilterToList<T>(System.Linq.IQueryable<T> list);
        [ViewMapping(ReadOnly = true)]
        System.ComponentModel.DataAnnotations.DisplayAttribute DisplayAttribute { get; }
        [ViewMapping(ReadOnly = true)]
        System.Collections.Generic.IEnumerable<string> DisplayProperties { get; }
        [ViewMapping(ReadOnly = true)]
        Type FilterType { get; }
        object GetFilterValue(object reportView);
        [ViewMapping(ReadOnly = true)]
        bool IsListFilter { get; }
        [ViewMapping(ReadOnly = true)]
        bool IsValueFilter { get; }
        [ViewMapping(ReadOnly = true)]
        System.Collections.IEnumerable ListValues { get; set; }
        [ViewMapping(ReadOnly = true)]
        Type ListView { get; }
        [ViewMapping(ReadOnly = true)]
        Type ListViewBO { get; }
        [ViewMapping(ReadOnly = true)]
        Type Model { get; }
        [ViewMapping(ReadOnly = true)]
        string PropertyName { get; set; }
        void SetFilterValue(object reportView);
        string Value { get; set; }
        [ViewMapping(ReadOnly = true)]
        string ValueProperty { get; set; }
    }
}
