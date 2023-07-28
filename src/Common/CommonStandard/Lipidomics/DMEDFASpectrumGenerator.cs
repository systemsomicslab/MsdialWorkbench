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
    public class DMEDFASpectrumGenerator : ILipidSpectrumGenerator // DMEDFA and DMEDOxFA
    {

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double C2NH7 = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.HydrogenMass * 7,
        }.Sum();

        private static readonly double C4N2H10_O = new[]
        {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 10,
            - MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C4N2H10 = new[]
        {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 10,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public DMEDFASpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public DMEDFASpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.DMEDFA || lipid.LipidClass == LbmClass.DMEDOxFA)
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
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetDMEDFASpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)//TBC
            {
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.GetTypedChains<AcylChain>(), adduct, nlMass));
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
        private SpectrumPeak[] GetDMEDFASpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor(DMED derv.)") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2NH7), 200d, "Precursor - C2NH7") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (lipid.Chains.OxidizedCount>0)
            {
                spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2NH7 - H2O), 50d, "Precursor - C2NH7 - H2O") { SpectrumComment = SpectrumComment.metaboliteclass });
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            var acylChain = acylChains.ToList();
            var abundance = 25d;
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain[0], adduct, nlMass, abundance));

            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
