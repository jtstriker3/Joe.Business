using System;
namespace Joe.Business
{
    interface IEmailProviderFactory
    {
        IEmailProvider CreateEmailProvider(Type iEmailType);
        IEmailProvider CreateEmailProviderByLookup();
    }
}
