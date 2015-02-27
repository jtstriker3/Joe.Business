using Joe.Business.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    public class Change
    {
        public Guid ID { get; set; }
        public String Data { get; set; }
        /// <summary>
        /// ViewModel Type that the data is serialized from
        /// </summary>
        public String ViewType { get; set; }
        public String ViewAssembly { get; set; }
        public String RepositoryType { get; set; }
        public String RepositoryAssembly { get; set; }
        public Boolean Completed { get; set; }
        public DateTime DateChangeRequested { get; set; }
        public DateTime? DateCompleted { get; set; }
        public String SubmittedByID { get; set; }
        public int ApprovalID { get; set; }
        public String Comments { get; set; }
        public ChangeStatus Status { get; set; }

        public virtual User SubmittedBy { get; set; }
        public virtual BusinessApproval Approval { get; set; }
        public virtual List<ApprovalResult> Resutls { get; set; }
    }
}
