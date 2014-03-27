using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Joe.Business
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class BusinessConfigurationAttribute : Attribute
    {
        public Boolean IncrementKey { get; set; }
        public Boolean GetListFromCache { get; set; }
        /// <summary>
        /// Validates that the User has the ability to Execute Create, Read, Update, Delete
        /// </summary>
        public Boolean EnforceSecurity { get; set; }
        /// <summary>
        /// Sets CRUD properties on ViewModel
        /// </summary>
        public Boolean SetCrud { get; set; }
        /// <summary>
        /// Wheather or not to set BOProperties for Lists
        /// </summary>
        public Boolean MapRepositoryFunctionsForList { get; set; }
        /// <summary>
        /// If set to false out Count var will remain 0. If true (Default) the count of the list will be returned
        /// </summary>
        public Boolean SetCount { get; set; }
        public Boolean UseCacheForSingleItem { get; set; }
        public Type SecurityType { get; set; }
        public Type DefualtSecurityType { get; set; }
        public IEnumerable<String> IncludeMappings { get; set; }

        public BusinessConfigurationAttribute()
        {
            EnforceSecurity = true;
            SetCrud = true;
            MapRepositoryFunctionsForList = true;
            SetCount = true;
            IncludeMappings = new String[0];
        }

        public BusinessConfigurationAttribute(params String[] includeMappings)
            : this()
        {
            IncludeMappings = includeMappings;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class IncludeAttribute : Attribute
    {
        public IEnumerable<String> IncludeMappings { get; private set; }

        public IncludeAttribute()
        {
            IncludeMappings = new String[0];
        }

        public IncludeAttribute(params String[] includeMappings)
        {
            IncludeMappings = includeMappings ?? new String[0];
        }
    }
}