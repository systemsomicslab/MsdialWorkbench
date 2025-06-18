using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class CeramideOadSpectrumGenerator : ILipidSpectrumGenerator {
        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private static readonly double CH3COO = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private static readonly double CH5O = new[] {
            MassDiffDictionary.CarbonMass,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH2O = new[] {
            MassDiffDictionary.CarbonMass,
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C2H7NO = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH4O2 = new[] {
            MassDiffDictionary.CarbonMass,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double C2H3N = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public CeramideOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public CeramideOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+CH3COO]-" ||
                adduct.AdductIonName == "[M-H]-";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 40.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCerNsOadSpectrum(lipid, adduct));
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

        private static SpectrumPeak[] GetCerNsOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 500d, "Precursor -H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sph) {
                    spectrum.AddRange(
                        new[] {
                            new SpectrumPeak(sph.Mass  - CH4O2 + MassDiffDictionary.HydrogenMass*2, 200d, $"{sph}-CH4O2") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(sph.Mass  - H2O + MassDiffDictionary.HydrogenMass*2, 200d, $"{sph}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(sph.Mass - 2*H2O + MassDiffDictionary.HydrogenMass*2, 500d, $"{sph}-2H2O") { SpectrumComment = SpectrumComment.acylchain },
                        }
                    );
                }
            }
            else if (adduct.AdductIonName == "[M+CH3COO]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH3COO - MassDiffDictionary.HydrogenMass, 200d, "Precursor-CH3COO") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH3COO - CH5O, 200d, "Precursor-CH3COO-CH5O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                    spectrum.AddRange(
                        new[] {
                            new SpectrumPeak(acyl.Mass - MassDiffDictionary.HydrogenMass*2, 50d, $"{acyl}-2H") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(acyl.Mass + C2H3N, 100d, $"{acyl}+C2H3N") { SpectrumComment = SpectrumComment.acylchain },
                        }
                    );
                }
            }
            else if (adduct.AdductIonName == "[M-H]-") {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH2O, 300d, "Precursor-CH2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH2O - MassDiffDictionary.HydrogenMass*2, 200d, "Precursor-CH2O-2H") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH2O -H2O, 200d, "Precursor-CH2O-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                    spectrum.AddRange(
                        new[] {
                            new SpectrumPeak(acyl.Mass - MassDiffDictionary.HydrogenMass*2, 700d, $"{acyl}-2H") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(acyl.Mass + C2H3N, 500d, $"{acyl}+C2H3N") { SpectrumComment = SpectrumComment.acylchain },
                        }
                    );
                }
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sph) {
                    spectrum.Add(new SpectrumPeak(sph.Mass - C2H7NO, 700d, $"{sph}-C2H7NO") { SpectrumComment = SpectrumComment.acylchain });
                }
            } else {
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O + MassDiffDictionary.HydrogenMass, 70d, "Precursor-H2O+H") { SpectrumComment = SpectrumComment.metaboliteclass },
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

