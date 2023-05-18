using CompMs.Common.DataStructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class ShorthandNotationDirectorTests
    {
        [TestMethod()]
        public void ConstructTest() {
            var mockBuilder = new FakeVisitorBuilder();
            var director = new ShorthandNotationDirector(mockBuilder);
            director.Construct();
            Assert.AreEqual(DoubleBondShorthandNotation.All, mockBuilder.AcylDoubleBondVisitor);
            Assert.AreEqual(DoubleBondShorthandNotation.AllForPlasmalogen, mockBuilder.AlkylDoubleBondVisitor);
            Assert.AreEqual(DoubleBondShorthandNotation.All, mockBuilder.SphingosineDoubleBondVisitor);
            Assert.AreEqual(OxidizedShorthandNotation.All, mockBuilder.AcylOxidizedVisitor);
            Assert.AreEqual(OxidizedShorthandNotation.All, mockBuilder.AlkylOxidizedVisitor);
            Assert.AreEqual(OxidizedShorthandNotation.AllForCeramide, mockBuilder.SphingosineOxidizedVisitor);
        }

        class FakeVisitorBuilder : ILipidomicsVisitorBuilder
        {
            public IVisitor<IDoubleBond, IDoubleBond> AcylDoubleBondVisitor, AlkylDoubleBondVisitor, SphingosineDoubleBondVisitor;
            public IVisitor<IOxidized, IOxidized> AcylOxidizedVisitor, AlkylOxidizedVisitor, SphingosineOxidizedVisitor;

            public void SetAcylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => AcylDoubleBondVisitor = doubleBondVisitor;
            public void SetAcylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => AcylOxidizedVisitor = oxidizedVisitor;
            public void SetAlkylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => AlkylDoubleBondVisitor = doubleBondVisitor;
            public void SetAlkylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => AlkylOxidizedVisitor = oxidizedVisitor;
            public void SetSphingoDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => SphingosineDoubleBondVisitor = doubleBondVisitor;
            public void SetSphingoOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => SphingosineOxidizedVisitor = oxidizedVisitor;
        }
    }
}