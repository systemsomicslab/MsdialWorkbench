using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class ShortHandGenerator : ITotalChainVariationGenerator
    {
        public IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains) {
            yield return Short(chains);
        }

        public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
            yield return Short(chains);
        }

        private TotalChain Short(SeparatedChains chains) {
            return new TotalChain(
                chains.CarbonCount,
                chains.DoubleBondCount,
                chains.OxidizedCount,
                chains.Chains.OfType<AcylChain>().Count(),
                chains.Chains.OfType<AlkylChain>().Count(),
                chains.Chains.OfType<SphingoChain>().Count()); 
        }

        public IEnumerable<ITotalChain> Separate(TotalChain chain) {
            yield return chain;
        }
    }
}
