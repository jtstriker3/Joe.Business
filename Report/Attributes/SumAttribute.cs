using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Joe.Business.Report.Attributes
{
    public class SumAttribute : Attribute
    {
        public int Precision { get; set; }
        public SumAttribute()
        {
            Precision = 2;
        }

        public SumAttribute(int precision)
        {
            Precision = precision;
        }
    }
}