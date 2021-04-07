using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataStructure.Tests
{
    [TestClass()]
    public class KdTreeTests
    {
        [TestMethod()]
        public void NearestNeighborTest() {
            var tree = KdTree<double[]>.Build(
                new List<double[]>
                {
                    new double[] { 0, 0 },
                    new double[] { 1, 8 },
                    new double[] { 8, 3 },
                    new double[] { 3, 2 },
                    new double[] { 5, 9 },
                    new double[] { 4, 3 },
                },
                v => v[0],
                v => v[1]);

            var result = tree.NearestNeighbor(new double[] { 4, 6 });
            Assert.AreEqual(4, result[0]);
            Assert.AreEqual(3, result[1]);
        }

        [TestMethod()]
        public void RangeSearchTest() {
            var ps = new List<double[]>
            {
                new double[] { 0, 0 },
                new double[] { 1, 8 },
                new double[] { 8, 3 },
                new double[] { 3, 2 },
                new double[] { 5, 9 },
                new double[] { 4, 3 },
            };
            var tree = KdTree<double[]>.Build(
                ps,
                v => v[0],
                v => v[1]);

            var results = tree.RangeSearch(new double[] { 1, 3 }, new double[] { 5, 8 });
            CollectionAssert.AreEquivalent(new List<double[]> { ps[1], ps[5] }, results);
        }

        [TestMethod()]
        public void NeighborSearchTest() {
            var ps = new List<double[]>
            {
                new double[] { 0, 0 },
                new double[] { 1, 8 },
                new double[] { 8, 3 },
                new double[] { 3, 2 },
                new double[] { 5, 9 },
                new double[] { 4, 3 },
            };
            var tree = KdTree<double[]>.Build(
                ps,
                v => v[0],
                v => v[1]);
            var results = tree.NeighborSearch(new double[] { 5, 4 }, 5);
            foreach (var result in results)
                Console.WriteLine($"{result[0]}, {result[1]}");
            CollectionAssert.AreEquivalent(new List<double[]> { ps[2], ps[3], ps[4], ps[5] }, results);
        }
    }
}