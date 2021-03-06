﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Tests
{
    public class ContextFactory : IContextFactory
    {
        public MapBack.IDBViewContext CreateContext<TModel>()
        {
            return new MockContext();
        }

        public MapBack.IDBViewContext CreateContext(Type modelType)
        {
            return new MockContext();
        }
    }
}
