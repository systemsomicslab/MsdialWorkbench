using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class PCSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H15NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.ProtonMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.ProtonMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 16,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.ProtonMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" && lipid.LipidClass == LbmClass.PC;
        }

        public IMSScanProperty Generate(SubLevelLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPCSpectrum(lipid));
            spectrum = spectrum.OrderBy(peak => peak.Mass).ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        public IMSScanProperty Generate(SomeAcylChainLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPCSpectrum(lipid));
            spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains));
            spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.OfType<SpecificAcylChain>()));
            spectrum = spectrum.OrderBy(peak => peak.Mass).ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        public IMSScanProperty Generate(PositionSpecificAcylChainLipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPCSpectrum(lipid));
            spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains));
            spectrum.AddRange(GetAcylPositionSpectrum(lipid, lipid.Chains[0]));
            spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.OfType<SpecificAcylChain>()));
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.First().Intensity, string.Join(", ", specs.Select(spec => spec.Comment))))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference
            {
                PrecursorMz = lipid.Mass + adduct.AdductIonAccurateMass,
                IonMode = adduct.IonMode,
                Spectrum = spectrum,
                Name = lipid.Name,
                Formula = molecule?.Formula,
                Ontology = molecule?.Ontology,
                // SMILES = molecule?.SMILES,
                // InChIKey = molecule?.InChIKey,
                AdductType = adduct,
                CompoundClass = lipid.LipidClass.ToString(),
                Charge = adduct.ChargeNumber,
            };
        }

        private SpectrumPeak[] GetPCSpectrum(ILipid lipid) {
            return new[] {
                new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(C5H15NO4P, 999d, "Header"),
                new SpectrumPeak(Gly_C, 999d, "Gly-C"),
                new SpectrumPeak(Gly_O, 999d, "Gly-O"),
            };
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IAcylChain> acylChains) {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IAcylChain acylChain) {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            return new[]
            {
                new SpectrumPeak(lipidMass - chainMass + MassDiffDictionary.ProtonMass, 750d, $"-{acylChain}"),
                new SpectrumPeak(lipidMass - chainMass - H2O + MassDiffDictionary.ProtonMass, 750d, $"-{acylChain}-H2O"),
                new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.OxygenMass, 500d, $"-{acylChain}-O"),
            };
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IAcylChain acylChain) {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass;
            return new[]
            {
                new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.OxygenMass - CH2 + MassDiffDictionary.ProtonMass, 500d, "-CH2(Sn1)"),
            };
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<SpecificAcylChain> acylChains) {
            return acylChains.SelectMany(acylChain => GetAcylDoubleBondSpectrum(lipid, acylChain));
        }

        private SpectrumPeak[] GetAcylDoubleBondSpectrum(ILipid lipid, SpecificAcylChain acylChain) {
            var chainLoss = lipid.Mass - acylChain.Mass + MassDiffDictionary.ProtonMass;
            var diffs = new double[acylChain.CarbonCount];
            for (int i = 0; i < acylChain.CarbonCount; i++) {
                diffs[i] = CH2;
            }
            diffs[0] += MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2;
            foreach (var i in acylChain.DoubleBondPosition) {
                diffs[i - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[i] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 1; i < acylChain.CarbonCount; i++) {
                diffs[i] += diffs[i - 1];
            }
            return Enumerable.Range(0, acylChain.CarbonCount - 1)
                .Select(i => new SpectrumPeak(chainLoss + diffs[i], 250d, $"{acylChain} C{i+1}"))
                .ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumComparer();
        class SpectrumComparer : IEqualityComparer<SpectrumPeak>
        {
            private static readonly double EPS = 1e6;
            public bool Equals(SpectrumPeak x, SpectrumPeak y) {
                return Math.Abs(x.Mass - y.Mass) <= EPS;
            }

            public int GetHashCode(SpectrumPeak obj) {
                return Math.Round(obj.Mass, 6).GetHashCode();
            }
        }
    }
}
