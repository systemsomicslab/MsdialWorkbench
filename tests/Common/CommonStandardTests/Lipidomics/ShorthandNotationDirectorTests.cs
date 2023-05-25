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
            Assert.IsNotNull(mockBuilder.AcylDoubleBondState);
            Assert.IsNotNull(mockBuilder.AcylOxidizedState);
            Assert.IsNotNull(mockBuilder.AlkylDoubleBondState);
            Assert.IsNotNull(mockBuilder.AlkylOxidizedState);
            Assert.IsNotNull(mockBuilder.SphingosineDoubleBondState);
            Assert.IsNotNull(mockBuilder.SphingosineOxidizedState);
        }

        class FakeVisitorBuilder : ILipidomicsVisitorBuilder
        {
            public DoubleBondIndeterminateState AcylDoubleBondState, AlkylDoubleBondState, SphingosineDoubleBondState;
            public OxidizedIndeterminateState AcylOxidizedState, AlkylOxidizedState, SphingosineOxidizedState;

            void ILipidomicsVisitorBuilder.SetChainsState(ChainsIndeterminateState state) { }
            void ILipidomicsVisitorBuilder.SetAcylDoubleBond(DoubleBondIndeterminateState state) => AcylDoubleBondState = state;
            void ILipidomicsVisitorBuilder.SetAcylOxidized(OxidizedIndeterminateState state) => AcylOxidizedState = state;
            void ILipidomicsVisitorBuilder.SetAlkylDoubleBond(DoubleBondIndeterminateState state) => AlkylDoubleBondState = state;
            void ILipidomicsVisitorBuilder.SetAlkylOxidized(OxidizedIndeterminateState state) => AlkylOxidizedState = state;
            void ILipidomicsVisitorBuilder.SetSphingoDoubleBond(DoubleBondIndeterminateState state) => SphingosineDoubleBondState = state;
            void ILipidomicsVisitorBuilder.SetSphingoOxidized(OxidizedIndeterminateState state) => SphingosineOxidizedState = state;
        }
    }
}