using Joe.Business.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    public class ApprovalResult
    {
        [Key, Column(Order = 0)]
        public Guid ChangeID { get; set; }
        [Key, Column(Order = 1)]
        public int ApprovalGroupID { get; set; }
        public String SubmittedByID { get; set; }
        public DateTime? DateSubmitted { get; set; }
        public ResultStatus Status { get; set; }


        public virtual User SubmittedBy { get; set; }
        public virtual Change Change { get; set; }
        public virtual ApprovalGroup ApprovalGroup { get; set; }
    }
}
