using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class ApprovalResultEmailView
    {
        public Guid ChangeID { get; set; }
        public int ApprovalGroupID { get; set; }
        public String ApprovalGroupName { get; set; }
        public String ApprovalName { get; set; }
        public String ChangeComment { get; set; }
    }
}
