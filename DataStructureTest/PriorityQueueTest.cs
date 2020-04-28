using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.DataStructure;

namespace DataStructureTest
{
    [TestClass]
    public class PriorityQueueTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var q = new PriorityQueue<int>((a, b) => a.CompareTo(b));

            q.Push(1);
            q.Push(5);
            q.Push(0);
            q.Push(1);
            q.Push(-4);
            q.Push(42);
            q.Push(52);
            q.Push(-78);
            q.Push(0);
            q.Push(19);
            q.Push(8);

            Assert.AreEqual(-78, q.Pop());
            Assert.AreEqual(-4, q.Pop());
            Assert.AreEqual(0, q.Pop());
            Assert.AreEqual(0, q.Pop());

            q.Push(-3);
            q.Push(4);

            Assert.AreEqual(-3, q.Pop());
            Assert.AreEqual(1, q.Pop());
            Assert.AreEqual(1, q.Pop());
            Assert.AreEqual(4, q.Pop());
            Assert.AreEqual(5, q.Pop());
            Assert.AreEqual(8, q.Pop());
            Assert.AreEqual(19, q.Pop());
            Assert.AreEqual(42, q.Pop());
            Assert.AreEqual(52, q.Pop());
        }
    }
}
