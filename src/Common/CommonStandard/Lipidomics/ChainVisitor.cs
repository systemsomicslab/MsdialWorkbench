using CompMs.Common.DataStructure;
using System;

namespace CompMs.Common.Lipidomics
{
    internal sealed class ChainVisitor : IVisitor<AcylChain, AcylChain>, IVisitor<AlkylChain, AlkylChain>, IVisitor<SphingoChain, SphingoChain> {
        private readonly IVisitor<AcylChain, AcylChain> _acylVisitor;
        private readonly IVisitor<AlkylChain, AlkylChain> _alkylVisitor;
        private readonly IVisitor<SphingoChain, SphingoChain> _sphingosineVisitor;

        public ChainVisitor(IVisitor<AcylChain, AcylChain> acylVisitor, IVisitor<AlkylChain, AlkylChain> alkylVisitor, IVisitor<SphingoChain, SphingoChain> sphingosineVisitor) {
            _acylVisitor = acylVisitor;
            _alkylVisitor = alkylVisitor;
            _sphingosineVisitor = sphingosineVisitor;
        }

        AcylChain IVisitor<AcylChain, AcylChain>.Visit(AcylChain item) => _acylVisitor.Visit(item);
        AlkylChain IVisitor<AlkylChain, AlkylChain>.Visit(AlkylChain item) => _alkylVisitor.Visit(item);
        SphingoChain IVisitor<SphingoChain, SphingoChain>.Visit(SphingoChain item) => _sphingosineVisitor.Visit(item);
    }

    internal sealed class AcylChainVisitor : IVisitor<AcylChain, AcylChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public AcylChainVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor, IVisitor<IOxidized, IOxidized> oxidizedVisitor) {
            _doubleBondVisitor = doubleBondVisitor ?? throw new ArgumentNullException(nameof(doubleBondVisitor));
            _oxidizedVisitor = oxidizedVisitor ?? throw new ArgumentNullException(nameof(oxidizedVisitor));
        }

        public AcylChain Visit(AcylChain item) {
            var db = _doubleBondVisitor.Visit(item.DoubleBond);
            var ox = _oxidizedVisitor.Visit(item.Oxidized);
            if (db.Equals(item.DoubleBond) && ox.Equals(item.Oxidized)) {
                return item;
            }
            return new AcylChain(item.CarbonCount, db, ox);
        }
    }

    internal sealed class AlkylChainVisitor : IVisitor<AlkylChain, AlkylChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public AlkylChainVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor, IVisitor<IOxidized, IOxidized> oxidizedVisitor) {
            _doubleBondVisitor = doubleBondVisitor ?? throw new ArgumentNullException(nameof(doubleBondVisitor));
            _oxidizedVisitor = oxidizedVisitor ?? throw new ArgumentNullException(nameof(oxidizedVisitor));
        }

        public AlkylChain Visit(AlkylChain item) {
            var db = _doubleBondVisitor.Visit(item.DoubleBond);
            var ox = _oxidizedVisitor.Visit(item.Oxidized);
            if (db.Equals(item.DoubleBond) && ox.Equals(item.Oxidized)) {
                return item;
            }
            return new AlkylChain(item.CarbonCount, db, ox);
        }
    }

    internal sealed class SphingosineChainVisitor : IVisitor<SphingoChain, SphingoChain>
    {
        private readonly IVisitor<IDoubleBond, IDoubleBond> _doubleBondVisitor;
        private readonly IVisitor<IOxidized, IOxidized> _oxidizedVisitor;

        public SphingosineChainVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor, IVisitor<IOxidized, IOxidized> oxidizedVisitor) {
            _doubleBondVisitor = doubleBondVisitor ?? throw new ArgumentNullException(nameof(doubleBondVisitor));
            _oxidizedVisitor = oxidizedVisitor ?? throw new ArgumentNullException(nameof(oxidizedVisitor));
        }

        public SphingoChain Visit(SphingoChain item) {
            var db = _doubleBondVisitor.Visit(item.DoubleBond);
            var ox = _oxidizedVisitor.Visit(item.Oxidized);
            if (db.Equals(item.DoubleBond) && ox.Equals(item.Oxidized)) {
                return item;
            }
            return new SphingoChain(item.CarbonCount, db, ox);
        }
    }
}
