using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;

namespace Msdial.StatisticsTests
{
    [TestClass]
    public class CentroidTest
    {
        [TestMethod]
        public void ValidCase1Test() {
            var xs = new List<double> { 0, 1, 2, 3 };
            var ys = new List<double> { 8, 2, 1, 4 };
            var zs = new List<double> { 4, 5, 3, 8 };
            var ws = new List<double> { 0, 2, 4, 1 };
            var result = StatisticsMathematics.CalculateCentroid(new List<List<double>> { xs, ys, zs, ws });
            Console.WriteLine(String.Join(",", result.Select(e=>e.ToString())));
            CollectionAssert.AreEqual(
                result, new List<double> { 3.0, 2.5, 2.5, 4.0 }
            );

        }

        [TestMethod]
        public void ValidCase2Test() {
            var xs = new List<double> { 7, 2, 2, 2 };
            var ys = new List<double> { 9, 2, 8, 3 };
            var zs = new List<double> { 1, 4, 3, 5 };
            var ws = new List<double> { 0, 2, 0, 2 };
            var result = StatisticsMathematics.CalculateCentroid(new List<List<double>> { xs, ys, zs, ws });
            Console.WriteLine(String.Join(",", result.Select(e=>e.ToString())));
            CollectionAssert.AreEqual(
                result, new List<double> { 4.25, 2.5, 3.25, 3.0 }
            );
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void EmptySetTest()
        {
            StatisticsMathematics.CalculateCentroid(Enumerable.Empty<List<double>>());
            Assert.Fail();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DifferentLengthTest()
        {
            var xs = new List<double> { 0, 1, 2, 3 };
            var ys = new List<double> { 4, 5, 6 };
            var zs = new List<double> { 7, 8, 9, 10 };
            StatisticsMathematics.CalculateCentroid(new List<List<double>> { xs, ys, zs });
            Assert.Fail();
        }

    }
}