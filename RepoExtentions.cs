using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Reflection;
using Joe.Map;
using System.Collections;
using System.Text;
using Joe.MapBack;

namespace Joe.Business
{
    public static class RepoExtentions
    {
        public static IEnumerable<Type> GetKeyTypes<TModel, TViewModel, TRepository>(this IRepository<TModel, TViewModel, TRepository> repo)
            where TModel : class, new()
            where TViewModel : class, new()
            where TRepository : class, IDBViewContext, new()
        {
            return GetKeyTypes<TModel, TViewModel, TRepository>();
        }

        public static Object[] GetTypedIDs<TModel, TViewModel, TRepository>(this IRepository<TModel, TViewModel, TRepository> repo, params Object[] ids)
            where TModel : class, new()
            where TViewModel : class, new()
            where TRepository : class, IDBViewContext, new()
        {

            return GetTypedIDs<TModel, TViewModel, TRepository>(ids);
        }

        public static IEnumerable<Type> GetKeyTypes<TModel, TViewModel, TRepository>()
        {
            foreach (PropertyInfo info in typeof(TViewModel).GetProperties())
            {
                var customAttr = new Joe.Map.ViewMappingHelper(info, typeof(TViewModel)).ViewMapping;
                if (customAttr != null && customAttr.Key)
                    yield return info.PropertyType;
            }
        }

        public static Object[] GetTypedIDs<TModel, TViewModel, TRepository>(params Object[] ids)
        {
            var idList = ids.ToList();
            var idTypesList = GetKeyTypes<TModel, TViewModel, TRepository>().ToList();
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
                if (vmh.ViewMapping.Key)
                    yield return new Tuple<PropertyInfo, Object>(info, info.GetValue(viewModel));
            }
        }
    }
}