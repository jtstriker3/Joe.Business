using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Joe.Map;
using Joe.Caching;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Joe.Business;
using Joe.MapBack;

namespace Joe.Business
{
    public static class StaticCacheHelper
    {
        public static List<Tuple<Type, Type>> CacheTypeDict { get; set; }

        public static void Init<TRepository>() where TRepository : IDBViewContext, new()
        {
            CacheTypeDict.ForEach(cachedPair => Cache.Instance.Add(cachedPair.Item2.Name, new TimeSpan(Configuration.CacheDuration, 0, 0), (Func<Object>)delegate()
                {
                    return AddCacheItem<TRepository>(cachedPair);
                }
            ));
        }

        public static Object AddCacheItem<TRepository>(Tuple<Type, Type> cachedPair) where TRepository : IDBViewContext, new()
        {
            var source = new TRepository().GetIQuery(cachedPair.Item1);
            var method = typeof(MapExtensions).GetMethods().Single(m => m.Name == "MapDBView"
                && m.IsGenericMethod == true
                && m.GetParameters().Single().ParameterType.IsGenericType == true
                && m.GetParameters().Single().ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)).MakeGenericMethod(new[] { cachedPair.Item1, cachedPair.Item2 });

            Expression ex = Expression.Call(method,
                Expression.Constant(source, typeof(IQueryable<>).MakeGenericType(cachedPair.Item1)));
            LambdaExpression lambda = Expression.Lambda(Expression.Call(typeof(Enumerable), "ToList", new[] { cachedPair.Item2 }, ex));
            return lambda.Compile().DynamicInvoke();
        }

        public static IEnumerable<TViewModel> GetCache<TViewModel>()
        {
            return (IEnumerable<TViewModel>)Cache.Instance.Get(typeof(TViewModel).Name);
        }

        public static void Flush(String key)
        {
            if (key == null) throw new ArgumentNullException("key");
            Cache.Instance.Flush(key);
        }

        public static void Flush()
        {
            Cache.Instance.Flush();
        }

    }
}
