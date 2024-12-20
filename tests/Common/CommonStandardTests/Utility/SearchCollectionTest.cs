#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Utility.Tests
{
    [TestClass]
    public class SearchCollectionTest
    {
        internal struct SampleStruct
        {
            public double X;
            public double Y;
        }

        [TestMethod]
        public void LowerBoundBaseTest() {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundNotContainsValueTest() {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundSmallestValueTest() {
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
        public void LowerBoundLargestValueTest() {
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
        public void LowerBoundFindFirstValueTest() {
            var arr = new double[] { 1d, 2d, 3d, 3d, 3d, 3d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.LowerBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundCustomComparisonTest() {
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
        public void LowerBoundCollectionClassTest() {
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
        public void LowerBoundStructTest() {
            var lst = new List<SampleStruct>();
            for (int i = 0; i < 10; i++) {
                lst.Add(new SampleStruct() { X = -i, Y = i });
            }
            var value = new SampleStruct() { X = 0, Y = 3 };
            var expected = 3;
            var actual = SearchCollection.LowerBound(lst, value, (a, b) => a.Y.CompareTo(b.Y));
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(lst.ToArray(), value, (a, b) => a.Y.CompareTo(b.Y));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LowerBoundDifferentTypeTest() {
            var lst = new List<SampleStruct>();
            for (int i = 0; i < 10; i++) {
                lst.Add(new SampleStruct() { X = -i, Y = i });
            }
            var value = 3;
            var expected = 3;
            var actual = SearchCollection.LowerBound(lst, value, (a, b) => a.Y.CompareTo(b));
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.LowerBound(lst.ToArray(), value, (a, b) => a.Y.CompareTo(b));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundBaseTest() {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 3;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundNotContainsValueTest() {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 2.5;
            var expected = 2;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundSmallestValueTest() {
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
        public void UpperBoundLargestValueTest() {
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
        public void UpperBoundFindFirstValueTest() {
            var arr = new double[] { 1d, 2d, 3d, 3d, 3d, 3d, 7d };
            var value = 2d;
            var expected = 2;
            var actual = SearchCollection.UpperBound(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(arr, value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundStructTest() {
            var lst = new List<SampleStruct>();
            for (int i = 0; i < 10; i++) {
                lst.Add(new SampleStruct() { X = -i, Y = i });
            }
            var value = new SampleStruct() { X = 0, Y = 3 };
            var expected = 4;
            var actual = SearchCollection.UpperBound(lst, value, (a, b) => a.Y.CompareTo(b.Y));
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(lst.ToArray(), value, (a, b) => a.Y.CompareTo(b.Y));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void UpperBoundDifferentTypeTest() {
            var lst = new List<SampleStruct>();
            for (int i = 0; i < 10; i++) {
                lst.Add(new SampleStruct() { X = -i, Y = i });
            }
            var value = 3;
            var expected = 4;
            var actual = SearchCollection.UpperBound(lst, value, (a, b) => a.Y.CompareTo(b));
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.UpperBound(lst.ToArray(), value, (a, b) => a.Y.CompareTo(b));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void BinarySearchTest() {
            var arr = new double[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d };
            var value = 3d;
            var expected = 2;
            var actual = SearchCollection.BinarySearch(arr, value, 0, arr.Length, Comparer<double>.Default.Compare);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.BinarySearch(arr, value);
            Assert.AreEqual(expected, actual);

            actual = SearchCollection.BinarySearch(arr, 1d);
            Assert.AreEqual(0, actual);

            actual = SearchCollection.BinarySearch(arr, 7d);
            Assert.AreEqual(6, actual);

            actual = SearchCollection.BinarySearch(arr, 0d);
            Assert.AreEqual(-1, actual);

            actual = SearchCollection.BinarySearch(arr, 8d);
            Assert.AreEqual(-1, actual);
        }

        [TestMethod]
        public void PermutationTest() {
            var collection = new[] { 1, 2, 3, 4, };
            var actuals = SearchCollection.Permutations(collection).Select(arr => (arr[0], arr[1], arr[2], arr[3])).ToArray();
            var expects = new[]
            {
                ( 1, 2, 3, 4), ( 1, 2, 4, 3), ( 1, 3, 2, 4), ( 1, 3, 4, 2), ( 1, 4, 2, 3), ( 1, 4, 3, 2),
                ( 2, 1, 3, 4), ( 2, 1, 4, 3), ( 2, 3, 1, 4), ( 2, 3, 4, 1), ( 2, 4, 1, 3), ( 2, 4, 3, 1),
                ( 3, 1, 2, 4), ( 3, 1, 4, 2), ( 3, 2, 1, 4), ( 3, 2, 4, 1), ( 3, 4, 1, 2), ( 3, 4, 2, 1),
                ( 4, 1, 2, 3), ( 4, 1, 3, 2), ( 4, 2, 1, 3), ( 4, 2, 3, 1), ( 4, 3, 1, 2), ( 4, 3, 2, 1),
            };
            CollectionAssert.AreEquivalent(expects, actuals);

            collection = new[] { 1, 1, 2, 3, };
            actuals = SearchCollection.Permutations(collection).Select(arr => (arr[0], arr[1], arr[2], arr[3])).ToArray();
            expects = new[]
            {
                ( 1, 1, 2, 3), ( 1, 1, 3, 2), ( 1, 2, 1, 3), ( 1, 2, 3, 1), ( 1, 3, 1, 2), ( 1, 3, 2, 1),
                ( 2, 1, 1, 3), ( 2, 1, 3, 1), ( 2, 3, 1, 1),
                ( 3, 1, 1, 2), ( 3, 1, 2, 1), ( 3, 2, 1, 1),
            };
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod()]
        public void CombinationTest() {
            IEnumerable<int> sample() {
                yield return 1;
                yield return 2;
                yield return 3;
                yield return 4;
            }
            IEnumerable<int> collection = sample();
            var actuals = SearchCollection.Combination(collection, 3).Select(arr => (arr[0], arr[1], arr[2])).ToArray();
            var expects = new[]
            {
                (1, 2, 3), (1, 2, 4), (1, 3, 4), (2, 3, 4),
            };
            CollectionAssert.AreEquivalent(expects, actuals);

            collection = new[] { 1, 2, 3, 4, };
            actuals = SearchCollection.Combination(collection, 3).Select(arr => (arr[0], arr[1], arr[2])).ToArray();
            expects = new[]
            {
                (1, 2, 3), (1, 2, 4), (1, 3, 4), (2, 3, 4),
            };
            CollectionAssert.AreEquivalent(expects, actuals);

            collection = new[] { 1, 1, 2, 3, };
            actuals = SearchCollection.Combination(collection, 3).Select(arr => (arr[0], arr[1], arr[2])).ToArray();
            expects = new[]
            {
                (1, 1, 2), (1, 1, 3), (1, 2, 3), (1, 2, 3),
            };
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod]
        public void CartesianProductTest() {
            var collection = new[] {
                new[] { 1, 2, 3, 4, },
                new[] { 5, 6, },
                new[] { 7, },
                new[] { 8, 9, },
            };

            var expects = new[]
            {
                new[] { 1, 5, 7, 8, },
                new[] { 1, 5, 7, 9, },
                new[] { 1, 6, 7, 8, },
                new[] { 1, 6, 7, 9, },
                new[] { 2, 5, 7, 8, },
                new[] { 2, 5, 7, 9, },
                new[] { 2, 6, 7, 8, },
                new[] { 2, 6, 7, 9, },
                new[] { 3, 5, 7, 8, },
                new[] { 3, 5, 7, 9, },
                new[] { 3, 6, 7, 8, },
                new[] { 3, 6, 7, 9, },
                new[] { 4, 5, 7, 8, },
                new[] { 4, 5, 7, 9, },
                new[] { 4, 6, 7, 8, },
                new[] { 4, 6, 7, 9, },
            };
            var actuals = SearchCollection.CartesianProduct(collection);

            foreach ((var exp, var act) in expects.ZipInternal(actuals)) {
                CollectionAssert.AreEqual(exp, act);
            }
        }

        [TestMethod]
        public void CartesianProductSingleElementTest()
        {
            int[][] collection = [
                [1,],
                [ 2, 3, 4, 5, ],
            ];

            int[][] expects = [
                [ 1, 2, ],
                [ 1, 3, ],
                [ 1, 4, ],
                [ 1, 5, ],
            ];
            var actuals = SearchCollection.CartesianProduct(collection);

            foreach ((var exp, var act) in expects.ZipInternal(actuals))
            {
                CollectionAssert.AreEqual(exp, act);
            }
        }
    }
}
