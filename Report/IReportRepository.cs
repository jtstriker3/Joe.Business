using Joe.MapBack;
using System;
namespace Joe.Business.Report
{
    public interface IReportRepository
    {
        System.Collections.IEnumerable GetFilterValues<TRepository>(IReportFilter filter)
            where TRepository : IDBViewContext, new();
        System.Collections.IEnumerable GetFilterValuesGeneric<TModel, TViewModel, TRepository>(IReportFilter filter)
            where TModel : class
            where TViewModel : class, new()
            where TRepository : class, Joe.MapBack.IDBViewContext, new();
        IReport GetReport(string name);
        System.Collections.Generic.IEnumerable<IReport> GetReports();
        System.Collections.IEnumerable GetSingleList<TRepository>(IReport inReport)
            where TRepository : IDBViewContext, new();
        object Run<TRepository>(IReport inReport, out IReport report);
        object RunReport<TModel, TViewModel, TRepository>(IReport report, bool listOverride = false)
            where TModel : class
            where TViewModel : class, new()
            where TRepository : class, Joe.MapBack.IDBViewContext, new();
        String GetFilterDisplay<TRepository>(IReportFilter filter)
          where TRepository : IDBViewContext, new();
    }
}
