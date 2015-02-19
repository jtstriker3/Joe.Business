using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Exceptions
{
    public class ContextFactoryNotSetException : Exception
    {
        public ContextFactoryNotSetException() : base("You Must implement IContextFactory. The Type will be automatically Looked up") { }
    }
}
