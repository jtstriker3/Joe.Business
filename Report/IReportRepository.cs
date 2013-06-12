using System;
namespace Joe.Business.Report
{
    public interface IReportRepository
    {
        System.Collections.IEnumerable GetFilterValues<TRepository>(IReportFilter filter);
        System.Collections.IEnumerable GetFilterValuesGeneric<TModel, TViewModel, TRepository>(IReportFilter filter)
            where TModel : class, new()
            where TViewModel : class, new()
            where TRepository : class, Joe.MapBack.IDBViewContext, new();
        IReport GetReport(string name);
        System.Collections.Generic.IEnumerable<IReport> GetReports();
        System.Collections.IEnumerable GetSingleList<TRepository>(IReport inReport);
        object Run<TRepository>(IReport inReport);
        object RunReport<TModel, TViewModel, TRepository>(IReport report, bool listOverride = false)
            where TModel : class, new()
            where TViewModel : class, new()
            where TRepository : class, Joe.MapBack.IDBViewContext, new();
    }
}
