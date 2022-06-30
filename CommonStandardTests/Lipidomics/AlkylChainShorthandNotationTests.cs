using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass]
    public class AlkylChainShorthandNotationTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetVisitAllTestData), DynamicDataSourceType.Method)]
        public void VisitAllTest(AlkylChain chain) {
            var visitor = AlkylChainShorthandNotation.All;
            var decomposer = new IdentityDecomposer<AlkylChain, AlkylChain>();
            var actual = chain.Accept(visitor, decomposer);
            Assert.AreEqual(chain.CarbonCount, actual.CarbonCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.DoubleBond.DecidedCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBond.UnDecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.OxidizedCount);
            Assert.AreEqual(0, actual.Oxidized.DecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.Oxidized.UnDecidedCount);
        }

        public static IEnumerable<object[]> GetVisitAllTestData() {
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(5)), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition(5, 8)), };
            yield return new object[] { new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.Z(12)), Oxidized.CreateFromPosition()), };
        }

        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetVisitAllForPlasmalogenTestData), DynamicDataSourceType.Method)]
        public void VisitAllForPlasmalogenTest(AlkylChain chain, int decidedDoubleBondCount, int undecidedDoubleBondCount) {
            var visitor = AlkylChainShorthandNotation.AllForPlasmalogen;
            var decomposer = new IdentityDecomposer<AlkylChain, AlkylChain>();
            var actual = chain.Accept(visitor, decomposer);
            Assert.AreEqual(chain.CarbonCount, actual.CarbonCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBondCount);
            Assert.AreEqual(decidedDoubleBondCount, actual.DoubleBond.DecidedCount);
            Assert.AreEqual(undecidedDoubleBondCount, actual.DoubleBond.UnDecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.OxidizedCount);
            Assert.AreEqual(0, actual.Oxidized.DecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.Oxidized.UnDecidedCount);
        }

        public static IEnumerable<object[]> GetVisitAllForPlasmalogenTestData() {
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition()), 0, 0, };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition()), 0, 2, };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1), Oxidized.CreateFromPosition()), 1, 0, };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), Oxidized.CreateFromPosition()), 1, 1, };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(5)), 0, 0, };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition(5, 8)), 0, 2, };
            yield return new object[] { new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.Z(12)), Oxidized.CreateFromPosition()), 0, 2, };
        }
    }
}
