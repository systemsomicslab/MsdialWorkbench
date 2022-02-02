using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class ChainGeneratorTests
    {
        [TestMethod()]
        public void SeparateTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            ITotalChain totalChain = new TotalChain(34, 2, 0, 2, 0, 0);

            var tmp = totalChain.GetCandidateSets(generator).ToArray();
            var actual = totalChain.GetCandidateSets(generator).Cast<MolecularSpeciesLevelChains>().ToArray();
            Assert.IsTrue(actual.All(set => set.Chains.Count == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a.Chains[0], typeof(AcylChain));
                Assert.IsInstanceOfType(a.Chains[1], typeof(AcylChain));
            }
            var tuples = actual.Select(act => act.Chains).Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
            foreach (var tuple in tuples) {
                System.Console.WriteLine(tuple);
            }
            var expects = new[]
            {
                ( 6, 0, 0, 28, 2, 0), ( 6, 1, 0, 28, 1, 0), ( 6, 2, 0, 28, 0, 0),
                ( 7, 0, 0, 27, 2, 0), ( 7, 1, 0, 27, 1, 0), ( 7, 2, 0, 27, 0, 0),
                ( 8, 0, 0, 26, 2, 0), ( 8, 1, 0, 26, 1, 0), ( 8, 2, 0, 26, 0, 0),
                ( 9, 0, 0, 25, 2, 0), ( 9, 1, 0, 25, 1, 0), ( 9, 2, 0, 25, 0, 0),
                (10, 0, 0, 24, 2, 0), (10, 1, 0, 24, 1, 0), (10, 2, 0, 24, 0, 0),
                (11, 0, 0, 23, 2, 0), (11, 1, 0, 23, 1, 0), (11, 2, 0, 23, 0, 0),
                (12, 0, 0, 22, 2, 0), (12, 1, 0, 22, 1, 0), (12, 2, 0, 22, 0, 0),
                (13, 0, 0, 21, 2, 0), (13, 1, 0, 21, 1, 0), (13, 2, 0, 21, 0, 0),
                (14, 0, 0, 20, 2, 0), (14, 1, 0, 20, 1, 0), (14, 2, 0, 20, 0, 0),
                (15, 0, 0, 19, 2, 0), (15, 1, 0, 19, 1, 0), (15, 2, 0, 19, 0, 0),
                (16, 0, 0, 18, 2, 0), (16, 1, 0, 18, 1, 0), (16, 2, 0, 18, 0, 0),
                (17, 0, 0, 17, 2, 0), (17, 1, 0, 17, 1, 0),
            };
            CollectionAssert.AreEquivalent(expects, tuples);

            totalChain = new TotalChain(34, 2, 0, 1, 1, 0);
            actual = totalChain.GetCandidateSets(generator).Cast<MolecularSpeciesLevelChains>().ToArray();
            Assert.IsTrue(actual.All(set => set.Chains.Count == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a.Chains[0], typeof(AlkylChain));
                Assert.IsInstanceOfType(a.Chains[1], typeof(AcylChain));
            }
            tuples = actual.Select(act => act.Chains).Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
            foreach (var tuple in tuples) {
                System.Console.WriteLine(tuple);
            }
            expects = new[]
            {
                ( 6, 0, 0, 28, 2, 0), ( 6, 1, 0, 28, 1, 0), ( 6, 2, 0, 28, 0, 0),
                ( 7, 0, 0, 27, 2, 0), ( 7, 1, 0, 27, 1, 0), ( 7, 2, 0, 27, 0, 0),
                ( 8, 0, 0, 26, 2, 0), ( 8, 1, 0, 26, 1, 0), ( 8, 2, 0, 26, 0, 0),
                ( 9, 0, 0, 25, 2, 0), ( 9, 1, 0, 25, 1, 0), ( 9, 2, 0, 25, 0, 0),
                (10, 0, 0, 24, 2, 0), (10, 1, 0, 24, 1, 0), (10, 2, 0, 24, 0, 0),
                (11, 0, 0, 23, 2, 0), (11, 1, 0, 23, 1, 0), (11, 2, 0, 23, 0, 0),
                (12, 0, 0, 22, 2, 0), (12, 1, 0, 22, 1, 0), (12, 2, 0, 22, 0, 0),
                (13, 0, 0, 21, 2, 0), (13, 1, 0, 21, 1, 0), (13, 2, 0, 21, 0, 0),
                (14, 0, 0, 20, 2, 0), (14, 1, 0, 20, 1, 0), (14, 2, 0, 20, 0, 0),
                (15, 0, 0, 19, 2, 0), (15, 1, 0, 19, 1, 0), (15, 2, 0, 19, 0, 0),
                (16, 0, 0, 18, 2, 0), (16, 1, 0, 18, 1, 0), (16, 2, 0, 18, 0, 0),
                (17, 0, 0, 17, 2, 0), (17, 1, 0, 17, 1, 0), (17, 2, 0, 17, 0, 0),
                (18, 0, 0, 16, 2, 0), (18, 1, 0, 16, 1, 0), (18, 2, 0, 16, 0, 0),
                (19, 0, 0, 15, 2, 0), (19, 1, 0, 15, 1, 0), (19, 2, 0, 15, 0, 0),
                (20, 0, 0, 14, 2, 0), (20, 1, 0, 14, 1, 0), (20, 2, 0, 14, 0, 0),
                (21, 0, 0, 13, 2, 0), (21, 1, 0, 13, 1, 0), (21, 2, 0, 13, 0, 0),
                (22, 0, 0, 12, 2, 0), (22, 1, 0, 12, 1, 0), (22, 2, 0, 12, 0, 0),
                (23, 0, 0, 11, 2, 0), (23, 1, 0, 11, 1, 0), (23, 2, 0, 11, 0, 0),
                (24, 0, 0, 10, 2, 0), (24, 1, 0, 10, 1, 0), (24, 2, 0, 10, 0, 0),
                (25, 0, 0,  9, 2, 0), (25, 1, 0,  9, 1, 0), (25, 2, 0,  9, 0, 0),
                (26, 0, 0,  8, 2, 0), (26, 1, 0,  8, 1, 0), (26, 2, 0,  8, 0, 0),
                (27, 0, 0,  7, 2, 0), (27, 1, 0,  7, 1, 0), (27, 2, 0,  7, 0, 0),
                (28, 0, 0,  6, 2, 0), (28, 1, 0,  6, 1, 0), (28, 2, 0,  6, 0, 0),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void SeparateShortChainTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            TotalChain totalChain = new TotalChain(10, 0, 0, 2, 0, 0);

            var actual = generator.Separate(totalChain).ToArray();
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod()]
        public void SeparateSphingoTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            ITotalChain totalChain = new TotalChain(34, 1, 2, 1, 0, 1);

            var tmp = totalChain.GetCandidateSets(generator).ToArray();
            var actual = totalChain.GetCandidateSets(generator).Cast<PositionLevelChains>().ToArray();
            Assert.IsTrue(actual.All(set => set.Chains.Count == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a.Chains[0], typeof(SphingoChain));
                Assert.IsInstanceOfType(a.Chains[1], typeof(AcylChain));
            }
            var tuples = actual.Select(act => act.Chains).Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
            foreach (var tuple in tuples) {
                System.Console.WriteLine(tuple);
            }
            var expects = new[]
            {
                ( 6, 1, 2, 28, 0, 0), ( 6, 0, 2, 28, 1, 0),
                ( 7, 1, 2, 27, 0, 0), ( 7, 0, 2, 27, 1, 0),
                ( 8, 1, 2, 26, 0, 0), ( 8, 0, 2, 26, 1, 0),
                ( 9, 1, 2, 25, 0, 0), ( 9, 0, 2, 25, 1, 0),
                (10, 1, 2, 24, 0, 0), (10, 0, 2, 24, 1, 0),
                (11, 1, 2, 23, 0, 0), (11, 0, 2, 23, 1, 0),
                (12, 1, 2, 22, 0, 0), (12, 0, 2, 22, 1, 0),
                (13, 1, 2, 21, 0, 0), (13, 0, 2, 21, 1, 0),
                (14, 1, 2, 20, 0, 0), (14, 0, 2, 20, 1, 0),
                (15, 1, 2, 19, 0, 0), (15, 0, 2, 19, 1, 0),
                (16, 1, 2, 18, 0, 0), (16, 0, 2, 18, 1, 0),
                (17, 1, 2, 17, 0, 0), (17, 0, 2, 17, 1, 0),
                (18, 1, 2, 16, 0, 0), (18, 0, 2, 16, 1, 0),
                (19, 1, 2, 15, 0, 0), (19, 0, 2, 15, 1, 0),
                (20, 1, 2, 14, 0, 0), (20, 0, 2, 14, 1, 0),
                (21, 1, 2, 13, 0, 0), (21, 0, 2, 13, 1, 0),
                (22, 1, 2, 12, 0, 0), (22, 0, 2, 12, 1, 0),
                (23, 1, 2, 11, 0, 0), (23, 0, 2, 11, 1, 0),
                (24, 1, 2, 10, 0, 0), (24, 0, 2, 10, 1, 0),
                (25, 1, 2,  9, 0, 0), (25, 0, 2,  9, 1, 0),
                (26, 1, 2,  8, 0, 0), (26, 0, 2,  8, 1, 0),
                (27, 1, 2,  7, 0, 0), (27, 0, 2,  7, 1, 0),
                (28, 1, 2,  6, 0, 0), (28, 0, 2,  6, 1, 0),
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GenerateTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
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
        public void GenerateAlkylTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
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
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
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
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
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
            };
            CollectionAssert.AreEquivalent(expects, tuples);
        }

        [TestMethod()]
        public void GeneratePositionSpecifiedAcylChainTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
            var alkylChain = new AcylChain(18, DoubleBond.CreateFromPosition(1, 9, 12), new Oxidized(2, 4, 5));

            var actual = alkylChain.GetCandidates(generator).OfType<AcylChain>().ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position, chain.DoubleBond.Bonds[2].Position)).ToArray();
            Assert.AreEqual(1, tuples.Length);
        }

        [TestMethod()]
        public void GeneratePositionSpecifiedAlkylChainTest() {
            var generator = new ChainGenerator(minLength: 6, begin: 3, end: 1, skip: 3);
            var alkylChain = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 9, 12), new Oxidized(2, 4, 5));

            var actual = alkylChain.GetCandidates(generator).OfType<AlkylChain>().ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBond.Count == 3));
            var tuples = actual.Select(chain => (chain.DoubleBond.Bonds[0].Position, chain.DoubleBond.Bonds[1].Position, chain.DoubleBond.Bonds[2].Position)).ToArray();
            Assert.AreEqual(1, tuples.Length);
        }
    }
}