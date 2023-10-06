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
    public class TGEidSpectrumGenerator : ILipidSpectrumGenerator
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

        public TGEidSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public TGEidSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.TG)
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
            spectrum.AddRange(GetTGSpectrum(lipid, adduct));
            if (lipid.Description.Has(LipidDescription.Chain)) {
                var chains = lipid.Chains.GetDeterminedChains().ToList();
                if (lipid.Chains.GetChainByPosition(2) is IChain sn2) {
                    spectrum.AddRange(GetAcylPositionSpectrum(lipid, sn2, adduct));
                    chains.Remove(sn2);
                }
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, chains, adduct));
                spectrum.AddRange(EidSpecificSpectrum(lipid, adduct, 0d, 200d));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct));
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

        private SpectrumPeak[] GetTGSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak((adduct.ConvertToMz(lipid.Mass))/2,150d, "[Precursor]2+") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-H2O, 150d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 150d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass },
                        //new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass-H2O, 150d, "[M+H]+ -H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            else if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)-H2O, 100d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
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
                        new SpectrumPeak(chainMass2 + C2H3O + MassDiffDictionary.OxygenMass, 100d, $"{acylChain}+C2H3O2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass2 + C2H3O + CH2, 100d, $"{acylChain}+C3H5O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.HydrogenMass * 2, 50d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - H2O, 200d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass2 + C3H5O2, 100d, $"{acylChain}+C3H5O2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass2 + C3H5O2 - H2O, 100d, $"{acylChain}+C3H3O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass , 50d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - H2O, 200d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var chainMass = acylChain.Mass + adductmass;
            var lipidMass = lipid.Mass + adductmass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(chainMass+ C2H3O, 100d, "Sn2 diagnostics") { SpectrumComment = SpectrumComment.snposition, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+ Sn2") { SpectrumComment = SpectrumComment.acylchain },
                        //new SpectrumPeak(lipidMass - chainMass + MassDiffDictionary.HydrogenMass*2, 50d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.OxygenMass , 200d, $"-{acylChain}-O Sn2") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(chainMass+ C2H3O, 100d, "Sn2 diagnostics") { SpectrumComment = SpectrumComment.snposition },
                        new SpectrumPeak(acylChain.Mass + Electron , 100d, $"{acylChain}+ Sn2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + C3H5O2, 100d, $"{acylChain}+C3H5O2 Sn2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + C3H5O2 - H2O, 100d, $"{acylChain}+C3H3O Sn2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass + MassDiffDictionary.HydrogenMass*2, 50d, $"-{acylChain} Sn2") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.OxygenMass, 200d, $"-{acylChain}-O Sn2") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            var spectrum = new List<SpectrumPeak>();
            foreach (var lossChain in acylChains)
            {
                nlMass = lossChain.Mass + MassDiffDictionary.OxygenMass + adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass;
                var chains = acylChains.Where((c) => !c.Equals(lossChain)).ToList();
                spectrum.AddRange(chains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, 5d)));
            }
            return spectrum.ToArray();
        }
        private static SpectrumPeak[] EidSpecificSpectrum(Lipid lipid, AdductIon adduct, double nlMass, double intensity)
        {
            var spectrum = new List<SpectrumPeak>();
            if (lipid.Chains is SeparatedChains acylChains)
            {
                foreach (var lossChain in lipid.Chains.GetDeterminedChains())
                {
                    nlMass = lossChain.Mass + MassDiffDictionary.OxygenMass + adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass;
                    var chains = lipid.Chains.GetDeterminedChains().Where((c) => !c.Equals(lossChain)).ToList();
                    foreach (var chain in chains)
                    {
                        if (chain.DoubleBond.Count == 0 || chain.DoubleBond.UnDecidedCount > 0) continue;
                        if (chain.DoubleBond.Count <= 3) { intensity = intensity * 0.5; }
                        spectrum.AddRange(EidSpecificSpectrumGenerator.EidSpecificSpectrumGen(lipid, chain, adduct, nlMass, intensity));
                    }
                }
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
