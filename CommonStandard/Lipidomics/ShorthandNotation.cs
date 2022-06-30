using CompMs.Common.DataStructure;
using System.Collections.Generic;
using System.Linq;

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
        private readonly int[] _excludes;

        public static DoubleBondShorthandNotation All { get; } = new DoubleBondShorthandNotation(new int[0]);

        public static DoubleBondShorthandNotation AllForPlasmalogen { get; } = new DoubleBondShorthandNotation(new[] { 1, });

        private DoubleBondShorthandNotation(params int[] excludes) {
            _excludes = excludes;
        }

        public IDoubleBond Visit(IDoubleBond item) {
            if (item.DecidedCount == 0) {
                return item;
            }
            if (_excludes.Length == 0) {
            return new DoubleBond(item.Count);
            }
            else {
                return new DoubleBond(item.Count, Product(item.Bonds));
            }
        }

        private List<IDoubleBondInfo> Product(IEnumerable<IDoubleBondInfo> bonds) {
            var results = new List<IDoubleBondInfo>();
            foreach (var bond in bonds) {
                if (_excludes.Contains(bond.Position)) {
                    results.Add(bond);
                }
            }
            return results;
        }

        public static DoubleBondShorthandNotation CreateExcludes(params int[] excludes) {
            return new DoubleBondShorthandNotation(excludes);
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
