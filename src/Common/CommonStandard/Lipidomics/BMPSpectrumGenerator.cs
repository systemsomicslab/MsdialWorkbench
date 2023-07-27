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
    public class BMPSpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C3H9O6P = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H6O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass * 2,
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

        public BMPSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public BMPSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.BMP)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var nlMass = adduct.AdductIonName == "[M+Na]+" ? 0.0 :
                adduct.AdductIonName == "[M+NH4]+" ? adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass + H2O * 2 : H2O * 2;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetBMPSpectrum(lipid, adduct));
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

        private SpectrumPeak[] GetBMPSpectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(lipid.Mass - C3H9O6P + adductmass, 150d, "Precursor -C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
               new SpectrumPeak(C3H9O6P + adductmass, 200d, "C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(C3H9O6P -H2O + adductmass, 200d, "C3H9O6P - H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.Add(
                    new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 200d, "[M+H]+")
                );
            }
            if (adduct.AdductIonName == "[M+H]+"|| adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(lipid.Mass + adductmass - H2O, 100d, "Precursor -H2O"),
                        new SpectrumPeak(lipid.Mass + adductmass - H2O *2, 100d, "Precursor -2H2O"),
                    }
                );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 10d));
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct)
        {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var nlMass = adduct.AdductIonName == "[M+Na]+" ? 0.0 : H2O * 2;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipidMass-nlMass - chainMass + adductmass, 50d, $"-{acylChain}"),
                new SpectrumPeak(lipidMass-nlMass - chainMass - MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass + adductmass, 50d, $"-{acylChain}-OH") { SpectrumComment = SpectrumComment.acylchain },
             };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(lipidMass - chainMass - C3H6O2 + adductmass, 400d, $"-C3H6O2 -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - C3H6O2 - H2O + adductmass, 50d, $"-C3H6O2 -{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
            }
            else
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(chainMass + C3H9O6P - H2O + adductmass, 150d, $"{acylChain}+C3H6O5P") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - C3H9O6P + adductmass, 450d, $"-C3H9O6P -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - C3H9O6P - H2O + adductmass, 50d, $"-C3H9O6P -{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                    }
                );
            }

            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipidMass - chainMass - H2O - CH2 , 100d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
            if (adduct.AdductIonName != "[M+Na]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(lipidMass - chainMass - H2O *2 - CH2 , 50d, "-H2O -CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
                    }
                );
            }
            return spectrum.ToArray();
        }


        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
