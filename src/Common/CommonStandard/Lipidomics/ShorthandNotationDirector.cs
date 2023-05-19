namespace CompMs.Common.Lipidomics
{
    public sealed class ShorthandNotationDirector
    {
        private readonly ILipidomicsVisitorBuilder _builder;

        public ShorthandNotationDirector(ILipidomicsVisitorBuilder builder)
        {
            _builder = builder;
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
        }

    }
}
