using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Joe.Caching;
using Joe.Initialize;
using Joe.Map;
using Joe.Business.Approval.Views;
using Joe.Business.Configuration;

namespace Joe.Business.Approval
{

    public class ApprovalProvider
    {
        protected const String approvalCacheKey = "5bb3ed31-3012-454c-8087-c6880563fc27";
        protected IEmailProvider EmailProvider { get; set; }
        private static ApprovalProvider _providerInstance;
        public static ApprovalProvider Instance
        {
            get
            {
                if (Configuration.BusinessConfigurationSection.Instance.UseApproval)
                {
                    _providerInstance = _providerInstance ?? new ApprovalProvider();
                    return _providerInstance;
                }
                return null;
            }
        }

        internal protected ApprovalProvider()
        {
            EmailProvider = FactoriesAndProviders.EmailProvider;

            Func<List<BusinessApproval>> approvaFunc = () =>
            {
                var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
                var approvalList = (IQueryable<BusinessApproval>)context.GetIPersistenceSet<BusinessApproval>();

                if (approvalList == null)
                    throw new Exception(String.Format("Type {0} must be part of your Context or set BussinessConfiguration.UseApproval = false", typeof(BusinessApproval).FullName));
                approvalList = approvalList
                                    .Include(a => a.ApprovalGroups)
                                    .Include(a => a.ApprovalGroups.Select(ag => ag.Users))
                                    .AsNoTracking();

                var result = approvalList.ToList<BusinessApproval>();
                context.Dispose();
                return result;

            };

            Cache.Instance.GetOrAdd(approvalCacheKey, new TimeSpan(8, 0, 0), approvaFunc);

        }

        protected List<BusinessApproval> GetCacehedApprovals()
        {
            return (List<BusinessApproval>)Cache.Instance.Get(approvalCacheKey);
        }

        protected BusinessApproval GetApprovalForType(Type type, String trigger)
        {
            return this.GetCacehedApprovals().Where(approval => approval.EntityType == type.FullName && (approval.Trigger == trigger)).FirstOrDefault();
        }

        protected BusinessApproval GetApprovalByID(int id)
        {
            return this.GetCacehedApprovals().SingleOrDefault(a => a.ID == id);
        }

        public Guid? ProcessApproval<TViewModel>(Type entityType, Type repositoryType, TViewModel viewModel)
        {

            var triggerAttribute = typeof(TViewModel).GetCustomAttribute<ApprovalAttribute>();
            String trigger = null;
            if (triggerAttribute != null)
                trigger = triggerAttribute.Trigger;
            //Get Approvals For Type
            var approval = this.GetApprovalForType(entityType, trigger);
            if (approval != null)
            {
                var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
                //Create Change and link to All Approvals
                var changeSet = context.GetIPersistenceSet<Change>();
                var changeRequest = changeSet.Create();
                changeRequest.ID = Guid.NewGuid();
                changeRequest.ApprovalID = approval.ID;
                changeRequest.DateChangeRequested = DateTime.Now;
                //Change viewModel to JSON
                changeRequest.Data = Newtonsoft.Json.JsonConvert.SerializeObject(viewModel);
                //Create Notifications
                changeRequest.ViewType = typeof(TViewModel).AssemblyQualifiedName;
                changeRequest.ViewAssembly = typeof(TViewModel).Assembly.FullName;
                changeRequest.RepositoryType = repositoryType.FullName;
                changeRequest.RepositoryAssembly = repositoryType.Assembly.FullName;
                changeRequest.SubmittedByID = Security.Security.Provider.UserID;
                changeSet.Add(changeRequest);
                context.SaveChanges();
                context.Dispose();

                return changeRequest.ID;
            }

            return null;
            //Create Notifications
        }

        public void SubmitChange(ChangeView changeView)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
            var set = context.GetIPersistenceSet<Change>();
            var approvaResultSet = context.GetIPersistenceSet<ApprovalResult>();
            var approvalSet = context.GetIPersistenceSet<BusinessApproval>();
            var change = set.Find(changeView.ID);

