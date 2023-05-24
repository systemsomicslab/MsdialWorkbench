using CompMs.Common.DataStructure;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    internal class ChainShorthandNotation : IVisitor<AcylChain, AcylChain>, IVisitor<AlkylChain, AlkylChain>, IVisitor<SphingoChain, SphingoChain>
    {
        private readonly ChainVisitor _chainVisitor;

        public static ChainShorthandNotation Default { get; } = new ChainShorthandNotation();

        private ChainShorthandNotation() {
            var builder = new ChainVisitorBuilder();
            var director = new ShorthandNotationDirector(builder);
            director.Construct();
            _chainVisitor = builder.Create();
        }

        public AcylChain Visit(AcylChain item) {
            return ((IVisitor<AcylChain, AcylChain>)_chainVisitor).Visit(item);
        }

        public AlkylChain Visit(AlkylChain item) {
            return ((IVisitor<AlkylChain, AlkylChain>)_chainVisitor).Visit(item);
        }

        public SphingoChain Visit(SphingoChain item) {
            return ((IVisitor<SphingoChain, SphingoChain>)_chainVisitor).Visit(item);
        }
    }

    internal class AcylChainShorthandNotation : IVisitor<AcylChain, AcylChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public static AcylChainShorthandNotation Default { get; } = new AcylChainShorthandNotation();

        private AcylChainShorthandNotation() {
            _doubleBondVisitor = DoubleBondIndeterminateState.AllPositions.AsVisitor();
            _oxidizedVisitor = OxidizedIndeterminateState.AllPositions.AsVisitor();
        }

        public AcylChain Visit(AcylChain chain) {
            var db = _doubleBondVisitor.Visit(chain.DoubleBond);
            var ox = _oxidizedVisitor.Visit(chain.Oxidized);
            if (chain.DoubleBond == db && chain.Oxidized == ox) {
                return chain;
            }
            return new AcylChain(chain.CarbonCount, db, ox);
        }
    }

    internal class AlkylChainShorthandNotation : IVisitor<AlkylChain, AlkylChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public static AlkylChainShorthandNotation Default { get; } = new AlkylChainShorthandNotation();

        private AlkylChainShorthandNotation() {
            _doubleBondVisitor = DoubleBondIndeterminateState.AllPositions.Exclude(1).AsVisitor();
            _oxidizedVisitor = OxidizedIndeterminateState.AllPositions.AsVisitor();
        }

        public AlkylChain Visit(AlkylChain chain) {
            var db = _doubleBondVisitor.Visit(chain.DoubleBond);
            var ox = _oxidizedVisitor.Visit(chain.Oxidized);
            if (chain.DoubleBond == db && chain.Oxidized == ox) {
                return chain;
            }
            return new AlkylChain(chain.CarbonCount, db, ox);
        }
    }

    internal class SphingoChainShorthandNotation : IVisitor<SphingoChain, SphingoChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public static SphingoChainShorthandNotation Default { get; } = new SphingoChainShorthandNotation();

        private SphingoChainShorthandNotation() {
            _doubleBondVisitor = DoubleBondIndeterminateState.AllPositions.AsVisitor();
            _oxidizedVisitor = OxidizedIndeterminateState.AllPositions.Exclude(1).AsVisitor();
        }

        public SphingoChain Visit(SphingoChain chain) {
            var db = _doubleBondVisitor.Visit(chain.DoubleBond);
            var ox = _oxidizedVisitor.Visit(chain.Oxidized);
            if (chain.DoubleBond == db && chain.Oxidized == ox) {
                return chain;
            }
            return new SphingoChain(chain.CarbonCount, db, ox);
        }
    }

    internal sealed class DoubleBondShorthandNotation : IVisitor<IDoubleBond, IDoubleBond>
    {
        private readonly int[] _excludes;

        public static DoubleBondShorthandNotation All { get; } = new DoubleBondShorthandNotation();

        public static DoubleBondShorthandNotation AllForPlasmalogen { get; } = new DoubleBondShorthandNotation(1);

        public DoubleBondShorthandNotation(params int[] excludes) {
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
        private readonly int[] _excludes;

        public static OxidizedShorthandNotation All { get; } = new OxidizedShorthandNotation();

        public static OxidizedShorthandNotation AllForCeramide { get; } = new OxidizedShorthandNotation(1);

        public OxidizedShorthandNotation(params int[] excludes) {
            _excludes = excludes;
        }


        public IOxidized Visit(IOxidized item) {
            if (item.DecidedCount == 0) {
                return item;
            }
            if (_excludes.Length == 0) {
                return new Oxidized(item.Count);
            }
            else {
                return new Oxidized(item.Count, Product(item.Oxidises));
            }
        }

        private List<int> Product(IEnumerable<int> oxdises) {
            var results = new List<int>();
            foreach (var ox in oxdises) {
                if (_excludes.Contains(ox)) {
                    results.Add(ox);
                }
            }
            return results;
        }

        public static OxidizedShorthandNotation CreateExcludes(params int[] excludes) {
            return new OxidizedShorthandNotation(excludes);
        }
    }
}
