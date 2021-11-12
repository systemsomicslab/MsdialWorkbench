using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class AcylChainParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var parser = new AcylChainParser();

            var acyl = parser.Parse("16:0");
            Assert.IsInstanceOfType(acyl, typeof(AcylChain));
            Assert.AreEqual(16, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(0, acyl.OxidizedCount);

            acyl = parser.Parse("18:3");
            Assert.IsInstanceOfType(acyl, typeof(AcylChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(3, acyl.DoubleBondCount);
            Assert.AreEqual(0, acyl.OxidizedCount);

            acyl = parser.Parse("18:0;O");
            Assert.IsInstanceOfType(acyl, typeof(AcylChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(1, acyl.OxidizedCount);

            acyl = parser.Parse("20:4;4O");
            Assert.IsInstanceOfType(acyl, typeof(AcylChain));
            Assert.AreEqual(20, acyl.CarbonCount);
            Assert.AreEqual(4, acyl.DoubleBondCount);
            Assert.AreEqual(4, acyl.OxidizedCount);

            acyl = parser.Parse("18:2(6,12)");
            Assert.IsInstanceOfType(acyl, typeof(AcylChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(2, acyl.DoubleBondCount);
            Assert.AreEqual(0, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 6, 12 }, ((AcylChain)acyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
        }
    }
}