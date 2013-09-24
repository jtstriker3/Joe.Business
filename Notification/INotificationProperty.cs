using System;
namespace Joe.Business.Notification
{
    public interface INotificationProperty
    {
        int ID { get; set; }
        string PropertyMap { get; set; }
        string Value { get; set; }
        Boolean WhenChanged { get; set; }
        Boolean WhenRemoved { get; set; }
        Boolean WhenAdded { get; set; }
        int NotificationID { get; set; }
    }
}
