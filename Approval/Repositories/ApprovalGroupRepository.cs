using Joe.Business.Approval.Views;
using Joe.Business.Common;
using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval.Repositories
{
    public class ApprovalGroupRepository : Repository<ApprovalGroup, ApprovalGroupView>
    {
        public ApprovalGroupRepository()
        {
            this.BeforeCreate += SetUsers;
            this.BeforeUpdate += SetUsers;
        }

        public void SetUsers(SaveDelegateArgs<ApprovalGroup, ApprovalGroupView> args)
        {
            if (args.ViewModel.UserIds != null)
            {
                var userSet = this.Context.GetIPersistenceSet<User>();
                var users = userSet.Where(user => args.ViewModel.UserIds.Contains(user.ID));
                args.Model.Users.Clear();
                args.Model.Users.AddRange(users);
            }
        }
    }
}
