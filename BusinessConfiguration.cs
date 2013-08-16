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
        public Boolean UseSecurity { get; set; }
        /// <summary>
        /// Sets CRUD properties on ViewModel
        /// </summary>
        public Boolean SetCrud { get; set; }
        /// <summary>
        /// Wheather or not to set BOProperties for Lists
        /// </summary>
        public Boolean MapRepositoryFunctionsForList { get; set; }
        public Type SecurityType { get; set; }
        public Type DefualtSecurityType { get; set; }
        public BusinessConfigurationAttribute()
        {
            UseSecurity = true;
            SetCrud = true;
            MapRepositoryFunctionsForList = true;
        }
    }
}