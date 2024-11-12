using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class LPId5OadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double C6H13O9P = new[] {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H2D5O5P = new[] { //  OCC(O)COP(O)(O)=O
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 5,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public LPId5OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public LPId5OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }
        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+NH4]+" ||
                adduct.AdductIonName == "[M-H]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 40.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetLPId5OadSpectrum(lipid, adduct));
            string[] oadId =
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
                //"OAD12",
                "OAD13",
                //"OAD14",
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

        private static SpectrumPeak[] GetLPId5OadSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M-H]-") {
                spectrum.AddRange(
                    new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(C6H13O9P-H2O), 500d, "Phosphoinositol -H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                            new SpectrumPeak(adduct.ConvertToMz(C3H2D5O5P), 100d, "Characteristic fragment") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains is SeparatedChains) {
                    foreach (AcylChain chain in lipid.Chains.GetDeterminedChains()) {
                        if (chain.CarbonCount != 0) {
                            spectrum.AddRange(
                                new[] {
                                    //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass), 30d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                                    //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass*2), 10d, $"-{chain}+H") { SpectrumComment = SpectrumComment.acylchain },
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass - H2O), 90d, $"-{chain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass*2 - H2O), 30d, $"-{chain}+H-H2O") { SpectrumComment = SpectrumComment.acylchain },
                                }
                            );
                        }
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
