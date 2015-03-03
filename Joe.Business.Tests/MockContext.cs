using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Tests
{
    class MockContext : Joe.MapBack.IDBViewContext
    {
        public MockContext()
        {
            People = new MockPersistenceSet<Person>() { new Person() { ID = "1", Name = "Joe" }, new Person() { ID = "2", Name = "Jim" }, new Person() { ID = "2", Name = "Brandon" } };
        }

        public MockPersistenceSet<Person> People { get; set; }

        public void Detach(object obj)
        {
            //Do Nothing
        }

        public IQueryable GetGenericQueryable(Type TModel)
        {
            var set = typeof(MockPersistenceSet<>).MakeGenericType(TModel);
            return (IQueryable)Activator.CreateInstance(set);
        }

        public MapBack.IPersistenceSet GetIPersistenceSet(Type TModel)
        {
            var set = typeof(MockPersistenceSet<>).MakeGenericType(TModel);
            return (MapBack.IPersistenceSet)Activator.CreateInstance(set);
        }

        public MapBack.IPersistenceSet<TModel> GetIPersistenceSet<TModel>() where TModel : class
        {
            return (MapBack.IPersistenceSet<TModel>)this.People;
        }

        public int SaveChanges()
        {
            //Do Nothing
            return 0;
        }

        public void Dispose()
        {
            //Do Nothing
        }
    }
}
