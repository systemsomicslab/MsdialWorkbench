using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class EtherPESpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C2H8NO4P = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C2H5N = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double H3PO4 = new[]
        {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.OxygenMass * 4,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.HydrogenMass * 10,
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

        public EtherPESpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public EtherPESpectrumGenerator(ISpectrumPeakGenerator peakGenerator)
        {
            this.spectrumGenerator = peakGenerator ?? throw new System.ArgumentNullException(nameof(peakGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;


        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.EtherPE)
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
            spectrum.AddRange(GetEtherPESpectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)
            {
                spectrum.AddRange(GetAlkylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                if (plChains.Chains[0] is AlkylChain alkyl)
                {
                    if (alkyl.DoubleBond.Bonds.Any(b => b.Position == 1))
                    {
                        spectrum.AddRange(GetEtherPEPSpectrum(lipid, alkyl, plChains.Chains[1], adduct));
                    }
                    else
                    {
                        spectrum.AddRange(GetEtherPEOSpectrum(lipid, plChains.Chains[0], plChains.Chains[1], adduct));
                    }
                    spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, 0d, 30d));
                    //spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass: C2H8NO4P, 30d));
                }
                if (plChains.Chains[1] is AcylChain acyl)
                {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, 0d, 50d));
                    //spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass: C2H8NO4P, 50d));
                }
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

        private SpectrumPeak[] GetEtherPESpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(C2H8NO4P), 100d, "Header"),
                new SpectrumPeak(adduct.ConvertToMz(Gly_C), 100d, "Gly-C"),
                new SpectrumPeak(adduct.ConvertToMz(Gly_O), 100d, "Gly-O"),
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 150d, "Precursor -C2H8NO4P"),
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H5N), 150d, "Precursor -C2H5N"),
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 500d, "Precursor -C2H8NO4P"),
                     }
                );

            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetEtherPEPSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass + MassDiffDictionary.HydrogenMass), 30d, $"-{alkylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass), 30d, $"-{acylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass), 150, $"-{alkylChain}-O"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass  - H2O), 30d, $"-{acylChain}-O"),
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(alkylChain.Mass + C2H8NO4P - MassDiffDictionary.HydrogenMass), 250d, "Sn1Ether+C2H8NO3P"), // Sn1 + O + C2H8NO3P
                        new SpectrumPeak(adduct.ConvertToMz(alkylChain.Mass + C2H8NO4P - H3PO4 - MassDiffDictionary.HydrogenMass), 150d, "Sn1Ether+C2H8NO3P-H3PO4"),
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P - alkylChain.Mass + MassDiffDictionary.HydrogenMass), 300, "NL of C2H8NO4P+Sn1"),
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetEtherPEOSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass + MassDiffDictionary.HydrogenMass), 50d, $"-{alkylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass + MassDiffDictionary.HydrogenMass), 50d, $"-{acylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass), 200d, $"-{alkylChain}-O"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - H2O), 200d, $"-{acylChain}-O"),

                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass- C2H8NO4P + MassDiffDictionary.HydrogenMass), 50d, $"- Header -{alkylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass- C2H8NO4P + MassDiffDictionary.HydrogenMass), 50d, $"- Header -{acylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass- C2H8NO4P - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), 200d, $"- Header -{alkylChain}-O"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass- C2H8NO4P - H2O), 200d, $"- Header -{acylChain}-O"),
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                          new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H5N - H2O + MassDiffDictionary.HydrogenMass), 500d, "Precursor -C2H6NO"),
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAlkylPositionSpectrum(ILipid lipid, IChain alkylChain, AdductIon adduct)
        {
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass - CH2), 50d, "-CH2(Sn1)"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - C2H8NO4P - MassDiffDictionary.OxygenMass - CH2), 50d, "- Header -CH2(Sn1)"),
            };
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
