using System;
namespace Joe.Business.Common
{
    public interface IUser
    {
        System.Collections.Generic.List<Notification.Notification> BCCNotifications { get; set; }
        System.Collections.Generic.List<Notification.Notification> CCNotifications { get; set; }
        string Email { get; set; }
        string ID { get; set; }
        System.Collections.Generic.List<Notification.Notification> ToNotifications { get; set; }
    }
}
