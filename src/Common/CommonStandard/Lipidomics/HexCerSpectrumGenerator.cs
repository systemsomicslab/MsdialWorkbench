using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class HexCerSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double C2H5NO = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double CH4O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C2H3NO = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double NH = new[]
        {
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.HydrogenMass * 1,
        }.Sum();
        private static readonly double C6H10O5 = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass *5,
        }.Sum();
        private static readonly double C5H10O4 = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass *4,
        }.Sum();
        public HexCerSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public HexCerSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.HexCer_NS || lipid.LipidClass == LbmClass.HexCer_NDS)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+" || adduct.AdductIonName == "[M+H-H2O]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlmass = adduct.AdductIonName == "[M+H]+" ? H2O + C6H10O5 : adduct.AdductIonName == "[M+H-H2O]+" ? C6H10O5 : 0.0;
            spectrum.AddRange(GetHexCerNSSpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)
            {
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo)
                {
                    spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                    if (adduct.AdductIonName == "[M+Na]+")
                    {
                        spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlmass, 30d)); //-header
                    }
                }
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl)
                {
                    spectrum.AddRange(GetAcylSpectrum(lipid, acyl, adduct));
                    if (adduct.AdductIonName == "[M+Na]+")
                    {
                        spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlmass, 30d)); //-header
                    }
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }
        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule)
        {
            return new MoleculeMsReference
            {
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

        private SpectrumPeak[] GetHexCerNSSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(C6H10O5+C2H5NO), 400d, "Hex + C2H5NO") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true},
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                    (
                        new[]
                        {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C5H10O4, 150d, "Precursor-C5H10O4"){ SpectrumComment = SpectrumComment.metaboliteclass },
                            new SpectrumPeak(adduct.ConvertToMz(C5H10O4)- MassDiffDictionary.HydrogenMass , 100d, "[C5H9O4+adduct]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                            new SpectrumPeak(adduct.ConvertToMz(C6H10O5 + H2O) , 100d, "[Hex+adduct+H]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                            new SpectrumPeak(adduct.ConvertToMz(C6H10O5 + MassDiffDictionary.OxygenMass) , 100d, "[Hex+adduct-H]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-C6H10O5-H2O, 100d, "Precursor-Hex-H2O") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                        }
                    );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 200d, "Precursor-H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-C6H10O5, 300d, "Precursor-Hex") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-C6H10O5-H2O*2, 200d, "Precursor-Hex-2H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-C6H10O5-H2O, 400d, "Precursor-Hex-H2O") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct)
        {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName != "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O, 200d, "[sph+H]+ -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2, 500d, "[sph+H]+ -2H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH4O2, 150d, "[sph+H]+ -CH4O2") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylSpectrum(ILipid lipid, AcylChain acyl, AdductIon adduct)
        {
            var chainMass = acyl.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>()
            {
                             new SpectrumPeak(adduct.ConvertToMz(chainMass) +C2H3NO + C6H10O5, 150d, "[FAA+C2H4O+Hex+adduct]+") { SpectrumComment = SpectrumComment.acylchain },

            };
            if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+H-H2O]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO - MassDiffDictionary.HydrogenMass -MassDiffDictionary.OxygenMass, 200d, "[FAA+C2H+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +NH, 200d, "[FAA+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO - MassDiffDictionary.HydrogenMass -MassDiffDictionary.OxygenMass, 200d, "[FAA+C2H+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
                if (adduct.AdductIonName == "[M+H]+")
                {
                    spectrum.AddRange
                    (
                         new[]
                         {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO, 150d, "[FAA+C2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                         }
                    );
                }
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(adduct.ConvertToMz(chainMass +C2H3NO -MassDiffDictionary.OxygenMass), 200d, "[FAA+C2H+Na]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
