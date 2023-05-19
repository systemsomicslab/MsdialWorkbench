using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal class ChainDecomposer<TResult> : IDecomposer<TResult, IChain>
    {
        public TResult Decompose(IAcyclicVisitor visitor, IChain element) {
            if (visitor is IVisitor<TResult, (IChain, IDoubleBond, IOxidized)> chainVisitor) {
                var db = element.Accept(visitor, DB_DECOMPOSER) ?? element.DoubleBond;
                var ox = element.Accept(visitor, OX_DECOMPOSER) ?? element.Oxidized;
                return chainVisitor.Visit((element, db, ox));
            }
            return default;
        }

        private static readonly DoubleBondDecomposer DB_DECOMPOSER = new DoubleBondDecomposer();
        private static readonly OxidizedDecomposer OX_DECOMPOSER = new OxidizedDecomposer();

        sealed class DoubleBondDecomposer : IDecomposer<IDoubleBond, IChain> {
            public IDoubleBond Decompose(IAcyclicVisitor visitor, IChain element) {
                if (visitor is IVisitor<IDoubleBond, IDoubleBond> dbVisitor) {
                    return dbVisitor.Visit(element.DoubleBond);
                }
                return default;
            }
        }

        sealed class OxidizedDecomposer : IDecomposer<IOxidized, IChain> {
            public IOxidized Decompose(IAcyclicVisitor visitor, IChain element) {
                if (visitor is IVisitor<IOxidized, IOxidized> dbVisitor) {
                    return dbVisitor.Visit(element.Oxidized);
                }
                return default;
            }
        }
    }
}
