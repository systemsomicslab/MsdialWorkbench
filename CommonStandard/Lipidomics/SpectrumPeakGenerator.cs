using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class SpectrumPeakGenerator : ISpectrumPeakGenerator
    {
        private static readonly double CH2 = new[]
       {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private IEnumerable<SpectrumPeak> GetDoubleBondSpectrum(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, double abundance) {
            if (chain.DoubleBond.UnDecidedCount != 0 || chain.CarbonCount == 0) {
                return Enumerable.Empty<SpectrumPeak>();
            }
            var chainLoss = lipid.Mass - chain.Mass - nlMass;
            var diffs = new double[chain.CarbonCount];
            for (int i = 0; i < chain.CarbonCount; i++) {
                diffs[i] = CH2;
            }

            var bondPositions = new List<int>();
            foreach (var bond in chain.DoubleBond.Bonds) {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
                bondPositions.Add(bond.Position);
            }
            for (int i = 1; i < chain.CarbonCount; i++) {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            for (int i = 0; i < chain.CarbonCount - 1; i++) {
                var factor = 1.0;
                if (bondPositions.Contains(i - 1)) { 
                    factor = 3.0;
                }
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), factor * abundance * 0.5, $"{chain} C{i + 1}-H") { SpectrumComment = SpectrumComment.doublebond });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), factor * abundance, $"{chain} C{i + 1}") { SpectrumComment = SpectrumComment.doublebond });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), factor * abundance * 0.5, $"{chain} C{i + 1}+H") { SpectrumComment = SpectrumComment.doublebond });
            }

            return peaks;
        }

        public IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetDoubleBondSpectrum(lipid, acylChain, adduct, nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2, abundance);

        public IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, abundance);
    }
}
