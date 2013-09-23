using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Notification
{
    public class NotificationEmail : Joe.Business.Notification.INotificationEmail
    {
        public int ID { get; set; }
        public String Template { get; set; }
        public String Message { get; set; }
        public String ShortMessage { get; set; }
    }
}
