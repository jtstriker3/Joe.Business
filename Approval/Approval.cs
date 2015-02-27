using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    /// <summary>
    /// Add To the Context To be Included in the Database
    /// </summary>
    public class BusinessApproval
    {
        public int ID { get; set; }
        [Required]
        public String Name { get; set; }
        public String Description { get; set; }
        /// <summary>
        /// Qulifier To be attached to Attribute on View That will tell it to trigger this approval
        /// </summary>
        public String Trigger { get; set; }
        /// <summary>
        /// Entity Type the Approval is Triggered For
        /// </summary>
        public String EntityType { get; set; }
        public String EntityAssembly { get; set; }
        public virtual List<Change> Changes { get; set; }
        public virtual List<ApprovalGroup> ApprovalGroups { get; set; }
    }
}
