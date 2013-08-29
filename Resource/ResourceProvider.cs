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
    public abstract class ResourceProvider : Joe.Business.Resource.IResourceProvider
    {
        protected const String _resouceCacheKey = "ee21b61d-5bdc-4adc-b56e-a7932a92565a";

        public abstract String GetResource(String Name, String type);

        public static ResourceProvider ProviderInstance { get; private set; }

        public static void InitilizeResourceProvider(Type resourceType, Type contextType)
        {
            var providerType = typeof(ResourceProvider<,>).MakeGenericType(resourceType, contextType);
            ProviderInstance = Repository.CreateObject(providerType) as ResourceProvider;
        }

        public void FlushResourceCache()
        {
            Joe.Caching.Cache.Instance.Flush(_resouceCacheKey);
        }
    }

    public class ResourceProvider<TResource, TContext> : ResourceProvider
        where TResource : class, IResource, new()
        where TContext : IDBViewContext, new()
    {
        protected ResourceProvider()
        {
            Func<List<TResource>> getResouces = () =>
            {
                var context = new TContext();
                var resourceList = context.GetIDbSet<TResource>();
                if (resourceList == null)
                    throw new Exception("Type TResource must be part of your Context");

                return resourceList.ToList();
            };

            Joe.Caching.Cache.Instance.Add(_resouceCacheKey, new TimeSpan(8, 0, 0), getResouces);
        }

        public override String GetResource(String name, String type)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture.Name;
            var currentUICulture = Thread.CurrentThread.CurrentUICulture.Name;
            var resource = ((List<TResource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                       res.Name == name
                       && res.Type == type
                       && res.Culture == currentCulture);
            if(resource == null)
                resource = ((List<TResource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                       res.Name == name
                       && res.Type == type
                       && res.Culture == currentUICulture);

            if (resource == null)
                foreach (var culture in Configuration.BusinessConfigurationSection.Instance.DefaultCultures.Split(',').Where(culture => culture != currentCulture))
                {
                    resource = ((List<TResource>)Cache.Instance.Get(_resouceCacheKey)).SingleOrDefault(res =>
                           res.Name == name
                           && res.Type == type
                           && res.Culture == culture);
                    if (resource != null)
                        break;
                }

            return resource != null ? resource.Value : name;
        }
    }
}
