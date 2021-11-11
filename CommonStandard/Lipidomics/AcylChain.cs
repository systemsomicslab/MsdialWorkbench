using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IChain {
        int CarbonCount { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        IEnumerable<IChain> GetCandidates(IChainGenerator generator);
    }

    public interface ITotalChain
    {
        int CarbonCount { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator);
    }

    public enum DoubleBondState
    {
        Unknown, E, Z,
    }

    public interface IDoubleBondInfo
    {
        int Position { get; }
        DoubleBondState State { get; }
    }

    public class DoubleBondInfo : IDoubleBondInfo
    {
        private DoubleBondInfo(int position, DoubleBondState state) {
            Position = position;
            State = state;
        }

        public int Position { get; }
        public DoubleBondState State { get; }

        static readonly Dictionary<(int, DoubleBondState), DoubleBondInfo> cache = new Dictionary<(int, DoubleBondState), DoubleBondInfo>();

        public static DoubleBondInfo Create(int position, DoubleBondState state = DoubleBondState.Unknown) {
            return cache.TryGetValue((position, state), out var info)
                ? info
                : (cache[(position, state)] = new DoubleBondInfo(position, state));
        }

        public static DoubleBondInfo E(int position) => Create(position, DoubleBondState.E);

        public static DoubleBondInfo Z(int position) => Create(position, DoubleBondState.Z);
    }

    public interface IDoubleBond
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }
    }

    public class DoubleBond : IDoubleBond
    {
        public DoubleBond(int count, params IDoubleBondInfo[] bonds) {
            Count = count;
            Bonds = new ReadOnlyCollection<IDoubleBondInfo>(bonds);
        }

        public DoubleBond(params IDoubleBondInfo[] bonds) : this(bonds.Length, bonds) {

        }

        public int Count { get; }

        public int DecidedCount => Bonds.Count;

        public int UnDecidedCount => Count - DecidedCount;

        public ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }

        public static DoubleBond CreateFromPosition(params int[] positions) {
            return new DoubleBond(positions.Length, positions.Select(p => DoubleBondInfo.Create(p)).ToArray());
        }
    }

    public interface IOxidized
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<int> Oxidises { get; }
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

        public IEnumerable<IChain[]> GetCandidateSets(IChainGenerator generator) {
            return generator.Separate(this, ChainCount);
        }

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(IChainGenerator generator) {
            return generator.Separate(this);
        }

        public override string ToString() {
            return ChainUtility.ToString(CarbonCount, DoubleBondCount, OxidizedCount);
        }

        private static double CalculateSubLevelMass(int carbon, int doubleBond, int oxidize, int chain, int alkyl) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * (carbon - chain + alkyl - doubleBond) + 2) * MassDiffDictionary.HydrogenMass + (chain - alkyl + oxidize) * MassDiffDictionary.OxygenMass;
        }
    }

    public class MolecularSpeciesLevelChains : ITotalChain
    {
        public MolecularSpeciesLevelChains(params IChain[] chains) {
            Chains = new ReadOnlyCollection<IChain>(chains);
        }

        public int CarbonCount => Chains.Sum(c => c.CarbonCount);

        public int DoubleBondCount => Chains.Sum(c => c.DoubleBondCount);

        public int OxidizedCount => Chains.Sum(c => c.OxidizedCount);

        public double Mass => Chains.Sum(c => c.Mass);

        public ReadOnlyCollection<IChain> Chains { get; }

        public IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator) {
            return generator.Permutate(this);
        }

        public override string ToString() {
            return string.Join("_", Chains.Select(c => c.ToString()));
        }
    }

    public class PositionLevelChains : ITotalChain
    {
        public PositionLevelChains(params IChain[] chains) {
            Chains = new ReadOnlyCollection<IChain>(chains);
        }

        public int CarbonCount => Chains.Sum(c => c.CarbonCount);

        public int DoubleBondCount => Chains.Sum(c => c.DoubleBondCount);

        public int OxidizedCount => Chains.Sum(c => c.OxidizedCount);

        public double Mass => Chains.Sum(c => c.Mass);

        public ReadOnlyCollection<IChain> Chains { get; }

        public IEnumerable<ITotalChain> GetCandidateSets(IChainGenerator generator) {
            return generator.Product(this);
        }

        public override string ToString() {
            return string.Join("/", Chains.Select(c => c.ToString()));
        }
    }

    public class AcylChain : IChain {
        public AcylChain(int carbonCount, int doubleBondCount, int oxidizedCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public double Mass => ChainUtility.CalculateAcylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return ChainUtility.ToString(CarbonCount, DoubleBondCount, OxidizedCount);
        }
    }

    public class SpecificAcylChain : IChain {
        public SpecificAcylChain(int carbonCount, List<int> doubleBondPosition, int oxidizedCount) {
            CarbonCount = carbonCount;
            this.doubleBondPosition = doubleBondPosition;
            DoubleBondPosition = this.doubleBondPosition.AsReadOnly();
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public ReadOnlyCollection<int> DoubleBondPosition { get; } // delta (from keton carbon)
        private List<int> doubleBondPosition;
        public int DoubleBondCount => DoubleBondPosition.Count;
        public int OxidizedCount { get; }
        public double Mass => ChainUtility.CalculateAcylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return Enumerable.Empty<IChain>();
        }

        public override string ToString() {
            return ChainUtility.ToString(CarbonCount, DoubleBondPosition, OxidizedCount);
        }
    }

    public class AlkylChain : IChain
    {
        public AlkylChain(int carbonCount, int doubleBondCount, int oxidizedCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public double Mass => ChainUtility.CalculateAlkylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return "O-" + ChainUtility.ToString(CarbonCount, DoubleBondCount, OxidizedCount);
        }
    }

    public class PlasmalogenAlkylChain : IChain
    {
        public PlasmalogenAlkylChain(int carbonCount, int doubleBondCount, int oxidizedCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount + 1;
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public double Mass => ChainUtility.CalculateAlkylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return "P-" + ChainUtility.ToString(CarbonCount, DoubleBondCount - 1, OxidizedCount);
        }
    }

    public class SpecificAlkylChain : IChain
    {
        public SpecificAlkylChain(int carbonCount, List<int> doubleBondPosition, int oxidizedCount) {
            CarbonCount = carbonCount;
            this.doubleBondPosition = doubleBondPosition;
            DoubleBondPosition = this.doubleBondPosition.AsReadOnly();
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public ReadOnlyCollection<int> DoubleBondPosition { get; }
        private List<int> doubleBondPosition;
        public int DoubleBondCount => DoubleBondPosition.Count;
        public int OxidizedCount { get; }
        public double Mass => ChainUtility.CalculateAlkylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return Enumerable.Empty<IChain>();
        }

        public override string ToString() {
            return ChainUtility.ToString(CarbonCount, DoubleBondPosition, OxidizedCount);
        }
    }

    static class ChainUtility {
        public static string ToString(int carbon, int doubleBond, int oxidize) {
            return string.Format("{0}:{1}{2}", carbon, doubleBond, OxidizeSymbol(oxidize));
        }

        public static string ToString(int carbon, IReadOnlyCollection<int> doubleBonds, int oxidize) {
            return string.Format("{0}:{1}{2}", carbon, DoubleBondSymbol(doubleBonds), OxidizeSymbol(oxidize));
        }

        private static string DoubleBondSymbol(IReadOnlyCollection<int> doubleBonds) {
            if (doubleBonds.Count == 0) {
                return "0";
            }
            return $"{doubleBonds.Count}({string.Join(",", doubleBonds)})";
        }

        private static string OxidizeSymbol(int oxidize) {
            if (oxidize == 0) {
                return "";
            }
            if (oxidize == 1) {
                return ";O";
            }
            return $";{oxidize}O";
        }

        public static double CalculateAcylMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond - 1) * MassDiffDictionary.HydrogenMass + (1 + oxidize) * MassDiffDictionary.OxygenMass;
        }

        public static double CalculateAlkylMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond + 1) * MassDiffDictionary.HydrogenMass + oxidize * MassDiffDictionary.OxygenMass;
        }
    }

}
