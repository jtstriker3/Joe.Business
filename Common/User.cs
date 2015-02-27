using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Joe.Business.Notification;
using Joe.Business.Approval;

namespace Joe.Business.Common
{
    public abstract class User : Joe.Business.Common.IUser
    {
        public string ID { get; set; }
        public String Email { get; set; }
        [InverseProperty("To")]
        public virtual List<Notification.Notification> ToNotifications { get; set; }
        [InverseProperty("CC")]
        public virtual List<Notification.Notification> CCNotifications { get; set; }
        [InverseProperty("Bcc")]
        public virtual List<Notification.Notification> BCCNotifications { get; set; }
        public virtual List<ApprovalGroup> ApprovalGroups { get; set; }
        [InverseProperty("UpdatedBy")]
        public virtual List<Joe.Business.History.History> HistoryRecords { get; set; }
    }
}
