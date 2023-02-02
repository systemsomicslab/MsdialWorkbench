using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.DataStructure.Tests
{
    [TestClass()]
    public class LazySegmentTreeTests
    {
        [TestMethod()]
        public void QueryTest() {
            var tree = new LazySegmentTree<Data, Lazy>(
                10,              // Tree size
                (x, y) => x + y, // Binary operation of monoid T
                (x, z) => x - z, // Binary operation between T and E
                (z, w) => z + w, // Binary operation of monoid E
                Data.Empty,      // Identity element of monoid T
                Lazy.Empty       // Identity element of monoid E
            );
            var xs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
            tree.Build(xs.Select(Data.AsValue).ToArray());

            Assert.AreEqual(10, tree.Query(0, 5).Value); // sum of [0, 5)
            Assert.AreEqual(28, tree.Query(1, 8).Value); // sum of [1, 8)
            Assert.AreEqual(45, tree.Query().Value); // sum of all
        }

        [TestMethod()]
        public void ApplyTest() {
            var tree = new LazySegmentTree<Data, Lazy>(
                10,              // Tree size
                (x, y) => x + y, // Binary operation of monoid T
                (x, z) => x - z, // Binary operation between T and E
                (z, w) => z + w, // Binary operation of monoid E
                Data.Empty,      // Identity element of monoid T
                Lazy.Empty       // Identity element of monoid E
            );
            var xs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
            tree.Build(xs.Select(Data.AsValue).ToArray());

            tree.Apply(3, new Lazy(2)); // { 0, 1, 2, 3 - 2, 4, 5, 6, 7, 8, 9, }
            Assert.AreEqual(8, tree.Query(0, 5).Value); // sum of [0, 5)
            Assert.AreEqual(26, tree.Query(1, 8).Value); // sum of [1, 8)
            Assert.AreEqual(43, tree.Query().Value); // sum of all

            tree.Apply(2, 7, new Lazy(1)); // { 0, 1, 2 - 1, 1 - 1, 4 - 1, 5 - 1, 6 - 1, 7, 8, 9, }
            Assert.AreEqual(5, tree.Query(0, 5).Value); // sum of [0, 5)
            Assert.AreEqual(21, tree.Query(1, 8).Value); // sum of [1, 8)
            Assert.AreEqual(38, tree.Query().Value); // sum of all
        }

        [TestMethod()]
        public void ApplyTest1() {
            var tree = new LazySegmentTree<Data, Lazy>(
                16,              // Tree size
                (x, y) => x + y, // Binary operation of monoid T
                (x, z) => x - z, // Binary operation between T and E
                (z, w) => z + w, // Binary operation of monoid E
                Data.Empty,      // Identity element of monoid T
                Lazy.Empty       // Identity element of monoid E
            );
            var xs = Enumerable.Range(0, 16);
            tree.Build(xs.Select(Data.AsValue).ToArray());

            tree.Apply(1, 15, new Lazy(1));
            Assert.AreEqual(91, tree.Query(1, 15).Value);
            Assert.AreEqual(65, tree.Query(3, 13).Value);
            Assert.AreEqual(39, tree.Query(5, 11).Value);
            Assert.AreEqual(13, tree.Query(7, 9).Value);
            Assert.AreEqual(106, tree.Query().Value);

            tree.Apply(1, 15, new Lazy(1));
            tree.Apply(1, 15, new Lazy(-1));
            Assert.AreEqual(91, tree.Query(1, 15).Value);
            Assert.AreEqual(65, tree.Query(3, 13).Value);
            Assert.AreEqual(39, tree.Query(5, 11).Value);
            Assert.AreEqual(13, tree.Query(7, 9).Value);
            Assert.AreEqual(106, tree.Query().Value);
        }

        [TestMethod()]
        public void FindFirstTest() {
            var tree = new LazySegmentTree<Data, Lazy>(
                10,              // Tree size
                (x, y) => x + y, // Binary operation of monoid T
                (x, z) => x - z, // Binary operation between T and E
                (z, w) => z + w, // Binary operation of monoid E
                Data.Empty,      // Identity element of monoid T
                Lazy.Empty       // Identity element of monoid E
            );
            var xs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
            tree.Build(xs.Select(Data.AsValue).ToArray());

            Assert.AreEqual(6, tree.FindFirst(0, d => d.Value > 10)); // 0 + 1 + 2 + 3 + 4 + 5 = 15 > 10
            Assert.AreEqual(6, tree.FindFirst(1, d => d.Value > 10)); // 1 + 2 + 3 + 4 + 5 = 15 > 10
            Assert.AreEqual(8, tree.FindFirst(2, d => d.Value > 20)); // 2 + 3 + 4 + 5 + 6 + 7 = 27 > 20
            Assert.AreEqual(10, tree.FindFirst(0, d => d.Value > 45)); // 0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 = 45
        }

        [TestMethod()]
        public void FindLastTest() {
            var tree = new LazySegmentTree<Data, Lazy>(
                10,              // Tree size
                (x, y) => x + y, // Binary operation of monoid T
                (x, z) => x - z, // Binary operation between T and E
                (z, w) => z + w, // Binary operation of monoid E
                Data.Empty,      // Identity element of monoid T
                Lazy.Empty       // Identity element of monoid E
            );
            var xs = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
            tree.Build(xs.Select(Data.AsValue).ToArray());

            Assert.AreEqual(3, tree.FindLast(6, d => d.Value > 10)); // 3 + 4 + 5 = 12 > 10
            Assert.AreEqual(1, tree.FindLast(7, d => d.Value > 20)); // 1 + 2 + 3 + 4 + 5 + 6 = 21 > 20
            Assert.AreEqual(-1, tree.FindLast(9, d => d.Value > 45)); // 0 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 = 45
        }

        class Data {
            public static readonly Data Empty = new Data(0, 0);

            public Data(int value, int size) {
                Value = value;
                Size = size;
            }

            public int Value { get; }
            public int Size { get; }

            public static Data operator+ (Data x, Data y) {
                return new Data(x.Value + y.Value, x.Size + y.Size);
            }

            public static Data AsValue(int x) {
                return new Data(x, 1);
            }
        }

        class Lazy {
            private readonly int _value;

            public static readonly Lazy Empty = new Lazy(0);

            public Lazy(int value) {
                _value = value;
            }

            public static Lazy operator+(Lazy x, Lazy y) {
                return new Lazy(x._value + y._value);
            }

            public static Data operator-(Data x, Lazy y) {
                return new Data(x.Value - y._value * x.Size, x.Size);
            }
        }
    }
}