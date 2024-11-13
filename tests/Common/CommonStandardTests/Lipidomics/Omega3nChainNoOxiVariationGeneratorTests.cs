using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class Omega3nChainNoOxiVariationGeneratorTests
    {
        private Omega3nChainNoOxiVariationGenerator _generator;

        [TestInitialize]
        public void Init()
        {
            _generator = new Omega3nChainNoOxiVariationGenerator();
        }

        [DataTestMethod()]
        [DynamicData(nameof(GetAcylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateAcylChainFromAcylTest(AcylChain chain)
        {
            var actual = _generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(AcylChain));
        }

        public static IEnumerable<object[]> GetAcylGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new AcylChain(18, new DoubleBond(0), new Oxidized(0)),
                };
                yield return new object[] {
                    new AcylChain(18, new DoubleBond(2), new Oxidized(0)),
                };
                yield return new object[] {
                    new AcylChain(18, new DoubleBond(2, DoubleBondInfo.Create(9)), new Oxidized(0)),
                };
                yield return new object[] {
                    new AcylChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new AcylChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetAlkylGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateAlkylChainFromAlkylChainTest(AlkylChain chain)
        {
            var actual = _generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(AlkylChain));
        }

        public static IEnumerable<object[]> GetAlkylGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(0), new Oxidized(0)),
                };
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(2), new Oxidized(0)),
                };
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(2, DoubleBondInfo.Create(1)), new Oxidized(0)),
                };
                yield return new object[] {
                    new AlkylChain(18, new DoubleBond(3, DoubleBondInfo.Create(1), DoubleBondInfo.Create(12)), new Oxidized(0)),
                };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetSphingosineGenerateTestDatas), DynamicDataSourceType.Property)]
        public void GenerateSphingosineFromSphingosineTest(SphingoChain chain)
        {
            var actual = _generator.Generate(chain).ToArray();
            CollectionAssert.AllItemsAreInstancesOfType(actual, typeof(SphingoChain));
        }

        public static IEnumerable<object[]> GetSphingosineGenerateTestDatas
        {
            get
            {
                yield return new object[] {
                    new SphingoChain(18, new DoubleBond(0), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new SphingoChain(18, new DoubleBond(2), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new SphingoChain(18, new DoubleBond(2, DoubleBondInfo.Create(12)), new Oxidized(2, 1, 3)),
                };
                yield return new object[] {
                    new SphingoChain(18, new DoubleBond(3, DoubleBondInfo.Create(6), DoubleBondInfo.Create(12)), new Oxidized(2, 1, 3)),
                };
            }
        }
    }
}