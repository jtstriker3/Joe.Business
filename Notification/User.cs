using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Joe.Business.Notification
{
    public abstract class User : Joe.Business.Notification.IUser
    {
        public string ID { get; set; }
        public String Email { get; set; }
        [InverseProperty("To")]
        public virtual List<Notification> ToNotifications { get; set; }
        [InverseProperty("CC")]
        public virtual List<Notification> CCNotifications { get; set; }
        [InverseProperty("Bcc")]
        public virtual List<Notification> BCCNotifications { get; set; }
    }
}
