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
using System.Text;
using System.Web;

namespace Joe.Business.Report
{
    public class ReportRepository : Joe.Business.Report.IReportRepository
    {
        private IEnumerable<IReport> _reports;
        public virtual IEnumerable<IReport> GetReports()
        {
            _reports = _reports ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
                                        assembly.GetTypes()).Where(type =>
                                        type.GetCustomAttribute<ReportAttribute>() != null)
                                        .Select(reportViewType => typeof(ChartReportAttribute).IsAssignableFrom(reportViewType.GetCustomAttribute<ReportAttribute>().GetType()) ? new ChartReport(reportViewType) : new Report(reportViewType));

            return _reports;
        }

        public virtual IReport GetReport(String name)
        {
            return this.GetReports().Single(reprort => reprort.Name == name);
        }

        public virtual Object Run<TRepository>(IReport inReport, out IReport report)
        {
            report = this.GetReport(inReport.Name);
            //inReport.UiHint = report.UiHint;
            //inReport.Chart = report.Chart;
            //inReport.ChartType = report.ChartType;
            report.MapBack(inReport);
            //inReport.Filters = report.Filters;
            //inReport.ReportView = report.ReportView;

            if (!report.IsCustomRepository)
            {
                var reportMethod = this.GetType().GetMethod("RunReport");
                reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ReportView, typeof(TRepository));
                return reportMethod.Invoke(this, new Object[] { report, false });
            }
            else
            {
                var reportMethod = this.GetType().GetMethod("RunCustomReport");
                reportMethod = reportMethod.MakeGenericMethod(report.ReportView);
                return reportMethod.Invoke(this, new Object[] { report });
            }
        }

        public virtual Object RunCustomReport<TFilters>(IReport report)
            where TFilters : new()
        {
            var repository = (ICustomRepository<TFilters>)Repository.CreateObject(report.ReportRepo);

            var filters = new TFilters();
            foreach (var filter in report.Filters)
                filter.SetFilterValue(filters);

            if (report.Chart)
                ((IChartReport)report).SetYAxisPlotLines(filters);

            return repository.RunReport(filters);
        }

        public virtual IEnumerable GetSingleList<TContext>(IReport report)
            where TContext : IDBViewContext, new()
        {
            var reportMethod = this.GetType().GetMethod("GetFilterSingleListGeneric");
            reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ListView, typeof(TContext));
            return (IEnumerable)reportMethod.Invoke(this, new Object[] { report.ReportRepo });
        }

        public virtual Object RunReport<TModel, TViewModel, TContext>(IReport report, Boolean listOverride = false)
            where TContext : class, IDBViewContext, new()
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TContext> repo = Repository<TModel, TViewModel, TContext>.CreateRepo(report.ReportRepo);

            if (!listOverride)
            {
                repo.ViewModelMapped += (object sender, ViewModelEventArgs<TViewModel> viewModelEventArgs) =>
                {
                    foreach (var filter in report.Filters)
                        filter.SetFilterValue(viewModelEventArgs.ViewModel);
                    //Remap Repo Functions after Setting Filters
                    repo.MapRepoFunction(viewModelEventArgs.ViewModel);
                };

                repo.ViewModelListMapped += (object sender, ViewModelListEventArgs<TViewModel> viewModelListEventArgs) =>
                {
                    var filtersNotAppliedToList = report.Filters.Where(filter => !filter.IsListFilter);
                    var filtersAppliedToList = report.Filters.Where(filter => filter.IsListFilter);
                    var viewModels = filtersNotAppliedToList.Count() > 0 ? (IQueryable<TViewModel>)viewModelListEventArgs.ViewModels.ToList().AsQueryable() : viewModelListEventArgs.ViewModels;

                    foreach (var filter in filtersNotAppliedToList)
                        foreach (var viewModel in viewModels)
                            if (!String.IsNullOrEmpty(filter.Value))
                            {
                                filter.SetFilterValue(viewModel);
                            }
                    foreach (var filter in filtersAppliedToList)
                        if (!String.IsNullOrEmpty(filter.Value))
                            viewModels = filter.ApplyFilterToList(viewModels);

                    foreach (var viewModel in viewModels)
                        repo.MapRepoFunction(viewModel);

                    return viewModels.AsQueryable();
                };

            }

            var dynamicFilterObj = new TViewModel();
            foreach (var filter in report.Filters)
                filter.SetFilterValue(dynamicFilterObj);

            if (report.Chart)
                ((IChartReport)report).SetYAxisPlotLines(dynamicFilterObj);


            if (report.Single && !listOverride)
                return repo.GetWithFilters(dynamicFilterObj, report.SingleID.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries));
            else
                return repo.Get(setCrudOverride: false, dynamicFilter: dynamicFilterObj);
        }

        public virtual IEnumerable GetFilterValues<TRepository>(IReportFilter filter)
            where TRepository : IDBViewContext, new()
        {
            if (filter.ListViewRepo != null)
            {
                var getFilterMethod = this.GetType().GetMethod("GetFilterValuesGeneric");
                getFilterMethod = getFilterMethod.MakeGenericMethod(filter.Model, filter.ListView, typeof(TRepository));
                return (IEnumerable)getFilterMethod.Invoke(this, new[] { filter });
            }
            else
            {
                var context = new TRepository();
                return context.GetIPersistenceSet(filter.Model).Map(filter.ListView);
            }
        }

        public virtual String GetFilterDisplay<TRepository>(IReportFilter filter)
           where TRepository : IDBViewContext, new()
        {
            if (filter.ListViewRepo != null)
            {
                var getFilterMethod = this.GetType().GetMethod("GetFilterDisplayGeneric");
                getFilterMethod = getFilterMethod.MakeGenericMethod(filter.Model, filter.ListView, typeof(TRepository));
                return getFilterMethod.Invoke(this, new[] { filter }).ToString();
            }
            else
            {
                var context = new TRepository();
                var selectedFilter = MapExtensions.Map(context.GetIPersistenceSet(filter.Model).Find(RepoExtentions.GetTypedIDs(filter.ListView, filter.GetValue().ToArray())), filter.ListView);

                return BuildDisplay(filter, selectedFilter);
            }
        }

        public virtual IEnumerable GetFilterSingleListGeneric<TModel, TViewModel, TRepository>(Type repositoryType)
            where TRepository : class, IDBViewContext, new()
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TRepository> repo = Repository<TModel, TViewModel, TRepository>.CreateRepo(repositoryType);

            return repo.Get(setCrudOverride: false);
        }

        public virtual IEnumerable GetFilterValuesGeneric<TModel, TViewModel, TRepository>(IReportFilter filter)
            where TRepository : class, IDBViewContext, new()
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TRepository> repo = Repository<TModel, TViewModel, TRepository>.CreateRepo(filter.ListViewRepo);

            return repo.Get(setCrudOverride: false, stringFilter: filter.RepoListFilter);
        }

        public virtual String GetFilterDisplayGeneric<TModel, TViewModel, TRepository>(IReportFilter filter)
            where TRepository : class, IDBViewContext, new()
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel, TRepository> repo = Repository<TModel, TViewModel, TRepository>.CreateRepo(filter.ListViewRepo);

            var selectedFilter = repo.Get(null, false, filter.GetValue().ToArray());

            return BuildDisplay(filter, selectedFilter);

        }

        protected String BuildDisplay<TViewModel>(IReportFilter filter, TViewModel selectedFilter)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var propStr in filter.DisplayProperties)
                builder.Append(typeof(TViewModel).GetProperty(propStr).GetValue(selectedFilter));

            return builder.ToString();
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
        public String UiHint { get; private set; }
        public Boolean Chart { get; private set; }
        public String Group { get; private set; }
        public Boolean IsCustomRepository { get; private set; }
        public String SubGroup { get; private set; }

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
            UiHint = reportAttribute.UiHint;
            Chart = typeof(IChartReport).IsAssignableFrom(this.GetType());
            Group = reportAttribute.Group;
            SubGroup = reportAttribute.SubGroup;

            IsCustomRepository = reportAttribute.IsCustomRepository;
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
        public String RepoListFilter { get; set; }
        public bool GetDisplayFromContext { get; private set; }
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
            RepoListFilter = reportFilterAttribute.RepoListFilter;
            GetDisplayFromContext = reportFilterAttribute.GetDisplayFromContext;

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
            this.SetFilterValue(reportView, false);
        }

        public virtual void SetFilterValue(Object reportView, Boolean nestedView)
        {
            if (!ReportFilterAttribute.IsListFilter)
            {
                if (ReportViewType.IsAssignableFrom(reportView.GetType()) || nestedView)
                {
                    var type = reportView.GetType();
                    var useFilterValue = Joe.Reflection.ReflectionHelper.TryGetEvalPropertyInfo(type, ReportFilterAttribute.FilterPropertyName + "Active");

                    if (useFilterValue != null && useFilterValue.PropertyType == typeof(Boolean) && Value != null)
                        Joe.Reflection.ReflectionHelper.SetEvalProperty(reportView, ReportFilterAttribute.FilterPropertyName + "Active", true);

                    if (Value != null)
                    {
                        var safeType = Nullable.GetUnderlyingType(FilterType) ?? FilterType;
                        var typedValue = Convert.ChangeType(Value, safeType);
                        var reportViewProperty = Joe.Reflection.ReflectionHelper.TryGetEvalPropertyInfo(type, ReportFilterAttribute.FilterPropertyName);
                        if (reportViewProperty != null && reportViewProperty.PropertyType == FilterType)
                            Joe.Reflection.ReflectionHelper.SetEvalProperty(reportView, ReportFilterAttribute.FilterPropertyName, typedValue);
                    }

                    this.SetFilterValuesResursive(reportView);
                }
                else
                    throw new Exception("Report View must be of Type of the Passed in Report View Type");
            }
        }

        protected virtual void SetFilterValuesResursive(Object reportView)
        {
            var type = reportView.GetType();
            foreach (var prop in type.GetProperties().Where(prop => prop.PropertyType.ImplementsIEnumerable()))
            {
                var value = prop.GetValue(reportView);
                if (value != null)
                {
                    if (prop.PropertyType.ImplementsIEnumerable())
                    {
                        var valueIEnumerable = (IEnumerable)value;

                        foreach (var subView in valueIEnumerable)
                            this.SetFilterValue(subView, true);
                    }
                    else
                    {
                        this.SetFilterValue(value, true);
                    }
                }
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

        public Boolean IsOptional()
        {
            var optionalProperty = this.ReportViewType.GetProperty(this.PropertyName + "Active");
            return optionalProperty != null;
        }

        public IEnumerable<String> GetValue()
        {
            return Value.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}