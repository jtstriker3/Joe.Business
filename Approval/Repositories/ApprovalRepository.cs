using Joe.Business.Approval.Views;
using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Repositories
{
    public class ApprovalRepository<TContext> : Repository<BusinessApproval, BusinessApprovalView, TContext>
        where TContext : IDBViewContext, new()
    {
        public ApprovalRepository()
        {
            this.AfterCreate += FlushCache;
            this.AfterUpdate += FlushCache;
        }

        public void FlushCache(SaveDelegateArgs<BusinessApproval, BusinessApprovalView> args)
        {
            ApprovalProvider.Instance.FlushApprovalCache();
        }

        public virtual IDictionary<String, String> GetAllModelTypes()
        {
            var modelTypeDictionary = new Dictionary<String, String>();
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.Namespace != null && type.Namespace.Contains(typeof(TContext).Namespace));

            foreach (var type in types)
                if (!modelTypeDictionary.ContainsKey(type.FullName))
                    modelTypeDictionary.Add(type.FullName, type.Name);

            return modelTypeDictionary;
        }
    }
}
