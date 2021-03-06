﻿using System;
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
        public static MethodInfo _getIPersistenceSetGenericMethod;
        public static MethodInfo _mapMehtod;
        public static List<Tuple<Type, Type>> CacheTypeDict { get; set; }

        public static void Init<TRepository>() where TRepository : IDBViewContext, new()
        {
            CacheTypeDict.ForEach(cachedPair => Cache.Instance.Add(cachedPair.Item2.Name, new TimeSpan(Configuration.BusinessConfigurationSection.Instance.CacheDuration, 0, 0), (Func<Object>)delegate()
                {
                    return AddCacheItem<TRepository>(cachedPair);
                }
            ));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TContext">Context that implements IDbViewContext</typeparam>
        /// <param name="cachedPair">Type 1 = Model Type 2 = ViewModel</param>
        /// <returns></returns>
        public static Object AddCacheItem<TContext>(Tuple<Type, Type> cachedPair) where TContext : IDBViewContext, new()
        {

            //var getIPersistenceSetGenericMethod = _getIPersistenceSetGenericMethod ?? typeof(TContext).GetMethods().Where(m => m.Name == "GetIPersistenceSet" && m.IsGenericMethod).First();
            //var genericIPersistenceSetGenericMethod = getIPersistenceSetGenericMethod.MakeGenericMethod(cachedPair.Item1);
            var source = new TContext().GetGenericQueryable(cachedPair.Item1); //genericIPersistenceSetGenericMethod.Invoke(new TContext(), null);
            var method = _mapMehtod ?? typeof(MapExtensions).GetMethods().Single(m => m.Name == "Map"
                && m.IsGenericMethod == true
                && m.GetParameters().First().ParameterType.IsGenericType == true
                && m.GetParameters().First().ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                ).MakeGenericMethod(new[] { cachedPair.Item1, cachedPair.Item2 });

            Expression ex = Expression.Call(method,
                Expression.Constant(source, typeof(IQueryable<>).MakeGenericType(cachedPair.Item1)), Expression.Constant(null));
            LambdaExpression lambda = Expression.Lambda(Expression.Call(typeof(Enumerable), "ToList", new[] { cachedPair.Item2 }, ex));
            return lambda.Compile().DynamicInvoke();
        }

        public static IEnumerable<TViewModel> GetListCache<TModel, TViewModel>()
        {
            return (IEnumerable<TViewModel>)Cache.Instance.Get(typeof(TViewModel).FullName + typeof(TModel).FullName + "List");
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
