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
        [DynamicData(nameof(GetConvertTestData), DynamicDataSourceType.Method)]
        public void DoubleBondConvertTest(DoubleBond db, int count, int decidedCount, int undecidedCount, string repr) {
            Assert.AreEqual(count, db.Count);
            Assert.AreEqual(decidedCount, db.DecidedCount);
            Assert.AreEqual(undecidedCount, db.UnDecidedCount);
            Assert.AreEqual(repr, db.ToString());
        }

        public static IEnumerable<object[]> GetConvertTestData() {
            yield return new object[] { DoubleBond.CreateFromPosition().Convert(DoubleBondShorthandNotation.All), 0, 0, 0, "0", };
            yield return new object[] { DoubleBond.CreateFromPosition(1).Convert(DoubleBondShorthandNotation.All), 1, 0, 1, "1", };
            yield return new object[] { DoubleBond.CreateFromPosition(9, 11).Convert(DoubleBondShorthandNotation.All), 2, 0, 2, "2", };
        }
    }
}