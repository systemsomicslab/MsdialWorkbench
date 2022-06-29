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
            yield return new object[] { new Oxidized(2), 2, 0, 2, ";2O", };
            yield return new object[] { new Oxidized(2, 1, 3), 2, 2, 0, ";1OH,3OH", };

            yield return new object[] { Oxidized.CreateFromPosition(1, 3), 2, 2, 0, ";1OH,3OH", };

            yield return new object[] { new Oxidized(0).Convert(OxidizedShorthandNotation.All), 0, 0, 0, "", };
            yield return new object[] { new Oxidized(1).Convert(OxidizedShorthandNotation.All), 1, 0, 1, ";O", };
            yield return new object[] { new Oxidized(2).Convert(OxidizedShorthandNotation.All), 2, 0, 2, ";2O", };
            yield return new object[] { new Oxidized(2, 1, 3).Convert(OxidizedShorthandNotation.All), 2, 0, 2, ";2O", };
        }
    }
}