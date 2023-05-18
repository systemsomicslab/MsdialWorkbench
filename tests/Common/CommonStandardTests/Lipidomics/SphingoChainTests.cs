using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class SphingoChainTests
    {
        [TestMethod()]
        public void SphingoChainTest() {
            var chain = new SphingoChain(18, new DoubleBond(0), Oxidized.CreateFromPosition(1, 3));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(0, chain.DoubleBondCount);
            Assert.AreEqual(2, chain.OxidizedCount);
            Assert.AreEqual(300.29025361466, chain.Mass, 1e-5); // C18H38O2N1
            Assert.AreEqual("18:0(1OH,3OH)", chain.ToString());
            var generator = new MockGenerator();
            var candidates = chain.GetCandidates(generator);
            CollectionAssert.AreEqual(new IChain[] { }, candidates.ToArray());
            Assert.IsTrue(generator.Called);

            chain = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3, 4));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(1, chain.DoubleBondCount);
            Assert.AreEqual(3, chain.OxidizedCount);
            Assert.AreEqual(314.26951774612, chain.Mass, 1e-5); // C18H36O3N1
            Assert.AreEqual("18:1(4)(1OH,3OH,4OH)", chain.ToString());

            chain = new SphingoChain(18, new DoubleBond(0), new Oxidized(3, 1, 3));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(0, chain.DoubleBondCount);
            Assert.AreEqual(3, chain.OxidizedCount);
            Assert.AreEqual(316.28516781026, chain.Mass, 1e-5); // C18H38O3N1
            Assert.AreEqual("18:0;O3", chain.ToString());
        }

        [DataTestMethod]
        [DynamicData(nameof(EquatableTestData), DynamicDataSourceType.Property)]
        public void EquatableTest(IChain chain, IChain other, bool expected) {
            Assert.AreEqual(expected, chain.Equals(other));
        }

        public static IEnumerable<object[]> EquatableTestData {
            get {
                IChain chain1 = new SphingoChain(18, new DoubleBond(2), new Oxidized(2, 1)),
                    chain2 = new SphingoChain(18, new DoubleBond(2), new Oxidized(2, 1)),
                    chain3 = new SphingoChain(16, new DoubleBond(2), new Oxidized(2, 1)),
                    chain4 = new SphingoChain(18, new DoubleBond(1), new Oxidized(2, 1)),
                    chain5 = new SphingoChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(2, 1)),
                    chain6 = new SphingoChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(2, 1)),
                    chain7 = new SphingoChain(18, new DoubleBond(2), Oxidized.CreateFromPosition(1, 3)),
                    chain8 = new SphingoChain(18, new DoubleBond(2), Oxidized.CreateFromPosition(1, 5));
                yield return new object[] { chain1, chain1, true, };
                yield return new object[] { chain1, chain2, true, };
                yield return new object[] { chain1, chain3, false, };
                yield return new object[] { chain1, chain4, false, };
                yield return new object[] { chain1, chain5, false, };
                yield return new object[] { chain5, chain6, false, };
                yield return new object[] { chain1, chain7, false, };
                yield return new object[] { chain7, chain8, false, };
            }
        }

        class MockGenerator : IChainGenerator
        {
            public bool Called { get; private set; } = false;

            public IEnumerable<IChain> Generate(AcylChain chain) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<IChain> Generate(AlkylChain chain) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<IChain> Generate(SphingoChain chain) {
                Called = true;
                return Enumerable.Empty<IChain>();
            }

            public bool CarbonIsValid(int carbon) {
                return true;
            }

            public bool DoubleBondIsValid(int carbon, int doubleBond) {
                return true;
            }
        }
    }
}