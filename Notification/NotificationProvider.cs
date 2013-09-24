using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Joe.MapBack;
using Joe.Caching;
using System.Threading;
using System.Linq.Expressions;
using Joe.Map;
using System.Collections;
using System.Text.RegularExpressions;
using System.Data.Entity;

namespace Joe.Business.Notification
{
    public abstract class NotificationProvider : Joe.Business.Notification.INotificationProvider
    {
        protected const String notificationCacheKey = "ee21b61d-5bdc-4adc-b56e-a7932a92565b";

        public static INotificationProvider ProviderInstance { get; private set; }

        public static void InitilizeResourceProvider<TContext>()
            where TContext : IDBViewContext, new()
        {
            var providerType = typeof(NotificationProvider<>).MakeGenericType(typeof(TContext));
            ProviderInstance = Repository.CreateObject(providerType) as NotificationProvider;
        }

        public void FlushNotificationCache()
        {
            Joe.Caching.Cache.Instance.Flush(notificationCacheKey);
        }

        public void FlushNotificationCache(INotification notification)
        {
            this.GetNotifications().Remove(notification);
        }

        public void AddToNotificationCache(INotification notification)
        {
            this.GetNotifications().Add(notification);
        }

        protected abstract ICollection<INotification> GetNotifications();

        protected Boolean ValidateNotificationProperties<T>(IEnumerable<INotificationProperty> notificationProperties, T target, T previousState = default(T))
        {
            foreach (var notificationProperty in notificationProperties)
            {
                var propertyInfo = Joe.Reflection.ReflectionHelper.GetEvalPropertyInfo(typeof(T), notificationProperty.PropertyMap);
                var propertyValue = Joe.Reflection.ReflectionHelper.GetEvalProperty(target, notificationProperty.PropertyMap);
                Object previousValue = null;
                if (previousState != null)
                    previousValue = Joe.Reflection.ReflectionHelper.GetEvalProperty(previousState, notificationProperty.PropertyMap);

                Object constant = null;
                if (notificationProperty.Value != null)
                {
                    if (typeof(Enum).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        var enumInt = 0;

                        if (int.TryParse(notificationProperty.Value, out enumInt))
                            constant = Enum.ToObject(propertyInfo.PropertyType, enumInt);
                        else
                            constant = Enum.Parse(propertyInfo.PropertyType, notificationProperty.Value);

                    }
                    else
                        constant = Convert.ChangeType(notificationProperty.Value, propertyInfo.PropertyType);
                }

                if (propertyInfo.PropertyType.ImplementsIEnumerable())
                {
                    Boolean runningValue = false;
                    if (notificationProperty.WhenAdded && previousValue != null)
                    {
                        if (((IEnumerable)propertyValue).Cast<Object>().Contains(propertyValue)
                              && !((IEnumerable)propertyValue).Cast<Object>().Contains(previousValue))
                        {
                            runningValue = true;
                        }
                    }
                    if (notificationProperty.WhenRemoved && previousValue != null)
                    {
                        if (!((IEnumerable)propertyValue).Cast<Object>().Contains(propertyValue)
                             && ((IEnumerable)propertyValue).Cast<Object>().Contains(previousValue))
                        {
                            runningValue = true;
                        }
                    }
                    if (!notificationProperty.WhenAdded && !notificationProperty.WhenRemoved)
                        if (((IEnumerable)propertyValue).Cast<Object>().Contains(propertyValue))
                        {
                            runningValue = true;
                        }

                    if (!runningValue)
                        return false;
                }
                else if (previousState != null)
                {
                    if (notificationProperty.WhenChanged
                            && constant != null)
                    {
                        if (!propertyValue.Equals(constant) || previousValue.Equals(constant))
                            return false;
                    }
                    else if (notificationProperty.WhenChanged && constant == null)
                    {
                        if (propertyValue.Equals(previousValue))
                            return false;
                    }
                    else if (!notificationProperty.WhenChanged)
                        if (!propertyValue.Equals(constant))
                            return false;
                }
                else if (!notificationProperty.WhenChanged)
                    if (!propertyValue.Equals(constant))
                        return false;

            }

            return true;
        }

        public virtual void ProcessNotifications<T>(String trigger, NotificationType notificationType, T target, T previousState = default(T), IEmailProvider emailProvider = null)
        {
            var notifications = this.GetNotifications().Where(notification =>
                                             notification.Trigger == trigger
                                             && notification.NotificationTypes == notificationType
                                             && this.ValidateNotificationProperties(notification.NotificationProperties.Cast<INotificationProperty>(), target, previousState));
            foreach (var notification in notifications)
            {
                switch (notification.AlertType)
                {
                    case AlertType.Application:
                        this.SaveAlert(notification, target);
                        break;
                    case AlertType.Email:
                        this.SendEmail(notification, target, emailProvider);
                        break;
                    case AlertType.All:
                        this.SaveAlert(notification, target);
                        this.SendEmail(notification, target, emailProvider);
                        break;

                }

                if (notification.OneOff)
                {
                    this.DeleteNotification(notification);
                    this.FlushNotificationCache(notification);
                }

            }
        }

