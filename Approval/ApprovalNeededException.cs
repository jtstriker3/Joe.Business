using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    public class ApprovalNeededException : Exception
    {
        public Guid ChangeID { get; set; }

        public ApprovalNeededException(Guid changeID)
        {
            ChangeID = changeID;
        }
        
    }
}
