using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataStructure.Tests
{
    [TestClass()]
    public class SegmentTreeTest
    {
        [TestMethod()]
        public void BuildTest() {
            var seg = new SegmentTree<int>(4, 0, (a, b) => a + b);

            seg.Set(0, 1);
            seg.Set(1, 3);
            seg.Set(2, -2);
            seg.Set(3, 8);
            seg.Build();
            Assert.AreEqual(10, seg.Query(0, 4));
            Assert.AreEqual(2, seg.Query(0, 3));
            Assert.AreEqual(1, seg.Query(1, 3));
            Assert.AreEqual(4, seg.Query(0, 2));

            seg.Set(0, 100);
            seg.Build();
            Assert.AreEqual(109, seg.Query(0, 4));
            Assert.AreEqual(101, seg.Query(0, 3));
            Assert.AreEqual(1, seg.Query(1, 3));
            Assert.AreEqual(103, seg.Query(0, 2));
        }

        [TestMethod()]
        public void UpdateTest() {
            var seg = new SegmentTree<int>(4, 0, (a, b) => a + b);

            seg.Set(0, 1);
            seg.Set(1, 3);
            seg.Set(2, -2);
            seg.Set(3, 8);
            seg.Build();
            Assert.AreEqual(10, seg.Query(0, 4));
            Assert.AreEqual(2, seg.Query(0, 3));
            Assert.AreEqual(1, seg.Query(1, 3));
            Assert.AreEqual(4, seg.Query(0, 2));

            seg.Update(0, 100);
            Assert.AreEqual(109, seg.Query(0, 4));
            Assert.AreEqual(101, seg.Query(0, 3));
            Assert.AreEqual(1, seg.Query(1, 3));
            Assert.AreEqual(103, seg.Query(0, 2));
        }

        [TestMethod()]
        public void QueryTest() {
            var seg = new SegmentTree<int>(4, 0, (a, b) => a + b);

            seg.Set(0, 1);
            seg.Set(1, 3);
            seg.Set(2, -2);
            seg.Set(3, 8);
            seg.Build();

            Assert.AreEqual(0, seg.Query(0, 0));
            Assert.AreEqual(1, seg.Query(0, 1));
            Assert.AreEqual(4, seg.Query(0, 2));
            Assert.AreEqual(2, seg.Query(0, 3));
            Assert.AreEqual(10, seg.Query(0, 4));

            Assert.AreEqual(0, seg.Query(1, 0));
            Assert.AreEqual(0, seg.Query(1, 1));
            Assert.AreEqual(3, seg.Query(1, 2));
            Assert.AreEqual(1, seg.Query(1, 3));
            Assert.AreEqual(9, seg.Query(1, 4));

            Assert.AreEqual(0, seg.Query(2, 0));
            Assert.AreEqual(0, seg.Query(2, 1));
            Assert.AreEqual(0, seg.Query(2, 2));
            Assert.AreEqual(-2, seg.Query(2, 3));
            Assert.AreEqual(6, seg.Query(2, 4));

            Assert.AreEqual(0, seg.Query(3, 0));
            Assert.AreEqual(0, seg.Query(3, 1));
            Assert.AreEqual(0, seg.Query(3, 2));
            Assert.AreEqual(0, seg.Query(3, 3));
            Assert.AreEqual(8, seg.Query(3, 4));

            Assert.AreEqual(0, seg.Query(4, 0));
            Assert.AreEqual(0, seg.Query(4, 1));
            Assert.AreEqual(0, seg.Query(4, 2));
            Assert.AreEqual(0, seg.Query(4, 3));
            Assert.AreEqual(0, seg.Query(4, 4));
        }
    }
}
