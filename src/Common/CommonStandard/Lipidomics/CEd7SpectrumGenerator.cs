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
    public class CEd7SpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 46,
            MassDiffDictionary.OxygenMass * 1,
        }.Sum();

       private static readonly double massBalanceD7 = new[]
       {
            MassDiffDictionary.Hydrogen2Mass * 7,
            - MassDiffDictionary.HydrogenMass * 7,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public CEd7SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
 
        public CEd7SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.CE_d7)
            {
                if (adduct.AdductIonName == "[M+NH4]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCESpectrum(lipid, adduct));
            var nlMass = adduct.AdductIonName == "[M+NH4]+" ? adduct.AdductIonAccurateMass : 0.0;
            lipid.Chains.ApplyToChain<AcylChain>(1, acylChain => spectrum.AddRange(GetAcylLevelSpectrum(lipid, acylChain, adduct)));
            //spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.OfType<AcylChain>(), adduct, nlMass));
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

        private SpectrumPeak[] GetCESpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(skelton+massBalanceD7-H2O+MassDiffDictionary.ProtonMass , 500d, "skelton") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true }, 
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-H2O , 50d, "Precursor -H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        //new SpectrumPeak(lipid.Mass+MassDiffDictionary.ProtonMass , 50d, "[M+H]+"){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(skelton+massBalanceD7-H2O , 500d, "skelton") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
            }
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct)
        {
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                   (
                        new[]
                        {
                        new SpectrumPeak(adduct.ConvertToMz(chainMass + H2O), 200d, $"{acylChain}+O") { SpectrumComment = SpectrumComment.acylchain },
                        }
                   );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 25d));
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
