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
    public class BOMappingAttribute : Attribute
    {
        String Method { get; set; }
        List<String> Parameters { get; set; }
        Type ModelCast { get; set; }

        /// <summary>
        /// This will invoke a method on the business object to set a Property in the View Model
        /// </summary>
        /// <param name="methodInfo">The Method to Invoke in the BO</param>
        /// <param name="parameterInfos">The Properties in the View Model to pass in a parameters</param>
        public BOMappingAttribute(String method, params String[] parameters)
        {
            Method = method;
            Parameters = parameters.ToList();
        }

        public BOMappingAttribute(String method, Type modelCast, params String[] parameters)
            : this(method, parameters)
        {
            ModelCast = modelCast;
        }

        public Boolean HasMethod
        {
            get
            {
                return !String.IsNullOrEmpty(Method);
            }
        }

        /// <summary>
        /// Call this to get the MethodInfo of the business object to invoke
        /// </summary>
        /// <param name="bo">The Business To Find the Method Info in</param>
        /// <returns></returns>
        public MethodInfo GetMethodInfo(IBusinessObject bo, Object viewModel, Type model)
        {
            return bo.GetType().GetMethod(Method, this.GetParametersTypes(viewModel, model).ToArray());
        }

        /// <summary>
        /// Call this to get the parameters of the View Model to pass into the Business Object Map Function
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
    public class NestedBOMappingAttribute : Attribute
    {
        List<String> Parameters { get; set; }
        public Type BusinessObject { get; private set; }
        public Type Model { get; private set; }

        public NestedBOMappingAttribute(Type businessObject, Type model, params String[] parameters)
        {
            Parameters = parameters.ToList();
            Model = model;
            BusinessObject = businessObject;
        }

        public Boolean HasBusinessObject
        {
            get
            {
                return BusinessObject != null;
            }
        }

        /// <summary>
        /// Call this to get the parameters of the View Model to pass into the Business Object Map Function
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
        public Type BusinessObject { get; private set; }
        public Type Model { get; private set; }
        public String Filter { get; set; }

        public AllValuesAttribute(Type businessObject, Type model)
        {

        }
    }
}
