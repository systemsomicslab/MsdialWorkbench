using CompMs.Common.Enum;
using CompMs.Common.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LipidGeneratorTests
    {
        [TestMethod()]
        public void SubLevelGenerateTest() {
            ILipid lipid = new Lipid(LbmClass.PC, 785.5935, new TotalChain(36, 2, 0, 2, 0, 0));
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is Lipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new[]
            {
                ( 10, 0, 0, 11, 0, 0),
                ( 20, 1, 0, 21, 1, 0),
                ( 30, 0, 1, 31, 0, 1),
            };
            var actuals = lipids.OfType<Lipid>()
                .Select(l => l.Chains.GetDeterminedChains())
                .Select(chains => (chains[0].CarbonCount, chains[0].DoubleBondCount, chains[0].OxidizedCount, chains[1].CarbonCount, chains[1].DoubleBondCount, chains[1].OxidizedCount))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod()]
        public void SomeAcylChainGenerateTest() {
            var acyl1 = new AcylChain(18, new DoubleBond(0), new Oxidized(0));
            var acyl2 = new AcylChain(18, new DoubleBond(2), new Oxidized(0));
            ILipid lipid = new Lipid(LbmClass.PC, 785.5935, new MolecularSpeciesLevelChains(acyl1, acyl2));
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is Lipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new (IChain, IChain)[]
            {
                (acyl1, acyl2),
                (acyl2, acyl1),
            };
            var actuals = lipids.OfType<Lipid>()
                .Select(l => l.Chains.GetDeterminedChains())
                .Select(chains => (chains[0], chains[1]))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);

            var acyl3 = new AcylChain(18, new DoubleBond(1), new Oxidized(0));
            lipid = new Lipid(LbmClass.PC, 785.5935, new MolecularSpeciesLevelChains(acyl3, acyl3));

            lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is Lipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            expects = new (IChain, IChain)[]
            {
                (acyl3, acyl3),
            };
            actuals = lipids.OfType<Lipid>()
                .Select(l => l.Chains.GetDeterminedChains())
                .Select(chains => (chains[0], chains[1]))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod()]
        public void PositionSpecificAcylChainLipid() {
            var acyl1 = new AcylChain(18, new DoubleBond(1), new Oxidized(0));
            var acyl2 = new AcylChain(18, new DoubleBond(1), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = generator.Generate(lipid).ToArray();
            Assert.IsTrue(lipids.All(l => l is Lipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new[]
            {
                ( 6,  6), ( 6,  9), ( 6, 12),
                ( 9,  6), ( 9,  9), ( 9, 12),
                (12,  6), (12,  9), (12, 12),
            };
            var actuals = lipids.OfType<Lipid>()
                .Select(l => l.Chains.GetDeterminedChains())
                .Select(chains => ((chains[0] as AcylChain).DoubleBond.Bonds[0].Position, (chains[1] as AcylChain).DoubleBond.Bonds[0].Position))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }
    }

    class MockAcylChainGenerator : ITotalChainVariationGenerator, IChainGenerator
    {
        private int c = 0;
        public IEnumerable<IChain> Generate(AcylChain chain) {
            System.Console.WriteLine(++c);
            yield return new AcylChain(18, DoubleBond.CreateFromPosition(6), new Oxidized(0));
            yield return new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            yield return new AcylChain(18, DoubleBond.CreateFromPosition(12), new Oxidized(0));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            yield return new AlkylChain(18, DoubleBond.CreateFromPosition(1, 6), new Oxidized(0));
            yield return new AlkylChain(18, DoubleBond.CreateFromPosition(1, 9), new Oxidized(0));
            yield return new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
        }

        public IEnumerable<ITotalChain> Separate(TotalChain chain) {
            yield return new MolecularSpeciesLevelChains(Enumerable.Range(10, chain.ChainCount).Select(v => new AcylChain(v, new DoubleBond(0), new Oxidized(0))).ToArray());
            yield return new MolecularSpeciesLevelChains(Enumerable.Range(20, chain.ChainCount).Select(v => new AcylChain(v, new DoubleBond(1), new Oxidized(0))).ToArray());
            yield return new MolecularSpeciesLevelChains(Enumerable.Range(30, chain.ChainCount).Select(v => new AcylChain(v, new DoubleBond(0), new Oxidized(1))).ToArray());
        }

        public IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains) {
            return SearchCollection.Permutations(chains.GetDeterminedChains()).Select(chains => new PositionLevelChains(chains));
        }

        public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
            return SearchCollection.CartesianProduct(chains.GetDeterminedChains().Select(c => c.GetCandidates(this).ToArray()).ToArray()).Select(chains => new PositionLevelChains(chains));
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