using CompMs.Common.Lipidomics;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class Omega3nChainGeneratorTests
    {
        [TestMethod()]
        [DataRow(6)]
        [DataRow(16)]
        [DataRow(18)]
        [DataRow(20)]
        [DataRow(22)]
        [DataRow(100)]
        public void CarbonIsValidTest(int carbon)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.CarbonIsValid(carbon);
            Assert.IsTrue(actual); // currently, it is always "true".
        }

        [TestMethod()]
        [DataRow(false, 6, -1)]
        [DataRow(true, 6, 0)]
        [DataRow(true, 6, 1)]
        [DataRow(false, 6, 2)]
        [DataRow(true, 16, 0)]
        [DataRow(true, 16, 1)]
        [DataRow(true, 18, 0)]
        [DataRow(true, 18, 1)]
        [DataRow(true, 18, 2)]
        [DataRow(true, 18, 3)]
        [DataRow(true, 20, 5)]
        [DataRow(false, 20, 6)]
        [DataRow(true, 22, 6)]
        [DataRow(false, 22, 7)]
        public void DoubleBondIsValidTest(bool expected, int carbon, int doubleBond)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.DoubleBondIsValid(carbon, doubleBond);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DynamicData(nameof(GetAcylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void AcylGenerateTest(IChain[] expected, AcylChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            Console.WriteLine("source: {0}", chain);
            actual.ToList().ForEach(a => Console.WriteLine(a));
            Assert.AreEqual(expected.Length, actual.Length);
            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.That.AreChainsEqual(e, a);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetAcylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateAcylChainFromAcylChainTest(IChain[] _, AcylChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(AcylChain));
        }

        public static IEnumerable<object[]> GetAcylGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new IChain[] {
                        new AcylChain(18, new DoubleBond(0), new Oxidized(0)),
                    },
                    new AcylChain(18, new DoubleBond(0), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AcylChain(18, DoubleBond.CreateFromPosition( 2,  4), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 11), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 6,  9), new Oxidized(0)),
                    },
                    new AcylChain(18, new DoubleBond(2), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 11), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 15), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 6,  9), new Oxidized(0)),
                    },
                    new AcylChain(18, new DoubleBond(2, DoubleBondInfo.Create(9)), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AcylChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0)),
                    },
                    new AcylChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AcylChain(18, DoubleBond.CreateFromPosition(6, 12, 15), new Oxidized(0)),
                        new AcylChain(18, DoubleBond.CreateFromPosition(6, 9, 12), new Oxidized(0)),
                    },
                    new AcylChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetAlkylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void AlkylGenerateTest(IChain[] expected, AlkylChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            Console.WriteLine("source: {0}", chain);
            actual.ToList().ForEach(a => Console.WriteLine(a));
            Assert.AreEqual(expected.Length, actual.Length);
            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.That.AreChainsEqual(e, a);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetAlkylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateAlkylChainFromAlkylChainTest(IChain[] _, AlkylChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(AlkylChain));
        }

        public static IEnumerable<object[]> GetAlkylGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, new DoubleBond(0), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(0), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 6,  9), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 1, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 1, 12), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 1,  9), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 1,  6), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(2), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, DoubleBond.CreateFromPosition(12, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition( 1, 12), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, DoubleBond.CreateFromPosition(6, 12, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(6, 9, 12), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1, 6, 12), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1,  9), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1,  6), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Create(1)), new Oxidized(0)),
                };
                yield return new object[] {
                    new IChain[] {
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12, 15), new Oxidized(0)),
                        new AlkylChain(18, DoubleBond.CreateFromPosition(1,  9, 12), new Oxidized(0)),
                    },
                    new AlkylChain(18, new DoubleBond(3, DoubleBondInfo.Create(1), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetSphingosineGenerateTestDatas), DynamicDataSourceType.Property)]
        public void SphingosineGenerateTest(IChain[] expected, SphingoChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            Assert.AreEqual(expected.Length, actual.Length);
            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.That.AreChainsEqual(e, a);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetSphingosineGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateSphingosineFromSphingosineTest(IChain[] _, SphingoChain chain)
        {
            var generator = new Omega3nChainGenerator();
            var actual = generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(SphingoChain));
        }

        public static IEnumerable<object[]> GetSphingosineGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new IChain[] {
                        new SphingoChain(18, new DoubleBond(0), new Oxidized(2, 1, 3)),
                    },
                    new SphingoChain(18, new DoubleBond(0), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new IChain[] {
                        new SphingoChain(18, DoubleBond.CreateFromPosition(4,  8), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(4, 14), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(8, 14), new Oxidized(2, 1, 3)),
                    },
                    new SphingoChain(18, new DoubleBond(2), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new IChain[] {
                        new SphingoChain(18, DoubleBond.CreateFromPosition(4, 12), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(8, 12), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(12, 14), new Oxidized(2, 1, 3)),
                    },
                    new SphingoChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new IChain[] {
                        new SphingoChain(18, DoubleBond.CreateFromPosition(4, 6, 12), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(6, 8, 12), new Oxidized(2, 1, 3)),
                        new SphingoChain(18, DoubleBond.CreateFromPosition(6, 12, 14), new Oxidized(2, 1, 3)),
                        // new SphingoChain(18, DoubleBond.CreateFromPosition(6, 12, 15), new Oxidized(2, 1, 3)),
                        // new SphingoChain(18, DoubleBond.CreateFromPosition(6, 9, 12), new Oxidized(2, 1, 3)),
                    },
                    new SphingoChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(2, 1, 3)),
                };
            }
        }
    }

    static class ChainAssertion
    {
        public static void AreChainsEqual(this Assert assert, IChain expected, IChain actual) {
            Assert.AreEqual(expected.GetType(), actual.GetType());
            Assert.AreEqual(expected.CarbonCount, actual.CarbonCount);
            Assert.AreEqual(expected.DoubleBondCount, actual.DoubleBondCount);
            CollectionAssert.AreEqual(expected.DoubleBond.Bonds, actual.DoubleBond.Bonds);
            Assert.AreEqual(expected.OxidizedCount, actual.OxidizedCount);
            CollectionAssert.AreEqual(expected.Oxidized.Oxidises, actual.Oxidized.Oxidises);
        }
    }
}