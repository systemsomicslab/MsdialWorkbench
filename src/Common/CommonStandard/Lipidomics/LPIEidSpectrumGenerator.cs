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
    public class LPIEidSpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C6H13O9P = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C6H10O5 = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();

        private static readonly double C3H9O6P = new[] { // 172.013675 OCC(O)COP(O)(O)=O
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 17,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 15,
            MassDiffDictionary.OxygenMass * 10,
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

        public LPIEidSpectrumGenerator() {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public LPIEidSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.LPI)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlMass = adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass + H2O;
            spectrum.AddRange(GetLPISpectrum(lipid, adduct));
            if (lipid.Description.Has(LipidDescription.Chain)) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains.GetDeterminedChains(), adduct));
                if (lipid.Chains is PositionLevelChains plChains)
                {
                    //spectrum.AddRange(GetAcylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                }
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct));
                spectrum.AddRange(EidSpecificSpectrum(lipid, adduct, nlMass, 150d));
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

        private SpectrumPeak[] GetLPISpectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(C6H13O9P + adductmass, 300d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                new SpectrumPeak(Gly_C + adductmass, 100d, "Gly-C") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(Gly_O + adductmass, 200d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass },
				new SpectrumPeak(C3H9O6P + adductmass, 300d, "C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
				new SpectrumPeak(C3H9O6P - H2O + adductmass, 300d, "C3H9O6P - H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(lipidMass - C6H13O9P, 500d, "[M+H]+ -Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(lipidMass - C6H10O5, 100d, "[M+H]+ -C6H10O5") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(lipidMass - C6H10O5 - H2O, 150d, "[M+H]+ -C6H12O6") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(lipidMass - H2O, 800, "[M+H]+ -H2O")
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange(
                new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange(
                    new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 800d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(lipidMass, 999d, "[M+H]+ "){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct)
        {
            var nlMass = adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass + H2O;
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 50d));
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
            var spectrum = new List<SpectrumPeak>();
            if (chainMass != 0.0)
            {
                    spectrum.AddRange
                    (
                         new[]
                         {
                         new SpectrumPeak(lipidMass - chainMass, 150d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                         new SpectrumPeak(lipidMass - chainMass - H2O, 100d, $"-{acylChain} -H2O") { SpectrumComment = SpectrumComment.acylchain },
                          new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 100d, $"{acylChain} acyl+") { SpectrumComment = SpectrumComment.acylchain },
                        }
                    );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass - MassDiffDictionary.HydrogenMass;
            var chainMass = acylChain.Mass;

            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass - MassDiffDictionary.OxygenMass - CH2), 100d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
        }

        private static SpectrumPeak[] EidSpecificSpectrum(Lipid lipid, AdductIon adduct, double nlMass, double intensity)
        {
            var spectrum = new List<SpectrumPeak>();
            if (lipid.Chains is SeparatedChains chains)
            {
                foreach (var chain in lipid.Chains.GetDeterminedChains())
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
