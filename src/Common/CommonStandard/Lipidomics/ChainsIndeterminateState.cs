using CompMs.Common.DataStructure;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public sealed class ChainsIndeterminateState : IVisitor<ITotalChain, ITotalChain>
    {
        public static ChainsIndeterminateState SpeciesLevel { get; } = new ChainsIndeterminateState(State.SpeciesLevel);
        public static ChainsIndeterminateState MolecularSpeciesLevel { get; } = new ChainsIndeterminateState(State.MolecularSpeciesLevel);
        public static ChainsIndeterminateState PositionLevel { get; } = new ChainsIndeterminateState(State.PositionLevel);

        private readonly State _state;
        private readonly IIndeterminate _impl;

        private ChainsIndeterminateState(State state) {
            switch (state) {
                case State.SpeciesLevel:
                    _impl = new SpeciesLevelIndeterminate();
                    break;
                case State.MolecularSpeciesLevel:
                    _impl = new MolecularSpeciesLevelIndeterminate();
                    break;
            }
        }

        public ITotalChain Indeterminate(ITotalChain chains) {
            return _impl?.Indeterminate(chains) ?? chains;
        }

        ITotalChain IVisitor<ITotalChain, ITotalChain>.Visit(ITotalChain item) {
            return Indeterminate(item);
        }

        enum State {
            None,
            SpeciesLevel,
            MolecularSpeciesLevel,
            PositionLevel,
        }

        interface IIndeterminate {
            ITotalChain Indeterminate(ITotalChain chains);
        }

        sealed class SpeciesLevelIndeterminate : IIndeterminate {
            public ITotalChain Indeterminate(ITotalChain chains) {
                if ((chains.Description & (LipidDescription.Chain | LipidDescription.SnPosition)) > 0) {
                    return new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, chains.AcylChainCount, chains.AlkylChainCount, chains.SphingoChainCount);
                }
                return chains;
            }
        }

        sealed class MolecularSpeciesLevelIndeterminate : IIndeterminate {
            public ITotalChain Indeterminate(ITotalChain chains) {
                if (chains.Description.HasFlag(LipidDescription.SnPosition) && chains is SeparatedChains sc) {
                    return new MolecularSpeciesLevelChains(sc.GetDeterminedChains());
                }
                return chains;
            }
        }
    }
}
