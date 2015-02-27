using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.History
{
    public class HistoryAttribute : Attribute
    {
        public Type ViewType { get; set; }

        public HistoryAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}