            if (change.Status == ChangeStatus.Created)
            {
                change.MapBack(changeView);
                change.Status = ChangeStatus.Submitted;
                changeView = change.Map<Change, ChangeView>();
                //Send Emails
                //This might have to be lazy loaded becasue we cannot include users
                var approval = approvalSet.Find(change.ApprovalID);


                foreach (var group in approval.ApprovalGroups)
                {
                    var result = approvaResultSet.Create();
                    approvaResultSet.Add(result);

                    result.ApprovalGroupID = group.ID;
                    result.ChangeID = change.ID;

                    foreach (var user in group.Users)
                    {
                        EmailProvider.SendMail(new Email<ApprovalResultEmailView>()
                        {
                            Model = new ApprovalResultEmailView()
                            {
                                ApprovalGroupID = group.ID,
                                ChangeID = change.ID,
                                ApprovalGroupName = group.Name,
                                ApprovalName = approval.Name,
                                ChangeComment = change.Comments
                            },
                            To = new List<String>() { user.Email },
                            Subject = String.Format("Change Request: {0}".GetGlobalResource(), approval.Name)
                        });
                    }
                }

                context.SaveChanges();
                context.Dispose();
            }
            // do nothing because it is already submitted
        }

        public ChangeView GetChange(Guid id)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
            return context.GetIPersistenceSet<Change>().Find(id).Map<Change, ChangeView>();
        }

        public void ApproveResult(Guid createID, int approvalGroupID)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
            var approvalResultSet = context.GetIPersistenceSet<ApprovalResult>();
            var result = approvalResultSet.Find(createID, approvalGroupID);

            result.Status = ResultStatus.Approved;
            result.DateSubmitted = DateTime.Now;
            result.SubmittedByID = Joe.Security.Security.Provider.UserID;
            context.SaveChanges();
            //If Last process change request
            if (!(result.Change.Resutls.Where(r => r.Status == ResultStatus.Created || r.Status == ResultStatus.Denied).Count() > 0))
            {
                //Invoke The Repository This type should be saved i suppose
                var repository = (IRepository)Activator.CreateInstance(result.Change.RepositoryAssembly, result.Change.RepositoryType).Unwrap();
                //Create a ViewModel
                var viewModelType = Type.GetType(result.Change.ViewType); //(result.Change.ViewAssembly, result.Change.ViewType).Unwrap();
                var viewModel = Newtonsoft.Json.JsonConvert.DeserializeObject(result.Change.Data, viewModelType);
                //Invoke Update passing true for Override Approval
                //This is bad because changing the definition of Update will result in a runtime error!
                var updateMethod = repository.GetType().GetMethod("Update", new[] { viewModelType, typeof(Boolean), typeof(object) });
                var updateResult = (Result)updateMethod.Invoke(repository, new object[] { viewModel, true, null });

                result.Change.Status = ChangeStatus.Approved;
                result.Change.DateCompleted = DateTime.Now;
                context.SaveChanges();
                //return result...as Email To all

                this.EmailProvider.SendMail(new Email<ApprovalCompletedResultView>()
                {
                    To = result.Change.Resutls.SelectMany(r => r.ApprovalGroup.Users.Select(u => u.Email)).Distinct().ToList(),
                    Model = new ApprovalCompletedResultView()
                    {
                        ApprovalName = result.Change.Approval.Name,
                        ChangeComments = result.Change.Comments,
                        ChangeRequestID = result.Change.ID.ToString(),
                        ValidationWarnings = updateResult.Warnings
                    },
                    Subject = "Change Completed".GetGlobalResource()
                });
            }
            else
            {
                result.Change.Status = ChangeStatus.AwaitingResults;
                context.SaveChanges();
            }
            context.Dispose();

        }

        public void DenyResult(Guid createID, int approvalGroupID)
        {
            var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<BusinessApproval>();
            var approvalResultSet = context.GetIPersistenceSet<ApprovalResult>();
            var result = approvalResultSet.Find(createID, approvalGroupID);

            result.Status = ResultStatus.Denied;


            result.SubmittedByID = Joe.Security.Security.Provider.UserID;

            if (result.Change.Status != ChangeStatus.Denied)
            {
                result.Change.Status = ChangeStatus.Denied;
                //Return Email Result To Let Every one know of the Denial
                this.EmailProvider.SendMail(new Email<ApprovalDeniedResultView>()
                {
                    To = result.Change.Resutls.SelectMany(r => r.ApprovalGroup.Users.Select(u => u.Email)).Distinct().ToList(),
                    BCC = new List<String>() { result.Change.SubmittedBy.Email },
                    Model = new ApprovalDeniedResultView()
                    {
                        ApprovalName = result.Change.Approval.Name,
                        ChangeComments = result.Change.Comments,
                        ChangeRequestID = result.Change.ID.ToString()
                    },
                    Subject = "Change Denied".GetGlobalResource()
                });
            }

            context.SaveChanges();
            context.Dispose();
        }

        public void FlushApprovalCache()
        {
            Caching.Cache.Instance.Flush(approvalCacheKey);
        }
    }
}
