using System;
namespace Joe.Business.Notification
{
    public interface INotification
    {
        AlertType AlertType { get; set; }
        System.Collections.Generic.List<User> Bcc { get; set; }
        System.Collections.Generic.List<User> CC { get; set; }
        int ID { get; set; }
        string Message { get; set; }
        string Name { get; set; }
        System.Collections.Generic.List<NotificationProperty> NotificationProperties { get; set; }
        NotificationType NotificationTypes { get; set; }
        bool OneOff { get; set; }
        string TemplateName { get; set; }
        System.Collections.Generic.List<User> To { get; set; }
        string Trigger { get; set; }
        Boolean CurrentUser { get; set; }
        Boolean Archive { get; set; }
        String Subject { get; set; }
        String ShortMessage { get; set; }
    }
}
