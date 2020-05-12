using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.DataStructure;

namespace DataStructureTest
{
    [TestClass]
    public class SearchCollectionTest
    {
        [TestMethod]
        public void LowerBoundBaseTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundNotContainsValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, 3d, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundSmallestValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 0d;
            var expected = 0;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void LowerBoundLargestValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 100d;
            var expected = 7;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void LowerBoundFindFirstValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 3d, 3d, 3d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundBaseTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 3;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundNotContainsValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.UpperBound(arr, 3d, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare));
        }

        [TestMethod]
        public void UpperBoundSmallestValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 0d;
            var expected = 0;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void UpperBoundLargestValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 100d;
            var expected = 7;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void UpperBoundFindFirstValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 3d, 3d, 3d, 7d };
            var value = 2d;
            var expected = 2;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);
        }
    }
}
