using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class ApprovalDeniedResultView
    {
        public String ApprovalName { get; set; }
        public String ChangeComments { get; set; }
        public String ChangeRequestID { get; set; }
    }
}
