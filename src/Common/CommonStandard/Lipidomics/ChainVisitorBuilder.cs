namespace CompMs.Common.Lipidomics
{
    internal sealed class ChainVisitorBuilder : ILipidomicsVisitorBuilder {
        private DoubleBondIndeterminateState _acylDoubleBondState, _alkylDoubleBondState, _sphingoDoubleBondState;
        private OxidizedIndeterminateState _acylOxidizedState, _alkylOxidizedState, _sphingoOxidizedState;

        public ChainVisitor Create() {
            var acylVisitor = new AcylChainVisitor(_acylDoubleBondState.AsVisitor(), _acylOxidizedState.AsVisitor());
            var alkylVisitor = new AlkylChainVisitor(_alkylDoubleBondState.AsVisitor(), _alkylOxidizedState.AsVisitor());
            var sphingoVisitor = new SphingosineChainVisitor(_sphingoDoubleBondState.AsVisitor(), _sphingoOxidizedState.AsVisitor());
            return new ChainVisitor(acylVisitor, alkylVisitor, sphingoVisitor);
        }

        void ILipidomicsVisitorBuilder.SetChainsState(ChainsIndeterminateState state) { }

        void ILipidomicsVisitorBuilder.SetAcylDoubleBond(DoubleBondIndeterminateState state) => _acylDoubleBondState = state;
        void ILipidomicsVisitorBuilder.SetAcylOxidized(OxidizedIndeterminateState state) => _acylOxidizedState = state;

        void ILipidomicsVisitorBuilder.SetAlkylDoubleBond(DoubleBondIndeterminateState state) => _alkylDoubleBondState = state;
        void ILipidomicsVisitorBuilder.SetAlkylOxidized(OxidizedIndeterminateState state) => _alkylOxidizedState = state;

        void ILipidomicsVisitorBuilder.SetSphingoDoubleBond(DoubleBondIndeterminateState state) => _sphingoDoubleBondState = state;
        void ILipidomicsVisitorBuilder.SetSphingoOxidized(OxidizedIndeterminateState state) => _sphingoOxidizedState = state;
    }
}
