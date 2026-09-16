using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CompMs.Common.Extension.Tests
{
    [TestClass()]
    public class IEnumerableExtensionTests {
        class SampleClass {
            public int Data { get; set; }
        }

        class SampleClass2 : IComparable<SampleClass2> {
            public int Data { get; set; }

            public int CompareTo(SampleClass2 other) {
                return Data.CompareTo(other.Data);
            }
        }


        [TestMethod()]
        public void ArgmaxTest1() {
            var xs = new List<int> { 4, 2, 6, 1, 5, 1, 6, 2 };
            Assert.AreEqual(2, xs.Argmax());
        }

        [TestMethod()]
        public void ArgmaxTest2() {
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                new SampleClass{Data = 6},
                new SampleClass{Data = 1},
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };
            Assert.AreEqual(2, xs.Argmax((a, b) => a.Data.CompareTo(b.Data)));
        }

        [TestMethod()]
        public void ArgmaxTest3() {
            var x = new SampleClass { Data = 1 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                new SampleClass{Data = 6},
                x,
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };
            Assert.AreEqual(x, xs.Argmax(e => -e.Data));
        }

        [TestMethod()]
        public void ArgmaxTest4() {
            var x = new SampleClass { Data = 6 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                x,
                new SampleClass{Data = 1},
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };

            Assert.AreEqual(x, xs.Argmax(e => new SampleClass2 { Data = e.Data }, Comparer<SampleClass2>.Default));
        }

        [TestMethod()]
        public void ArgmaxTest5() {
            var x = new SampleClass { Data = 1 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                new SampleClass{Data = 6},
                x,
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };

            Assert.AreEqual(x, xs.Argmax(e => e.Data, (a, b) => b.CompareTo(a)));
        }

        [TestMethod()]
        public void ArgminTest1() {
            var xs = new List<int> { 4, 2, 6, 1, 5, 1, 6, 2 };
            Assert.AreEqual(3, xs.Argmin());
        }

        [TestMethod()]
        public void ArgminTest2() {
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                new SampleClass{Data = 6},
                new SampleClass{Data = 1},
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };
            Assert.AreEqual(3, xs.Argmin((a, b) => a.Data.CompareTo(b.Data)));
        }

        [TestMethod()]
        public void ArgminTest3() {
            var x = new SampleClass { Data = 6 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                x,
                new SampleClass{Data = 1},
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };
            Assert.AreEqual(x, xs.Argmin(e => -e.Data));
        }

        [TestMethod()]
        public void ArgminTest4() {
            var x = new SampleClass { Data = 1 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                new SampleClass{Data = 6},
                x,
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };

            Assert.AreEqual(x, xs.Argmin(e => new SampleClass2 { Data = e.Data }, Comparer<SampleClass2>.Default));
        }

        [TestMethod()]
        public void ArgminTest5() {
            var x = new SampleClass { Data = 6 };
            var xs = new List<SampleClass> {
                new SampleClass{Data = 4},
                new SampleClass{Data = 2},
                x,
                new SampleClass{Data = 1},
                new SampleClass{Data = 5},
                new SampleClass{Data = 1},
                new SampleClass{Data = 6},
                new SampleClass{Data = 2},
            };

            Assert.AreEqual(x, xs.Argmin(e => e.Data, (a, b) => b.CompareTo(a)));
        }

        [TestMethod()]
        public void ChunkTest()
        {
            var actuals = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Chunk(3);
            var expects = new List<int[]> { new[] { 1, 2, 3, }, new[] { 4, 5, 6, }, new[] { 7, 8, 9 } };

            foreach (var (actual, expect) in actuals.ZipInternal(expects)) {
                CollectionAssert.AreEqual(expect, actual);
            }

            actuals = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.Chunk(4);
            expects = new List<int[]> { new[] { 1, 2, 3, 4, }, new[] { 5, 6, 7, 8, }, new[] { 9, 10, } };
            foreach (var (actual, expect) in actuals.ZipInternal(expects)) {
                CollectionAssert.AreEqual(expect, actual);
            }
        }

        [TestMethod()]
        public void ZipTest1() {
            var actuals = new List<int> { 1, 2, 3 }.Zip(new List<int> { 4, 5, 6 }, new List<int> { 7, 8, 9 });
            var expects = new List<(int, int, int)> { (1, 4, 7), (2, 5, 8), (3, 6, 9) };

            foreach ((var expect, var actual) in expects.ZipInternal(actuals)) {
                (var e1, var e2, var e3) = expect;
                (var a1, var a2, var a3) = actual;
                Assert.AreEqual(e1, a1);
                Assert.AreEqual(e2, a2);
                Assert.AreEqual(e3, a3);
            }
        }

        [TestMethod()]
        public void SequenceTest1() {
            var actuals = new List<List<int>> {
                new List<int> {  1,  2,  3,  4,  5},
                new List<int> {  6,  7,  8,  9, 10},
                new List<int> { 11, 12, 13, 14, 15},
                new List<int> { 16, 17, 18, 19, 20},
            }.Sequence();
            var expects = new List<List<int>> {
                new List<int> {  1,  6, 11, 16},
                new List<int> {  2,  7, 12, 17},
                new List<int> {  3,  8, 13, 18},
                new List<int> {  4,  9, 14, 19},
                new List<int> {  5, 10, 15, 20},
            };

            foreach ((var expect, var actual) in expects.ZipInternal(actuals)) {
                CollectionAssert.AreEqual(expect, actual);
            }
        }

        [TestMethod()]
        public void SequenceEmptyTest() {
            var actuals = new List<int>[0].Sequence();
            Assert.IsFalse(actuals.Any());
        }
    }
}