using System;
namespace Joe.Business.Resource
{
    interface IResourceProvider
    {
        void FlushResourceCache();
        string GetResource(string Name, string type);
    }
}
