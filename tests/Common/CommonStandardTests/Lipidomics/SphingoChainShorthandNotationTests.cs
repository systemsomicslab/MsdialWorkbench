using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass]
    public class SphingoChainShorthandNotationTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetSphingoChainAcceptTestData), DynamicDataSourceType.Method)]
        public void VisitTest(SphingoChain chain, int decidedOxidize, int undecidedOxidize) {
            var visitor = SphingoChainShorthandNotation.Default;
            var decomposer = new IdentityDecomposer<SphingoChain, SphingoChain>();
            var actual = chain.Accept<SphingoChain>(visitor, decomposer);
            Assert.AreEqual(chain.CarbonCount, actual.CarbonCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.DoubleBond.DecidedCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBond.UnDecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.OxidizedCount);
            Assert.AreEqual(decidedOxidize, actual.Oxidized.DecidedCount);
            Assert.AreEqual(undecidedOxidize, actual.Oxidized.UnDecidedCount);
        }

        public static IEnumerable<object[]> GetSphingoChainAcceptTestData() {
            yield return new object[] { new SphingoChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(1)), 1, 0, };
            yield return new object[] { new SphingoChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition(1)), 1, 0, };
            yield return new object[] { new SphingoChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(1, 3)), 1, 1, };
            yield return new object[] { new SphingoChain(18, new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.Z(12)), Oxidized.CreateFromPosition(1, 3)), 1, 1, };
        }
    }
}
