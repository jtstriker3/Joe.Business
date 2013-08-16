using Joe.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Joe.Business.Report
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class ReportFilterAttribute : Attribute, Joe.Business.Report.IReportFilterAttribute
    {
        public String FilterPropertyName { get; private set; }

        /// <summary>
        /// Set this to =, !=, > ect.
        /// This is only valid on Reports that are not Single Focued
        /// If this is set it will be applied to the resulting list of the report
        /// </summary>
        public String Operator { get; set; }
        public Type ListView { get; private set; }
        public Type ListViewRepo { get; private set; }
        public Type Model { get; private set; }
        public IEnumerable<String> DisplayProperties { get; private set; }
        public String ValueProperty { get; set; }
        public int Order { get; set; }
        public Boolean IsValueFilter
        {
            get
            {
                return ListView != null && ListViewRepo != null;
            }
        }
        public Boolean IsListFilter
        {
            get
            {
                return !String.IsNullOrEmpty(Operator);
            }
        }

        public ReportFilterAttribute(int order, String filterPropertyName, Type listView, Type listViewRepo, Type model, params String[] displayProperties)
        {
            FilterPropertyName = filterPropertyName;
            ListView = listView;
            ListViewRepo = listViewRepo;
            Model = model;
            DisplayProperties = displayProperties;
            Order = order;
        }

        public ReportFilterAttribute(int order, String filterPropertyName)
        {
            FilterPropertyName = filterPropertyName;
            Order = order;
        }
    }
}