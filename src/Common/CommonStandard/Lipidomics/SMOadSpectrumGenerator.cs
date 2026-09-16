using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class SMOadSpectrumGenerator : ILipidSpectrumGenerator {

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

        private static readonly double CH3COO = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private static readonly double CH3 = new[] {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double HCO3 = new[] {
            MassDiffDictionary.HydrogenMass,
            MassDiffDictionary.CarbonMass,
            MassDiffDictionary.OxygenMass * 3,
        }.Sum();

        private static readonly double C2H2 = new[] {
            MassDiffDictionary.CarbonMass *2,
            MassDiffDictionary.HydrogenMass * 2,
        }.Sum();

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public SMOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public SMOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+CH3COO]-" ||
                adduct.AdductIonName == "[M+HCO3]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 40.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetSMOadSpectrum(lipid, adduct));
            string[] oadId = new string[] {
                "OAD01",
                "OAD02",
                "OAD02+O",
                "OAD03",
                "OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                "OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                "OAD12",
                //"OAD13",
                "OAD14",
                "OAD15",
                "OAD15+O",
                "OAD16",
                "OAD17",
                "OAD12+O",
                "OAD12+O+H",
                "OAD12+O+2H",
                "OAD01+H",
                "SphOAD",
                "SphOAD+H",
                "SphOAD+2H",
                //"SphOAD-CO"
            };

            if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, abundance, oadId));
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private static SpectrumPeak[] GetSMOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sph) {
                    spectrum.Add(new SpectrumPeak(sph.Mass - 2 * H2O + MassDiffDictionary.HydrogenMass * 2, 50d, $"{sph}-CH4O2") { SpectrumComment = SpectrumComment.acylchain });
                }
            }
            else if (adduct.AdductIonName == "[M+Na]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C5H14NO4P), 100d, "NL of header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H9N), 100d, "NL of C3H9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+CH3COO]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass-CH3COO-CH3), 999d, "Precursor-CH3COO-CH3") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(C5H14NO4P-CH3, 300d, "Characteristic fragment") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains is SeparatedChains) {
                    if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                        spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - CH3COO - CH3 - acyl.Mass + MassDiffDictionary.HydrogenMass), 10d, $"NL of CH3 and {acyl}") { SpectrumComment = SpectrumComment.acylchain });
                    }
                }
            }
            else if (adduct.AdductIonName == "[M+HCO3]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 50d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - HCO3 - MassDiffDictionary.HydrogenMass), 200d, "NL of HCO3") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - HCO3 - MassDiffDictionary.HydrogenMass - C3H9N), 500d, "NL of HCO3 and C3H9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - HCO3 - MassDiffDictionary.HydrogenMass - C3H9N - C2H2), 999d, "NL of HCO3 and C3H9N and C2H2") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(C5H14NO4P - MassDiffDictionary.HydrogenMass, 500d, "C3H9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - HCO3 - acyl.Mass - C2H2 - MassDiffDictionary.HydrogenMass*6), 10d, $"-{acyl} - C2H8") { SpectrumComment = SpectrumComment.acylchain });
                }
            }
            return spectrum.ToArray();
        }

        private static MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference {
                PrecursorMz = adduct.ConvertToMz(lipid.Mass),
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
        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}

