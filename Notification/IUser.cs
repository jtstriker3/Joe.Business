using System;
namespace Joe.Business.Notification
{
    public interface IUser
    {
        System.Collections.Generic.List<Notification> BCCNotifications { get; set; }
        System.Collections.Generic.List<Notification> CCNotifications { get; set; }
        string Email { get; set; }
        string ID { get; set; }
        System.Collections.Generic.List<Notification> ToNotifications { get; set; }
    }
}
