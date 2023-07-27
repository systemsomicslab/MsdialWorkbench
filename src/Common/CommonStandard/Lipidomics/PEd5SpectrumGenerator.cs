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
    public class PEd5SpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C2H8NO4P = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double CH5N = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 5,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 3,
        }.Sum();

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double CD2 = new[]
        {
            MassDiffDictionary.Hydrogen2Mass *2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public PEd5SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public PEd5SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.PE_d5)
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
            spectrum.AddRange(GetPESpectrum(lipid, adduct));
            if (lipid.Description.Has(LipidDescription.Chain)) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains.GetDeterminedChains(), adduct));
                lipid.Chains.ApplyToChain(1, chain => spectrum.AddRange(GetAcylPositionSpectrum(lipid, chain, adduct)));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct, nlMass: C2H8NO4P));
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

        private SpectrumPeak[] GetPESpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 500d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true  },
                new SpectrumPeak(adduct.ConvertToMz(C2H8NO4P), 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(Gly_C), 400d, "Gly-C")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(Gly_O), 300d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)/2, 100d, "[Precursor]2+") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(lipid.Mass - C2H8NO4P + MassDiffDictionary.ProtonMass, 250d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - CH5N +MassDiffDictionary.HydrogenMass), 100d, "Precursor -CH4N") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - CH5N - MassDiffDictionary.CarbonMass), 300d, "Precursor -C2H5N") { SpectrumComment = SpectrumComment.metaboliteclass }
                     }
                );
            }

            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 30d));
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct)
        {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                    new[]
                    {
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 100d, $"{acylChain} acyl+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass), 100d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - C2H8NO4P), 100d, $"-Header -{acylChain}"){ SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), 100d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - C2H8NO4P - H2O), 100d, $"-Header -{acylChain}-O"){ SpectrumComment = SpectrumComment.acylchain },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - MassDiffDictionary.HydrogenMass*2), 100d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - H2O), 100d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var chainMass = acylChain.Mass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - MassDiffDictionary.OxygenMass - CD2), 100d, "-CD2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - C2H8NO4P - MassDiffDictionary.OxygenMass - CD2), 100d, "-Header -CD2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
                     }
                );
            }
            return spectrum.ToArray();
        }


        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
