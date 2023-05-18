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
            _builder.SetAcylDoubleBondVisitor(DoubleBondShorthandNotation.All);
            _builder.SetAcylOxidizedVisitor(OxidizedShorthandNotation.All);

            // for alkyl
            _builder.SetAlkylDoubleBondVisitor(DoubleBondShorthandNotation.AllForPlasmalogen);
            _builder.SetAlkylOxidizedVisitor(OxidizedShorthandNotation.All);

            // for sphingosine
            _builder.SetSphingoDoubleBondVisitor(DoubleBondShorthandNotation.All);
            _builder.SetSphingoOxidizedVisitor(OxidizedShorthandNotation.AllForCeramide);
        }

    }
}
