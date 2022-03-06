using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AcylChainTests
    {
        [TestMethod()]
        public void AcylChainTest() {
            var chain = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(0, chain.OxidizedCount);
            Assert.AreEqual(263.23749018977, chain.Mass, 0.00001); // C18H31O1
            Assert.AreEqual("18:2(9,12)", chain.ToString());
            var generator = new MockGenerator();
            var candidates = chain.GetCandidates(generator);
            CollectionAssert.AreEqual(new IChain[] { }, candidates.ToArray());
            Assert.IsTrue(generator.Called);

            chain = new AcylChain(18, new DoubleBond(2), new Oxidized(1));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(1, chain.OxidizedCount);
            Assert.AreEqual(279.23240438537, chain.Mass, 0.00001); // C18H31O2
            Assert.AreEqual("18:2;O", chain.ToString());

            chain = new AcylChain(18, new DoubleBond(2), new Oxidized(2));
            Assert.AreEqual(18, chain.CarbonCount);
            Assert.AreEqual(2, chain.DoubleBondCount);
            Assert.AreEqual(2, chain.OxidizedCount);
            Assert.AreEqual(295.22731858097, chain.Mass, 0.00001); // C18H31O3
            Assert.AreEqual("18:2;2O", chain.ToString());
        }

        class MockGenerator : IChainGenerator
        {
            public bool Called { get; private set; } = false;

            public IEnumerable<IChain> Generate(AcylChain chain) {
                Called = true;
                return Enumerable.Empty<IChain>();
            }

            public IEnumerable<IChain> Generate(AlkylChain chain) {
                throw new System.NotImplementedException();
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