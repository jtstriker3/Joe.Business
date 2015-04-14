using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Tests
{
    class SecurityProvider : Security.ISecurityProvider
    {
        public bool IsUserInRole(string userID, params string[] roles)
        {
            return true;
        }

        public bool IsUserInRole(params string[] roles)
        {
            return true;
        }

        public string UserID
        {
            get
            {
                return System.Threading.Thread.CurrentPrincipal.Identity.Name;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> GetCurrentUsersRoles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetUsersRoles(string userID)
        {
            throw new NotImplementedException();
        }

        public bool IsUserInRole(string userID, IEnumerable<string> roles)
        {
            throw new NotImplementedException();
        }
    }
}
