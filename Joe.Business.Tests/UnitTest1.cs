using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Joe.Business.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test()
        {
            var repository = new MockRepository<Person>();
            var cacheRepository = new CacheRepository<Person>();

            var people = repository.Get();

            Assert.IsTrue(people.Count() == 3);

            var cachedPeople = cacheRepository.Get();

            Assert.IsTrue(cachedPeople.Count() == 3);
        }
    }
}
