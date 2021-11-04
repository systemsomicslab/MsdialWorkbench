using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AcylChainGeneratorTests
    {
        [TestMethod()]
        public void SeparateTest() {
            var generator = new AcylChainGenerator(begin: 2, skip: 3);
            var totalChain = new TotalAcylChain(34, 2, 0);

            var actual = totalChain.GetCandidateSets(generator, 2).ToArray();
            Assert.IsTrue(actual.All(set => set.Length == 2));
            var tuples = actual.Select(set => (set[0].CarbonCount, set[0].DoubleBondCount, set[0].OxidizedCount, set[1].CarbonCount, set[1].DoubleBondCount, set[1].OxidizedCount)).ToArray();
            foreach (var tuple in tuples) {
                System.Console.WriteLine(tuple);
            }
            var expects = new[]
            {
                ( 1, 0, 0, 33, 2, 0),
                ( 2, 0, 0, 32, 2, 0),
                ( 3, 0, 0, 31, 2, 0), ( 3, 1, 0, 31, 1, 0),
                ( 4, 0, 0, 30, 2, 0), ( 4, 1, 0, 30, 1, 0),
                ( 5, 0, 0, 29, 2, 0), ( 5, 1, 0, 29, 1, 0),
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
        }

        [TestMethod()]
        public void GenerateTest() {
            var generator = new AcylChainGenerator(begin: 3, skip: 3);
            var acylChain = new AcylChain(18, 2, 0);

            var actual = acylChain.GetCandidates(generator).OfType<SpecificAcylChain>().ToArray();
            Assert.IsTrue(actual.All(chain => chain.DoubleBondPosition.Count == 2));
            var tuples = actual.Select(chain => (chain.DoubleBondPosition[0], chain.DoubleBondPosition[1])).ToArray();
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
    }
}