using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    internal class  EtherPEOadSpectrumGenerator : ILipidSpectrumGenerator
    {
        private readonly double C2H8NO4P = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private readonly double DMAG = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public EtherPEOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public EtherPEOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            if (lipid.Name.Contains("PE O-")) {
                if (adduct.AdductIonName == "[M+H]+" ||
                    adduct.AdductIonName == "[M-H]-") {
                    return true;
                }
            }
            if (lipid.Name.Contains("PE P-")) {
                if (adduct.AdductIonName == "[M+H]+") {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 40.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            string[] oadId =
                adduct.IonMode == IonMode.Positive ?
                new[] {
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
                    "OAD01+H" } :
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
                    "OAD15+O",
                    "OAD16",
                    //"OAD17",
                    "OAD12+O",
                    "OAD12+O+H",
                    "OAD12+O+2H",
                    //"OAD01+H"
                };

            (AlkylChain alkyl, AcylChain acyl) = lipid.Chains.Deconstruct<AlkylChain, AcylChain>();
            if (alkyl != null && acyl != null) {
                if (adduct.AdductIonName == "[M+H]+") {
                    if (alkyl.DoubleBond.Bonds.Any(b => b.Position == 1)) {
                        spectrum.AddRange(GetEtherPEPSpectrum(lipid, alkyl, acyl, adduct));
                    } else {
                        spectrum.AddRange(GetEtherPEOSpectrum(lipid, alkyl, acyl, adduct));
                    }
                } else {
                    spectrum.AddRange(GetEtherPEOSpectrum(lipid, alkyl, acyl, adduct));
                }
                if (lipid.Chains.DoubleBondCount != 0) {
                    spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass, abundance, oadId));
                    //spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass: C2H8NO4P, 30d));
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, abundance, oadId));
                    //spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass: C2H8NO4P, 50d));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private SpectrumPeak[] GetEtherPEPSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct) {
            if (adduct.AdductIonName == "[M+H]+") {
                var spectrum = new List<SpectrumPeak>();
                spectrum.AddRange( 
                    new [] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 100d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 200d, $"-C2H8NO4P"),
                        new SpectrumPeak(adduct.ConvertToMz(alkylChain.Mass + C2H8NO4P - MassDiffDictionary.HydrogenMass), 500d, $"{alkylChain}+C2H8NO3P") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(acylChain.Mass + DMAG - MassDiffDictionary.HydrogenMass), 999d, $"{acylChain} DMAG") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
                return spectrum.ToArray();
            } else {
                return null;
            }
        }

        private SpectrumPeak[] GetEtherPEOSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct) {
            if (adduct.AdductIonName == "[M-H]-") {
                var spectrum = new List<SpectrumPeak>();
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C2H8NO4P), 100d, "Header"),
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass + MassDiffDictionary.HydrogenMass), 200d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(acylChain.Mass + MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass), 500d, $"{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
                return spectrum.ToArray();
            } else {
                var spectrum = new List<SpectrumPeak>();
                spectrum.AddRange(
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 100d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C2H8NO4P), 100d, "Header"),
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 999d, $"-C2H8NO4P") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(acylChain.Mass  - MassDiffDictionary.HydrogenMass), 30d, $"{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
                return spectrum.ToArray();
            }
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
