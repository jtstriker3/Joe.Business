using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Joe.Map;
using Joe.Business;

namespace Joe.Business
{
    public static class ManyToManyExtentions
    {
        public static void ForEach<T>(this IEnumerable<T> on,
                               Action<T> action)
        {
            foreach (T item in on)
                action(item);
        }

        public static IEnumerable<TRelationViewModel> AllManyToManyNoActiveFilter<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(this TViewModel viewModel, IEnumerable<TRelationViewModel> listViewModel)
            where TViewModel : class
            where TCachedViewModel : class
            where TRelationViewModel : class, IManyToMany, new()
        {
            return
                AllManyToManyNoActiveFilter
                    <TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(viewModel,
                                                                                                             listViewModel,
                                                                                                             null, null,
                                                                                                             null);

        }

        public static IEnumerable<TRelationViewModel> AllManyToManyNoActiveFilter<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(this TViewModel viewModel, IEnumerable<TRelationViewModel> listViewModel, Action<TCachedViewModel, TRelationViewModel> cumstomCachedMappings)
            where TViewModel : class
            where TCachedViewModel : class
            where TRelationViewModel : class, IManyToMany, new()
        {
            return AllManyToManyNoActiveFilter
                <TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(viewModel,
                                                                                                         listViewModel,
                                                                                                         cumstomCachedMappings, null, null);
        }

        public static IEnumerable<TRelationViewModel> AllManyToManyNoActiveFilter<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(this TViewModel viewModel, IEnumerable<TRelationViewModel> listViewModel, Action<TCachedViewModel, TRelationViewModel> cumstomCachedMappings, Action<TViewModel, TRelationViewModel> customViewMappings)
            where TViewModel : class
            where TCachedViewModel : class
            where TRelationViewModel : class, IManyToMany, new()
        {
            return AllManyToManyNoActiveFilter<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(viewModel, listViewModel, cumstomCachedMappings, customViewMappings, null);
        }

        public static IEnumerable<TRelationViewModel> AllManyToManyNoActiveFilter<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationModel, TRelationViewModel>(this TViewModel viewModel, IEnumerable<TRelationViewModel> listViewModel, Action<TCachedViewModel, TRelationViewModel> cumstomCachedMappings, Action<TViewModel, TRelationViewModel> customViewMappings, Action<TRelationViewModel, TRelationViewModel> customIncludedMappings)
            where TViewModel : class
            where TCachedViewModel : class
            where TRelationViewModel : class, IManyToMany, new()
        {
            if (listViewModel != null)
            {
                var relationViewModels = listViewModel as List<TRelationViewModel> ?? listViewModel.ToList();
                var fullList = new List<TRelationViewModel>();
                foreach (var cachedViewModel in (List<TCachedViewModel>)StaticCacheHelper.Cache.Get(typeof(TCachedViewModel).Name))
                {
                    var relationViewModel = Activator.CreateInstance<TRelationViewModel>();

                    if (cumstomCachedMappings != null)
                        cumstomCachedMappings(cachedViewModel, relationViewModel);
                    if (customViewMappings != null)
                        customViewMappings(viewModel, relationViewModel);

                    relationViewModel.ID1 =
                        (int)
                        (GetMappedProperty<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationViewModel>(
                            viewModel, cachedViewModel, "ID1") ?? 0);
                    relationViewModel.ID2 =
                        (int)
                        (GetMappedProperty<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationViewModel>(
                            viewModel, cachedViewModel, "ID2") ?? 0);
                    ;
                    relationViewModel.Name1 =
                        (String)
                        GetMappedProperty<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationViewModel>(
                            viewModel, cachedViewModel, "Name1");
                    relationViewModel.Name2 =
                        (String)
                        GetMappedProperty<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationViewModel>(
                            viewModel, cachedViewModel, "Name2");
                    var includedRelationViewModel =
                        relationViewModels.WhereVM<TRelationModel, TRelationViewModel>(relationViewModel);
                    relationViewModel.Included = includedRelationViewModel != null;
                    if (includedRelationViewModel != null && customIncludedMappings != null)
                        customIncludedMappings(includedRelationViewModel, relationViewModel);

                    fullList.Add(relationViewModel);
                }
                return fullList.OrderDBView();
            }
            return null;
        }

        private static Object GetMappedProperty<TModel, TViewModel, TCachedModel, TCachedViewModel, TRelationViewModel>(TViewModel viewModel, TCachedViewModel chachedViewModel, String property)
            where TViewModel : class
            where TCachedViewModel : class
            where TRelationViewModel : IManyToMany
        {
            var propAttr = (ViewMappingAttribute)typeof(TRelationViewModel).GetProperty(property).GetCustomAttributes(typeof(ViewMappingAttribute), true).SingleOrDefault();
            if (propAttr != null)
            {
                String modelNameProperty = propAttr.ColumnPropertyName.Replace(typeof(TModel).Name + ".", String.Empty);
                String cachedModelNameProperty = propAttr.ColumnPropertyName.Replace(typeof(TCachedModel).Name + ".", String.Empty);
                var viewModelInfo = typeof(TViewModel).GetPropertyByAttribute(modelNameProperty);
                var cachedViewModelInfo = typeof(TCachedViewModel).GetPropertyByAttribute(cachedModelNameProperty);

                return viewModelInfo != null
                           ? viewModelInfo.GetValue(viewModel, null)
                           : cachedViewModelInfo != null ? cachedViewModelInfo.GetValue(chachedViewModel, null) : null;
            }
            return null;
        }

        private static IEnumerable<PropertyInfo> GetKeyProperties<T>(this T t)
        {
            return (from info in typeof(T).GetProperties()
                    let viewMapping = (ViewMappingAttribute)info.GetCustomAttributes(true).SingleOrDefault(attr => attr is ViewMappingAttribute)
                    where
                        viewMapping != null
                        && viewMapping.Key
                    select info);
        }

        private static PropertyInfo GetPropertyByAttribute(this Type type, String propertyName)
        {
            return (from info in type.GetProperties()
                    let viewMapping = (ViewMappingAttribute)info.GetCustomAttributes(true).SingleOrDefault(attr => attr is ViewMappingAttribute)
                    where
                        viewMapping != null
                        && viewMapping.ColumnPropertyName == propertyName
                    select info).SingleOrDefault();
        }

        private static T ConvertToType<T>(this Object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }
    }
}
