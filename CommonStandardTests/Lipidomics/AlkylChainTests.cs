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

            public IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<ITotalChain> Separate(TotalChain chain) {
                throw new System.NotImplementedException();
            }
        }
    }
}