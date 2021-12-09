using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ITotalChain
    {
        int CarbonCount { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        int ChainCount { get; }
        double Mass { get; }

        IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator);
    }

    public class TotalChains : ITotalChain {
        public TotalChains(int carbonCount, int doubleBondCount, int oxidizedCount, int chainCount, int alkylChainCount = 0) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
            ChainCount = chainCount;
            AlkylChainCount = alkylChainCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public int ChainCount { get; }
        public int AlkylChainCount { get; }

        public double Mass => CalculateSubLevelMass(CarbonCount, DoubleBondCount, OxidizedCount, ChainCount, AlkylChainCount);

        private static double CalculateSubLevelMass(int carbon, int doubleBond, int oxidize, int chain, int alkyl) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * (carbon - chain + alkyl - doubleBond) + chain) * MassDiffDictionary.HydrogenMass + (chain - alkyl + oxidize) * MassDiffDictionary.OxygenMass;
        }

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(IChainGenerator generator) {
            return generator.Separate(this);
        }

        public override string ToString() {
            return string.Format("{0}:{1}{2}", CarbonCount, DoubleBondCount, OxidizeSymbol(OxidizedCount));
        }

        private static string OxidizeSymbol(int oxidize) {
            if (oxidize == 0) {
                return "";
            }
            if (oxidize == 1) {
                return ";O";
            }
            return $";O{oxidize}";
        }
    }

    public abstract class SeparatedChains
    {
        public SeparatedChains(IChain[] chains) {
            Chains = new ReadOnlyCollection<IChain>(chains);           
        }

        public int CarbonCount => Chains.Sum(c => c.CarbonCount);

        public int DoubleBondCount => Chains.Sum(c => c.DoubleBondCount);

        public int OxidizedCount => Chains.Sum(c => c.OxidizedCount);

        public double Mass => Chains.Sum(c => c.Mass);

        public int ChainCount => Chains.Count;

        public ReadOnlyCollection<IChain> Chains { get; }
    }

    public class MolecularSpeciesLevelChains : SeparatedChains, ITotalChain
    {
        public MolecularSpeciesLevelChains(params IChain[] chains) : base(chains) {

        }

        public IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator) {
            return generator.Permutate(this);
        }

        public override string ToString() {
            return string.Join("_", Chains.Select(c => c.ToString()));
        }
    }

    public class PositionLevelChains : SeparatedChains, ITotalChain
    {
        public PositionLevelChains(params IChain[] chains) : base(chains) {

        }

        public IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator) {
            return generator.Product(this);
        }

        public override string ToString() {
            return string.Join("/", Chains.Select(c => c.ToString()));
        }
    }
}
