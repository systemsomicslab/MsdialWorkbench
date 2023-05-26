using CompMs.Common.DataStructure;
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
            var actual = chain.Accept(ChainShorthandNotation.Default, IdentityDecomposer<IChain, IChain>.Instance);
            Assert.AreEqual(chain.GetType(), actual.GetType());
        }

        public static IEnumerable<object[]> GetChainTypeTestData() {
            yield return new object[] { new AcylChain(18, new DoubleBond(0), new Oxidized(0)), };
            yield return new object[] { new AlkylChain(18, new DoubleBond(0), new Oxidized(0)), };
            yield return new object[] { new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0)), };
            yield return new object[] { new SphingoChain(18, new DoubleBond(0), Oxidized.CreateFromPosition(1)), };
        }

        [DataTestMethod]
        [DynamicData(nameof(ShorthandTestData), DynamicDataSourceType.Property)]
        public void ShorthandTest(IChain chain, IChain expected) {
            var actual = chain.Accept(ChainShorthandNotation.Default, IdentityDecomposer<IChain, IChain>.Instance);
            Assert.That.AreEqual(expected, actual);
        }

        public static IEnumerable<object[]> ShorthandTestData {
            get {
                AcylChain acyl181 = new AcylChain(18, new DoubleBond(1), new Oxidized(0)),
                    acyl181_9 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
                AlkylChain alkyl181 = new AlkylChain(18, new DoubleBond(1), new Oxidized(0)),
                    alkyl181_9 = new AlkylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0)),
                    alkylp181 = new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Create(1)), new Oxidized(0)),
                    alkylp181_9 = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 9), new Oxidized(0));
                SphingoChain sphingosine181 = new SphingoChain(18, new DoubleBond(0), new Oxidized(2, 1)),
                    sphingosine181_3 = new SphingoChain(18, new DoubleBond(0), Oxidized.CreateFromPosition(1, 3));

                yield return new object[] { acyl181, acyl181, };
                yield return new object[] { acyl181_9, acyl181, };
                yield return new object[] { alkyl181, alkyl181, };
                yield return new object[] { alkyl181_9, alkyl181, };
                yield return new object[] { alkylp181, alkylp181, };
                yield return new object[] { alkylp181_9, alkylp181, };
                yield return new object[] { sphingosine181, sphingosine181, };
                yield return new object[] { sphingosine181_3, sphingosine181, };
            }
        }
    }
}
