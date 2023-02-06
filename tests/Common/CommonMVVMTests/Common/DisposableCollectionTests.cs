using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.CommonMVVM.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CompMs.CommonMVVM.Common.Tests
{
    [TestClass()]
    public class DisposableCollectionTests
    {
        [TestMethod()]
        public void DisposeTest() {
            var collection = new DisposableCollection();
            collection.Add(new A());
            collection.Add(new A());

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            Console.SetOut(writer);
            collection.Dispose();
            Assert.AreEqual("A is Disposed.A is Disposed.", builder.ToString());
        }

        [TestMethod()]
        public void DisposeTest2() {
            var collection = new DisposableCollection();
            collection.Add(new A());

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            Console.SetOut(writer);
            collection.Dispose();
            collection.Add(new A());
            Assert.AreEqual("A is Disposed.A is Disposed.", builder.ToString());
        }
    }

    class A : IDisposable
    {
        public void Dispose() {
            Console.Write("A is Disposed.");
        }
    }
}