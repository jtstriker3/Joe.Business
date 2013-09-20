using System;
namespace Joe.Business.Notification
{
    public interface INotificationProvider
    {
        void AddToNotificationCache(INotification notification);
        void DeleteNotification(INotification notification);
        void FlushNotificationCache();
        void FlushNotificationCache(INotification notification);
        void ProcessNotifications<T>(string trigger, NotificationType notificationType, T target, Joe.Business.IEmailProvider emailProvider = null);
        void SaveAlert<T>(INotification notification, T target);
        void SendEmial<T>(INotification notification, T target, Joe.Business.IEmailProvider emailProvider);
    }
}
