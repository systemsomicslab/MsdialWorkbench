using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public abstract class SeparatedChains
    {
        public SeparatedChains(IChain[] chains, LipidDescription description) {
            Chains = new ReadOnlyCollection<IChain>(chains);           
            if (chains.All(c => c.DoubleBond.UnDecidedCount == 0)) {
                description |= LipidDescription.DoubleBondPosition;
            }
            Description = description;
        }

        public int CarbonCount => Chains.Sum(c => c.CarbonCount);

        public int DoubleBondCount => Chains.Sum(c => c.DoubleBondCount);

        public int OxidizedCount => Chains.Sum(c => c.OxidizedCount);

        public int AcylChainCount => Chains.OfType<AcylChain>().Count();
        public int AlkylChainCount => Chains.OfType<AlkylChain>().Count();
        public int SphingoChainCount => Chains.OfType<SphingoChain>().Count();

        public double Mass => Chains.Sum(c => c.Mass);

        public int ChainCount => Chains.Count;

        public LipidDescription Description { get; }

        public ReadOnlyCollection<IChain> Chains { get; }
    }
}
