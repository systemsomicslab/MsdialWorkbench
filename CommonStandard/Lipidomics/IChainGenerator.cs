using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface IChainGenerator
    {
        IEnumerable<ITotalChain> Separate(TotalChain chain);

        IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains);

        IEnumerable<ITotalChain> Product(PositionLevelChains chains);

        IEnumerable<IChain> Generate(AcylChain chain);

        IEnumerable<IChain> Generate(AlkylChain chain);

        IEnumerable<IChain> Generate(SphingoChain chain);
    }
}
