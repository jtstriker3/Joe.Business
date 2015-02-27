using Joe.Map;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class BusinessApprovalView
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

        //Many To Many
        [ViewMapping(ReadOnly = true)]
        public virtual List<ApprovalGroupView> ApprovalGroups { get; set; }

        [AllValues(typeof(ApprovalGroup), "ApprovalGroups"), JsonIgnore]
        [ViewMapping("ApprovalGroups", WriteOnly = true, HowToHandleCollections = CollectionHandleType.ParentCollection, MapBackListData = false)]
        public IEnumerable<ApprovalGroupView> AllApprovalGroups { get; set; }

        [RepoMapping("GetAllModelTypes")]
        public IDictionary<String, String> AllModelTypes { get; set; }
    }
}
