using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Utility;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Linq;
using System.Collections.ObjectModel;

namespace CompMs.Common.Utility.Tests
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

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundNotContainsValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(arr, value);
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

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);

            value = 3d;
            expected = 3;
            actual = SearchCollection.LowerBound(arr, value, 3, arr.Length);
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

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);

            value = 4d;
            expected = 2;
            actual = SearchCollection.LowerBound(arr, value, 0, 2);
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

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundCustomComparisonTest()
        {
            var arr1 = new (int, int)[] { (2, 3), (1, 5), (10000, 6), (-12, 9), (0, 12), (0, 13), (-4, 20) };
            var value1 = (-1, 6);
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr1, value1, (a, b) => a.Item2.CompareTo(b.Item2));
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(arr1, value1, 0, arr1.Length, (a, b) => a.Item2.CompareTo(b.Item2));
            Assert.AreEqual(expected, actual);

            var arr2 = new int[] { 10, 7, 3, 1, 0, -2, -4, -5 };
            var value2 = -2;
            expected = 5;
            actual = SearchCollection.LowerBound(arr2, value2, (a, b) => b.CompareTo(a));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundCollectionClassTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);

            var lst = arr.ToList();
            actual = SearchCollection.LowerBound(lst, value);
            Assert.AreEqual(expected, actual);

            var observable = new ObservableCollection<double>(arr);
            actual = SearchCollection.LowerBound(observable, value);
            Assert.AreEqual(expected, actual);

            var readonlycollection = new ReadOnlyCollection<double>(arr);
            actual = SearchCollection.LowerBound(readonlycollection, value);
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

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundNotContainsValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundSmallestValueTest()
        {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 0d;
            var expected = 0;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);

            value = 3d;
            expected = 3;
            actual = SearchCollection.UpperBound(arr, value, 3, arr.Length);
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

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);

            value = 4d;
            expected = 2;
            actual = SearchCollection.UpperBound(arr, value, 0, 2);
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

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }
    }
}
