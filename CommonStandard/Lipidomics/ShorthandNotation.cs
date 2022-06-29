using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal class DoubleBondShorthandNotation
    {
        public static DoubleBondShorthandNotation All { get; } = new DoubleBondShorthandNotation();

        private DoubleBondShorthandNotation() {

        }

    }
    internal sealed class OxidizedShorthandNotation : IVisitor<IOxidized, IOxidized>
    {
        public static OxidizedShorthandNotation All { get; } = new OxidizedShorthandNotation();

        private OxidizedShorthandNotation() {
            
        }

        public IOxidized Visit(IOxidized item) {
            if (item.DecidedCount == 0) {
                return item;
            }
            return new Oxidized(item.Count);
        }
    }
}
