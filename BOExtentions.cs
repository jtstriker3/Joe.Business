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
    public static class BOExtentions
    {
        public static IEnumerable<Type> GetKeyTypes<TModel, TViewModel, TRepository>(this IBusinessObject<TModel, TViewModel, TRepository> bo)
            where TModel : class, new()
            where TViewModel : class, new()
            where TRepository : class, IDBViewContext, new()
        {
            return GetKeyTypes<TModel, TViewModel, TRepository>();
        }

        public static Object[] GetTypedIDs<TModel, TViewModel, TRepository>(this IBusinessObject<TModel, TViewModel, TRepository> bo, params Object[] ids)
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
                typedIDs.Add(Convert.ChangeType(idList[i], idTypesList[i]));
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
    }
}