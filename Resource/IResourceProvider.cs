using System;
namespace Joe.Business.Resource
{
    public interface IResourceProvider
    {
        void FlushResourceCache();
        string GetResource(string Name, string type);
    }
}
