using Joe.Business.Common;
using Joe.Business.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    public class ApprovalGroup
    {
        public int ID { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public virtual List<User> Users { get; set; }
        public virtual List<BusinessApproval> Approvals { get; set; }
    }
}