        public abstract void SendEmail<T>(INotification notification, T target, IEmailProvider emailProvider);

        public abstract void SaveAlert<T>(INotification notification, T target);

        public abstract void DeleteNotification(INotification notification);

        protected String ParseMessage<T>(String message, T target)
        {
            if (message != null)
            {
                Regex regex = new Regex(@"(?<!\\)@[a-zA-Z0-9.-]*");
                var matches = regex.Matches(message);

                foreach (Match match in matches)
                {
                    try
                    {
                        var propertyName = match.Value.Replace("@", String.Empty);
                        var value = Joe.Reflection.ReflectionHelper.GetEvalProperty(target, propertyName);
                        if (value != null)
                            message = message.Replace(match.Value, value.ToString());
                        else
                            message = message.Replace(match.Value, "NULL");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Error Parsing Message for: {0}", match.Value), ex);
                    }
                }
            }
            return message;

        }
    }

    public class NotificationProvider<TContext> : NotificationProvider
        where TContext : IDBViewContext, new()
    {
        protected NotificationProvider()
        {
            Func<List<INotification>> getResouces = () =>
            {
                var context = new TContext();
                var notificationList = context.GetIDbSet<Notification>()
                    .Include(notification => notification.NotificationProperties)
                    .Include(notification => notification.Bcc)
                    .Include(notification => notification.CC)
                    .Include(notification => notification.To)
                    .AsNoTracking();
                if (notificationList == null)
                    throw new Exception(String.Format("Type {0} must be part of your Context", typeof(Notification).FullName));

                return notificationList.ToList<INotification>();
            };

            Joe.Caching.Cache.Instance.Add(notificationCacheKey, new TimeSpan(8, 0, 0), getResouces);
        }

        protected override ICollection<INotification> GetNotifications()
        {
            var notifications = ((List<INotification>)Cache.Instance.Get(notificationCacheKey));

            return notifications;
        }

        public override void SaveAlert<T>(INotification notification, T target)
        {
            var context = new TContext();
            var alertDbSet = context.GetIDbSet<Alert>();
            if (alertDbSet != null)
            {
                foreach (var user in notification.To)
                {
                    var alert = alertDbSet.Create();
                    alert.AlertDate = DateTime.Now;
                    alert.Message = this.ParseMessage(notification.Message, target);
                    alert.ShortMessage = this.ParseMessage(notification.Message, target);
                    alert.UserID = user.ID;
                    alertDbSet.Add(alert);
                }
                if (notification.CurrentUser)
                {
                    var alert = alertDbSet.Create();
                    alert.AlertDate = DateTime.Now;
                    alert.Message = this.ParseMessage(notification.Message, target);
                    alert.ShortMessage = this.ParseMessage(notification.Message, target);
                    alert.UserID = Joe.Security.Security.Provider.UserID;
                    alertDbSet.Add(alert);
                }
                context.SaveChanges();
            }
            else
                throw new Exception(String.Format("Type {0} must be part of your Context", typeof(Alert).FullName));
        }

        public override void SendEmail<T>(INotification notification, T target, IEmailProvider emailProvider)
        {
            if (emailProvider != null)
            {
                var context = new TContext();
                var toList = notification.To.Select(user => user.Email).ToList();
                if (notification.CurrentUser)
                {
                    var currentUser = context.GetIDbSet<User>().Find(Joe.Security.Security.Provider.UserID);
                    if (currentUser != null)
                        toList.Add(currentUser.Email);
                }
                var notificationEmail = new NotificationEmail()
                {
                    Template = notification.TemplateName,
                    Message = this.ParseMessage(notification.Message, target),
                    ShortMessage = this.ParseMessage(notification.ShortMessage, target)
                };

                Email<INotificationEmail> email = new Email<INotificationEmail>()
                {
                    BCC = notification.Bcc.Select(user => user.Email).ToList(),
                    CC = notification.CC.Select(user => user.Email).ToList(),
                    To = toList,
                    Subject = notification.Subject,
                    Model = notificationEmail
                };

                if (notification.Archive)
                {
                    var notificationEmailIDbSet = context.GetIDbSet<NotificationEmail>();
                    if (notificationEmailIDbSet != null)
                    {
                        notificationEmailIDbSet.Add(notificationEmail);
                        context.SaveChanges();
                    }
                }

                emailProvider.SendMail(email);
            }
        }

        public override void DeleteNotification(INotification notification)
        {
            var context = new TContext();
            context.GetIDbSet<Notification>().Remove((Notification)notification);
            context.SaveChanges();
        }
    }
}
