namespace CompMs.Common.Lipidomics
{
    internal class DoubleBondShorthandNotation
    {
        public static DoubleBondShorthandNotation All { get; } = new DoubleBondShorthandNotation();

        private DoubleBondShorthandNotation() {

        }

    }
    internal sealed class OxidizedShorthandNotation
    {
        public static OxidizedShorthandNotation All { get; } = new OxidizedShorthandNotation();

        private OxidizedShorthandNotation() {
            
        }

    }
}
