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
    public class CARSpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double CHO2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 1,
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double C7H15NO3 = new[]
        {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 15,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 1,
        }.Sum();


        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public CARSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public CARSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.CAR)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCARSpectrum(lipid, adduct));
            var nlMass = 0.0;
            lipid.Chains.ApplyToChain<AcylChain>(1, acylChain => spectrum.AddRange(GetAcylLevelSpectrum(lipid, acylChain, adduct)));
            spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct, nlMass));
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

        private SpectrumPeak[] GetCARSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) -CHO2, 150d, "Precursor-CHO2") { SpectrumComment = SpectrumComment.metaboliteclass }, // M-45
                new SpectrumPeak(adduct.ConvertToMz(C7H15NO3) , 150d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true }, //172
                new SpectrumPeak(adduct.ConvertToMz(C7H15NO3 - H2O) , 150d, "Header-H2O") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true}, //144
            };
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct)
        {
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange
           (
                new[]
                {
                        new SpectrumPeak(adduct.ConvertToMz(chainMass) , 50d, $"[Acyl]+"){ SpectrumComment = SpectrumComment.acylchain },
                }
           );
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 25d));
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
