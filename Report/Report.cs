using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Report
{
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
}
