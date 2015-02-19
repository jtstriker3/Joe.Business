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
using Joe.Security;
using Joe.Business.Common;

namespace Joe.Business.Notification
{
    public class NotificationProvider : Joe.Business.Notification.INotificationProvider
    {
        protected const String notificationCacheKey = "ee21b61d-5bdc-4adc-b56e-a7932a92565b";

        private static INotificationProvider _providerInstance;
        public static INotificationProvider ProviderInstance
        {
            get
            {
                _providerInstance = _providerInstance ?? new NotificationProvider();
                return _providerInstance;
            }
        }

        //public static void InitilizeResourceProvider<TContext>()
        //    where TContext : IDBViewContext, new()
        //{
        //    var providerType = typeof(NotificationProvider<>).MakeGenericType(typeof(TContext));
        //    ProviderInstance = Repository.CreateObject(providerType) as NotificationProvider;
        //}

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

        protected Boolean ValidateNotificationProperties<T>(IEnumerable<INotificationProperty> notificationProperties, T target, T previousState = default(T))
        {
            foreach (var notificationProperty in notificationProperties)
            {
                try
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
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error Evalulating Notification Criteria: {0}", notificationProperty.PropertyMap), ex);
                }

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

        protected Boolean ValidateUser(INotification notification, IUser user)
        {

            List<String> securityGroups = new List<string>();
            if (notification.SecurityGroups != null)
                securityGroups.AddRange(notification.SecurityGroups.Split(','));
            if (notification.SecurityAreas != null)
            {
                var areas = notification.SecurityAreas.Split(',');

                foreach (var area in areas)
                {
                    var appArea = Joe.Security.SecurityConfiguration.GetInstance().ApplicationAreas.Cast<ApplicationArea>().SingleOrDefault(apa => apa.Name == area);

                    if (appArea != null)
                    {
                        if (appArea.AllRoles != null)
                            securityGroups.AddRange(appArea.AllRoles.Split(','));
                        if (appArea.CreateRoles != null)
                            securityGroups.AddRange(appArea.CreateRoles.Split(','));
                        if (appArea.ReadRoles != null)
                            securityGroups.AddRange(appArea.ReadRoles.Split(','));
                        if (appArea.UpdateRoles != null)
                            securityGroups.AddRange(appArea.UpdateRoles.Split(','));
                        if (appArea.DeleteRoles != null)
                            securityGroups.AddRange(appArea.DeleteRoles.Split(','));
                    }
                }
            }

            if (securityGroups.Count() > 0)
                return Joe.Security.Security.Provider.IsUserInRole(user.ID, securityGroups.ToArray());

            return true;
        }

        protected void RemoveInvalidUsers(INotification notification, ICollection<IUser> users)
        {
            foreach (var user in users.ToList())
            {
                if (!ValidateUser(notification, user))
                    users.Remove(user);
            }
        }

        protected NotificationProvider()
        {
            Func<List<INotification>> getResouces = () =>
            {
                var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<INotification>();
                var notificationList = (IQueryable<Notification>)context.GetIPersistenceSet<Notification>();

                if (notificationList == null)
                    throw new Exception(String.Format("Type {0} must be part of your Context", typeof(Notification).FullName));

                notificationList = notificationList.Include(notification => notification.NotificationProperties)
                                    .Include(notification => notification.Bcc)
                                    .Include(notification => notification.CC)
                                    .Include(notification => notification.To)
                                    .AsNoTracking();


                return notificationList.ToList<INotification>();
            };

            Joe.Caching.Cache.Instance.Add(notificationCacheKey, new TimeSpan(8, 0, 0), getResouces);
        }

        protected ICollection<INotification> GetNotifications()
        {
            var notifications = ((List<INotification>)Cache.Instance.Get(notificationCacheKey));

            return notifications;
        }

        public void SaveAlert<T>(INotification notification, T target)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<INotification>();
            var alertDbSet = context.GetIPersistenceSet<Alert>();
            if (alertDbSet != null)
            {
                var toList = notification.To.Cast<IUser>().ToList();
                this.RemoveInvalidUsers(notification, toList);
                CheckOwner(notification, target, context, toList);
                foreach (var user in toList)
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

        public void SendEmail<T>(INotification notification, T target, IEmailProvider emailProvider)
        {
            if (emailProvider != null)
            {
                var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<INotification>();
                var userList = notification.To.Cast<IUser>().ToList();
                this.RemoveInvalidUsers(notification, userList);
                CheckOwner<T>(notification, target, context, userList);
                var toList = userList.Select(user => user.Email).ToList();
                if (notification.CurrentUser)
                {
                    var currentUser = context.GetIPersistenceSet<User>().Find(Joe.Security.Security.Provider.UserID);
                    if (currentUser != null && !String.IsNullOrWhiteSpace(currentUser.Email) && !toList.Contains(currentUser.Email))
                        toList.Add(currentUser.Email);
                }
                CheckOwner<T>(notification, target, context, userList);
                var notificationEmail = new NotificationEmail()
                {
                    Template = notification.TemplateName,
                    Message = this.ParseMessage(notification.Message, target),
                    ShortMessage = this.ParseMessage(notification.ShortMessage, target)
                };

                var ccUsers = notification.CC.Cast<IUser>().ToList();
                RemoveInvalidUsers(notification, ccUsers);
                var bccUsers = notification.Bcc.Cast<IUser>().ToList();
                RemoveInvalidUsers(notification, bccUsers);

                Email<INotificationEmail> email = new Email<INotificationEmail>()
                {
                    BCC = bccUsers.Select(user => user.Email).Where(emailAddress => !String.IsNullOrWhiteSpace(emailAddress)).ToList(),
                    CC = ccUsers.Select(user => user.Email).Where(emailAddress => !String.IsNullOrWhiteSpace(emailAddress)).ToList(),
                    To = toList.Where(emailAddress => !String.IsNullOrWhiteSpace(emailAddress)).ToList(),
                    Subject = notification.Subject,
                    Model = notificationEmail
                };

                if (notification.Archive)
                {
                    var notificationEmailIDbSet = context.GetIPersistenceSet<NotificationEmail>();
                    if (notificationEmailIDbSet != null)
                    {
                        notificationEmailIDbSet.Add(notificationEmail);
                        context.SaveChanges();
                    }
                }
                if (email.To.Count() > 0 || email.BCC.Count() > 0 || email.CC.Count() > 0)
                    emailProvider.SendMail(email);
            }
        }

        private static void CheckOwner<T>(INotification notification, T target, IDBViewContext context, List<IUser> inList)
        {
            var toList = inList.Select(user => user.ID);
            if (notification.Owner != null)
            {
                foreach (var owner in notification.Owner.Split(','))
                {
                    var ownerInfo = Reflection.ReflectionHelper.TryGetEvalPropertyInfo(typeof(T), owner);
                    var ownerValue = Reflection.ReflectionHelper.GetEvalProperty(target, owner);
                    if (ownerInfo != null)
                    {
                        if (typeof(IUser).IsAssignableFrom(ownerInfo.PropertyType))
                        {
                            var iUser = ownerValue as IUser;
                            if (iUser != null && !String.IsNullOrWhiteSpace(iUser.Email) && !toList.Contains(iUser.ID))
                                inList.Add(iUser);
                        }
                        else if (ownerInfo.PropertyType == typeof(String))
                        {
                            var user = context.GetIPersistenceSet<User>().Find(ownerValue);
                            if (user != null && !String.IsNullOrWhiteSpace(user.Email) && !toList.Contains(user.ID))
                                inList.Add(user);
                        }
                    }
                }
            }
        }

        public void DeleteNotification(INotification notification)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<INotification>();
            context.GetIPersistenceSet<Notification>().Remove((Notification)notification);
            context.SaveChanges();
        }
    }
}
