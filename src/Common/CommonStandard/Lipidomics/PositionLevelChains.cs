using CompMs.Common.DataStructure;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class PositionLevelChains : SeparatedChains, ITotalChain
    {
        public PositionLevelChains(params IChain[] chains) : base(chains, LipidDescription.Class | LipidDescription.Chain | LipidDescription.SnPosition) {

        }

        public IEnumerable<ITotalChain> GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Product(this);
        }

        public override string ToString() {
            return string.Join("/", Chains.Select(c => c.ToString()));
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            return chains.ChainCount == ChainCount && chains is PositionLevelChains pChains
                && Enumerable.Range(0, ChainCount).All(i => Chains[i].Includes(pChains.Chains[i]));
        }

        public bool Equals(ITotalChain other) {
            return other is PositionLevelChains pChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && Chains.Zip(pChains.Chains, (a, b) => a.Equals(b)).All(p => p);
        }

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, PositionLevelChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }
    }
}
