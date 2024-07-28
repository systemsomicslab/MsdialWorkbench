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

        private static readonly double C2H2N = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.NitrogenMass *1,
        }.Sum();

        private static readonly double Electron = 0.00054858026;

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public CeramideOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public CeramideOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+";
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

            if (lipid.Chains is PositionLevelChains plChains) {
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo) {
                    spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                    var sphingoToAcyl = new AcylChain(sphingo.CarbonCount, sphingo.DoubleBond, sphingo.Oxidized);
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, sphingoToAcyl, adduct, nlMass, 30d, oadId));
                }
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl) {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, 30d, oadId));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private SpectrumPeak[] GetCerNsOadSpectrum(Lipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 500d, "Precursor -H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            } else {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                    }
                );
            }
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct) {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+") {
                spectrum.AddRange
                (
                     new[] {
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O,30d, "[sph+H]+ -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2, 50d, "[sph+H]+ -H2O*2") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
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

