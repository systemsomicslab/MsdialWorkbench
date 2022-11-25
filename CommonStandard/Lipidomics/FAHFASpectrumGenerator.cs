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
    public class FAHFASpectrumGenerator : ILipidSpectrumGenerator
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

        public FAHFASpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public FAHFASpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.FAHFA)
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
            spectrum.AddRange(GetFAHFASpectrum(lipid, adduct));
            if (lipid.Chains is MolecularSpeciesLevelChains mlChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, (AcylChain)mlChains.Chains[1], adduct));
                spectrum.AddRange(GetOxPositionSpectrum(lipid, (AcylChain)mlChains.Chains[1], adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>(), adduct, nlMass));
            }
            if (lipid.Chains is PositionLevelChains plChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, (AcylChain)plChains.Chains[1], adduct));
                spectrum.AddRange(GetOxPositionSpectrum(lipid, (AcylChain)plChains.Chains[1], adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.Chains.OfType<AcylChain>(), adduct, nlMass));
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

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonAccurateMass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange
            (
                 new[]
                 {
                        new SpectrumPeak(chainMass+ C4N2H10_O + MassDiffDictionary.OxygenMass +MassDiffDictionary.ProtonMass, 50d, $"{acylChain}(+DMED)") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass+ C4N2H10_O + MassDiffDictionary.ProtonMass, 700d, $"{acylChain}(+DMED) -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass+ C4N2H10_O -C2NH7 + MassDiffDictionary.ProtonMass, 700d, $"{acylChain}(+DMED) -H2O -C2NH7") { SpectrumComment = SpectrumComment.acylchain, IsAbsolutelyRequiredFragmentForAnnotation = true },
                 }
            );
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetOxPositionSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var spectrum = new List<SpectrumPeak>();
            if (acylChain.Oxidized.UnDecidedCount > 0 || acylChain.Oxidized.Count > 1) { return spectrum.ToArray(); }
            spectrum.AddRange
            (
                 new[]
                 {
                        new SpectrumPeak(adduct.ConvertToMz(
                            C4N2H10 
                            + MassDiffDictionary.HydrogenMass * 2 
                            + 12 * 2
                            + MassDiffDictionary.OxygenMass * 2
                            + CH2 * (acylChain.Oxidized.Oxidises[0]-2)
                            )
                            , 300d, $"{acylChain} O position") { SpectrumComment = SpectrumComment.snposition, IsAbsolutelyRequiredFragmentForAnnotation = true  },
                 }
            );
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetFAHFASpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass + C4N2H10_O), 999d, "Precursor(DMED derv.)") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass + C4N2H10_O - C2NH7), 50d, "Precursor - C2NH7") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            nlMass = -C4N2H10_O;
            var spectrum = new List<SpectrumPeak>();
            var acylChain = acylChains.ToList(); 
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain[0], adduct, nlMass, 25d));
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
