﻿using Joe.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RepoMappingAttribute : Attribute
    {
        String Method { get; set; }
        List<String> Parameters { get; set; }
        Type ModelCast { get; set; }
        public Type HelperClass { get; set; }
        public String Condition { get; set; }

        /// <summary>
        /// This will invoke a method on the repository object to set a Property in the View Model
        /// </summary>
        /// <param name="methodInfo">The Method to Invoke in the Repo</param>
        /// <param name="parameterInfos">The Properties in the View Model to pass in a parameters</param>
        public RepoMappingAttribute(String method, params String[] parameters)
        {
            Method = method;
            Parameters = parameters.ToList();
        }

        /// <summary>
        /// Init RepoMapping Attribute
        /// </summary>
        /// <param name="method">Method To Call</param>
        /// <param name="modelCast">Cast the model to this type if trying to call a method that takes its parent type</param>
        /// <param name="parameters">Properties from View Model to pass in as parameters</param>
        public RepoMappingAttribute(String method, Type modelCast, params String[] parameters)
            : this(method, parameters)
        {
            ModelCast = modelCast;
        }

        /// <summary>
        /// Init RepoMapping Attribute
        /// </summary>
        /// <param name="method">Method To Call</param>
        /// <param name="modelCast">Cast the model to this type if trying to call a method that takes its parent type Pass null to ignore this</param>
        /// <param name="helperClass">Class to call if Method is not part of repository</param>
        /// <param name="parameters">Properties from View Model to pass in as parameters</param>
        public RepoMappingAttribute(String method, Type modelCast, Type helperClass, params String[] parameters)
            : this(method, modelCast, parameters)
        {
            HelperClass = helperClass;
        }

        public Boolean HasMethod
        {
            get
            {
                return !String.IsNullOrEmpty(Method);
            }
        }

        /// <summary>
        /// Call this to get the MethodInfo of the repository object to invoke
        /// </summary>
        /// <param name="repo">The repository To Find the Method Info in</param>
        /// <returns></returns>
        public MethodInfo GetMethodInfo(IRepository repo, Object viewModel, Type model)
        {
            var type = HelperClass ?? repo.GetType();
            return type.GetMethod(Method, this.GetParametersTypes(viewModel, model).ToArray());
        }

        /// <summary>
        /// Call this to get the parameters of the View Model to pass into the repository Object Map Function
        /// </summary>
        /// <param name="viewModel">The View Model to find the Properties In</param>
        /// <returns></returns>
        public IEnumerable<Object> GetParameters<TViewModel, TModel>(TViewModel viewModel, Func<TViewModel, TModel> func = null)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter == "$Model")
                    if (func != null)
                        yield return func(viewModel);
                    else
                        yield return null;
                else
                    yield return ReflectionHelper.GetEvalProperty(viewModel, parameter);
            }
        }

        private IEnumerable<Type> GetParametersTypes(Object viewModel, Type model)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter == "$Model")
                    if (ModelCast != null)
                        if (ModelCast.IsAssignableFrom(model))
                            yield return ModelCast;
                        else
                            throw new Exception("Spciefed Model Cast type is not assignable to the passed in Type");
                    else
                        yield return model;
                else
                    yield return ReflectionHelper.GetEvalPropertyInfo(viewModel, parameter).PropertyType;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class NestedRepoMappingAttribute : Attribute
    {
        List<String> Parameters { get; set; }
        public Type Repository { get; private set; }
        public Type Model { get; private set; }
        public String Condition { get; set; }

        public NestedRepoMappingAttribute(Type repository, Type model, params String[] parameters)
        {
            Parameters = parameters.ToList();
            Model = model;
            Repository = repository;
        }

        public Boolean HasRepository
        {
            get
            {
                return Repository != null;
            }
        }

        /// <summary>
        /// Call this to get the parameters of the View Model to pass into the repository Object Map Function
        /// </summary>
        /// <param name="viewModel">The View Model to find the Properties In</param>
        /// <returns></returns>
        public void SetParameters<TViewModel>(TViewModel viewModel, Object nestedView)
        {
            foreach (var parameter in Parameters)
            {
                var map = parameter.Split(':');
                String parentProperty, nestedProperty;
                if (map.Count() > 1)
                {
                    parentProperty = map.First();
                    nestedProperty = map.ElementAt(1);
                }
                else
                {
                    parentProperty = map.First();
                    nestedProperty = map.First();
                }

                var value = ReflectionHelper.GetEvalProperty(viewModel, parentProperty);
                ReflectionHelper.SetEvalProperty(nestedView, nestedProperty, value);

            }
        }

        private IEnumerable<Type> GetParametersTypes(Object viewModel, Type model)
        {
            foreach (var parameter in Parameters)
            {
                yield return ReflectionHelper.GetEvalPropertyInfo(viewModel, parameter).PropertyType;
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AllValuesAttribute : Attribute
    {
        public Type Repository { get; private set; }
        public Type Model { get; private set; }
        public String IncludedList { get; set; }
        public String Filter { get; set; }
        public Boolean SetForList { get; set; }
        public String Condition { get; set; }

        /// <summary>
        /// Maps Possible Values of An Entity Type. This will use the repository to map the entities.
        /// </summary>
        /// <param name="repository">Repo to query from. Must Implement IRepository```</param>
        /// <param name="model">Type of Entity to map from</param>
        /// <param name="includedList">If many to many then this is the list of already included Entities. This should be a property in the view that maps the the Entity's Relation Property</param>
        public AllValuesAttribute(Type repository, Type model, String includedList)
            : this(model, includedList)
        {
            Repository = repository;
        }
        /// <summary>
        /// Maps Possible Values of An Entity Type. This will use the repository to map the entities.
        /// </summary>
        /// <param name="repository">Repo to query from. Must Implement IRepository```</param>
        /// <param name="model">Type of Entity to map from</param>
        public AllValuesAttribute(Type repository, Type model)
            : this(model)
        {
            Repository = repository;
        }

        /// <summary>
        /// This will map the entities directly from the context.
        /// </summary>
        /// <param name="model">Type of Entity to map from</param>
        /// <param name="includedList">If many to many then this is the list of already included Entities. This should be a property in the view that maps the the Entity's Relation Property</param>
        public AllValuesAttribute(Type model, String includedList)
            : this(model)
        {
            IncludedList = includedList;
        }

        /// <summary>
        /// This will map the entities directly from the context.
        /// </summary>
        /// <param name="model">Type of Entity to map from</param>

        public AllValuesAttribute(Type model)
        {
            Model = model;
            SetForList = Configuration.BusinessConfigurationSection.Instance.SetAllValuesForList;
        }
    }
}
