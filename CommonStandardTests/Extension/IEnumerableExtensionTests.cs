using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace CompMs.Common.Extension.Tests
{
    [TestClass()]
    public class IEnumerableExtensionTests
    {
        class SampleClass
        {
            public int Data { get; set; }
        }

        class SampleClass2 : IComparable<SampleClass2>
        {
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
    }
}