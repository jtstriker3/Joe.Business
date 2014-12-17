using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Joe.Map;
using System.Collections;
using System.Text;
using Joe.MapBack;
using System.Linq.Expressions;
using System.Data.Entity;

namespace Joe.Business
{
    public static class RepoExtentions
    {
        public static IEnumerable<Type> GetKeyTypes<TModel, TViewModel>(this IRepository<TModel, TViewModel> repo)
            where TModel : class, new()
            where TViewModel : class, new()
        {
            return GetKeyTypes<TViewModel>();
        }

        public static Object[] GetTypedIDs<TModel, TViewModel>(this IRepository<TModel, TViewModel> repo, params Object[] ids)
            where TModel : class
            where TViewModel : class, new()
        {

            return GetTypedIDs<TViewModel>(ids);
        }

        public static IEnumerable<Type> GetKeyTypes<TViewModel>()
        {
            return GetKeyTypes(typeof(TViewModel));
        }

        public static IEnumerable<Type> GetKeyTypes(Type viewModelType)
        {
            foreach (PropertyInfo info in viewModelType.GetProperties())
            {
                var customAttr = new Joe.Map.ViewMappingHelper(info, viewModelType).ViewMapping;
                if (customAttr != null && customAttr.Key)
                    yield return info.PropertyType;
            }
        }

        public static Object[] GetTypedIDs<TViewModel>(params Object[] ids)
        {
            return GetTypedIDs(typeof(TViewModel), ids);
        }

        public static Object[] GetTypedIDs(Type viewModelType, params Object[] ids)
        {
            var idList = ids.ToList();
            var idTypesList = GetKeyTypes(viewModelType).ToList();
            var typedIDs = new List<Object>();
            for (int i = 0; i < idList.Count; i++)
            {
                if (idTypesList[i] != typeof(Guid))
                    typedIDs.Add(Convert.ChangeType(idList[i], idTypesList[i]));
                else
                    typedIDs.Add(new Guid(idList[i].ToString()));

            }

            return typedIDs.ToArray();
        }

        public static String ToCommaDeleminatedList(this IEnumerable list)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var item in list)
            {
                if (builder.ToString().Length > 0)
                    builder.Append(", " + item.ToString());
                else
                    builder.Append(item.ToString());
            }

            return builder.ToString();
        }


        /// <summary>
        /// Reflection Extentions to mimic .Net 4.5
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="inharit"></param>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(this Type type, Boolean inharit = true)
        {
            return (T)type.GetCustomAttributes(typeof(T), inharit).SingleOrDefault();
        }

        public static T GetCustomAttribute<T>(this PropertyInfo info, Boolean inharit = true)
        {
            return (T)info.GetCustomAttributes(typeof(T), inharit).SingleOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, Boolean inharit = true)
        {
            return type.GetCustomAttributes(typeof(T), inharit).Cast<T>();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this PropertyInfo info, Boolean inharit = true)
        {
            return info.GetCustomAttributes(typeof(T), inharit).Cast<T>();
        }

        public static void SetValue(this PropertyInfo info, Object obj, Object value)
        {
            info.SetValue(obj, value, null);
        }

        public static Object GetValue(this PropertyInfo info, Object obj)
        {
            return info.GetValue(obj, null);
        }

        public static IEnumerable<Tuple<PropertyInfo, Object>> GetKeyInfo<TViewModel, TModel>(TViewModel viewModel)
        {
            foreach (PropertyInfo info in viewModel.GetType().GetProperties())
            {
                ViewMappingHelper vmh = new ViewMappingHelper(info, typeof(TModel));
                if (vmh.ViewMapping != null)
                    if (vmh.ViewMapping.Key)
                        yield return new Tuple<PropertyInfo, Object>(info, info.GetValue(viewModel));
            }
        }

        public static IEnumerable<PropertyInfo> GetKeyMembers<TViewModel, TModel>()
        {
            foreach (PropertyInfo info in typeof(TViewModel).GetProperties())
            {
                ViewMappingHelper vmh = new ViewMappingHelper(info, typeof(TModel));
                if (vmh.ViewMapping != null)
                    if (vmh.ViewMapping.Key)
                        yield return info;
            }
        }

        internal static TEntity Find<TEntity, TViewModel>(this IQueryable<TEntity> source, Boolean database, params object[] keyValues)
        {
            var keys = GetKeyMembers<TViewModel, TEntity>();

            var parameterExpression = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name);

            Expression compare = null;
            var count = 0;
            foreach (var key in keys)
            {

                Expression compareSingle = Expression.Property(parameterExpression, key.Name);
                var constantExpression = Expression.Constant(keyValues[count]);
                if (!database && key.PropertyType == typeof(String))
                {
                    MethodInfo methodInfo = typeof(String).GetMethod("ToLower", new Type[] { });
                    compareSingle = Expression.Call(compareSingle, methodInfo);

                    constantExpression = Expression.Constant(keyValues[count].ToString().ToLower());
                }

                compareSingle = Expression.Equal(compareSingle, constantExpression);
                if (count > 0)
                    compare = Expression.And(compare, compareSingle);
                else
                    compare = compareSingle;
                count++;
            }

            var lambda = (Expression<Func<TEntity, Boolean>>)Expression.Lambda(compare, new ParameterExpression[] { parameterExpression });
            return source.SingleOrDefault(lambda);
        }

        public static IQueryable<TModel> BuildIncludeMappings<TModel>(this IQueryable<TModel> source, params String[] includeMappings)
            where TModel : class
        {
            foreach (var mapping in includeMappings)
                source = source.Include(mapping);

            return source;
        }

        public static T ShallowClone<T>(this T objectToClone)
        {
            var newObject = (T)Expression.Lambda(Expression.Block(Expression.New(typeof(T)))).Compile().DynamicInvoke();

            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.CanWrite)
                {
                    var value = prop.GetValue(objectToClone);
                    if (value != null)
                    {
                        //if (prop.PropertyType.ImplementsIEnumerable())
                        //{
                        //    value = ((IEnumerable)value).AsQueryable().AsNoTracking();
                        //}
                        prop.SetValue(newObject, value);
                    }
                }
            }

            return newObject;
        }

        public static String GetGlobalResource(this String name)
        {
            var resourceProvider = Joe.Business.Resource.ResourceProvider.ProviderInstance;
            if (resourceProvider != null)
                return resourceProvider.GetResource(name, "Global");
            return name;
        }

    }
}