using CompMs.Common.DataStructure;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    internal sealed class ChainsDecomposer : IDecomposer<ITotalChain, TotalChain>, IDecomposer<ITotalChain, MolecularSpeciesLevelChains>, IDecomposer<ITotalChain, PositionLevelChains>
    {
        ITotalChain IDecomposer<ITotalChain, TotalChain>.Decompose<T>(IAcyclicVisitor visitor, T element) {
            return element;
        }

        ITotalChain IDecomposer<ITotalChain, MolecularSpeciesLevelChains>.Decompose<T>(IAcyclicVisitor visitor, T element) {
            var chains = element.GetDeterminedChains().Select(c => c.Accept(visitor, IdentityDecomposer<IChain, IChain>.Instance)).ToArray();
            return new MolecularSpeciesLevelChains(chains);
        }

        ITotalChain IDecomposer<ITotalChain, PositionLevelChains>.Decompose<T>(IAcyclicVisitor visitor, T element) {
            var chains = element.GetDeterminedChains().Select(c => c.Accept(visitor, IdentityDecomposer<IChain, IChain>.Instance)).ToArray();
            return new PositionLevelChains(chains);
        }
    }
}
