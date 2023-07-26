using CompMs.Common.DataStructure;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class MolecularSpeciesLevelChains : SeparatedChains, ITotalChain
    {
        private static readonly ChainComparer CHAIN_COMPARER = new ChainComparer();

        public MolecularSpeciesLevelChains(params IChain[] chains) : base(chains.OrderBy(c => c, CHAIN_COMPARER).ToArray(), LipidDescription.Class | LipidDescription.Chain) {

        }

        IChain ITotalChain.GetChain(int snPosition) {
            return null;
        }

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Permutate(this);
        }

        public override string ToString() {
            if (GetAllChains().Count(c => c.CarbonCount > 0) == 1) {
                return GetAllChains().First(c => c.CarbonCount > 0).ToString(); // for LPC...
            }
            return string.Join("_", GetAllChains().Select(c => c.ToString()));
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            if (chains.ChainCount != ChainCount || !(chains is SeparatedChains sChains)) {
                return false;
            }

            var matching = new BipartiteMatching(ChainCount + chains.ChainCount);
            for (int i = 0; i < GetAllChains().Length; i++) {
                for (int j = 0; j < sChains.GetAllChains().Length; j++) {
                    if (GetAllChains()[i].Includes(sChains.GetAllChains()[j])) {
                        matching.AddEdge(i, j + ChainCount);
                    }
                }
            }
            return matching.Match() == ChainCount;
        }

        public override bool Equals(ITotalChain other) {
            return other is MolecularSpeciesLevelChains mChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && GetAllChains().Zip(mChains.GetAllChains(), (a, b) => a.Equals(b)).All(p => p);
        }

        public override TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, MolecularSpeciesLevelChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }

        class ChainComparer : IComparer<IChain> {
            public int Compare(IChain x, IChain y) {
                var priorityx = (TypeToOrder(x), x.DoubleBondCount, x.CarbonCount, x.OxidizedCount);
                var priorityy = (TypeToOrder(y), y.DoubleBondCount, y.CarbonCount, y.OxidizedCount);
                return priorityx.CompareTo(priorityy);
            }

            private int TypeToOrder(IChain x) {
                switch (x) {
                    case SphingoChain _:
                        return 0;
                    case AlkylChain _:
                        return 1;
                    case AcylChain _:
                        return 2;
                    default:
                        return 3;
                }
            }
        }
    }
}
