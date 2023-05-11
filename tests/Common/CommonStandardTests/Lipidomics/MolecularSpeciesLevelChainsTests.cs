using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class MolecularSpeciesLevelChainsTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(EqualsTestData), DynamicDataSourceType.Property)]
        public void EqualsTest(ITotalChain chains, ITotalChain other, bool expected) {
            Assert.AreEqual(expected, chains.Equals(other));
        }

        public static IEnumerable<object[]> EqualsTestData {
            get {
                IOxidized ox = new Oxidized(0);
                IDoubleBond bond0 = new DoubleBond(0), bond1 = new DoubleBond(1);
                IChain acyl160 = new AcylChain(16, bond0, ox),
                    acyl181 = new AcylChain(18, bond1, ox),
                    acyl161 = new AcylChain(16, bond1, ox),
                    acyl180 = new AcylChain(18, bond0, ox),
                    alkyl160 = new AlkylChain(16, bond0, ox);
                ITotalChain chains1 = new MolecularSpeciesLevelChains(acyl160, acyl181),
                    chains2 = new MolecularSpeciesLevelChains(acyl181, acyl160),
                    chains3 = new MolecularSpeciesLevelChains(acyl160, acyl160),
                    chains4 = new MolecularSpeciesLevelChains(acyl161, acyl180),
                    chains5 = new TotalChain(34, 1, 0, 2, 0, 0),
                    chains6 = new PositionLevelChains(acyl160, acyl181),
                    chains7 = new MolecularSpeciesLevelChains(alkyl160, acyl181);
                yield return new object[] { chains1, chains1, true, };
                yield return new object[] { chains1, chains2, true, };
                yield return new object[] { chains1, chains3, false, };
                yield return new object[] { chains1, chains4, false, };
                yield return new object[] { chains1, chains5, false, };
                yield return new object[] { chains1, chains6, false, };
                yield return new object[] { chains1, chains7, false, };
            }
        }
    }
}