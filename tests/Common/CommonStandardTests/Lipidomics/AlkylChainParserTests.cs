using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AlkylChainParserTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(ParseTestData), DynamicDataSourceType.Property)]
        public void ParseTest(string name, int carbons, int doublebonds, int oxidizes, bool isPlasmalogen) {
            var parser = new AlkylChainParser();
            var alkyl = parser.Parse(name);
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(carbons, alkyl.CarbonCount);
            Assert.AreEqual(isPlasmalogen, ((AlkylChain)alkyl).IsPlasmalogen);
            Assert.AreEqual(doublebonds, alkyl.DoubleBondCount);
            Assert.AreEqual(oxidizes, alkyl.OxidizedCount);
        }

        public static IEnumerable<object[]> ParseTestData {
            get {
                yield return new object[] { "O-16:0", 16, 0, 0, false, };
                yield return new object[] { "O-18:3", 18, 3, 0, false, };
                yield return new object[] { "O-18:0;O", 18, 0, 1, false, };
                yield return new object[] { "O-20:4;O4", 20, 4, 4, false, };
                yield return new object[] { "O-20:4(5OH,6OH,11OH,12OH)", 20, 4, 4, false, };
                yield return new object[] { "O-18:2(6,12)", 18, 2, 0, false, };
                yield return new object[] { "O-18:2(9Z,11E)", 18, 2, 0, false, };
                yield return new object[] { "P-16:0", 16, 1, 0, true, };
                yield return new object[] { "P-18:2", 18, 3, 0, true, };
                yield return new object[] { "P-18:0;O", 18, 1, 1, true, };
                yield return new object[] { "P-20:4;O4", 20, 5, 4, true, };
                yield return new object[] { "P-20:4(5OH,6OH,11OH,12OH)", 20, 5, 4, true, };
                yield return new object[] { "P-18:2(6,12)", 18, 3, 0, true, };
                yield return new object[] { "P-18:2(9Z,11E)", 18, 3, 0, true, };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ParsePositionsTestData), DynamicDataSourceType.Property)]
        public void ParsePositionsTest(string name, int[] dbs, DoubleBondState[] states, int[] oxs) {
            var parser = new AlkylChainParser();
            var alkyl = parser.Parse(name);
            if (dbs is not null) {
                CollectionAssert.AreEqual(dbs, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            }
            if (states is not null) {
                CollectionAssert.AreEqual(states, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.State).ToArray());
            }
            if (oxs is not null) {
                CollectionAssert.AreEqual(oxs, ((AlkylChain)alkyl).Oxidized.Oxidises);
            }
        }

        public static IEnumerable<object[]> ParsePositionsTestData {
            get {
                yield return new object[] { "O-20:4(5OH,6OH,11OH,12OH)", null, null, new[] { 5, 6, 11, 12, }, };
                yield return new object[] { "O-18:2(6,12)", new[] { 6, 12 }, null, null, };
                yield return new object[] { "O-18:2(9Z,11E)", new[] { 9, 11 }, new[] { DoubleBondState.Z, DoubleBondState.E }, null, };
                yield return new object[] { "P-20:4(5OH,6OH,11OH,12OH)", null, null, new[] { 5, 6, 11, 12 }, };
                yield return new object[] { "P-18:2(6,12)", new[] { 1, 6, 12 }, null, null, };
                yield return new object[] { "P-18:2(9Z,11E)", new[] { 1, 9, 11 }, new[] { DoubleBondState.Unknown, DoubleBondState.Z, DoubleBondState.E }, null, };
            }
        }
    }
}