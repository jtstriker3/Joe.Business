using Joe.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Report
{
    public class ReportFilter : Joe.Business.Report.IReportFilter
    {
        private Type ReportViewType { get; set; }
        private IReportFilterAttribute ReportFilterAttribute { get; set; }
        public String Value { get; set; }
        public IEnumerable<String> IEnumerableValue { get; set; }
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
            IEnumerableValue = new List<String>();
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

                    Object typedValue = null;
                    if (FilterType.ImplementsIEnumerable() && this.IEnumerableValue != null)
                    {
                        var genericType = FilterType.GetGenericArguments().FirstOrDefault();
                        typedValue = IEnumerableValue.Select(value => this.ChangeType(genericType, value));
                        typedValue = this.Cast(genericType, (IEnumerable)typedValue);
                    }
                    else if (Value != null)
                        typedValue = this.ChangeType(FilterType, Value);


                    var reportViewProperty = Joe.Reflection.ReflectionHelper.TryGetEvalPropertyInfo(type, ReportFilterAttribute.FilterPropertyName);
                    if (reportViewProperty != null && reportViewProperty.PropertyType == FilterType)
                        Joe.Reflection.ReflectionHelper.SetEvalProperty(reportView, ReportFilterAttribute.FilterPropertyName, typedValue);

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

        protected ICollection CreateList(Type genericType)
        {
            var listType = typeof(List<>).MakeGenericType(genericType);
            return (ICollection)Repository.CreateObject(listType);
        }

        protected Object ChangeType(Type type, Object value)
        {
            var safeType = Nullable.GetUnderlyingType(type) ?? type;
            if (safeType.IsEnum)
                return Enum.Parse(safeType, value.ToString());
            else
                return Convert.ChangeType(value, safeType);

        }

        protected IEnumerable Cast(Type genericType, IEnumerable list)
        {
            var castMethod = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(genericType);

            return (IEnumerable)castMethod.Invoke(null, new[] { list });
        }
    }
}
