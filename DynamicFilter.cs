﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business
{
    /// <summary>
    /// This is for applying dynamic filters
    /// This will be called as a BO Mapping
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DynamicFilterAttribute : Attribute
    {
        private string Filter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter">Property To Filter On:Operator:Property In View To Use as Filter</param>
        public DynamicFilterAttribute(String filter)
        {
            Filter = filter;
        }

        public String GetFilter(Object viewModel)
        {
            var filters = Filter.Split(':');
            String builtFilter = null;
            for (int i = 2; i < filters.Length; i = i + 3)
            {
                if (builtFilter != null)
                    builtFilter += ':';
                builtFilter += filters[i - 2] + ':';
                builtFilter += filters[i - 2] + ':';

                builtFilter += Joe.Reflection.ReflectionHelper.GetEvalProperty(viewModel, filters[i]);
            }
            return builtFilter;
        }
    }
}