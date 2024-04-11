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
    public class PId5SpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C6H13O9P = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C6H10O5 = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 5,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 10,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 3,
        }.Sum();

        private static readonly double CD2 = new[]
        {
            MassDiffDictionary.Hydrogen2Mass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O= new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public PId5SpectrumGenerator() {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public PId5SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.PI_d5)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var nlMass = adduct.AdductIonName == "[M+Na]+" ? 0.0 : adduct.ConvertToMz(C6H13O9P) - MassDiffDictionary.ProtonMass;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPISpectrum(lipid, adduct));
            if (lipid.Description.Has(LipidDescription.Chain)) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains.GetDeterminedChains(), adduct));
                lipid.Chains.ApplyToChain(1, chain => spectrum.AddRange(GetAcylPositionSpectrum(lipid, chain, adduct)));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct, nlMass: nlMass));
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

        private SpectrumPeak[] GetPISpectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(C6H13O9P + adductmass, 500d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(Gly_C + adductmass, 250d, "Gly-C") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(Gly_O + adductmass, 200d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange(
                    new SpectrumPeak[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C6H10O5), 400d, "Precursor -C6H10O5") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
            }
            else
            {
                spectrum.AddRange(
                    new SpectrumPeak[]
                    {
                        new SpectrumPeak(lipid.Mass - C6H13O9P + MassDiffDictionary.ProtonMass, 500d, "Precursor -Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C6H10O5), 100d, "Precursor -C6H10O5") { SpectrumComment = SpectrumComment.metaboliteclass,  IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
            }
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.Add(
                    new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 200d, "[M+H]+")
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
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                         new SpectrumPeak(lipidMass - chainMass , 80d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                         new SpectrumPeak(lipidMass - chainMass-H2O , 80d, $"-{acylChain}-H2O"),
                         new SpectrumPeak(lipidMass - chainMass - C6H13O9P , 100d, $"-C6H13O9P -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                         new SpectrumPeak(lipidMass - chainMass - C6H13O9P-H2O , 80d, $"-C6H13O9P -{acylChain}-H2O"),
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                         new SpectrumPeak(lipidMass - chainMass , 100d, $"-{acylChain} Acyl") { SpectrumComment = SpectrumComment.acylchain },
                         new SpectrumPeak(lipidMass - chainMass -H2O+ MassDiffDictionary.HydrogenMass, 100d, $"-{acylChain} Acyl-O-") { SpectrumComment = SpectrumComment.acylchain },
                         //new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass - C6H10O5), 100d, $"-C6H10O5 -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass+ adductmass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass ;

            return new[]
            {
                new SpectrumPeak(lipidMass - chainMass -H2O+ MassDiffDictionary.HydrogenMass - CD2, 50d, "-CD2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
                //new SpectrumPeak(lipidMass - chainMass - C6H13O9P-H2O - CD2 , 50d, "-Header -CD2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
        }


        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
