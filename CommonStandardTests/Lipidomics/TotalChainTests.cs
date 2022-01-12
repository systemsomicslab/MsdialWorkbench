using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class TotalChainTests
    {
        [TestMethod()]
        public void TotalChainsTest() {
            var chains = new TotalChain(36, 2, 0, 2, 0, 0);
            Assert.AreEqual(36, chains.CarbonCount);
            Assert.AreEqual(2, chains.DoubleBondCount);
            Assert.AreEqual(0, chains.OxidizedCount);
            Assert.AreEqual(2, chains.ChainCount);
            Assert.AreEqual(530.5062805078201, chains.Mass, 1e-5);
            var generator = new MockGenerator();
            var candidates = ((ITotalChain)chains).GetCandidateSets(generator);
            CollectionAssert.AreEqual(new ITotalChain[0], candidates.ToArray());

            chains = new TotalChain(36, 2, 0, 1, 1, 0);
            Assert.AreEqual(36, chains.CarbonCount);
            Assert.AreEqual(2, chains.DoubleBondCount);
            Assert.AreEqual(0, chains.OxidizedCount);
            Assert.AreEqual(2, chains.ChainCount);
            Assert.AreEqual(516.5270163763599, chains.Mass, 1e-5);

            chains = new TotalChain(52, 3, 0, 3, 0, 0);
            Assert.AreEqual(52, chains.CarbonCount);
            Assert.AreEqual(3, chains.DoubleBondCount);
            Assert.AreEqual(0, chains.OxidizedCount);
            Assert.AreEqual(3, chains.ChainCount);
            Assert.AreEqual(767.7281206334501, chains.Mass, 1e-5);

            chains = new TotalChain(34, 1, 2, 1, 0, 1);
            Assert.AreEqual(34, chains.CarbonCount);
            Assert.AreEqual(1, chains.DoubleBondCount);
            Assert.AreEqual(2, chains.OxidizedCount);
            Assert.AreEqual(2, chains.ChainCount);
            Assert.AreEqual(537.5120937402901, chains.Mass, 1e-5);
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
                throw new System.NotImplementedException();
            }

            public IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
                throw new System.NotImplementedException();
            }

            public IEnumerable<ITotalChain> Separate(TotalChain chain) {
                Called = true;
                return Enumerable.Empty<ITotalChain>();
            }
        }
    }
}