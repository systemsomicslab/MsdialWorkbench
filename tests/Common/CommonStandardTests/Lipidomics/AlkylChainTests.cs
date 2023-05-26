using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AlkylChainTests
    {
        [TestMethod()]
        public void AlkylChainTest() {
            var chain = new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(0, chain.OxidizedCount);
            Assert.AreEqual(249.25822605831002, chain.Mass, 1e-5); // C18H33
            Assert.AreEqual("O-18:2(9,12)", chain.ToString());
            var generator = new MockGenerator();
            var candidates = chain.GetCandidates(generator);
            CollectionAssert.AreEqual(new IChain[] { }, candidates.ToArray());
            Assert.IsTrue(generator.Called);

            chain = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(0, chain.OxidizedCount);
            Assert.AreEqual(249.25822605831002, chain.Mass, 1e-5); // C18H33
            Assert.AreEqual("P-18:1(12)", chain.ToString());

            chain = new AlkylChain(18, new DoubleBond(2), new Oxidized(1));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(1, chain.OxidizedCount);
            Assert.AreEqual(265.25314025391003, chain.Mass, 1e-5); // C18H33O1
            Assert.AreEqual("O-18:2;O", chain.ToString());

            chain = new AlkylChain(18, new DoubleBond(2), new Oxidized(2));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(2, chain.OxidizedCount);
            Assert.AreEqual(281.24805444951, chain.Mass, 1e-5); // C18H33O2
            Assert.AreEqual("O-18:2;O2", chain.ToString());
        }

        [DataTestMethod]
        [DynamicData(nameof(EquatableTestData), DynamicDataSourceType.Property)]
        public void EquatableTest(IChain chain, IChain other, bool expected) {
            Assert.AreEqual(expected, chain.Equals(other));
        }

        public static IEnumerable<object[]> EquatableTestData {
            get {
                IChain chain1 = new AlkylChain(18, new DoubleBond(2), new Oxidized(1)),
                    chain2 = new AlkylChain(18, new DoubleBond(2), new Oxidized(1)),
                    chain3 = new AlkylChain(16, new DoubleBond(2), new Oxidized(1)),
                    chain4 = new AlkylChain(18, new DoubleBond(1), new Oxidized(1)),
                    chain5 = new AlkylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(1)),
                    chain6 = new AlkylChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(1)),
                    chain7 = new AlkylChain(18, new DoubleBond(2), Oxidized.CreateFromPosition(1)),
                    chain8 = new AlkylChain(18, new DoubleBond(2), Oxidized.CreateFromPosition(2));
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
                Called = true;
                return Enumerable.Empty<IChain>();
            }

            public IEnumerable<IChain> Generate(SphingoChain chain) {
                throw new System.NotImplementedException();
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