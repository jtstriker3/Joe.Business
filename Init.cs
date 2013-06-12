using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Joe.Business
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class Init : Attribute
    {
        /// <summary>
        /// Fuction to call to preform starup logic
        /// Defaults to Init
        /// </summary>
        public String Function { get; set; }
        public IEnumerable<Type> GenericArguments { get; private set; }

        public Init(params Type[] genericArguments)
        {
            GenericArguments = genericArguments;
            Function = "Init";
        }

        public static void RunInitFunctions()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(Init), false).Count() > 0));

            foreach (var type in types)
            {
                var initAttrList = type.GetCustomAttributes(typeof(Init), false) as IEnumerable<Init>;

                foreach (var initAttr in initAttrList)
                {

                    Type fullType = type;
                    if (initAttr.GenericArguments.Count() > 0)
                        fullType = type.MakeGenericType(initAttr.GenericArguments.ToArray());

                    var function = fullType.GetMethod(initAttr.Function);
                    if (function.IsStatic)
                        if (function.GetParameters().Count() == 0)
                            function.Invoke(null, null);
                        else
                            throw new Exception("Function must take 0 Parameters");
                    else throw new Exception("Function Declared in Init Attribute is not Static");
                }
            }
        }
    }
}
