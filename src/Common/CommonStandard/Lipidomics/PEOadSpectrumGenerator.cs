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
    public class PEOadSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C2H8NO4P = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H5O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C2H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass*2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private readonly ISpectrumPeakGenerator spectrumGenerator;
        public PEOadSpectrumGenerator()
        {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public PEOadSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M-H]-")
            {
                return true;
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var abundance = 30;
            var nlMass = adduct.IonMode == IonMode.Positive ? C2H8NO4P : 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPEOadSpectrum(lipid, adduct));

            if (lipid.Chains is PositionLevelChains plChains)
            {
                foreach (AcylChain chain in plChains.Chains)
                {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, nlMass, abundance));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private SpectrumPeak[] GetPEOadSpectrum(Lipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 999d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true }
                    }
                );
                if (lipid.Chains is SeparatedChains Chains)
                {
                    foreach (AcylChain chain in Chains.Chains)
                    {
                        spectrum.AddRange
                        (
                            new[]
                            {
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass), 50d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chain.Mass + MassDiffDictionary.HydrogenMass-H2O), 40d, $"-{chain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(chain.Mass - MassDiffDictionary.HydrogenMass), 20d, $"{chain} Acyl+") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(chain.Mass + C3H5O2), 20d, $"-{chain} +C3H5O2") { SpectrumComment = SpectrumComment.acylchain },
                                new SpectrumPeak(adduct.ConvertToMz(chain.Mass + C2H2O), 30d, $"-{chain} +C2H2O") { SpectrumComment = SpectrumComment.acylchain },
                           }
                        );
                    }
                }
            }
            else if (adduct.AdductIonName == "[M-H]-")
            {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C2H8NO4P), 30d, "Header-") { SpectrumComment = SpectrumComment.metaboliteclass, },
                    }
                );
                if (lipid.Chains is SeparatedChains Chains)
                {
                    foreach (AcylChain chain in Chains.Chains)
                    {
                        spectrum.AddRange
                        (
                            new[]
                            {
                                new SpectrumPeak(chain.Mass+MassDiffDictionary.OxygenMass+Electron, 30d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                            }
                        );
                    }
                }
            }
            else
            {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                    }
                );
            }
            return spectrum.ToArray();
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

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}

