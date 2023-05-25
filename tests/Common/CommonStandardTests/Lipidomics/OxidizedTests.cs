using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class OxidizedTests
    {
        [TestMethod()]
        [DataTestMethod]
        [DynamicData(nameof(OxidizedTestData), DynamicDataSourceType.Method)]
        public void OxidizedTest(Oxidized ox, int count, int decidedCount, int undecidedCount, string repr) {
            Assert.AreEqual(count, ox.Count);
            Assert.AreEqual(decidedCount, ox.DecidedCount);
            Assert.AreEqual(undecidedCount, ox.UnDecidedCount);
            Assert.AreEqual(repr, ox.ToString());
        }

        public static IEnumerable<object[]> OxidizedTestData() {
            yield return new object[] { new Oxidized(0), 0, 0, 0, "", };
            yield return new object[] { new Oxidized(1), 1, 0, 1, ";O", };
            yield return new object[] { new Oxidized(2), 2, 0, 2, ";O2", };
            yield return new object[] { new Oxidized(2, 1, 3), 2, 2, 0, "(1OH,3OH)", };

            yield return new object[] { Oxidized.CreateFromPosition(1, 3), 2, 2, 0, "(1OH,3OH)", };
        }

        [TestMethod()]
        [DataTestMethod]
        [DynamicData(nameof(EquatableTestData), DynamicDataSourceType.Property)]
        public void EquatableTest(IOxidized ox, IOxidized other, bool expected) {
            Assert.AreEqual(expected, ox.Equals(other));
        }

        public static IEnumerable<object[]> EquatableTestData {
            get {
                IOxidized ox1 = new Oxidized(0), ox2 = new Oxidized(0), ox3 = new Oxidized(1),
                    ox4 = Oxidized.CreateFromPosition(1), ox5 = Oxidized.CreateFromPosition(1), ox6 = Oxidized.CreateFromPosition(2);
                yield return new object[] { ox1, ox2, true, };
                yield return new object[] { ox1, ox1, true, };
                yield return new object[] { ox1, ox3, false, };
                yield return new object[] { ox3, ox4, false, };
                yield return new object[] { ox4, ox5, true, };
                yield return new object[] { ox4, ox6, false, };
            }
        }
    }
}