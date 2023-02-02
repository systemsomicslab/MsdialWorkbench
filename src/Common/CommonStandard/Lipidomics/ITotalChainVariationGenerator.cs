using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface ITotalChainVariationGenerator
    {
        IEnumerable<ITotalChain> Separate(TotalChain chain);

        IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains);

        IEnumerable<ITotalChain> Product(PositionLevelChains chains);
    }
}
