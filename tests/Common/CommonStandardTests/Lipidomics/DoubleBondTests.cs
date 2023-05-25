using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class DoubleBondTests
    {
        [TestMethod()]
        public void DoubleBondTest() {
            var db = new DoubleBond(0);
            Assert.AreEqual(0, db.Count);
            Assert.AreEqual(0, db.DecidedCount);
            Assert.AreEqual(0, db.UnDecidedCount);
            Assert.AreEqual("0", db.ToString());
            
            db = new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.E(11));
            Assert.AreEqual(2, db.Count);
            Assert.AreEqual(2, db.DecidedCount);
            Assert.AreEqual(0, db.UnDecidedCount);
            Assert.AreEqual(2, db.Bonds.Count);
            Assert.AreEqual("2(9Z,11E)", db.ToString());
            
            db = new DoubleBond(3, DoubleBondInfo.Create(1));
            Assert.AreEqual(3, db.Count);
            Assert.AreEqual(1, db.DecidedCount);
            Assert.AreEqual(2, db.UnDecidedCount);
            Assert.AreEqual(1, db.Bonds.Count);
            Assert.AreEqual("3(1)", db.ToString());
        }

        [TestMethod()]
        public void DoubleBondCreateTest() {
            var db = DoubleBond.CreateFromPosition();
            Assert.AreEqual(0, db.Count);
            Assert.AreEqual(0, db.DecidedCount);
            Assert.AreEqual(0, db.UnDecidedCount);
            Assert.AreEqual("0", db.ToString());

            db = DoubleBond.CreateFromPosition(9, 11);
            Assert.AreEqual(2, db.Count);
            Assert.AreEqual(2, db.DecidedCount);
            Assert.AreEqual(0, db.UnDecidedCount);
            Assert.AreEqual(2, db.Bonds.Count);
            Assert.AreEqual("2(9,11)", db.ToString());

            db = DoubleBond.CreateFromPosition(1);
            Assert.AreEqual(1, db.Count);
            Assert.AreEqual(1, db.DecidedCount);
            Assert.AreEqual(0, db.UnDecidedCount);
            Assert.AreEqual(1, db.Bonds.Count);
            Assert.AreEqual("1(1)", db.ToString());
        }

        [TestMethod()]
        [DataTestMethod]
        [DynamicData(nameof(GetAcceptTestData), DynamicDataSourceType.Method)]
        public void DoubleBondAcceptTest(DoubleBond db) {
            var visitor = new FakeVisitor();
            var decomposer = new IdentityDecomposer<IDoubleBond, IDoubleBond>();
            var actual = db.Accept(visitor, decomposer);
            Assert.AreEqual(visitor.Expected, actual);
        }

        public static IEnumerable<object[]> GetAcceptTestData() {
            yield return new object[] { DoubleBond.CreateFromPosition(), };
            yield return new object[] { DoubleBond.CreateFromPosition(1), };
            yield return new object[] { DoubleBond.CreateFromPosition(9, 11), };
        }

        class FakeVisitor : IVisitor<DoubleBond, DoubleBond>
        {
            public DoubleBond Expected { get; private set; }
            DoubleBond IVisitor<DoubleBond, DoubleBond>.Visit(DoubleBond item) {
                return Expected = item;
            }
        }

        [TestMethod()]
        [DataTestMethod]
        [DynamicData(nameof(EqualityTestData), DynamicDataSourceType.Property)]
        public void EqualityTest(IDoubleBond db, IDoubleBond other, bool expected) {
            Assert.AreEqual(expected, db.Equals(other));
        }

        public static IEnumerable<object[]> EqualityTestData {
            get {
                IDoubleBond b1 = new DoubleBond(0), b2 = new DoubleBond(1),
                    b3 = DoubleBond.CreateFromPosition(1), b4 = DoubleBond.CreateFromPosition(2),
                    b5 = new DoubleBond(0);
                yield return new object[] { b1, b5, true, };
                yield return new object[] { b1, b1, true, };
                yield return new object[] { b1, b2, false, };
                yield return new object[] { b2, b3, false, };
                yield return new object[] { b3, b4, false, };
            }
        }
    }
}