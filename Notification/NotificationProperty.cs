using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Notification
{
    public class NotificationProperty : Joe.Business.Notification.INotificationProperty
    {
        public int ID { get; set; }
        public String PropertyMap { get; set; }
        public String Value { get; set; }
        public Boolean WhenChanged { get; set; }
        public Boolean WhenRemoved { get; set; }
        public Boolean WhenAdded { get; set; }
        public int NotificationID { get; set; }
        public virtual Notification Notification { get; set; }
    }
}
