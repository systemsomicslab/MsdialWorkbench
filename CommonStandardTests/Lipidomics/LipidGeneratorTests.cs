using CompMs.Common.Enum;
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
            ILipid lipid = new SubLevelLipid(LbmClass.PC, 2, 785.5935, new TotalAcylChain(36, 2, 0, 2));
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is SomeAcylChainLipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new[]
            {
                ( 10, 0, 0, 11, 0, 0),
                ( 20, 1, 0, 21, 1, 0),
                ( 30, 0, 1, 31, 0, 1),
            };
            var actuals = lipids.OfType<SomeAcylChainLipid>()
                .Select(l => (l.Chains[0].CarbonCount, l.Chains[0].DoubleBondCount, l.Chains[0].OxidizedCount, l.Chains[1].CarbonCount, l.Chains[1].DoubleBondCount, l.Chains[1].OxidizedCount))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod()]
        public void SomeAcylChainGenerateTest() {
            var acyl1 = new AcylChain(18, 0, 0);
            var acyl2 = new AcylChain(18, 2, 0);
            ILipid lipid = new SomeAcylChainLipid(LbmClass.PC, 785.5935, acyl1, acyl2);
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is PositionSpecificAcylChainLipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new (IChain, IChain)[]
            {
                (acyl1, acyl2),
                (acyl2, acyl1),
            };
            var actuals = lipids.OfType<PositionSpecificAcylChainLipid>()
                .Select(l => (l.Chains[0], l.Chains[1]))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);

            var acyl3 = new AcylChain(18, 1, 0);
            lipid = new SomeAcylChainLipid(LbmClass.PC, 785.5935, acyl3, acyl3);

            lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is PositionSpecificAcylChainLipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            expects = new (IChain, IChain)[]
            {
                (acyl3, acyl3),
            };
            actuals = lipids.OfType<PositionSpecificAcylChainLipid>()
                .Select(l => (l.Chains[0], l.Chains[1]))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }

        [TestMethod()]
        public void PositionSpecificAcylChainLipid() {
            var acyl1 = new AcylChain(18, 1, 0);
            var acyl2 = new AcylChain(18, 1, 0);
            ILipid lipid = new PositionSpecificAcylChainLipid(LbmClass.PC, 785.5935, acyl1, acyl2);
            var generator = new LipidGenerator(new MockAcylChainGenerator());

            var lipids = lipid.Generate(generator).ToArray();
            Assert.IsTrue(lipids.All(l => l is PositionSpecificAcylChainLipid));
            Assert.IsTrue(lipids.All(l => l.Mass == lipid.Mass));
            Assert.IsTrue(lipids.All(l => l.LipidClass == lipid.LipidClass));

            var expects = new[]
            {
                ( 6,  6), ( 6,  9), ( 6, 12),
                ( 9,  6), ( 9,  9), ( 9, 12),
                (12,  6), (12,  9), (12, 12),
            };
            var actuals = lipids.OfType<PositionSpecificAcylChainLipid>()
                .Select(l => ((l.Chains[0] as SpecificAcylChain).DoubleBondPosition[0], (l.Chains[1] as SpecificAcylChain).DoubleBondPosition[0]))
                .ToArray();
            CollectionAssert.AreEquivalent(expects, actuals);
        }
    }

    class MockAcylChainGenerator : IChainGenerator
    {
        private int c = 0;
        public IEnumerable<SpecificAcylChain> Generate(AcylChain chain) {
            System.Console.WriteLine(++c);
            yield return new SpecificAcylChain(18, new List<int> { 6, }, 0);
            yield return new SpecificAcylChain(18, new List<int> { 9, }, 0);
            yield return new SpecificAcylChain(18, new List<int> { 12, }, 0);
        }

        public IEnumerable<SpecificAlkylChain> Generate(AlkylChain chain) {
            yield return new SpecificAlkylChain(18, new List<int> { 6, }, 0);
            yield return new SpecificAlkylChain(18, new List<int> { 9, }, 0);
            yield return new SpecificAlkylChain(18, new List<int> { 12, }, 0);
        }

        public IEnumerable<SpecificAlkylChain> Generate(PlasmalogenAlkylChain chain) {
            yield return new SpecificAlkylChain(18, new List<int> { 1, 6, }, 0);
            yield return new SpecificAlkylChain(18, new List<int> { 1, 9, }, 0);
            yield return new SpecificAlkylChain(18, new List<int> { 1, 12, }, 0);
        }

        IEnumerable<IChain[]> IChainGenerator.Separate(TotalAcylChain chain, int numChain) {
            yield return Enumerable.Range(10, numChain).Select(v => new AcylChain(v, 0, 0)).ToArray();
            yield return Enumerable.Range(20, numChain).Select(v => new AcylChain(v, 1, 0)).ToArray();
            yield return Enumerable.Range(30, numChain).Select(v => new AcylChain(v, 0, 1)).ToArray();
        }
    }
}