namespace CompMs.Common.Lipidomics
{
    internal sealed class LipidConverterBuilder : ILipidomicsVisitorBuilder
    {
        private DoubleBondIndeterminateState _acylDoubleBondState, _alkylDoubleBondState, _sphingosineDoubleBondState;
        private OxidizedIndeterminateState _acylOxidizedState, _alkylOxidizedState, _sphingosineOxidizedState;
        private ChainsIndeterminateState _chainsIndeterminate;

        public LipidAnnotationLevelConverter Create() {
            var acylVisitor = new AcylChainVisitor(_acylDoubleBondState.AsVisitor(), _acylOxidizedState.AsVisitor());
            var alkylVisitor = new AlkylChainVisitor(_alkylDoubleBondState.AsVisitor(), _alkylOxidizedState.AsVisitor());
            var sphingosineVisitor = new SphingosineChainVisitor(_sphingosineDoubleBondState.AsVisitor(), _sphingosineOxidizedState.AsVisitor());
            var chainVisitor = new ChainVisitor(acylVisitor, alkylVisitor, sphingosineVisitor);
            var chainsVisitor = new ChainsVisitor(chainVisitor, _chainsIndeterminate);
            return new LipidAnnotationLevelConverter(chainsVisitor);
        }

        void ILipidomicsVisitorBuilder.SetChainsState(ChainsIndeterminateState state) {
            _chainsIndeterminate = state;
        }

        void ILipidomicsVisitorBuilder.SetAcylDoubleBond(DoubleBondIndeterminateState state) {
            _acylDoubleBondState = state;
        }

        void ILipidomicsVisitorBuilder.SetAcylOxidized(OxidizedIndeterminateState state) {
            _acylOxidizedState = state;
        }

        void ILipidomicsVisitorBuilder.SetAlkylDoubleBond(DoubleBondIndeterminateState state) {
            _alkylDoubleBondState = state;
        }

        void ILipidomicsVisitorBuilder.SetAlkylOxidized(OxidizedIndeterminateState state) {
            _alkylOxidizedState = state;
        }

        void ILipidomicsVisitorBuilder.SetSphingoDoubleBond(DoubleBondIndeterminateState state) {
            _sphingosineDoubleBondState = state;
        }

        void ILipidomicsVisitorBuilder.SetSphingoOxidized(OxidizedIndeterminateState state) {
            _sphingosineOxidizedState = state;
        }
    }
}
