using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass]
    public class ChainShorthandNotationTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(GetChainTypeTestData), DynamicDataSourceType.Method)]
        public void ChainTypeTest(IChain chain) {
            var actual = chain.Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
            Assert.AreEqual(chain.GetType(), actual.GetType());
        }

        public static IEnumerable<object[]> GetChainTypeTestData() {
            yield return new object[] { new AcylChain(18, new DoubleBond(0), new Oxidized(0)), };
            yield return new object[] { new AlkylChain(18, new DoubleBond(0), new Oxidized(0)), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0)), };
            yield return new object[] { new SphingoChain(18, new DoubleBond(0), Oxidized.CreateFromPosition(1)), };
        }
    }
}
