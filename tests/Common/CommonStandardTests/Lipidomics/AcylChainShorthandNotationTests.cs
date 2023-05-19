using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass]
    public class AcylChainShorthandNotationTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetAcylChainAcceptTestData), DynamicDataSourceType.Method)]
        public void VisitTest(AcylChain chain) {
            var visitor = AcylChainShorthandNotation.Default;
            var decomposer = new IdentityDecomposer<AcylChain, AcylChain>();
            var actual = chain.Accept<AcylChain>(visitor, decomposer);
            Assert.AreEqual(chain.CarbonCount, actual.CarbonCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBondCount);
            Assert.AreEqual(0, actual.DoubleBond.DecidedCount);
            Assert.AreEqual(chain.DoubleBondCount, actual.DoubleBond.UnDecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.OxidizedCount);
            Assert.AreEqual(0, actual.Oxidized.DecidedCount);
            Assert.AreEqual(chain.OxidizedCount, actual.Oxidized.UnDecidedCount);
        }

        public static IEnumerable<object[]> GetAcylChainAcceptTestData() {
            yield return new object[] { new AcylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition()), };
            yield return new object[] { new AcylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition(5)), };
            yield return new object[] { new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition(5, 8)), };
            yield return new object[] { new AcylChain(18, new DoubleBond(2, DoubleBondInfo.Z(9), DoubleBondInfo.Z(12)), Oxidized.CreateFromPosition()), };
        }
    }
}
