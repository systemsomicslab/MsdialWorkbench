using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal class ChainDecomposer<TResult> : IDecomposer<TResult, IChain, (IChain, IDoubleBond, IOxidized)>, IDecomposer<IDoubleBond, IChain, IDoubleBond>, IDecomposer<IOxidized, IChain, IOxidized>
    {
        public TResult Decompose(IAcyclicVisitor visitor, IChain element) {
            if (visitor is IVisitor<TResult, (IChain, IDoubleBond, IOxidized)> chainVisitor) {
                var db = element.Accept(visitor, (IDecomposer<IDoubleBond, IChain, IDoubleBond>)this) ?? element.DoubleBond;
                var ox = element.Accept(visitor, (IDecomposer<IOxidized, IChain, IOxidized>)this) ?? element.Oxidized;
                return chainVisitor.Visit((element, db, ox));
            }
            return default;
        }

        IDoubleBond IDecomposer<IDoubleBond, IChain, IDoubleBond>.Decompose(IAcyclicVisitor visitor, IChain element) {
            if (visitor is IVisitor<IDoubleBond, IDoubleBond> dbVisitor) {
                return dbVisitor.Visit(element.DoubleBond);
            }
            return default;
        }

        IOxidized IDecomposer<IOxidized, IChain, IOxidized>.Decompose(IAcyclicVisitor visitor, IChain element) {
            if (visitor is IVisitor<IOxidized, IOxidized> dbVisitor) {
                return dbVisitor.Visit(element.Oxidized);
            }
            return default;
        }
    }
}
