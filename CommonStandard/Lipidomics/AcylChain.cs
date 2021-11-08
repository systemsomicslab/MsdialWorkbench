using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IAcylChain {
        int CarbonCount { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        IEnumerable<IAcylChain> GetCandidates(IAcylChainGenerator generator);
    }

    public class TotalAcylChain {
        public TotalAcylChain(int carbonCount, int doubleBondCount, int oxidizedCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }

        public double Mass => CalculateSubLevelMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<AcylChain[]> GetCandidateSets(IAcylChainGenerator generator, int numChain) {
            return generator.Separate(this, numChain);
        }

        public override string ToString() {
            return AcylChainUtility.ToString(CarbonCount, DoubleBondCount, OxidizedCount);
        }

        private static double CalculateSubLevelMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond - 2) * MassDiffDictionary.HydrogenMass + (2 + oxidize) * MassDiffDictionary.OxygenMass;
        }
    }

    public class AcylChain : IAcylChain {
        public AcylChain(int carbonCount, int doubleBondCount, int oxidizedCount) {
            CarbonCount = carbonCount;
            DoubleBondCount = doubleBondCount;
            OxidizedCount = oxidizedCount;
        }

        public int CarbonCount { get; }
        public int DoubleBondCount { get; }
        public int OxidizedCount { get; }
        public double Mass => AcylChainUtility.CalculateMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IAcylChain> GetCandidates(IAcylChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return AcylChainUtility.ToString(CarbonCount, DoubleBondCount, OxidizedCount);
        }
    }

    public class SpecificAcylChain : IAcylChain {
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
        public double Mass => AcylChainUtility.CalculateMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IAcylChain> GetCandidates(IAcylChainGenerator generator) {
            return Enumerable.Empty<IAcylChain>();
        }

        public override string ToString() {
            return AcylChainUtility.ToString(CarbonCount, DoubleBondPosition, OxidizedCount);
        }
    }

    static class AcylChainUtility {
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
            return $"{doubleBonds.Count}(d{string.Join(",", doubleBonds)})";
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

        public static double CalculateMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond - 1) * MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass + (MassDiffDictionary.OxygenMass * oxidize);
        }
    }

}
