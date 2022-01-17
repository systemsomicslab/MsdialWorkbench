using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class EtherPCSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H14NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H9N = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double C5H11N = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 11,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 16,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
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
            if (lipid.LipidClass == LbmClass.EtherPC)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetEtherPCSpectrum(lipid,adduct));
            if (lipid.Chains is PositionLevelChains plChains) {
                spectrum.AddRange(GetAlkylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                if (plChains.Chains[0] is AlkylChain alkyl) {
                    if (alkyl.DoubleBond.Bonds.Any(b => b.Position == 1)) {
                        spectrum.AddRange(GetEtherPCPSpectrum(lipid, alkyl, plChains.Chains[1], adduct));
                    }
                    else
                    {
                        spectrum.AddRange(GetEtherPCOSpectrum(lipid, plChains.Chains[0], plChains.Chains[1], adduct));
                    }
                    spectrum.AddRange(GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct));
                }
                if (plChains.Chains[1] is AcylChain acyl) {
                    spectrum.AddRange(SpectrumGeneratorUtility.GetAcylDoubleBondSpectrum(lipid, acyl, adduct));
                }
            }
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
                SMILES = molecule?.SMILES,
                InChIKey = molecule?.InChIKey,
                AdductType = adduct,
                CompoundClass = lipid.LipidClass.ToString(),
                Charge = adduct.ChargeNumber,
            };
        }

        private SpectrumPeak[] GetEtherPCSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipid.Mass + adduct.AdductIonAccurateMass, 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(Gly_C + adduct.AdductIonAccurateMass, 400d, "Gly-C"),
                new SpectrumPeak(Gly_O + adduct.AdductIonAccurateMass, 400d, "Gly-O"),
                new SpectrumPeak(C5H14NO4P + adduct.AdductIonAccurateMass, 500d, "Header"),
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(lipid.Mass - C3H9N + adduct.AdductIonAccurateMass, 500d, "Precursor -C3H9N"),
                        new SpectrumPeak(lipid.Mass - C5H11N + adduct.AdductIonAccurateMass, 200d, "Precursor -C5H11N"),
                    }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetEtherPCPSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass+ adduct.AdductIonAccurateMass;
            return new[]
            {
                //new SpectrumPeak(lipidMass - alkylChain.Mass, 100d, $"-{alkylChain}"),
                //new SpectrumPeak(lipidMass - acylChain.Mass, 100d, $"-{acylChain}"),
                new SpectrumPeak(lipidMass - alkylChain.Mass - MassDiffDictionary.OxygenMass,100, $"-{alkylChain}-O"),
                new SpectrumPeak(lipidMass - acylChain.Mass - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass, 200d, $"-{acylChain}-O"),
            };
        }

        private SpectrumPeak[] GetEtherPCOSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass + adduct.AdductIonAccurateMass;
            return new[]
            {
                new SpectrumPeak(lipidMass - alkylChain.Mass, 100d, $"-{alkylChain}"),
                new SpectrumPeak(lipidMass - acylChain.Mass + MassDiffDictionary.HydrogenMass, 100d, $"-{acylChain}"),
                new SpectrumPeak(lipidMass - alkylChain.Mass - MassDiffDictionary.OxygenMass, 100d, $"-{alkylChain}-O"),
                new SpectrumPeak(lipidMass - acylChain.Mass - H2O, 100d, $"-{acylChain}-O"),
            };
        }

        private SpectrumPeak[] GetAlkylPositionSpectrum(ILipid lipid, IChain alkylChain, AdductIon adduct) {
            return new[]
            {
                new SpectrumPeak(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass - CH2 + adduct.AdductIonAccurateMass, 150d, "-CH2(Sn1)"),
            };
        }

        private IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain alkylChain,AdductIon adduct) {
            var chainLoss = lipid.Mass - alkylChain.Mass + adduct.AdductIonAccurateMass;
            var diffs = new double[alkylChain.CarbonCount];
            for (int i = 0; i < alkylChain.CarbonCount; i++) {
                diffs[i] = CH2;
            }
            foreach (var bond in alkylChain.DoubleBond.Bonds) {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 1; i < alkylChain.CarbonCount; i++) {
                diffs[i] += diffs[i - 1];
            }
            return diffs.Take(alkylChain.CarbonCount - 1)
                .Select((diff, i) => new SpectrumPeak(chainLoss + diff, 50d, $"{alkylChain} C{i + 1}"));
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
