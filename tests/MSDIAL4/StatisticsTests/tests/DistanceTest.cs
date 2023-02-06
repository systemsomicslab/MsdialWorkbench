using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rfx.Riken.OsakaUniv;

namespace Msdial.StatisticsTests
{
    
    [TestClass]
    public class CalculateEuclideanDistanceTest
    {
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 2, 3, 4, 5}, new double[] { 2, 3, 4, 5, 6}, 2.2360679)]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 2, 6, 3}, 4.12310562)]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 0, 0, 0}, 0.0)]
        [DataRow(new double[] { -1, -2, -3}, new double[] { -2, -6, -3}, 4.12310562)]
        public void ValidCaseTest(double[] xs, double[] ys, double expected)
        {
            Assert.AreEqual(expected, StatisticsMathematics.CalculateEuclideanDistance(xs, ys), 0.0000001);
        }
    }

    [TestClass]
    public class UnsafeCalculatePearsonDistanceTest
    {
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 2, 3, 4, 5}, new double[] { 2, 3, 4, 5, 6}, 0.0)]
        [DataRow(new double[] { 2, 3, 4, 5, 6}, new double[] { 1, 2, 3, 4, 5}, 0.0)]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 2, 6, 3}, 0.759807769)]
        [DataRow(new double[] { 2, 6, 3}, new double[] { 1, 2, 3}, 0.759807769)]
        [DataRow(new double[] { -1, -2, -3}, new double[] { -2, -6, -3}, 0.759807769)]
        [DataRow(new double[] { -2, -6, -3}, new double[] { -1, -2, -3}, 0.759807769)]
        [DataRow(new double[] { 2, 6, 3}, new double[] { -1, -2, -3}, 1.2401922307076307)]
        [DataRow(new double[] { -2, -6, -3}, new double[] { 1, 2, 3}, 1.2401922307076307)]
        public void ValidCaseTest(double[] xs, double[] ys, double expected)
        {
            var result = StatisticsMathematics.UnsafeCalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(expected, result, 0.0000001);
        }
        
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 1, 2, 3})]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 0, 0, 0})]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 0, 0, 0})]
        public void InputZerosTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.UnsafeCalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(result, double.PositiveInfinity);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 1, 1}, new double[] { 1, 2, 3})]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 10, 10, 10})]
        [DataRow(new double[] { 8, 8, 8}, new double[] { 0, 0, 0})]
        public void InputSameElementsTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.UnsafeCalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(double.PositiveInfinity, result);
        }

    }
  
    [TestClass]
    public class CalculatePearsonDistanceTest
    {
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 2, 3, 4, 5}, new double[] { 2, 3, 4, 5, 6})]
        [DataRow(new double[] { 2, 3, 4, 5, 6}, new double[] { 1, 2, 3, 4, 5})]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 2, 6, 3})]
        [DataRow(new double[] { 2, 6, 3}, new double[] { 1, 2, 3})]
        [DataRow(new double[] { -1, -2, -3}, new double[] { -2, -6, -3})]
        [DataRow(new double[] { -2, -6, -3}, new double[] { -1, -2, -3})]
        public void ValidCaseTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.CalculatePearsonCorrelationDistance(xs, ys);
            var expected = StatisticsMathematics.UnsafeCalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(expected, result, 0.0000001);
        }
        
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 1, 2, 3})]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 0, 0, 0})]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 0, 0, 0})]
        public void InputZerosTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.CalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 1, 1}, new double[] { 1, 2, 3})]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 10, 10, 10})]
        [DataRow(new double[] { 8, 8, 8}, new double[] { 0, 0, 0})]
        public void InputSameElementsTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.CalculatePearsonCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(1, result);
        }
    }
  
    [TestClass]
    public class CalculateSpearmanDistanceTest
    {
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 2, 3, 4, 5}, new double[] { 2, 3, 4, 5, 6}, 0.0)]
        [DataRow(new double[] { 2, 3, 4, 5, 6}, new double[] { 1, 2, 3, 4, 5}, 0.0)]
        [DataRow(new double[] { 1, 2, 3}, new double[] { 2, 6, 3}, 0.5)]
        [DataRow(new double[] { 2, 6, 3}, new double[] { 1, 2, 3}, 0.5)]
        [DataRow(new double[] { -1, -2, -3}, new double[] { -2, -6, -3}, 0.5)]
        [DataRow(new double[] { -2, -6, -3}, new double[] { -1, -2, -3}, 0.5)]
        public void ValidCaseTest(double[] xs, double[] ys, double expected)
        {
            var result = StatisticsMathematics.CalculateSpearmanCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(expected, result, 0.0000001);
        }
        
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 1, 2, 3, 3 }, new double[] { 2, 3, 4, 5 }, 0.051316701949)]
        public void ContainsSameValueTest(double[] xs, double[] ys, double expected)
        {
            var result = StatisticsMathematics.CalculateSpearmanCorrelationDistance(xs, ys);
            Console.WriteLine(result);
            Assert.AreEqual(expected, result, 0.0000001);
        }
        
        [TestMethod]
        [DataTestMethod]
        [DataRow(new double[] { 0, 0, 0}, new double[] { 0, 0, 0})]
        [DataRow(new double[] { 1, 1, 1}, new double[] { 2, 3, 4})]
        [DataRow(new double[] { 2, 3, 4}, new double[] { 1, 1, 1})]
        public void SameValuesAllTest(double[] xs, double[] ys)
        {
            var result = StatisticsMathematics.CalculateSpearmanCorrelationDistance(xs, ys);
            Assert.AreEqual(1, result);
        }
    }

    [TestClass]
    public class WardDistanceTest {
        [TestMethod]
        public void ValidCaseTest()
        {
            List<List<double>> xs = new List<List<double>>
            {
                new List<double> {3, 4, 5},
                new List<double> {6, 7, 8},
                new List<double> {9, 10, 11},
                new List<double> {12, 13, 14}
            };
            List<List<double>> ys = new List<List<double>>
            {
                new List<double> {-3, 13, 61},
                new List<double> {-2, 22, 78},
                new List<double> {1, 33, 97},
                new List<double> {6, 46, 118}
            };
            double expected = 0.0818974995993;
            var result = StatisticsMathematics.CalculateWardDistance(xs, ys, StatisticsMathematics.CalculatePearsonCorrelationDistance);
            Assert.AreEqual(expected, result, 0.000000001);
            Console.WriteLine(result);
        }
    }
}
