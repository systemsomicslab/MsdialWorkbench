using CompMs.Common.DataStructure;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ITotalChain : IEquatable<ITotalChain>, IVisitableElement
    {
        int CarbonCount { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        int ChainCount { get; }
        int AcylChainCount { get; }
        int AlkylChainCount { get; }
        int SphingoChainCount { get; }
        double Mass { get; }
        LipidDescription Description { get; }

        bool Includes(ITotalChain chains);
        IEnumerable<ITotalChain> GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator);
    }

    public static class TotalChainExtension
    {
        public static ITotalChain GetChains(this LipidMolecule lipid) {
            var prop = LipidClassDictionary.Default.LbmItems[lipid.LipidClass];
            switch (lipid.AnnotationLevel) {
                case 1:
                    return new TotalChain(lipid.TotalCarbonCount, lipid.TotalDoubleBondCount, lipid.TotalOxidizedCount, acylChainCount: prop.AcylChain, alkylChainCount: prop.AlkylChain, sphingoChainCount: prop.SphingoChain);
                case 2:
                case 3:
                    return new MolecularSpeciesLevelChains(GetEachChains(lipid, prop));
                default:
                    break;
            }
            return default;
        }

        private static IChain[] GetEachChains(LipidMolecule lipid, LipidClassProperty prop) {
            var chains = new IChain[prop.TotalChain];
            if (prop.TotalChain >= 1) {
                if (prop.SphingoChain >= 1) {
                    chains[0] = new SphingoChain(lipid.Sn1CarbonCount, new DoubleBond(lipid.Sn1DoubleBondCount), new Oxidized(lipid.Sn1Oxidizedount));
                    // chains[0] = SphingoParser.Parse(lipid.Sn1AcylChainString);
                }
                else if (prop.AlkylChain >= 1) {
                    chains[0] = new AlkylChain(lipid.Sn1CarbonCount, new DoubleBond(lipid.Sn1DoubleBondCount), new Oxidized(lipid.Sn1Oxidizedount));
                }
                else {
                    chains[0] = new AcylChain(lipid.Sn1CarbonCount, new DoubleBond(lipid.Sn1DoubleBondCount), new Oxidized(lipid.Sn1Oxidizedount));
                }
            }
            if (prop.TotalChain >= 2) {
                chains[1] = new AcylChain(lipid.Sn2CarbonCount, new DoubleBond(lipid.Sn2DoubleBondCount), new Oxidized(lipid.Sn2Oxidizedount));
            }
            if (prop.TotalChain >= 3) {
                chains[2] = new AcylChain(lipid.Sn3CarbonCount, new DoubleBond(lipid.Sn3DoubleBondCount), new Oxidized(lipid.Sn3Oxidizedount));
            }
            if (prop.TotalChain >= 4) {
                chains[3] = new AcylChain(lipid.Sn4CarbonCount, new DoubleBond(lipid.Sn4DoubleBondCount), new Oxidized(lipid.Sn4Oxidizedount));
            }
            return chains;
        }
    }

    public class TotalChain : ITotalChain {
        public TotalChain(int carbonCount, int doubleBondCount, int oxidizedCount, int acylChainCount, int alkylChainCount, int sphingoChainCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
            AcylChainCount = acylChainCount;
            AlkylChainCount = alkylChainCount;
            SphingoChainCount = sphingoChainCount;
            Description = LipidDescription.Class;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public int ChainCount => AcylChainCount + AlkylChainCount + SphingoChainCount;
        public int AcylChainCount { get; }
        public int AlkylChainCount { get; }
        public int SphingoChainCount { get; }

        public LipidDescription Description { get; }

        public double Mass => CalculateSubLevelMass(CarbonCount, DoubleBondCount, OxidizedCount, ChainCount, AcylChainCount, AlkylChainCount, SphingoChainCount);

        private static double CalculateSubLevelMass(int carbon, int doubleBond, int oxidize, int chain, int acyl, int alkyl, int sphingo) {
            var carbonGain = carbon * MassDiffDictionary.CarbonMass;
            var hydrogenGain = (2 * carbon - 2 * doubleBond + chain) * MassDiffDictionary.HydrogenMass;
            var oxygenGain = oxidize * MassDiffDictionary.OxygenMass;
            var acylGain = acyl * AcylGain;
            var alkylGain = alkyl * AlkylGain;
            var sphingoGain = sphingo * SphingoGain;
            var result = carbonGain + hydrogenGain + oxygenGain + acylGain + alkylGain + sphingoGain;
            return result;
        }

        private static readonly double AcylGain = MassDiffDictionary.OxygenMass - 2 * MassDiffDictionary.HydrogenMass;

        private static readonly double AlkylGain = 0d;

        private static readonly double SphingoGain = MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass;

        bool ITotalChain.Includes(ITotalChain chains) {
            return CarbonCount == chains.CarbonCount
                && DoubleBondCount == chains.DoubleBondCount
                && OxidizedCount == chains.OxidizedCount;
        }

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Separate(this);
        }

        public override string ToString() {
            return string.Format("{0}{1}:{2}{3}", EtherSymbol(AlkylChainCount), CarbonCount, DoubleBondCount, OxidizeSymbol(OxidizedCount));
        }

        private static string EtherSymbol(int ether) {
            switch (ether) {
                case 0:
                    return "";
                case 2:
                    return "dO-";
                case 4:
                    return "eO-";
                case 1:
                default:
                    return "O-";
            }
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

        public bool Equals(ITotalChain other) {
            return other is TotalChain tChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && AcylChainCount == tChains.AcylChainCount
                && AlkylChainCount == tChains.AlkylChainCount
                && SphingoChainCount == tChains.SphingoChainCount;
        }

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, TotalChain> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }
    }

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

    public class MolecularSpeciesLevelChains : SeparatedChains, ITotalChain
    {
        private static readonly ChainComparer CHAIN_COMPARER = new ChainComparer();

        public MolecularSpeciesLevelChains(params IChain[] chains) : base(chains.OrderBy(c => c, CHAIN_COMPARER).ToArray(), LipidDescription.Class | LipidDescription.Chain) {

        }

        public IEnumerable<ITotalChain> GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Permutate(this);
        }

        public override string ToString() {
            if (Chains.Count(c => c.CarbonCount > 0) == 1) {
                return Chains.First(c => c.CarbonCount > 0).ToString(); // for LPC...
            }
            return string.Join("_", Chains.Select(c => c.ToString()));
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            if (chains.ChainCount != ChainCount || !(chains is SeparatedChains sChains)) {
                return false;
            }

            var matching = new BipartiteMatching(ChainCount + chains.ChainCount);
            for (int i = 0; i < Chains.Count; i++) {
                for (int j = 0; j < sChains.Chains.Count; j++) {
                    if (Chains[i].Includes(sChains.Chains[j])) {
                        matching.AddEdge(i, j + ChainCount);
                    }
                }
            }
            return matching.Match() == ChainCount;
        }

        public bool Equals(ITotalChain other) {
            return other is MolecularSpeciesLevelChains mChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && Chains.Zip(mChains.Chains, (a, b) => a.Equals(b)).All(p => p);
        }

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, MolecularSpeciesLevelChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }

        class ChainComparer : IComparer<IChain> {
            public int Compare(IChain x, IChain y) {
                var priorityx = (TypeToOrder(x), x.DoubleBondCount, x.CarbonCount, x.OxidizedCount);
                var priorityy = (TypeToOrder(y), y.DoubleBondCount, y.CarbonCount, y.OxidizedCount);
                return priorityx.CompareTo(priorityy);
            }

            private int TypeToOrder(IChain x) {
                switch (x) {
                    case SphingoChain _:
                        return 0;
                    case AlkylChain _:
                        return 1;
                    case AcylChain _:
                        return 2;
                    default:
                        return 3;
                }
            }
        }
    }

    public class PositionLevelChains : SeparatedChains, ITotalChain
    {
        public PositionLevelChains(params IChain[] chains) : base(chains, LipidDescription.Class | LipidDescription.Chain | LipidDescription.SnPosition) {

        }

        public IEnumerable<ITotalChain> GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            return totalChainGenerator.Product(this);
        }

        public override string ToString() {
            return string.Join("/", Chains.Select(c => c.ToString()));
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            return chains.ChainCount == ChainCount && chains is PositionLevelChains pChains
                && Enumerable.Range(0, ChainCount).All(i => Chains[i].Includes(pChains.Chains[i]));
        }

        public bool Equals(ITotalChain other) {
            return other is PositionLevelChains pChains
                && ChainCount == other.ChainCount
                && CarbonCount == other.CarbonCount
                && DoubleBondCount == other.DoubleBondCount
                && OxidizedCount == other.OxidizedCount
                && Description == other.Description
                && Chains.Zip(pChains.Chains, (a, b) => a.Equals(b)).All(p => p);
        }

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, PositionLevelChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }
    }
}
