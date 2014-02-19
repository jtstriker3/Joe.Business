using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Notification
{
    public static class NotificationExtensions
    {
        public static Task ProcessNotificationsAsync<T>(this INotificationProvider provider, string trigger, NotificationType notificationType, T target, T previousState = default(T), Joe.Business.IEmailProvider emailProvider = null)
        {
            Action action = () =>
            {
                provider.ProcessNotifications(trigger, notificationType, target, previousState, emailProvider);
            };
            var task = new Task(action);

            task.Start();

            return task;
        }
    }
}
