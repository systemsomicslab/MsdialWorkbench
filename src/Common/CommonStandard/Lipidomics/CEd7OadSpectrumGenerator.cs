using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class CEd7OadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double skelton = new[] {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 38,
            MassDiffDictionary.Hydrogen2Mass * 7,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public CEd7OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public CEd7OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+NH4]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M-H]-";
        }
        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = adduct.IonMode == IonMode.Positive ? 40.0 : 20.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCEOadSpectrum(lipid, adduct));
            string[] oadId =
                adduct.IonMode == IonMode.Positive ?
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
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                "OAD01+H" } :
                new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                //"OAD03",
                //"OAD04",
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
                //"OAD17",
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                //"OAD01+H"
                };

            if (lipid.Chains is PositionLevelChains) {
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

        private static SpectrumPeak[] GetCEOadSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak> {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+NH4]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(skelton+MassDiffDictionary.ProtonMass , 500d, "skelton") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-H2O , 50d, "Precursor -H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        //new SpectrumPeak(lipid.Mass+MassDiffDictionary.ProtonMass , 50d, "[M+H]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            if (adduct.AdductIonName == "[M-H]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(skelton+MassDiffDictionary.ProtonMass , 500d, "skelton") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-H2O , 50d, "Precursor -H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(skelton , 500d, "skelton") { SpectrumComment = SpectrumComment.metaboliteclass },
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
