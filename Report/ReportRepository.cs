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

        public virtual Object Run(IReport inReport, out IReport report)
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
                reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ReportView);
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

        public virtual IEnumerable GetSingleList(IReport report)
        {
            var reportMethod = this.GetType().GetMethod("GetFilterSingleListGeneric");
            reportMethod = reportMethod.MakeGenericMethod(report.Model, report.ListView);
            return (IEnumerable)reportMethod.Invoke(this, new Object[] { report.ReportRepo });
        }

        public virtual Object RunReport<TModel, TViewModel>(IReport report, Boolean listOverride = false)
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel> repo = Repository<TModel, TViewModel>.CreateRepo(report.ReportRepo);

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

        public virtual IEnumerable GetFilterValues(IReportFilter filter)
        {
            if (filter.ListViewRepo != null)
            {
                var getFilterMethod = this.GetType().GetMethod("GetFilterValuesGeneric");
                getFilterMethod = getFilterMethod.MakeGenericMethod(filter.Model, filter.ListView);
                return (IEnumerable)getFilterMethod.Invoke(this, new[] { filter });
            }
            else
            {
                var context = Joe.Business.Configuration.FactoriesAndProviders.ContextFactory.CreateContext(filter.Model);
                return context.GetIPersistenceSet(filter.Model).Map(filter.ListView);
            }
        }

        public virtual String GetFilterDisplay(IReportFilter filter)
        {
            if (filter.ListViewRepo != null)
            {
                var getFilterMethod = this.GetType().GetMethod("GetFilterDisplayGeneric");
                getFilterMethod = getFilterMethod.MakeGenericMethod(filter.Model, filter.ListView);
                return getFilterMethod.Invoke(this, new[] { filter }).ToString();
            }
            else
            {
                var context = Joe.Business.Configuration.FactoriesAndProviders.ContextFactory.CreateContext(filter.Model);
                var selectedFilter = MapExtensions.Map(context.GetIPersistenceSet(filter.Model).Find(RepoExtentions.GetTypedIDs(filter.ListView, filter.GetValue().ToArray())), filter.ListView);

                return BuildDisplay(filter, selectedFilter);
            }
        }

        public virtual IEnumerable GetFilterSingleListGeneric<TModel, TViewModel>(Type repositoryType)
            where TModel : class
            where TViewModel : class, new()
        {
            IRepository<TModel, TViewModel> repo = Repository<TModel, TViewModel>.CreateRepo(repositoryType);

            return repo.Get(setCrudOverride: false);
        }

        public virtual IEnumerable GetFilterValuesGeneric<TModel, TViewModel>(IReportFilter filter)
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel> repo = Repository<TModel, TViewModel>.CreateRepo(filter.ListViewRepo);

            return repo.Get(setCrudOverride: false, stringFilter: filter.RepoListFilter);
        }

        public virtual String GetFilterDisplayGeneric<TModel, TViewModel>(IReportFilter filter)
            where TModel : class
            where TViewModel : class, new()
        {

            IRepository<TModel, TViewModel> repo = Repository<TModel, TViewModel>.CreateRepo(filter.ListViewRepo);

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
}