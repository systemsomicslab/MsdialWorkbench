using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class ChainsIndeterminateStateTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(IndeterminateTestData), DynamicDataSourceType.Property)]
        public void IndeterminateTest(ChainsIndeterminateState state, ITotalChain chain, ITotalChain expected) {
            var actual = state.Indeterminate(chain);
            Assert.IsTrue(actual.Equals(expected));
        }

        public static IEnumerable<object[]> IndeterminateTestData {
            get {
                IChain acyl181 = new AcylChain(18, new DoubleBond(1), new Oxidized(0)), acyl160 = new AcylChain(16, new DoubleBond(0), new Oxidized(0));
                ITotalChain acyls341 = new TotalChain(34, 1, 0, 2, 0, 0), acyls160_181 = new MolecularSpeciesLevelChains(acyl160, acyl181), acyls160181 = new PositionLevelChains(acyl160, acyl181);
                yield return new object[] { ChainsIndeterminateState.SpeciesLevel, acyls160181, acyls341, };
                yield return new object[] { ChainsIndeterminateState.SpeciesLevel, acyls160_181, acyls341, };
                yield return new object[] { ChainsIndeterminateState.SpeciesLevel, acyls341, acyls341, };
                yield return new object[] { ChainsIndeterminateState.MolecularSpeciesLevel, acyls160181, acyls160_181, };
                yield return new object[] { ChainsIndeterminateState.MolecularSpeciesLevel, acyls160_181, acyls160_181, };
                yield return new object[] { ChainsIndeterminateState.MolecularSpeciesLevel, acyls341, acyls341, };
                yield return new object[] { ChainsIndeterminateState.PositionLevel, acyls160181, acyls160181, };
                yield return new object[] { ChainsIndeterminateState.PositionLevel, acyls160_181, acyls160_181, };
                yield return new object[] { ChainsIndeterminateState.PositionLevel, acyls341, acyls341, };
            }
        }
    }
}