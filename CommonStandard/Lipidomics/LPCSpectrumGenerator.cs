using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class LPCSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H14NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H9N = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double C5H13NO = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 16,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public LPCSpectrumGenerator() {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public LPCSpectrumGenerator(ISpectrumPeakGenerator peakGenerator) {
            this.spectrumGenerator = peakGenerator ?? throw new System.ArgumentNullException(nameof(peakGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;


        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            if (lipid.LipidClass == LbmClass.LPC)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetLPCSpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, plChains.Chains, adduct));
                spectrum.AddRange(GetAcylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.Chains.OfType<AcylChain>(), adduct));
            }
            if (lipid.Chains is MolecularSpeciesLevelChains mlChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, mlChains.Chains, adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>(), adduct));
            }
            //spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains));
            //spectrum.AddRange(GetAcylPositionSpectrum(lipid, lipid.Chains[0]));
            //spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.OfType<AcylChain>()));
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment))))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference
            {
                PrecursorMz = lipid.Mass + adduct.AdductIonAccurateMass,
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

        private SpectrumPeak[] GetLPCSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipid.Mass +adduct.AdductIonAccurateMass, 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(C5H14NO4P+adduct.AdductIonAccurateMass, 400d, "Header"),
                new SpectrumPeak(Gly_C+adduct.AdductIonAccurateMass, 100d, "Gly-C"),
                new SpectrumPeak(Gly_O+adduct.AdductIonAccurateMass, 100d, "Gly-O"),
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                         new SpectrumPeak(C5H13NO + adduct.AdductIonAccurateMass, 200d, "C5H14NO+"),
                         new SpectrumPeak(C5H14NO4P - H2O + adduct.AdductIonAccurateMass, 50d, "Header - H2O"),
                     }
                );
            }
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                         new SpectrumPeak(lipid.Mass - C3H9N + adduct.AdductIonAccurateMass, 800d, "Precursor -C3H9N"),
                         new SpectrumPeak(lipid.Mass - C3H9N -MassDiffDictionary.OxygenMass + adduct.AdductIonAccurateMass, 200d, "Precursor -C3H9NO"),
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                    new[]
                    {
                         new SpectrumPeak(lipid.Mass -H2O +adduct.AdductIonAccurateMass, 100d, "Precursor - H2O"),
                    }
                );

            }

            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct) {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain,adduct));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass + adduct.AdductIonAccurateMass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            return new[]
            {
                new SpectrumPeak(lipidMass - chainMass , 100d, $"-{acylChain}"),
                new SpectrumPeak(lipidMass - chainMass -H2O , 100d, $"-{acylChain}-O"),
            };
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass;
            return new[]
            {
                new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.OxygenMass 
                - CH2 - MassDiffDictionary.HydrogenMass  + adduct.AdductIonAccurateMass, 100d, "-CH2(Sn1)"),
            };
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains,AdductIon adduct) {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, 0d, 50d));
        }

        
        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
