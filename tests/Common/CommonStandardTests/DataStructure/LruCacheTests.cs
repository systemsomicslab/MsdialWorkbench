using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.DataStructure.Tests
{
    [TestClass]
    public class LruCacheTests
    {
        [TestMethod()]
        public void PushTest() {
            var cache = new LruCache<int, string>(5);
            Assert.IsFalse(cache.ContainsKey(1));
            cache.Put(1, "1");
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("1", cache.Get(1));
        }

        [TestMethod()]
        public void UpdateTest() {
            var cache = new LruCache<int, string>(5);
            cache.Put(1, "1");
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("1", cache.Get(1));
            cache.Put(1, "10");
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("10", cache.Get(1));
        }

        [TestMethod()]
        public void CachedTest() {
            var cache = new LruCache<int, string>(5);
            cache.Put(1, "1");
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("1", cache.Get(1));
            cache.Put(2, "2");
            Assert.IsTrue(cache.ContainsKey(2));
            Assert.AreEqual("2", cache.Get(2));
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("1", cache.Get(1));
        }

        [TestMethod()]
        public void RemoveOldTest() {
            var cache = new LruCache<int, string>(5);
            cache.Put(1, "1");
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("1", cache.Get(1));

            cache.Put(2, "2");
            cache.Put(3, "3");
            cache.Put(4, "4");
            cache.Put(5, "5");
            cache.Put(6, "6");
            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsTrue(cache.ContainsKey(2));
            Assert.IsTrue(cache.ContainsKey(3));
            Assert.IsTrue(cache.ContainsKey(4));
            Assert.IsTrue(cache.ContainsKey(5));
            Assert.IsTrue(cache.ContainsKey(6));

            cache.Put(2, "20");
            cache.Put(7, "7");
            Assert.IsTrue(cache.ContainsKey(2));
            Assert.IsFalse(cache.ContainsKey(3));
            Assert.IsTrue(cache.ContainsKey(4));
            Assert.IsTrue(cache.ContainsKey(5));
            Assert.IsTrue(cache.ContainsKey(6));
            Assert.IsTrue(cache.ContainsKey(7));
        }
    }
}
