using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class ChainGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var acylChain = new AcylChain(18, new DoubleBond(2), new Oxidized(0));

            var actual = acylChain.GetCandidates(generator).ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 2));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position)).ToArray();
            var expects = new[]
            {
                ( 3,  6), ( 3,  7), ( 3,  8), ( 3,  9), ( 3, 10), ( 3, 11), ( 3, 12), ( 3, 13), ( 3, 14), ( 3, 15), ( 3, 16), ( 3, 17),
                          ( 4,  7), ( 4,  8), ( 4,  9), ( 4, 10), ( 4, 11), ( 4, 12), ( 4, 13), ( 4, 14), ( 4, 15), ( 4, 16), ( 4, 17),
                                    ( 5,  8), ( 5,  9), ( 5, 10), ( 5, 11), ( 5, 12), ( 5, 13), ( 5, 14), ( 5, 15), ( 5, 16), ( 5, 17),
                                              ( 6,  9), ( 6, 10), ( 6, 11), ( 6, 12), ( 6, 13), ( 6, 14), ( 6, 15), ( 6, 16), ( 6, 17),
                                                        ( 7, 10), ( 7, 11), ( 7, 12), ( 7, 13), ( 7, 14), ( 7, 15), ( 7, 16), ( 7, 17),
                                                                  ( 8, 11), ( 8, 12), ( 8, 13), ( 8, 14), ( 8, 15), ( 8, 16), ( 8, 17),
                                                                            ( 9, 12), ( 9, 13), ( 9, 14), ( 9, 15), ( 9, 16), ( 9, 17),
                                                                                      (10, 13), (10, 14), (10, 15), (10, 16), (10, 17),
                                                                                                (11, 14), (11, 15), (11, 16), (11, 17),
                                                                                                          (12, 15), (12, 16), (12, 17),
                                                                                                                    (13, 16), (13, 17),
                                                                                                                              (14, 17),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GenerateOxidizeVariationTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var acylChain = new AcylChain(14, new DoubleBond(0), new Oxidized(2));

            var actual = acylChain.GetCandidates(generator).ToArray();
            Assert.IsTrue(actual.All(chain => chain.Oxidized.Count == 2));
            var tuples = actual.Select(chain => (chain.Oxidized.Oxidises[0], chain.Oxidized.Oxidises[1])).ToArray();
            var expects = new[]
            {
                ( 2,  3), ( 2,  4), ( 2,  5), ( 2,  6), ( 2,  7), ( 2,  8), ( 2,  9), ( 2, 10), ( 2, 11), ( 2, 12), ( 2, 13), ( 2, 14),
                          ( 3,  4), ( 3,  5), ( 3,  6), ( 3,  7), ( 3,  8), ( 3,  9), ( 3, 10), ( 3, 11), ( 3, 12), ( 3, 13), ( 3, 14),
                                    ( 4,  5), ( 4,  6), ( 4,  7), ( 4,  8), ( 4,  9), ( 4, 10), ( 4, 11), ( 4, 12), ( 4, 13), ( 4, 14),
                                              ( 5,  6), ( 5,  7), ( 5,  8), ( 5,  9), ( 5, 10), ( 5, 11), ( 5, 12), ( 5, 13), ( 5, 14),
                                                        ( 6,  7), ( 6,  8), ( 6,  9), ( 6, 10), ( 6, 11), ( 6, 12), ( 6, 13), ( 6, 14),
                                                                  ( 7,  8), ( 7,  9), ( 7, 10), ( 7, 11), ( 7, 12), ( 7, 13), ( 7, 14),
                                                                            ( 8,  9), ( 8, 10), ( 8, 11), ( 8, 12), ( 8, 13), ( 8, 14),
                                                                                      ( 9, 10), ( 9, 11), ( 9, 12), ( 9, 13), ( 9, 14),
                                                                                                (10, 11), (10, 12), (10, 13), (10, 14),
                                                                                                          (11, 12), (11, 13), (11, 14),
                                                                                                                    (12, 13), (12, 14),
                                                                                                                              (13, 14),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GenerateAlkylTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var alkylChain = new AlkylChain(18, new DoubleBond(2), new Oxidized(0));

            var actual = alkylChain.GetCandidates(generator).ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 2));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position)).ToArray();
            var expects = new[]
            {
                ( 3,  6), ( 3,  7), ( 3,  8), ( 3,  9), ( 3, 10), ( 3, 11), ( 3, 12), ( 3, 13), ( 3, 14), ( 3, 15), ( 3, 16), ( 3, 17),
                          ( 4,  7), ( 4,  8), ( 4,  9), ( 4, 10), ( 4, 11), ( 4, 12), ( 4, 13), ( 4, 14), ( 4, 15), ( 4, 16), ( 4, 17),
                                    ( 5,  8), ( 5,  9), ( 5, 10), ( 5, 11), ( 5, 12), ( 5, 13), ( 5, 14), ( 5, 15), ( 5, 16), ( 5, 17),
                                              ( 6,  9), ( 6, 10), ( 6, 11), ( 6, 12), ( 6, 13), ( 6, 14), ( 6, 15), ( 6, 16), ( 6, 17),
                                                        ( 7, 10), ( 7, 11), ( 7, 12), ( 7, 13), ( 7, 14), ( 7, 15), ( 7, 16), ( 7, 17),
                                                                  ( 8, 11), ( 8, 12), ( 8, 13), ( 8, 14), ( 8, 15), ( 8, 16), ( 8, 17),
                                                                            ( 9, 12), ( 9, 13), ( 9, 14), ( 9, 15), ( 9, 16), ( 9, 17),
                                                                                      (10, 13), (10, 14), (10, 15), (10, 16), (10, 17),
                                                                                                (11, 14), (11, 15), (11, 16), (11, 17),
                                                                                                          (12, 15), (12, 16), (12, 17),
                                                                                                                    (13, 16), (13, 17),
                                                                                                                              (14, 17),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GeneratePlasmalogenAlkylTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var alkylChain = new AlkylChain(18, new DoubleBond(3, DoubleBondInfo.Create(1)), new Oxidized(0));

            var actual = alkylChain.GetCandidates(generator).ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position, chain.DoubleBond.Bonds[2].Position)).ToArray();
            var expects = new[]
            {
                (1,  4,  7), (1,  4,  8), (1,  4,  9), (1,  4, 10), (1,  4, 11), (1,  4, 12), (1,  4, 13), (1,  4, 14), (1,  4, 15), (1,  4, 16), (1,  4, 17),
                             (1,  5,  8), (1,  5,  9), (1,  5, 10), (1,  5, 11), (1,  5, 12), (1,  5, 13), (1,  5, 14), (1,  5, 15), (1,  5, 16), (1,  5, 17),
                                          (1,  6,  9), (1,  6, 10), (1,  6, 11), (1,  6, 12), (1,  6, 13), (1,  6, 14), (1,  6, 15), (1,  6, 16), (1,  6, 17),
                                                       (1,  7, 10), (1,  7, 11), (1,  7, 12), (1,  7, 13), (1,  7, 14), (1,  7, 15), (1,  7, 16), (1,  7, 17),
                                                                    (1,  8, 11), (1,  8, 12), (1,  8, 13), (1,  8, 14), (1,  8, 15), (1,  8, 16), (1,  8, 17),
                                                                                 (1,  9, 12), (1,  9, 13), (1,  9, 14), (1,  9, 15), (1,  9, 16), (1,  9, 17),
                                                                                              (1, 10, 13), (1, 10, 14), (1, 10, 15), (1, 10, 16), (1, 10, 17),
                                                                                                           (1, 11, 14), (1, 11, 15), (1, 11, 16), (1, 11, 17),
                                                                                                                        (1, 12, 15), (1, 12, 16), (1, 12, 17),
                                                                                                                                     (1, 13, 16), (1, 13, 17),
                                                                                                                                                  (1, 14, 17),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GenerateSphingosineTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var alkylChain = new SphingoChain(18, new DoubleBond(1, DoubleBondInfo.Create(1)), new Oxidized(3, 1, 3));

            var actual = alkylChain.GetCandidates(generator).ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBondCount == 1));
            Assert.IsTrue(actual.All(chain => chain.OxidizedCount == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.Oxidized.Oxidises[0], chain.Oxidized.Oxidises[1], chain.Oxidized.Oxidises[2])).ToArray();
            var expects = new[]
            {
                (1, 1, 3,  4),
                (1, 1, 3,  5),
                (1, 1, 3,  6),
                (1, 1, 3,  7),
                (1, 1, 3,  8),
                (1, 1, 3,  9),
                (1, 1, 3, 10),
                (1, 1, 3, 11),
                (1, 1, 3, 12),
                (1, 1, 3, 13),
                (1, 1, 3, 14),
                (1, 1, 3, 15),
                (1, 1, 3, 16),
                (1, 1, 3, 17),
                (1, 1, 3, 18),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GeneratePositionSpecifiedAcylChainTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var alkylChain = new AcylChain(18, DoubleBond.CreateFromPosition(1, 9, 12), new Oxidized(2, 4, 5));

            var actual = alkylChain.GetCandidates(generator).OfType<AcylChain>().ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position, chain.DoubleBond.Bonds[2].Position)).ToArray();
            Assert.AreEqual(1, tuples.Length);
        }

        [TestMethod()]
        public void GeneratePositionSpecifiedAlkylChainTest() {
            var generator = new ChainGenerator(begin: 3, end: 1, skip: 3);
            var alkylChain = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 9, 12), new Oxidized(2, 4, 5));

            var actual = alkylChain.GetCandidates(generator).OfType<AlkylChain>().ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position, chain.DoubleBond.Bonds[2].Position)).ToArray();
            Assert.AreEqual(1, tuples.Length);
        }
    }
}