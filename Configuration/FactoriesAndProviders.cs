using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Configuration
{
    public static class FactoriesAndProviders
    {
        private static IContextFactory _contextFactory;
        public static IContextFactory ContextFactory
        {
            get
            {
                _contextFactory = _contextFactory ?? CreateFactoryByLookup<IContextFactory>();
                return _contextFactory;
            }
            set
            {
                _contextFactory = value;
            }
        }

        public static IEmailProvider _emailProvider;
        public static IEmailProvider EmailProvider
        {
            get
            {
                _emailProvider = _emailProvider ?? CreateFactoryByLookup<IEmailProvider>();
                return _emailProvider;
            }
            set
            {
                _emailProvider = value;
            }
        }

        public static T CreateFactoryByLookup<T>()
        {
            try
            {
                //This only runs once so no reason to cache assemblies
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type =>
                    type.IsClass
                    && !type.IsAbstract
                    && type.GetInterfaces().Where(iface => typeof(T).IsAssignableFrom(iface)).Count() > 0);

                if (types.Count() == 1)
                    return (T)CreateObject(types.First());
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.EventLog appLog = new System.Diagnostics.EventLog();
                    appLog.Source = "Joe.Business";
                    appLog.WriteEntry(String.Format("Could Not Set {0}: " + ex.Message, typeof(T).FullName));
                }
                catch
                {
                    //Do Nothing
                }
            }
            return default(T);
        }

        internal static Object CreateObject(Type type)
        {

            var newExpression = Expression.New(type);
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return lambdaExpression.Compile().DynamicInvoke();
        }

        internal static T CreateObject<T>()
        {

            var newExpression = Expression.New(typeof(T));
            var blockExpression = Expression.Block(newExpression);
            var lambdaExpression = Expression.Lambda(blockExpression);

            return (T)lambdaExpression.Compile().DynamicInvoke();
        }
    }
}
