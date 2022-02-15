using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AlkylChainParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new AlkylChainParser();

            var alkyl = parser.Parse("O-16:0");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(16, alkyl.CarbonCount);
            Assert.AreEqual(0, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-18:3");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-18:0;O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(0, alkyl.DoubleBondCount);
            Assert.AreEqual(1, alkyl.OxidizedCount);

            //alkyl = parser.Parse("O-20:4;O4");
            alkyl = parser.Parse("O-20:4;4O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(4, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);

            alkyl = parser.Parse("O-20:4;5OH,6OH,11OH,12OH");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(4, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 5, 6, 11, 12 }, ((AlkylChain)alkyl).Oxidized.Oxidises);

            alkyl = parser.Parse("O-18:2(6,12)");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(2, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 6, 12 }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());

            alkyl = parser.Parse("O-18:2(9Z,11E)");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(2, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 9, 11 }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { DoubleBondState.Z, DoubleBondState.E }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.State).ToArray());

            alkyl = parser.Parse("P-16:0");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(16, alkyl.CarbonCount);
            Assert.AreEqual(1, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-18:2");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-18:0;O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(1, alkyl.DoubleBondCount);
            Assert.AreEqual(1, alkyl.OxidizedCount);

            //alkyl = parser.Parse("P-20:4;O4");
            alkyl = parser.Parse("P-20:4;4O");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(5, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);

            alkyl = parser.Parse("P-20:4;5OH,6OH,11OH,12OH");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(20, alkyl.CarbonCount);
            Assert.AreEqual(5, alkyl.DoubleBondCount);
            Assert.AreEqual(4, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 5, 6, 11, 12 }, ((AlkylChain)alkyl).Oxidized.Oxidises);

            alkyl = parser.Parse("P-18:2(6,12)");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 6, 12 }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());

            alkyl = parser.Parse("P-18:2(9Z,11E)");
            Assert.IsInstanceOfType(alkyl, typeof(AlkylChain));
            Assert.AreEqual(18, alkyl.CarbonCount);
            Assert.AreEqual(3, alkyl.DoubleBondCount);
            Assert.AreEqual(0, alkyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 9, 11 }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { DoubleBondState.Unknown, DoubleBondState.Z, DoubleBondState.E }, ((AlkylChain)alkyl).DoubleBond.Bonds.Select(b => b.State).ToArray());
        }
    }
}