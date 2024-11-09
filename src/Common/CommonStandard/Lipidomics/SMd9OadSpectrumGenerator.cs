using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class SMd9OadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double C5H5D9NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 9,
        }.Sum();

        private static readonly double CD3 = new[] {
            MassDiffDictionary.Hydrogen2Mass * 3,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double C3D9N = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.Hydrogen2Mass * 9,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double CH3COO = new[] {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public SMd9OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public SMd9OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+CH3COO]-";
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

        private static SpectrumPeak[] GetSMOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+Na]+") {
                spectrum.AddRange(
                     new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C3D9N, 150d, "Precursor-C3D9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C5H5D9NO4P, 150d, "NL of header") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C5H5D9NO4P), 999d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+CH3COO]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass-CH3COO-CD3), 999d, "Precursor-CH3COO-CD3") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(C5H5D9NO4P-CD3, 300d, "Characteristic fragment") { SpectrumComment = SpectrumComment.metaboliteclass },
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

