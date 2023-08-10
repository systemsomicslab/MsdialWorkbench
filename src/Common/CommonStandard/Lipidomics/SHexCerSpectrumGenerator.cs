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
    public class SHexCerSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C6H10O5 = new[] {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();
        private static readonly double SO3 = new[] {
            MassDiffDictionary.SulfurMass * 1,
            MassDiffDictionary.OxygenMass * 3,
        }.Sum();
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double CH4O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C8H16NO6 = new[]
        {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 15,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *6,
        }.Sum();
        private static readonly double C2H2N = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.NitrogenMass *1,
        }.Sum();

        private static readonly double C2H6N = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.NitrogenMass *1,
        }.Sum();

        private static readonly double Na = 22.98977;

        public SHexCerSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public SHexCerSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.SHexCer)
            {
                if (adduct.AdductIonName == "[M+H]+"||adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlmass = adduct.AdductIonName == "[M+H]+" ? SO3 + H2O: SO3;
            spectrum.AddRange(GetSHexCerSpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains)
            {
                if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo)
                {
                    spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                    //spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlmass, 20d));
                }
                if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl)
                {
                    spectrum.AddRange(GetAcylSpectrum(lipid, acyl, adduct));
                    //spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlmass, 20d));
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

        private SpectrumPeak[] GetSHexCerSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - H2O), 200d, "[M-H2SO4+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true},
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3), 100d, "[M-SO3+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass},
                        new SpectrumPeak(adduct.ConvertToMz(C8H16NO6), 150d, "[C8H16NO6+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5 - H2O*2), 150d, "[M-C6H12O9S-H2O+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5 - H2O), 250d, "[M-C6H12O9S+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5), 100d, "[M-C6H10O8S+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C8H16NO6 + SO3), 150d, "[C8H16NO9S+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                    new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3), 700d, "[M-SO3+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass},
                        new SpectrumPeak(adduct.ConvertToMz(C6H10O5 + H2O) , 400d, "[Hex+Na]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C8H16NO6), 500d, "[C8H16NO6+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5 - H2O), 150d, "[M-C6H12O9S+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5), 250d, "[M-C6H10O8S+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - SO3 - C6H10O5 + MassDiffDictionary.CarbonMass +MassDiffDictionary.OxygenMass), 200d, "[M-C5H10O7S+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C8H16NO6 + SO3), 100d, "[C8H16NO9S+Na]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            };

            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct)
        {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(chainMass - H2O),100d, "[Sph-H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(chainMass - H2O*2) ,200d, "[Sph-2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(chainMass - CH4O2), 80d, "[sph-CH4O2+H]+ ") { SpectrumComment = SpectrumComment.acylchain },
                        //new SpectrumPeak(adduct.ConvertToMz(chainMass + C6H10O5 - MassDiffDictionary.NitrogenMass - MassDiffDictionary.HydrogenMass*2), 80d, "[sph+Hex+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylSpectrum(ILipid lipid, AcylChain acyl, AdductIon adduct)
        {
            var chainMass = acyl.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass) , 150d, "[FAA+H]+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + C2H2N) , 200d, "[FA+C2H2N+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                    new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + MassDiffDictionary.NitrogenMass + C6H10O5 + H2O + MassDiffDictionary.NitrogenMass + MassDiffDictionary.CarbonMass) , 100d, "[FAA+C2H2+Hex+Na]+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass*4) , 100d, "[FAA+3H+Na]+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass*4 + Na) , 200d, "[FAA+3H+2Na]+") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
            };
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
