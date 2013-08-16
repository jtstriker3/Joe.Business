using Joe.Business;
using Joe.Map;
using Joe.MapBack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;

namespace Joe.Business.Report
{
    public class ReportRepository : Joe.Business.Report.IReportRepository
    {
        private IEnumerable<Report> _reports;
        public virtual IEnumerable<IReport> GetReports()
        {
            _reports = _reports ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
                                        assembly.GetTypes()).Where(type =>
                                        type.GetCustomAttribute<ReportAttribute>() != null).Select(reportViewType => new Report(reportViewType));

            return _reports;
        }

        public virtual IReport GetReport(String name)
        {
            return this.GetReports().Single(reprort => reprort.Name == name);
        }

        public virtual Object Run<TRepository>(IReport inReport)
        {
            var report = this.GetReport(inReport.Name);
            report.MapBack(inReport);
            var reportMethod = this.GetType().GetMethod("RunReport");
            reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ReportView, typeof(TRepository));

            return reportMethod.Invoke(this, new Object[] { report, false });
        }

        public virtual IEnumerable GetSingleList<TRepository>(IReport report)
        {
            var reportMethod = this.GetType().GetMethod("RunReport");
            reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ListView, typeof(TRepository));

            return (IEnumerable)reportMethod.Invoke(this, new Object[] { report, true });
        }

        public virtual Object RunReport<TModel, TViewModel, TRepository>(IReport report, Boolean listOverride = false)
            where TRepository : class, IDBViewContext, new()
            where TModel : class, new()
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TRepository> repo = Repository<TModel, TViewModel, TRepository>.CreateRepo(report.ReportRepo);

            if (!listOverride)
            {
                repo.ViewModelMapped += (object sender, ViewModelEventArgs<TViewModel> viewModelEventArgs) =>
                {
                    foreach (var filter in report.Filters)
                        filter.SetFilterValue(viewModelEventArgs.ViewModel);
                };

                repo.ViewModelListMapped += (object sender, ViewModelListEventArgs<TViewModel> viewModelListEventArgs) =>
                {
                    var filtersNotAppliedToList = report.Filters.Where(filter => !filter.IsListFilter);
                    var filtersAppliedToList = report.Filters.Where(filter => filter.IsListFilter);
                    var viewModels = filtersNotAppliedToList.Count() > 0 ? (IQueryable<TViewModel>)viewModelListEventArgs.ViewModels.ToList().AsQueryable() : viewModelListEventArgs.ViewModels;

                    foreach (var filter in filtersNotAppliedToList)
                        foreach (var viewModel in viewModels)
                            if (!String.IsNullOrEmpty(filter.Value))
                                filter.SetFilterValue(viewModel);
                    foreach (var filter in filtersAppliedToList)
                        if (!String.IsNullOrEmpty(filter.Value))
                            viewModels = filter.ApplyFilterToList(viewModels);

                    return viewModels.AsQueryable();
                };

            }

            var dynamicFilterObj = new TViewModel();
            foreach (var filter in report.Filters)
                filter.SetFilterValue(dynamicFilterObj);

            if (report.Single && !listOverride)
                return repo.GetWithFilters(dynamicFilterObj, false, report.SingleID.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries));
            else
                return repo.Get(setCrudOverride: false, dynamicFilter: dynamicFilterObj);
        }

        public virtual IEnumerable GetFilterValues<TRepository>(IReportFilter filter)
        {
            var getFilterMethod = this.GetType().GetMethod("GetFilterValuesGeneric");
            getFilterMethod = getFilterMethod.MakeGenericMethod(filter.Model, filter.ListView, typeof(TRepository));
            return (IEnumerable)getFilterMethod.Invoke(this, new[] { filter });
        }

        public virtual IEnumerable GetFilterValuesGeneric<TModel, TViewModel, TRepository>(IReportFilter filter)
            where TRepository : class, IDBViewContext, new()
            where TModel : class, new()
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TRepository> repo = Repository<TModel, TViewModel, TRepository>.CreateRepo(filter.ListViewRepo);

            return repo.Get(setCrudOverride: false);
        }

    }

    public class Report : Joe.Business.Report.IReport
    {
        public Type ReportView { get; private set; }
        public String Name { get; set; }
        public String SingleID { get; set; }
        public String Description { get; private set; }
        public Type ReportRepo { get; private set; }
        public Boolean Single { get; private set; }
        public Type Model { get; private set; }
        public Type ListView { get; private set; }
        public IEnumerable<String> ListViewDisplayProperties { get; private set; }
        public IEnumerable SingleChoices { get; set; }

        //So MVC Can map back properties. This will not be valid until properties mapped to a report initilized with a Report Type
        public Report() { }

        public Report(Type reportView)
        {
            ReportView = reportView;
            var reportAttribute = reportView.GetCustomAttribute<ReportAttribute>();
            Name = reportAttribute.Name;
            Description = reportAttribute.Description;
            ReportRepo = reportAttribute.Repository;
            Single = reportAttribute.Single;
            Model = reportAttribute.Model;
            ListView = reportAttribute.ListView;
            ListViewDisplayProperties = reportAttribute.ListViewDisplayProperties;

        }

        private IEnumerable<ReportFilter> _filters;
        public IEnumerable<ReportFilter> Filters
        {
            get
            {
                _filters = _filters ?? (ReportView != null ?
                    ReportView.GetCustomAttributes<ReportFilterAttribute>(true).OrderBy(rfa => rfa.Order).Select(filter => new ReportFilter(ReportView, filter)).ToList()
                    : null);

                return _filters;

            }
            set
            {
                _filters = value;
            }
        }
    }

    public class ReportFilter : Joe.Business.Report.IReportFilter
    {
        private Type ReportViewType { get; set; }
        private IReportFilterAttribute ReportFilterAttribute { get; set; }
        public String Value { get; set; }
        [ViewMapping(Key = true)]
        public String PropertyName { get; set; }
        private PropertyInfo Info { get; set; }
        public Type ListView { get; private set; }
        public Type ListViewRepo { get; private set; }
        public Type Model { get; private set; }
        public IEnumerable<String> DisplayProperties { get; private set; }
        public String ValueProperty { get; set; }
        public Boolean IsListFilter { get; private set; }
        public Boolean IsValueFilter { get; private set; }
        public IEnumerable ListValues { get; set; }
        /// <summary>
        /// Set this to =, !=, > ect.
        /// This is only valid on Reports that are not Single Focued
        /// If this is set it will be applied to the resulting list of the report
        /// </summary>

        public ReportFilter() { }

        public ReportFilter(Type reportView, IReportFilterAttribute reportFilterAttribute)
        {
            ReportViewType = reportView;
            ReportFilterAttribute = reportFilterAttribute;
            Info = Joe.Reflection.ReflectionHelper.GetEvalPropertyInfo(ReportViewType, ReportFilterAttribute.FilterPropertyName);
            PropertyName = Info.Name;
            ListView = reportFilterAttribute.ListView;
            ListViewRepo = reportFilterAttribute.ListViewRepo;
            Model = reportFilterAttribute.Model;
            DisplayProperties = reportFilterAttribute.DisplayProperties;
            ValueProperty = reportFilterAttribute.ValueProperty;
            IsListFilter = reportFilterAttribute.IsListFilter;
            IsValueFilter = reportFilterAttribute.IsValueFilter;

        }

        public virtual IQueryable<T> ApplyFilterToList<T>(IQueryable<T> list)
        {
            if (this.ReportFilterAttribute.IsListFilter)
            {
                var filterString = this.ReportFilterAttribute.FilterPropertyName + ":" + this.ReportFilterAttribute.Operator + ":" + this.Value;
                return list.Filter(filterString);
            }
            return list;
        }

        public virtual void SetFilterValue(Object reportView)
        {
            if (!ReportFilterAttribute.IsListFilter)
            {
                if (ReportViewType.IsAssignableFrom(reportView.GetType()))
                {
                    var typedValue = Convert.ChangeType(Value, FilterType);
                    Joe.Reflection.ReflectionHelper.SetEvalProperty(reportView, ReportFilterAttribute.FilterPropertyName, typedValue);
                }
                else
                    throw new Exception("Report View must be of Type of the Passed in Report View Type");
            }
        }

        public virtual Object GetFilterValue(Object reportView)
        {
            if (!ReportFilterAttribute.IsListFilter)
            {
                if (ReportViewType.IsAssignableFrom(reportView.GetType()))
                    return Joe.Reflection.ReflectionHelper.GetEvalProperty(reportView, ReportFilterAttribute.FilterPropertyName);
                throw new Exception("Report View must be of Type of the Passed in Report View Type");
            }
            return null;
        }

        public virtual Type FilterType
        {
            get
            {
                return Info != null ? Info.PropertyType : null;
            }
        }

        public virtual DisplayAttribute DisplayAttribute
        {
            get
            {
                return Info != null ? Info.GetCustomAttribute<DisplayAttribute>() : null;
            }
        }
    }
}