using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Tests
{
    public class MockRepository<TViewModel> : Joe.Business.Repository<Person, TViewModel>
        where TViewModel : class, new()
    {

    }

    [BusinessConfiguration(GetListFromCache = true)]
    public class CacheRepository<TViewModel> : Joe.Business.Repository<Person, TViewModel>
        where TViewModel : class, new()
    {

    }
}
