using System;
namespace Joe.Business.Resource
{
    public interface IResource
    {
        string Culture { get; set; }
        string Name { get; set; }
        string Type { get; set; }
        string Value { get; set; }
    }
}
