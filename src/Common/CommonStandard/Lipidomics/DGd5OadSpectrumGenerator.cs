using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class DGd5OadSpectrumGenerator : ILipidSpectrumGenerator {
        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public DGd5OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public DGd5OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+NH4]+";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 30;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetDGOadSpectrum(lipid, adduct));
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
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                "OAD01+H" };
            string[] oadIdLossH2O =
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

            if (lipid.Chains is MolecularSpeciesLevelChains) {
                var chains = lipid.Chains.GetDeterminedChains().ToArray();
                for (int i = 0; i < chains.Length; i++) {
                    var chain = new AcylChain(chains[i].CarbonCount, chains[i].DoubleBond, chains[i].Oxidized);
                    nlMass = chains[(i + 1) % chains.Length].Mass;
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, nlMass, abundance, oadId));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private static SpectrumPeak[] GetDGOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+NH4]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 50d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(lipid.Mass - H2O + MassDiffDictionary.ProtonMass, 200d, "[M+H]+ -H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains is SeparatedChains Chains) {
                    foreach (AcylChain chain in Chains.GetDeterminedChains()) {
                        spectrum.AddRange(
                            new[] {
                                new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass - Electron, 500d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass - Electron + MassDiffDictionary.HydrogenMass, 200d, $"-{chain}+H") { SpectrumComment = SpectrumComment.acylchain },
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

