using Joe.MapBack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business
{
    public interface IContextFactory
    {
        IDBViewContext CreateContext<TModel>();
    }
}
