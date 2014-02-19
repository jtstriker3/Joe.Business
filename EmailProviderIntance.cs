using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business
{
    public static class EmailProviderIntance
    {
        public static IEmailProvider _emailProvider;
        public static IEmailProvider EmailProvider
        {
            get
            {
                _emailProvider = _emailProvider ?? EmailProviderFactory.Instance.CreateEmailProviderByLookup();
                return _emailProvider;
            }
            set
            {
                _emailProvider = value;
            }
        }
    }
}
