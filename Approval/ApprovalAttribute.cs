using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    /// <summary>
    /// Place on view to trigger Approval
    /// </summary>
    public class ApprovalAttribute : Attribute
    {
        public String Trigger { get; private set; }

        public ApprovalAttribute(String trigger)
        {
            Trigger = trigger;
        }
    }
}
