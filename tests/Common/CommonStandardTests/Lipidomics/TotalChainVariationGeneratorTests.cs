using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class TotalChainVariationGeneratorTests
    {
        [TestMethod()]
        public void SeparateTest() {
            var generator = new TotalChainVariationGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            ITotalChain totalChain = new TotalChain(34, 2, 0, 2, 0, 0);

            var tmp = totalChain.GetCandidateSets(generator).ToArray();
            var actual = totalChain.GetCandidateSets(generator).Cast<MolecularSpeciesLevelChains>().Select(act => act.GetDeterminedChains()).ToArray();
            Assert.IsTrue(actual.All(set => set.Length == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a[0], typeof(AcylChain));
                Assert.IsInstanceOfType(a[1], typeof(AcylChain));
            }
            var tuples = actual.Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
            foreach (var tuple in tuples) {
                System.Console.WriteLine(tuple);
            }
            var expects = new[]
            {
                ( 6, 0, 0, 28, 2, 0), ( 6, 1, 0, 28, 1, 0), (28, 0, 0,  6, 2, 0),
                ( 7, 0, 0, 27, 2, 0), ( 7, 1, 0, 27, 1, 0), (27, 0, 0,  7, 2, 0),
                ( 8, 0, 0, 26, 2, 0), ( 8, 1, 0, 26, 1, 0), (26, 0, 0,  8, 2, 0),
                ( 9, 0, 0, 25, 2, 0), ( 9, 1, 0, 25, 1, 0), (25, 0, 0,  9, 2, 0),
                (10, 0, 0, 24, 2, 0), (10, 1, 0, 24, 1, 0), (24, 0, 0, 10, 2, 0),
                (11, 0, 0, 23, 2, 0), (11, 1, 0, 23, 1, 0), (23, 0, 0, 11, 2, 0),
                (12, 0, 0, 22, 2, 0), (12, 1, 0, 22, 1, 0), (22, 0, 0, 12, 2, 0),
                (13, 0, 0, 21, 2, 0), (13, 1, 0, 21, 1, 0), (21, 0, 0, 13, 2, 0),
                (14, 0, 0, 20, 2, 0), (14, 1, 0, 20, 1, 0), (20, 0, 0, 14, 2, 0),
                (15, 0, 0, 19, 2, 0), (15, 1, 0, 19, 1, 0), (19, 0, 0, 15, 2, 0),
                (16, 0, 0, 18, 2, 0), (16, 1, 0, 18, 1, 0), (18, 0, 0, 16, 2, 0),
                (17, 0, 0, 17, 2, 0), (17, 1, 0, 17, 1, 0),
            };
            CollectionAssert.AreEquivalent(expects, tuples);

            totalChain = new TotalChain(34, 2, 0, 1, 1, 0);
            actual = totalChain.GetCandidateSets(generator).Select(act => act.GetDeterminedChains()).ToArray();
            Assert.IsTrue(actual.All(set => set.Length == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a[0], typeof(AlkylChain));
                Assert.IsInstanceOfType(a[1], typeof(AcylChain));
            }
            tuples = actual.Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
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
            var generator = new TotalChainVariationGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            TotalChain totalChain = new TotalChain(10, 0, 0, 2, 0, 0);

            var actual = generator.Separate(totalChain).ToArray();
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod()]
        public void SeparateSphingoTest() {
            var generator = new TotalChainVariationGenerator(minLength: 6, begin: 2, end: 1, skip: 3);
            ITotalChain totalChain = new TotalChain(34, 1, 2, 1, 0, 1);

            var tmp = totalChain.GetCandidateSets(generator).ToArray();
            var actual = totalChain.GetCandidateSets(generator).Cast<PositionLevelChains>().Select(act => act.GetDeterminedChains()).ToArray();
            Assert.IsTrue(actual.All(set => set.Length == 2));
            foreach (var a in actual) {
                Assert.IsInstanceOfType(a[0], typeof(SphingoChain));
                Assert.IsInstanceOfType(a[1], typeof(AcylChain));
            }
            var tuples = actual.Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
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
    }
}