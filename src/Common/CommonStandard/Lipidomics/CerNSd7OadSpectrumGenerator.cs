using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class CerNSd7OadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH5O = new[] {
            MassDiffDictionary.CarbonMass,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH3COO = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private static readonly double sphD7MassBalance = new[] {
            MassDiffDictionary.Hydrogen2Mass * 7,
            - MassDiffDictionary.HydrogenMass * 7,
        }.Sum();

        public CerNSd7OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }
        public CerNSd7OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M-H]-" ||
                adduct.AdductIonName == "[M+CH3COO]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var nlMass = adduct.AdductIonName == "[M+H]+" ? H2O : 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCerNSd7Spectrum(lipid, adduct));
            string[] oadId = new string[] {
                //"OAD01",
                "OAD02",
                //"OAD02+O",
                "OAD03",
                //"OAD04",
                "OAD05",
                "OAD06",
                "OAD07",
                "OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                //"OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                //"OAD17",
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                //"OAD01+H"
                "SphOAD",
                //"SphOAD+H",
                //"SphOAD+2H",
                "SphOAD-CO"
            };

            if (lipid.Chains is MolecularSpeciesLevelChains) {
                foreach (AcylChain chain in lipid.Chains.GetDeterminedChains()) {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, nlMass - sphD7MassBalance - MassDiffDictionary.HydrogenMass, 30d, oadId));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private static SpectrumPeak[] GetCerNSd7Spectrum(ILipid lipid, AdductIon adduct) {
            var lipidD7mass = lipid.Mass + sphD7MassBalance;

            var spectrum = new List<SpectrumPeak> {
                new SpectrumPeak(adduct.ConvertToMz(lipidD7mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };

            if (adduct.AdductIonName == "[M+H]+") {                
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O*2, 100d, "Precursor-2H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+CH3COO]-") {                
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - CH3COO - MassDiffDictionary.HydrogenMass, 200d, "Precursor-CH3COO") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - CH3COO - CH5O, 200d, "Precursor-CH3COO-CH5O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M-H]-") {                
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O - CH5O - MassDiffDictionary.HydrogenMass*2, 200d, "Precursor-H2O-CH5O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O - MassDiffDictionary.HydrogenMass*2, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            } else {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O + MassDiffDictionary.HydrogenMass, 70d, "Precursor-H2O+H") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            return spectrum.ToArray();
        }
        
        private static MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference {
                PrecursorMz = adduct.ConvertToMz(lipid.Mass + sphD7MassBalance),
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
