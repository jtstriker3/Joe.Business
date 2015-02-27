using Joe.Business.Common;
using Joe.Map;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class ApprovalGroupView
    {
        public Boolean Included { get; set; }
        public int ID { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }


        ////Many To Many
        //[ViewMapping(ReadOnly = true)]
        //public IEnumerable<UserView> Users { get; set; }

        [AllValues(typeof(User)), JsonIgnore]
        //[ViewMapping("Users", ReadOnly = true, HowToHandleCollections = CollectionHandleType.ParentCollection, MapBackListData = false)]
        public IEnumerable<UserView> AllUsers { get; set; }

        [ViewMapping("Users-ID", ReadOnly = true)]
        public IEnumerable<String> UserIds { get; set; }
    }
}
