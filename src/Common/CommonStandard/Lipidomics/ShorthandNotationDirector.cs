namespace CompMs.Common.Lipidomics
{
    public sealed class ShorthandNotationDirector
    {
        private readonly ILipidomicsVisitorBuilder _builder;

        public ShorthandNotationDirector(ILipidomicsVisitorBuilder builder)
        {
            _builder = builder;
            builder.SetAcylOxidized(OxidizedIndeterminateState.Identity);
            builder.SetAcylDoubleBond(DoubleBondIndeterminateState.Identity);
            builder.SetAlkylOxidized(OxidizedIndeterminateState.Identity);
            builder.SetAlkylDoubleBond(DoubleBondIndeterminateState.Identity);
            builder.SetSphingoOxidized(OxidizedIndeterminateState.Identity);
            builder.SetSphingoDoubleBond(DoubleBondIndeterminateState.Identity);
            builder.SetChainsState(ChainsIndeterminateState.PositionLevel);
        }

        public void Construct() {
            // for acyl
            _builder.SetAcylOxidized(OxidizedIndeterminateState.AllPositions);
            _builder.SetAcylDoubleBond(DoubleBondIndeterminateState.AllPositions);
            // for alkyl
            _builder.SetAlkylOxidized(OxidizedIndeterminateState.AllPositions);
            _builder.SetAlkylDoubleBond(DoubleBondIndeterminateState.AllPositions.Exclude(1));
            // for sphingosine
            _builder.SetSphingoOxidized(OxidizedIndeterminateState.AllPositions.Exclude(1));
            _builder.SetSphingoDoubleBond(DoubleBondIndeterminateState.AllPositions);
            // for chains
            _builder.SetChainsState(ChainsIndeterminateState.SpeciesLevel);
        }

        public void SetSpeciesLevel() {
            _builder.SetChainsState(ChainsIndeterminateState.SpeciesLevel);
        }

        public void SetMolecularSpeciesLevel() {
            _builder.SetChainsState(ChainsIndeterminateState.MolecularSpeciesLevel);
        }

        public void SetPositionLevel() {
            _builder.SetChainsState(ChainsIndeterminateState.PositionLevel);
        }

        public void SetDoubleBondPositionLevel() {
            _builder.SetAcylDoubleBond(DoubleBondIndeterminateState.AllCisTransIsomers);
            _builder.SetAlkylDoubleBond(DoubleBondIndeterminateState.AllCisTransIsomers);
            _builder.SetSphingoDoubleBond(DoubleBondIndeterminateState.AllCisTransIsomers);
        }

        public void SetDoubleBondNumberLevel() {
            _builder.SetAcylDoubleBond(DoubleBondIndeterminateState.AllPositions);
            _builder.SetAlkylDoubleBond(DoubleBondIndeterminateState.AllPositions.Exclude(1));
            _builder.SetSphingoDoubleBond(DoubleBondIndeterminateState.AllPositions);
        }

        public void SetOxidizedPositionLevel() {
            _builder.SetAcylOxidized(OxidizedIndeterminateState.Identity);
            _builder.SetAlkylOxidized(OxidizedIndeterminateState.Identity);
            _builder.SetSphingoOxidized(OxidizedIndeterminateState.Identity);
        }

        public void SetOxidizedNumberLevel() {
            _builder.SetAcylOxidized(OxidizedIndeterminateState.AllPositions);
            _builder.SetAlkylOxidized(OxidizedIndeterminateState.AllPositions);
            _builder.SetSphingoOxidized(OxidizedIndeterminateState.AllPositions.Exclude(1));
        }
    }
}
