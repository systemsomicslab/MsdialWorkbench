using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public static class SpectrumGeneratorUtility {
        
        private static readonly double CH2 = new[]
       {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        [Obsolete]
        public static IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(
            ILipid lipid, AcylChain acylChain, AdductIon adduct, double NLMass = 0.0, double abundance = 50.0) {
            if (acylChain.DoubleBond.UnDecidedCount != 0 || acylChain.CarbonCount == 0) {
                return Enumerable.Empty<SpectrumPeak>();
            }
            var chainLoss = lipid.Mass - acylChain.Mass - NLMass;
            var diffs = new double[acylChain.CarbonCount];
            for (int i = 0; i < acylChain.CarbonCount; i++) {
                diffs[i] = CH2;
            }

            diffs[0] += MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2;

            foreach (var bond in acylChain.DoubleBond.Bonds) {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 1; i < acylChain.CarbonCount; i++) {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            for (int i = 0; i < acylChain.CarbonCount - 1; i++) {
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), abundance, $"{acylChain} C{i + 1}"));
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{acylChain} C{i + 1}-H"));
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{acylChain} C{i + 1}+H"));
            }

            return peaks;
        }

        [Obsolete]
        public static IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(
            ILipid lipid, AlkylChain alkylChain, AdductIon adduct, double NLMass = 0.0, double abundance = 50.0)
        {
            var chainLoss = lipid.Mass - alkylChain.Mass - NLMass;
            var diffs = new double[alkylChain.CarbonCount];
            for (int i = 0; i < alkylChain.CarbonCount; i++)
            {
                diffs[i] = CH2;
            }

            foreach (var bond in alkylChain.DoubleBond.Bonds)
            {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 1; i < alkylChain.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            for (int i = 0; i < alkylChain.CarbonCount - 1; i++)
            {
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), abundance, $"{alkylChain} C{i + 1}"));
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{alkylChain} C{i + 1}-H"));
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{alkylChain} C{i + 1}+H"));
            }

            return peaks;
        }
    }
}
