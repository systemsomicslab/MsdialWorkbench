using CompMs.MsdialCore.DataObj.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class MsdialIgnoreSavingSerializerTests
    {
        [TestMethod()]
        [ExpectedException(typeof(AggregateException))]
        public void LoadAsyncTest() {
            var serializer = new MsdialIgnoreSavingSerializer();
            var task = serializer.LoadAsync(null, "TestTitle", "TestFolder", string.Empty);
            task.Wait();
            Assert.IsTrue(task.IsCanceled);
        }

        [TestMethod()]
        public void SaveAsyncTest() {
            var serializer = new MsdialIgnoreSavingSerializer();
            var storage = new MockStorage();
            var task = serializer.SaveAsync(storage, null, "TestTitle", string.Empty);
            task.Wait();
            Assert.IsTrue(task.IsCompleted);
        }
    }
}