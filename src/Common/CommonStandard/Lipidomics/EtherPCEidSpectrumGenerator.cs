using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class EtherPCEidSpectrumGenerator : ILipidSpectrumGenerator
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

        private static readonly double C5H11N = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 11,
            MassDiffDictionary.NitrogenMass,
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

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public EtherPCEidSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public EtherPCEidSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new System.ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.EtherPC)
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
            spectrum.AddRange(GetEtherPCSpectrum(lipid, adduct));
            lipid.Chains.ApplyToChain(1, chain => spectrum.AddRange(GetSn1PositionSpectrum(lipid, chain, adduct)));
            (AlkylChain alkyl, AcylChain acyl) = lipid.Chains.Deconstruct<AlkylChain, AcylChain>();
            if (alkyl != null && acyl != null) {
                if (alkyl.DoubleBond.Bonds.Any(b => b.Position == 1))
                {
                    spectrum.AddRange(GetEtherPCPSpectrum(lipid, alkyl, acyl, adduct));
                }
                else
                {
                    spectrum.AddRange(GetEtherPCOSpectrum(lipid, alkyl, acyl, adduct));
                }
                spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, 0d, 20d));
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, 0d, 20d));
            }
            spectrum.AddRange(EidSpecificSpectrum(lipid, adduct, 0d, 300d));
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

        private SpectrumPeak[] GetEtherPCSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(Gly_C), 400d, "Gly-C") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                new SpectrumPeak(adduct.ConvertToMz(Gly_O), 400d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 500d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) / 2, 200d, "[Precursor]2+") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetEtherPCPSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            return new[]
            {
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass), 100d, $"-{alkylChain}"),
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass), 100d, $"-{acylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass), 100, $"-{alkylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), 200d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
            };
        }

        private SpectrumPeak[] GetEtherPCOSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass), 100d, $"-{alkylChain}"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass + MassDiffDictionary.HydrogenMass), 100d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass), 100d, $"-{alkylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - H2O), 100d, $"-{acylChain}-O"){ SpectrumComment = SpectrumComment.acylchain },
            };
        }

        private SpectrumPeak[] GetSn1PositionSpectrum(ILipid lipid, IChain alkylChain, AdductIon adduct)
        {
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass - CH2), 150d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
        }
        private static SpectrumPeak[] EidSpecificSpectrum(Lipid lipid, AdductIon adduct, double nlMass, double intensity)
        {
            var spectrum = new List<SpectrumPeak>();
            if (lipid.Chains is SeparatedChains chains)
            {
                foreach (var chain in chains.GetDeterminedChains())
                {
                    if (chain.DoubleBond.Count == 0 || chain.DoubleBond.UnDecidedCount > 0) continue;
                    spectrum.AddRange(EidSpecificSpectrumGenerator.EidSpecificSpectrumGen(lipid, chain, adduct, nlMass, intensity));
                }
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
