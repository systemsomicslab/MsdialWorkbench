using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class PCOadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double C5H14NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH3 = new[] {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.CarbonMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public PCOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public PCOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+HCOO]-" ||
                adduct.AdductIonName == "[M+CH3COO]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = adduct.IonMode == IonMode.Positive ? 40.0:20.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPCOadSpectrum(lipid, adduct));
            string[] oadId =
                adduct.IonMode == IonMode.Positive ?
                new string[] {
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
                "OAD01+H"  } :
            new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                "OAD03",
                "OAD04",
                "OAD05",
                //"OAD06",
                //"OAD07",
                //"OAD08",
                //"OAD09",
                "OAD10",
                //"OAD11",
                //"OAD12",
                "OAD13",
                //"OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                //"OAD17",
                "OAD12+O",
                "OAD12+O+H",
                "OAD12+O+2H",
                //"OAD01+H" 
            };
 
            if (lipid.Chains is MolecularSpeciesLevelChains) {
                foreach (AcylChain chain in lipid.Chains.GetDeterminedChains()) {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, nlMass, abundance, oadId));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private static SpectrumPeak[] GetPCOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 999d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
                if (lipid.Chains is SeparatedChains) {
                    foreach (AcylChain chain in lipid.Chains.GetDeterminedChains().Cast<AcylChain>()) {
                        spectrum.AddRange(
                            new[] {
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass), 30d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass-H2O), 15d, $"-{chain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass*2), 10d, $"-{chain}+H") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass*2-H2O), 10d, $"-{chain}-H2O +H") { SpectrumComment = SpectrumComment.acylchain },
                            }
                        );
                    }
                }
            }
            else if (adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(lipid.Mass-CH3, 100d, "[M-CH3]-") { SpectrumComment = SpectrumComment.metaboliteclass, },
                    }
                );
                if (lipid.Chains is SeparatedChains) {
                    foreach (AcylChain chain in lipid.Chains.GetDeterminedChains()) {
                        spectrum.AddRange(
                            new[] {
                                new SpectrumPeak(chain.Mass+MassDiffDictionary.OxygenMass+Electron, 30d, $"{chain} FA") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(chain.Mass+MassDiffDictionary.OxygenMass+Electron+MassDiffDictionary.HydrogenMass, 10d, $"{chain} FA +H") { SpectrumComment = SpectrumComment.acylchain },
                            }
                        );
                    }
                }
            } else {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                    }
                );
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

