using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public sealed class SpectrumGeneratorUtility {
        private SpectrumGeneratorUtility() { }

        private static readonly double CH2 = new[]
       {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public static IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(
            ILipid lipid, AcylChain acylChain, AdductIon adduct, double NLMass = 0.0) {
            var chainLoss = lipid.Mass - acylChain.Mass + adduct.AdductIonAccurateMass - NLMass;
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
                peaks.Add(new SpectrumPeak(chainLoss + diffs[i], 50d, $"{acylChain} C{i + 1}"));
                peaks.Add(new SpectrumPeak(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass, 25d, $"{acylChain} C{i + 1}-H"));
                peaks.Add(new SpectrumPeak(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass, 25d, $"{acylChain} C{i + 1}+H"));
            }

            return peaks.ToArray();
        }
    }
}
