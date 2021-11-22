using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IChain {
        int CarbonCount { get; }
        IDoubleBond DoubleBond { get; }
        IOxidized Oxidized { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        IEnumerable<IChain> GetCandidates(IChainGenerator generator);
    }

    public class AcylChain : IChain
    {
        public AcylChain(int carbonCount, IDoubleBond doubleBond, IOxidized oxidized) {
            CarbonCount = carbonCount;
            DoubleBond = doubleBond;
            Oxidized = oxidized;
        }

        public IDoubleBond DoubleBond { get; }

        public IOxidized Oxidized { get; }

        public int CarbonCount { get; }

        public int DoubleBondCount => DoubleBond.Count;

        public int OxidizedCount => Oxidized.Count;

        public double Mass => CalculateAcylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return $"{CarbonCount}:{DoubleBond.ToStringAsAcyl()}{Oxidized}";
        }

        static double CalculateAcylMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond - 1) * MassDiffDictionary.HydrogenMass + (1 + oxidize) * MassDiffDictionary.OxygenMass;
        }
    }

    public class AlkylChain : IChain
    {
        public AlkylChain(int carbonCount, IDoubleBond doubleBond, IOxidized oxidized) {
            CarbonCount = carbonCount;
            DoubleBond = doubleBond;
            Oxidized = oxidized;
        }
        public IDoubleBond DoubleBond { get; }

        public IOxidized Oxidized { get; }

        public int CarbonCount { get; }

        public int DoubleBondCount => DoubleBond.Count;

        public int OxidizedCount => Oxidized.Count;

        public double Mass => CalculateAlkylMass(CarbonCount, DoubleBondCount, OxidizedCount);

        static double CalculateAlkylMass(int carbon, int doubleBond, int oxidize) {
            return carbon * MassDiffDictionary.CarbonMass + (2 * carbon - 2 * doubleBond + 1) * MassDiffDictionary.HydrogenMass + oxidize * MassDiffDictionary.OxygenMass;
        }

        public IEnumerable<IChain> GetCandidates(IChainGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return (DoubleBond.Bonds.Any(b => b.Position == 1) ? "P-" : "O-") + $"{CarbonCount}:{DoubleBond.ToStringAsAlkyl()}{Oxidized}";
        }
    }
}
