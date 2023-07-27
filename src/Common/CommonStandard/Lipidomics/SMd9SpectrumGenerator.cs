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
    public class SMd9SpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H5D9NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 9,
        }.Sum();
        private static readonly double C3D9N = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.Hydrogen2Mass * 9,
            MassDiffDictionary.NitrogenMass,
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
        private static readonly double C2H3NO = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double C2H2N = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.NitrogenMass *1,
        }.Sum();

        public SMd9SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public SMd9SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.Chains.OxidizedCount == 3) { return false; }
            if (lipid.LipidClass == LbmClass.SM_d9)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+")
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
            spectrum.AddRange(GetSMSpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)
            {
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

        private SpectrumPeak[] GetSMSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(C5H5D9NO4P), 500d, "Header")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(C5H5D9NO4P+C2H3NO-MassDiffDictionary.OxygenMass), 150d, "Header+C2H3N")  { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C3D9N, 150d, "Precursor-C3D9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C5H5D9NO4P, 150d, "Precursor-Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(C5H5D9NO4P+C2H3NO+MassDiffDictionary.CarbonMass), 150d, "Header+C3H3NO")  { SpectrumComment = SpectrumComment.metaboliteclass }, // need to consider
                     }
                );
            }
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
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2,100d, "[sph+H]+ -Header -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        //new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH4O2, 100d, "[sph+H]+ -CH4O2"),
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
                new SpectrumPeak(adduct.ConvertToMz(chainMass) +C2H2N , 200d, "[FAA+C2H+adduct]+") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(chainMass) +C5H5D9NO4P+C2H2N -MassDiffDictionary.HydrogenMass, 200d, "[FAA+C2H+Header+adduct]+") { SpectrumComment = SpectrumComment.acylchain },
            };
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
