using Joe.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Views
{
    public class ChangeView
    {
        public Guid ID { get; set; }
        [ViewMapping(ReadOnly = true)]
        public DateTime DateChangeRequested { get; set; }
        [ViewMapping(ReadOnly = true)]
        public DateTime? DateCompleted { get; set; }
        public String Comments { get; set; }
        [ViewMapping(ReadOnly = true)]
        public ChangeStatus Status { get; set; }

        [ViewMapping(ReadOnly = true)]
        public String ApprovalName { get; set; }
    }
}
