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
    public class TGSpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C2H3O = new[]
        {
            MassDiffDictionary.CarbonMass*2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C3H5O2 = new[]
{
            MassDiffDictionary.CarbonMass*3,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.OxygenMass*2,
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

        private static readonly double Electron = 0.00054858026;

        public TGSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public TGSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.TG)
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
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetTGSpectrum(lipid, adduct));
            if (lipid.Chains is MolecularSpeciesLevelChains mlChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, mlChains.Chains, adduct));
                var nlMass = 0.0;
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>(), adduct, nlMass));
            }
            if (lipid.Chains is PositionLevelChains plChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, plChains.Chains, adduct));
                //spectrum.AddRange(GetAcylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                var nlMass = 0.0;
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.Chains.OfType<AcylChain>(), adduct, nlMass));
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment))))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule)
        {
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

        private SpectrumPeak[] GetTGSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipid.Mass + adduct.AdductIonAccurateMass, 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                //new SpectrumPeak((lipid.Mass + adduct.AdductIonAccurateMass)/2, 150d, "[Precursor]2+") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(lipid.Mass + adduct.AdductIonAccurateMass-H2O, 150d, "Precursor-H2O"),
                        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 150d, "[M+H]+"),
                        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass-H2O, 150d, "[M+H]+ -H2O"),
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(lipid.Mass + adduct.AdductIonAccurateMass-H2O, 100d, "Precursor-H2O"),
                     }
                );
            }

            return spectrum.ToArray();
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
            var chainMass2 = acylChain.Mass + adductmass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(chainMass2 + C2H3O + MassDiffDictionary.OxygenMass, 100d, $"{acylChain}+C2H3O2"),
                        new SpectrumPeak(chainMass2 + C2H3O + CH2, 100d, $"{acylChain}+C3H5O"),
                        new SpectrumPeak(chainMass2 + C2H3O, 100d, $"{acylChain}+C2H3O"),
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+"),
                        new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.HydrogenMass * 2, 50d, $"-{acylChain}"),
                        new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass, 200d, $"-{acylChain}-O"),
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+"),
                        new SpectrumPeak(chainMass2 + C3H5O2, 100d, $"{acylChain}+C3H5O2"),
                        new SpectrumPeak(chainMass2 + C3H5O2 - H2O, 100d, $"{acylChain}+C3H3O"),
                        new SpectrumPeak(lipidMass - chainMass , 50d, $"-{acylChain}"),
                        new SpectrumPeak(lipidMass - chainMass - H2O, 200d, $"-{acylChain}-O"),
                     }
                );
            }
            return spectrum.ToArray();
        }

        //private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        //{
        //    var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
        //    var lipidMass = lipid.Mass + adductmass;
        //    var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass + adductmass;
        //    return new[]
        //    {
        //        new SpectrumPeak(chainMass+ C2H3O, 100d, "Sn2 diagnostics"),
        //    };
        //}

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 25d));
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
