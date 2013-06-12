using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Joe.Business.Report
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ReportAttribute : Attribute, Joe.Business.Report.IReportAttribute
    {
        public String Name { get; private set; }
        public String Description { get; private set; }
        public Type BusinessObject { get; private set; }
        public Type Model { get; private set; }
        public Type ListView { get; set; }
        public IEnumerable<String> ListViewDisplayProperties { get; private set; }
        public Boolean Single
        {
            get
            {
                return ListView != null;
            }
        }

        public ReportAttribute(String name, Type businessObject, Type model, String description, params String[] listViewDisplayProperties)
        {
            Name = name;
            BusinessObject = businessObject;
            Model = model;
            ListViewDisplayProperties = listViewDisplayProperties;
            Description = description;
        }
    }
}