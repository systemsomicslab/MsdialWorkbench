using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class SphingoChainParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new SphingoChainParser();

            var acyl = parser.Parse("16:0;O2");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(16, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(2, acyl.OxidizedCount);

            acyl = parser.Parse("18:3;O2");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(3, acyl.DoubleBondCount);
            Assert.AreEqual(2, acyl.OxidizedCount);

            acyl = parser.Parse("18:0(1OH,3OH)");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(2, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 3 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("20:4;O4");
            //acyl = parser.Parse("20:4;4O");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(20, acyl.CarbonCount);
            Assert.AreEqual(4, acyl.DoubleBondCount);
            Assert.AreEqual(4, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 3 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:2(6,12)(1OH,3OH)");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(2, acyl.DoubleBondCount);
            Assert.AreEqual(2, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 6, 12 }, ((SphingoChain)acyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:2(9Z,11E);O2");
            //acyl = parser.Parse("18:2(9Z,11E);2O");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(2, acyl.DoubleBondCount);
            Assert.AreEqual(2, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 9, 11 }, ((SphingoChain)acyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { DoubleBondState.Z, DoubleBondState.E }, ((SphingoChain)acyl).DoubleBond.Bonds.Select(b => b.State).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:0;O3");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(3, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:0(1OH,3OH,4OH)");
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(0, acyl.DoubleBondCount);
            Assert.AreEqual(3, acyl.OxidizedCount);
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:1;O3"); // 
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(1, acyl.DoubleBondCount);
            Assert.AreEqual(3, acyl.OxidizedCount);
            CollectionAssert.AreEqual(Array.Empty<int>(), ((SphingoChain)acyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, ((SphingoChain)acyl).Oxidized.Oxidises);

            acyl = parser.Parse("18:1(1OH,3OH,4OH)"); // "18:1(4);1OH,3OH,4OH"
            Assert.IsInstanceOfType(acyl, typeof(SphingoChain));
            Assert.AreEqual(18, acyl.CarbonCount);
            Assert.AreEqual(1, acyl.DoubleBondCount);
            Assert.AreEqual(3, acyl.OxidizedCount);
            CollectionAssert.AreEqual(Array.Empty<int>(), ((SphingoChain)acyl).DoubleBond.Bonds.Select(b => b.Position).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, ((SphingoChain)acyl).Oxidized.Oxidises);

        }
    }
}