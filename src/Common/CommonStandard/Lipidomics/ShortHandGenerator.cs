using System.Collections.Generic;

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

        private ITotalChain Short(ITotalChain chains) {
            return ChainsIndeterminateState.SpeciesLevel.Indeterminate(chains);
        }

        public IEnumerable<ITotalChain> Separate(TotalChain chain) {
            yield return chain;
        }
    }
}
