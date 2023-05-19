using CompMs.Common.DataStructure;
using CompMs.Common.Lipidomics;

namespace CommonStandardTests.Lipidomics
{
    internal sealed class ChainVisitorBuilder : ILipidomicsVisitorBuilder
    {
        private IVisitor<IDoubleBond, IDoubleBond> _acylDoubleBondVisitor, _alkylDoubleBondVisitor, _sphingoDoubleBondVisitor;
        private IVisitor<IOxidized, IOxidized> _acylOxidizedVisitor, _alkylOxidizedVisitor, _sphingoOxidizedVisitor;

        public ChainVisitor Create() {
            var acylVisitor = new AcylChainVisitor(_acylDoubleBondVisitor, _acylOxidizedVisitor);
            var alkylVisitor = new AlkylChainVisitor(_alkylDoubleBondVisitor, _alkylOxidizedVisitor);
            var sphingoVisitor = new SphingosineChainVisitor(_sphingoDoubleBondVisitor, _sphingoOxidizedVisitor);
            return new ChainVisitor(acylVisitor, alkylVisitor, sphingoVisitor);
        }

        void ILipidomicsVisitorBuilder.SetAcylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => _acylDoubleBondVisitor = doubleBondVisitor;
        void ILipidomicsVisitorBuilder.SetAcylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => _acylOxidizedVisitor = oxidizedVisitor;
        void ILipidomicsVisitorBuilder.SetAlkylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => _alkylDoubleBondVisitor = doubleBondVisitor;
        void ILipidomicsVisitorBuilder.SetAlkylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => _alkylOxidizedVisitor = oxidizedVisitor;
        void ILipidomicsVisitorBuilder.SetSphingoDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor) => _sphingoDoubleBondVisitor = doubleBondVisitor;
        void ILipidomicsVisitorBuilder.SetSphingoOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor) => _sphingoOxidizedVisitor = oxidizedVisitor;
    }
}
