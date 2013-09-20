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
    }
}
