using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Joe.Business
{
    public class EmailProviderFactory : Joe.Business.IEmailProviderFactory
    {
        private static EmailProviderFactory _instance;
        public static EmailProviderFactory Instance
        {
            get
            {
                _instance = _instance ?? new EmailProviderFactory();
                return _instance;
            }
        }

        public IEmailProvider CreateEmailProvider(Type iEmailType)
        {
            if (typeof(IEmailProvider).IsAssignableFrom(iEmailType) && iEmailType.IsClass && !iEmailType.IsAbstract)
                return (IEmailProvider)Expression.Lambda(Expression.Block(Expression.New(iEmailType))).Compile().DynamicInvoke();
            else
                throw new InvalidCastException("You passed in a type that does not implement IEmailProvider or is not an instantiable Class, in the future you should do this");

        }

        public IEmailProvider CreateEmailProviderByLookup()
        {
            try
            {
                //This only runs once so no reason to cache assemblies
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type =>
                    type.IsClass
                    && !type.IsAbstract
                    && type.GetInterfaces().Where(iface => typeof(IEmailProvider).IsAssignableFrom(iface)).Count() > 0);

                if (types.Count() == 1)
                    return EmailProviderFactory.Instance.CreateEmailProvider(types.Single());
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.EventLog appLog = new System.Diagnostics.EventLog();
                    appLog.Source = "Joe.Business";
                    appLog.WriteEntry("Could not set Email Provider: " + ex.Message);
                }
                catch
                {
                    //Do Nothing
                }
            }
            return null;
        }
    }
}
