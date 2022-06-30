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
        [DynamicData(nameof(GetAlkylChainAcceptTestData), DynamicDataSourceType.Method)]
        public void VisitTest(AlkylChain chain) {
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

        public static IEnumerable<object[]> GetAlkylChainAcceptTestData() {
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(5)), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition(5, 8)), };
            yield return new object[] { new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.Z(12)), Oxidized.CreateFromPosition()), };
        }
    }
}
