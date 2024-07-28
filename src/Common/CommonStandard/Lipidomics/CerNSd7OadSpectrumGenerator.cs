using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
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
        private static readonly double CH6O2 = new[] {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double CH4O2 = new[] {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C2H3NO = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double NH = new[] {
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.HydrogenMass * 1,
        }.Sum();
        private static readonly double CH3O = new[] {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double CH2 = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();
        private static readonly double sphD7MassBalance = new[] {
            MassDiffDictionary.Hydrogen2Mass * 7,
            - MassDiffDictionary.HydrogenMass * 7,
        }.Sum();
        private static readonly double CD2 = new[] {
            MassDiffDictionary.Hydrogen2Mass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double Electron = 0.00054858026;

        public CerNSd7OadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }
        public CerNSd7OadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+";
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

            if (lipid.Chains is PositionLevelChains plChains) {
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo) {
                    spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                    var sphingoToAcyl = new AcylChain(sphingo.CarbonCount, sphingo.DoubleBond, sphingo.Oxidized);
                    //spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, sphingoToAcyl, adduct, nlMass - sphD7MassBalance, 30d, oadId));
                    spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlMass - sphD7MassBalance, 30d, oadId));
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

        private SpectrumPeak[] GetCerNSd7Spectrum(ILipid lipid, AdductIon adduct) {
            var lipidD7mass = lipid.Mass + sphD7MassBalance;
            var spectrum = new List<SpectrumPeak> {
                new SpectrumPeak(adduct.ConvertToMz(lipidD7mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+Na]+") {
                spectrum.AddRange
                (
                     new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - CH3O, 150d, "Precursor-CH3O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                     }
                );
            } else {
                spectrum.AddRange
                (
                     new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O + MassDiffDictionary.HydrogenMass, 70d, "Precursor-H2O+H") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        //new SpectrumPeak(lipidD7mass + MassDiffDictionary.ProtonMass - CH6O2, 100d, "[M+H]+ -CH6O2") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
                if (adduct.AdductIonName == "[M+H]+") {
                    spectrum.AddRange
                    (
                         new[] {
                             new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O*2, 100d, "Precursor-2H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                             new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O*2 + MassDiffDictionary.HydrogenMass, 70d, "Precursor-2H2O+H") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                         }
                    );
                }
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct) {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass + sphD7MassBalance;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName != "[M+Na]+") {
                spectrum.AddRange
                (
                     new[] {
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O, 200d, "[sph+H]+ -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2, 500d, "[sph+H]+ -2H2O"){ SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH4O2, 150d, "[sph+H]+ -CH4O2"){ SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
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
