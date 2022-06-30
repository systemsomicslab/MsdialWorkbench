using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal class AlkylChainShorthandNotation : IVisitor<AlkylChain, AlkylChain>
    {
        private readonly DoubleBondShorthandNotation _doubleBondNotation;
        private readonly OxidizedShorthandNotation _oxidizedNotation;

        public static AlkylChainShorthandNotation All { get; } = new AlkylChainShorthandNotation(DoubleBondShorthandNotation.All, OxidizedShorthandNotation.All);

        private AlkylChainShorthandNotation(DoubleBondShorthandNotation doubleBondNotation, OxidizedShorthandNotation oxidizedNotation) {
            _doubleBondNotation = doubleBondNotation ?? throw new System.ArgumentNullException(nameof(doubleBondNotation));
            _oxidizedNotation = oxidizedNotation ?? throw new System.ArgumentNullException(nameof(oxidizedNotation));
        }

        public AlkylChain Visit(AlkylChain item) {
            var db = _doubleBondNotation.Visit(item.DoubleBond);
            var ox = _oxidizedNotation.Visit(item.Oxidized);
            if (db == item.DoubleBond && ox == item.Oxidized) {
                return item;
            }
            return new AlkylChain(item.CarbonCount, db, ox);
        }
    }

    internal class AcylChainShorthandNotation : IVisitor<AcylChain, AcylChain>
    {
        private readonly DoubleBondShorthandNotation _doubleBondNotation;
        private readonly OxidizedShorthandNotation _oxidizedNotation;

        public static AcylChainShorthandNotation All { get; } = new AcylChainShorthandNotation(DoubleBondShorthandNotation.All, OxidizedShorthandNotation.All);

        private AcylChainShorthandNotation(DoubleBondShorthandNotation doubleBondNotation, OxidizedShorthandNotation oxidizedNotation) {
            _doubleBondNotation = doubleBondNotation ?? throw new System.ArgumentNullException(nameof(doubleBondNotation));
            _oxidizedNotation = oxidizedNotation ?? throw new System.ArgumentNullException(nameof(oxidizedNotation));
        }

        public AcylChain Visit(AcylChain item) {
            var db = _doubleBondNotation.Visit(item.DoubleBond);
            var ox = _oxidizedNotation.Visit(item.Oxidized);
            if (db == item.DoubleBond && ox == item.Oxidized) {
                return item;
            }
            return new AcylChain(item.CarbonCount, db, ox);
        }
    }

    internal class DoubleBondShorthandNotation : IVisitor<IDoubleBond, IDoubleBond>
    {
        public static DoubleBondShorthandNotation All { get; } = new DoubleBondShorthandNotation();

        private DoubleBondShorthandNotation() {

        }

        public IDoubleBond Visit(IDoubleBond item) {
            if (item.DecidedCount == 0) {
                return item;
            }
            return new DoubleBond(item.Count);
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
