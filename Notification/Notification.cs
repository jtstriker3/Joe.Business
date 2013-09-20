﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Notification
{
    public class Notification : Joe.Business.Notification.INotification
    {
        public int ID { get; set; }
        public String Name { get; set; }
        public String Trigger { get; set; }
        public String TemplateName { get; set; }
        public String Message { get; set; }
        public Boolean OneOff { get; set; }
        public NotificationType NotificationTypes { get; set; }
        public virtual List<NotificationProperty> NotificationProperties { get; set; }
        public virtual List<User> Bcc { get; set; }
        public virtual List<User> CC { get; set; }
        public virtual List<User> To { get; set; }
        public AlertType AlertType { get; set; }
        public Boolean CurrentUser { get; set; }
    }
}