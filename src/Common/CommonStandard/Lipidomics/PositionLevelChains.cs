using CompMs.Common.DataStructure;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class PositionLevelChains : SeparatedChains, ITotalChain
    {
        private readonly IChain[] _chains;

        public PositionLevelChains(params IChain[] chains) : base(chains.Select((c, i) => (c, i)).ToArray(), LipidDescription.Class | LipidDescription.Chain | LipidDescription.SnPosition) {
            _chains = chains;
        }

        IChain ITotalChain.GetChain(int snPosition) {
            if (snPosition > _chains.Length) {
                return null;
            }
            return _chains[snPosition - 1];
        }

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Product(this);
        }

        public override string ToString() {
            return string.Join("/", GetAllChains().Select(c => c.ToString()));
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            return chains.ChainCount == ChainCount && chains is PositionLevelChains pChains
                && Enumerable.Range(0, ChainCount).All(i => GetAllChains()[i].Includes(pChains.GetAllChains()[i]));
        }

        public override bool Equals(ITotalChain other) {
            return other is PositionLevelChains pChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && GetAllChains().Zip(pChains.GetAllChains(), (a, b) => a.Equals(b)).All(p => p);
        }

        public override TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, PositionLevelChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }
    }
}
