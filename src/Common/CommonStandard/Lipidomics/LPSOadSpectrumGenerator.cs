using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    internal class LPSOadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double C3H8NO6P = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double CHO2 = new[] {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 1,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private static readonly double C3H5NO2 = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double C3H9O6P = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public LPSOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public LPSOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M-H]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 30;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetLPSOadSpectrum(lipid, adduct));
            string[] oadId =
                new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                "OAD03",
                "OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                //"OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                "OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                "OAD17",
                "OAD12+O",
                "OAD12+O+H",
                //"OAD12+O+2H",
                "OAD01+H"
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

        private static SpectrumPeak[] GetLPSOadSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C3H8NO6P), 100d, "Header"){ SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H8NO6P), 500d, "Precursor -C3H8NO6P"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 100d, "Precursor -H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H9O6P), 100d, "Phosphoglycerol"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H9O6P- H2O), 100d, "Phosphoglycerol - H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains is SeparatedChains) {
                    foreach (AcylChain chain in lipid.Chains.GetDeterminedChains()) {
                        if (chain.CarbonCount != 0) {
                            spectrum.AddRange(
                            new[] {
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass), 30d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                            });
                        }
                    }
                }
            }
            else if (adduct.AdductIonName == "[M-H]-") {
                spectrum.AddRange(
                     new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H5NO2), 999d, "Precursor -C3H5NO2") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H9O6P), 100d, "Phosphoglycerol"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H9O6P- H2O), 500d, "Phosphoglycerol - H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
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
