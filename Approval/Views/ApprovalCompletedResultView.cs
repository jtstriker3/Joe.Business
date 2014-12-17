using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class ApprovalCompletedResultView
    {
        public IEnumerable<ValidationWarning> ValidationWarnings { get; set; }
        public String ApprovalName { get; set; }
        public String ChangeComments { get; set; }
        public String ChangeRequestID { get; set; }
    }
}
