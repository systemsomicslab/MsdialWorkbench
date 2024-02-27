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
    public class CeramidePhytoSphSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double CH6O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C3H4NO = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
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
        private static readonly double CH3O = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double CH4O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        public CeramidePhytoSphSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public CeramidePhytoSphSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.Cer_NP || lipid.LipidClass == LbmClass.Cer_AP || lipid.LipidClass == LbmClass.Cer_ABP)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+H-H2O]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlmass = 0.0;
            spectrum.AddRange(GetPhytoCerSpectrum(lipid, adduct));
            if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo)
            {
                spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlmass, 30d));
            }
            if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl)
            {
                spectrum.AddRange(GetAcylSpectrum(lipid, acyl, adduct));
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlmass, 30d));
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

        private SpectrumPeak[] GetPhytoCerSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CH3O, 150d, "Precursor-CH3O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                     }
                );
            }
            else
            {
                if (lipid.LipidClass == LbmClass.Cer_ABP)
                {
                    spectrum.AddRange
                     (
                          new[]
                          {
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                          }
                     );

                }
                else
                {
                    spectrum.AddRange
                        (
                             new[]
                             {
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                                new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass - CH6O2, 100d, "[M+H]+ -CH6O2") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                             }
                        );
                }
                if (adduct.AdductIonName == "[M+H]+")
                {
                    spectrum.AddRange
                    (
                         new[]
                         {
                             new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O*2, 100d, "Precursor-2H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                         }
                    );
                }
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
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2, 500d, "[sph+H]+ -2H2O"){ SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH6O2, 150d, "[sph+H]+ -CH6O2"){ SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 150d, "[sph+H]+ "){ SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylSpectrum(ILipid lipid, AcylChain acyl, AdductIon adduct)
        {
            var chainMass = acyl.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(adduct.ConvertToMz(chainMass) +C2H3NO + MassDiffDictionary.HydrogenMass, 150d, "[FAA+C2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +NH, 200d, "[FAA+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
                if (adduct.AdductIonName == "[M+H]+")
                {
                    spectrum.AddRange
                    (
                         new[]
                         {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO + H2O + 12, 150d, "[FAA+C3H4O2+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C3H4NO, 150d, "[FAA+C3H3O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO, 150d, "[FAA+C2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO - MassDiffDictionary.HydrogenMass -MassDiffDictionary.OxygenMass, 200d, "[FAA+C2H+H]+") { SpectrumComment = SpectrumComment.acylchain },
                         }
                    );
                }
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
