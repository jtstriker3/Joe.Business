using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joe.Business.Tests
{
    public class MockPersistenceSet<T> : Joe.MapBack.IPersistenceSet<T>
        where T : class
    {
        private ObservableCollection<T> _mockCollection;
        IQueryable _query;

        public MockPersistenceSet()
        {
            _mockCollection = new ObservableCollection<T>();
            _query = _mockCollection.AsQueryable();
        }

        public T Add(T entity)
        {
            _mockCollection.Add(entity);
            return entity;
        }

        public T Attach(T entity)
        {
            _mockCollection.Add(entity);
            return entity;
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            return Activator.CreateInstance<TDerivedEntity>();
        }

        public T Create()
        {
            return Activator.CreateInstance<T>();
        }

        public T Find(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public IList<T> Local
        {
            get { return _mockCollection; }
        }

        public T Remove(T entity)
        {
            _mockCollection.Remove(entity);
            return entity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _mockCollection.GetEnumerator();
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return _query.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _query.Provider; }
        }
    }
}
