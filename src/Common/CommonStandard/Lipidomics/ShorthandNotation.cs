using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    internal class ChainShorthandNotation : IVisitor<AcylChain, AcylChain>, IVisitor<AlkylChain, AlkylChain>, IVisitor<SphingoChain, SphingoChain>
    {
        private readonly AcylChainShorthandNotation _acyl;
        private readonly AlkylChainShorthandNotation _alkyl;
        private readonly SphingoChainShorthandNotation _sphingosine;

        public static ChainShorthandNotation Default { get; } = new ChainShorthandNotation(AcylChainShorthandNotation.Default, AlkylChainShorthandNotation.Default, SphingoChainShorthandNotation.Default);

        public ChainShorthandNotation(AcylChainShorthandNotation acyl, AlkylChainShorthandNotation alkyl, SphingoChainShorthandNotation sphingosine) {
            _acyl = acyl ?? throw new ArgumentNullException(nameof(acyl));
            _alkyl = alkyl ?? throw new ArgumentNullException(nameof(alkyl));
            _sphingosine = sphingosine ?? throw new ArgumentNullException(nameof(sphingosine));
        }

        public AcylChain Visit(AcylChain item) {
            return ((IVisitor<AcylChain, AcylChain>)_acyl).Visit(item);
        }

        public AlkylChain Visit(AlkylChain item) {
            return ((IVisitor<AlkylChain, AlkylChain>)_alkyl).Visit(item);
        }

        public SphingoChain Visit(SphingoChain item) {
            return ((IVisitor<SphingoChain, SphingoChain>)_sphingosine).Visit(item);
        }
    }

    internal class AcylChainShorthandNotation : IVisitor<AcylChain, AcylChain>
    {
        private readonly DoubleBondShorthandNotation _doubleBondNotation;
        private readonly OxidizedShorthandNotation _oxidizedNotation;

        public static AcylChainShorthandNotation Default { get; } = new AcylChainShorthandNotation(DoubleBondShorthandNotation.All, OxidizedShorthandNotation.All);

        private AcylChainShorthandNotation(DoubleBondShorthandNotation doubleBondNotation, OxidizedShorthandNotation oxidizedNotation) {
            _doubleBondNotation = doubleBondNotation ?? throw new ArgumentNullException(nameof(doubleBondNotation));
            _oxidizedNotation = oxidizedNotation ?? throw new ArgumentNullException(nameof(oxidizedNotation));
        }

        public AcylChain Visit(AcylChain chain) {
            var db = _doubleBondNotation.Visit(chain.DoubleBond);
            var ox = _oxidizedNotation.Visit(chain.Oxidized);
            if (chain.DoubleBond == db && chain.Oxidized == ox) {
                return chain;
            }
            return new AcylChain(chain.CarbonCount, db, ox);
        }
    }

    internal class AlkylChainShorthandNotation : IVisitor<AlkylChain, AlkylChain>
    {
        private readonly DoubleBondShorthandNotation _doubleBondNotation;
        private readonly OxidizedShorthandNotation _oxidizedNotation;

        public static AlkylChainShorthandNotation Default { get; } = new AlkylChainShorthandNotation(DoubleBondShorthandNotation.AllForPlasmalogen, OxidizedShorthandNotation.All);

        private AlkylChainShorthandNotation(DoubleBondShorthandNotation doubleBondNotation, OxidizedShorthandNotation oxidizedNotation) {
            _doubleBondNotation = doubleBondNotation ?? throw new ArgumentNullException(nameof(doubleBondNotation));
            _oxidizedNotation = oxidizedNotation ?? throw new ArgumentNullException(nameof(oxidizedNotation));
        }

        public AlkylChain Visit(AlkylChain chain) {
            var db = _doubleBondNotation.Visit(chain.DoubleBond);
            var ox = _oxidizedNotation.Visit(chain.Oxidized);
            if (chain.DoubleBond == db && chain.Oxidized == ox) {
                return chain;
            }
            return new AlkylChain(chain.CarbonCount, db, ox);
        }
    }

    internal class SphingoChainShorthandNotation : IVisitor<SphingoChain, SphingoChain>
    {
        private readonly DoubleBondShorthandNotation _doubleBondNotation;
        private readonly OxidizedShorthandNotation _oxidizedNotation;

        public static SphingoChainShorthandNotation Default { get; } = new SphingoChainShorthandNotation(DoubleBondShorthandNotation.All, OxidizedShorthandNotation.AllForCeramide);

        private SphingoChainShorthandNotation(DoubleBondShorthandNotation doubleBondNotation, OxidizedShorthandNotation oxidizedNotation) {
            _doubleBondNotation = doubleBondNotation ?? throw new ArgumentNullException(nameof(doubleBondNotation));
            _oxidizedNotation = oxidizedNotation ?? throw new ArgumentNullException(nameof(oxidizedNotation));
        }

        public SphingoChain Visit(SphingoChain chain) {
            var db = _doubleBondNotation.Visit(chain.DoubleBond);
            var ox = _oxidizedNotation.Visit(chain.Oxidized);
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
