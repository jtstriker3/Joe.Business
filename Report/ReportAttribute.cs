using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Joe.Business.Report
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ReportAttribute : Attribute
    {
        public String Name { get; private set; }
        public String Description { get; private set; }
        public Type Repository { get; private set; }
        public Type Model { get; private set; }
        public Type ListView { get; private set; }
        public String UiHint { get; set; }
        public IEnumerable<String> ListViewDisplayProperties { get; private set; }
        public String Group { get; set; }
        public String SubGroup { get; set; }

        public Boolean IsCustomRepository
        {
            get
            {
                return Repository.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomRepository<>));
            }
        }

        public Boolean Single
        {
            get
            {
                return ListView != null;
            }
        }

        /// <summary>
        /// Single Item Report. Will Generate a DropDown List to Select From
        /// </summary>
        public ReportAttribute(String name, String description, Type repository, Type model, Type listView, String[] listViewDisplayProperties)
        {
            Name = name;
            Repository = repository;
            Model = model;
            ListViewDisplayProperties = listViewDisplayProperties;
            Description = description;
            ListView = listView;
        }

        /// <summary>
        /// IEnumerable Result
        /// </summary>
        public ReportAttribute(String name, String description, Type repository, Type model)
        {
            Name = name;
            Repository = repository;
            Model = model;
            Description = description;

        }

        /// <summary>
        /// Custom Repository to be Invoked that Returns the Report Result
        /// </summary>
        public ReportAttribute(String name, String description, Type repository)
        {
            Name = name;
            Repository = repository;
            Description = description;

            if (!repository.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomRepository<>)))
                throw new ArgumentException("If Model Type is not specified then Repository Type must implement ICustomRepository<>");

        }
    }
}