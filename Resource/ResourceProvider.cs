using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Joe.MapBack;
using Joe.Caching;
using System.Threading;
using System.Linq.Expressions;

namespace Joe.Business.Resource
{
    public class ResourceProvider : Joe.Business.Resource.IResourceProvider
    {
        protected const String _resouceCacheKey = "ee21b61d-5bdc-4adc-b56e-a7932a92565a";

        private static IResourceProvider _providerInstance;
        public static IResourceProvider ProviderInstance
        {
            get
            {
                _providerInstance = _providerInstance ?? new ResourceProvider();
                return _providerInstance;
            }
        }

        protected ResourceProvider()
        {
            Func<List<Resource>> getResouces = () =>
            {
                var context = Configuration.FactoriesAndProviders.ContextFactory.CreateContext<Resource>();
                var resourceList = context.GetIPersistenceSet<Resource>();
                if (resourceList == null)
                    throw new Exception("Type TResource must be part of your Context");

                return resourceList.ToList();
            };

            Joe.Caching.Cache.Instance.Add(_resouceCacheKey, new TimeSpan(8, 0, 0), getResouces);
        }

        public String GetResource(String name, String type)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture.Name;
            var currentUICulture = Thread.CurrentThread.CurrentUICulture.Name;
            var resource = ((List<Resource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                       res.Name == name
                       && res.Type == type
                       && res.Culture == currentCulture);
            if (resource == null)
                resource = ((List<Resource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                       res.Name == name
                       && res.Type == type
                       && res.Culture == currentUICulture);

            if (resource == null)
                foreach (var culture in Configuration.BusinessConfigurationSection.Instance.DefaultCultures.Split(',').Where(culture => culture != currentCulture))
                {
                    resource = ((List<Resource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                           res.Name == name
                           && res.Type == type
                           && res.Culture == culture);
                    if (resource != null)
                        break;
                }

            return resource != null ? resource.Value : name;
        }

        public void FlushResourceCache()
        {
            Joe.Caching.Cache.Instance.Flush(_resouceCacheKey);
        }
    }
}
