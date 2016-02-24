using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Joe.Business
{
    public class ValidationWarning
    {
        public String PropertyName { get; set; }
        public String Message { get; set; }
        public dynamic Identity { get; set; } = new ExpandoObject();

        public ValidationWarning(String message)
        {
            Message = message;
            PropertyName = String.Empty;
        }

        public ValidationWarning(String propertyName, String message)
            : this(message)
        {
            PropertyName = propertyName;
        }
    }
}
