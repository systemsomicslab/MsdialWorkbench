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
    public class CLEidSpectrumGenerator : ILipidSpectrumGenerator
    {
        //CL explain rule -> CL 2 chain(sn1,sn2)/2 chain(sn3,sn4)
        //CL sn1_sn2_sn3_sn4 (follow the rules of alignment) -- MolecularSpeciesLevelChains
        //CL sn1_sn2/sn3_sn4 -- MolecularSpeciesLevelChains <- cannot parsing now
        //CL sn1/sn2/sn3/sn4  -- PositionLevelChains 

        private static readonly double C3H6O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double C3H3O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 3,
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

        public CLEidSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public CLEidSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.CL)
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
            var nlMass = adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetCLSpectrum(lipid, adduct));

            if (lipid.Description.Has(LipidDescription.Chain)) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, lipid.Chains.GetDeterminedChains(), adduct));
                //spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>().Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct));
                if (lipid.Chains.GetChainByPosition(1) is AcylChain c1 && lipid.Chains.GetChainByPosition(2) is AcylChain c2 && lipid.Chains.GetChainByPosition(3) is AcylChain c3 && lipid.Chains.GetChainByPosition(4) is AcylChain c4) {
                    var sn1sn2 = new[] { c1, c2, };
                    var sn3sn4mass = lipid.Mass - (c3.Mass + c4.Mass + C3H3O2 + MassDiffDictionary.HydrogenMass);
                    spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, sn1sn2.Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct, sn3sn4mass + nlMass));
                    spectrum.AddRange(GetAcylPositionSpectrum(lipid, c1, adduct, sn3sn4mass + nlMass));
                    var sn3sn4 = new[] { c3, c4, };
                    var sn1sn2mass = lipid.Mass - (c1.Mass + c2.Mass + C3H3O2 + MassDiffDictionary.HydrogenMass);
                    spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, sn3sn4.Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct, sn1sn2mass + nlMass));
                    spectrum.AddRange(GetAcylPositionSpectrum(lipid, c3, adduct, sn1sn2mass + nlMass));
                    spectrum.AddRange(EidSpecificSpectrum(lipid, adduct, nlMass, 200d));
                }
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
        private SpectrumPeak[] GetCLSpectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass-H2O), 999d, "Precursor -H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)/2, 150d, "[Precursor]2+") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass-H2O)/2, 150d, "[Precursor -H2O]2+") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
            };
            //if (adduct.AdductIonName == "[M+NH4]+")
            //{
            //    spectrum.Add(
            //        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 200d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass }
            //    );
            //}
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass -MassDiffDictionary.HydrogenMass, 10d));
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var acylChainsArr = acylChains.ToArray();
            var sn1sn2mass = acylChainsArr[0].Mass + acylChainsArr[1].Mass + C3H3O2 + MassDiffDictionary.HydrogenMass;
            var sn3sn4mass = acylChainsArr[2].Mass + acylChainsArr[3].Mass + C3H3O2 + MassDiffDictionary.HydrogenMass;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipidMass - sn1sn2mass + MassDiffDictionary.ProtonMass, 30d, $"[M-Sn1-Sn2-C3H3O2+H]+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - sn3sn4mass + MassDiffDictionary.ProtonMass, 30d, $"[M-Sn3-Sn4-C3H3O2+H]+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - sn1sn2mass + MassDiffDictionary.ProtonMass-H2O, 30d, $"[M-Sn1-Sn2-C3H3O2-H2O+H]+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - sn3sn4mass + MassDiffDictionary.ProtonMass-H2O, 30d, $"[M-Sn3-Sn4-C3H3O2-H2O+H]+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(sn1sn2mass + MassDiffDictionary.ProtonMass, 500d, $"[Sn1+Sn2+C3H3O2+H]+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(sn3sn4mass + MassDiffDictionary.ProtonMass, 500d, $"[Sn3+Sn4+C3H3O2+H]+"){ SpectrumComment = SpectrumComment.acylchain },

            };
            spectrum.AddRange(acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct)));

            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 50d, $"{acylChain} acyl+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(chainMass +C3H6O2+ MassDiffDictionary.ProtonMass, 100d, $"{acylChain} +C3H6O2"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(chainMass +C3H6O2 - MassDiffDictionary.OxygenMass + MassDiffDictionary.ProtonMass, 50d, $"{acylChain} +C3H6O"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - adduct.AdductIonAccurateMass + MassDiffDictionary.ProtonMass) , 20d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - chainMass - adduct.AdductIonAccurateMass + MassDiffDictionary.ProtonMass- H2O) , 20d, $"-{acylChain}- H2O") { SpectrumComment = SpectrumComment.acylchain },
            };

            return spectrum.ToArray();
        }


        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct, double nlMass)
        {
            var lipidMass = lipid.Mass + adduct.AdductIonAccurateMass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipidMass - chainMass- nlMass - CH2 , 20d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
            return spectrum.ToArray();
        }

        private static SpectrumPeak[] EidSpecificSpectrum(Lipid lipid, AdductIon adduct, double nlMass, double intensity)
        {
            var spectrum = new List<SpectrumPeak>();
            if (lipid.Chains.GetChainByPosition(1) is IChain c1 && lipid.Chains.GetChainByPosition(2) is IChain c2 && lipid.Chains.GetChainByPosition(3) is IChain c3 && lipid.Chains.GetChainByPosition(4) is IChain c4)
            {
                var sn1sn2mass = lipid.Mass - (c1.Mass + c2.Mass + C3H3O2 + MassDiffDictionary.HydrogenMass);
                var sn3sn4mass = lipid.Mass - (c3.Mass + c4.Mass + C3H3O2 + MassDiffDictionary.HydrogenMass);
                var sn1sn2 = new[] { c1, c2, };
                var sn3sn4 = new[] { c3, c4, };
                foreach (var chain in sn1sn2)
                {
                    if (chain.DoubleBond.Count == 0 || chain.DoubleBond.UnDecidedCount > 0) continue;
                    if (chain.DoubleBond.Count <= 3) { intensity = intensity * 0.5; }
                    spectrum.AddRange(EidSpecificSpectrumGenerator.EidSpecificSpectrumGen(lipid, chain, adduct, sn3sn4mass + nlMass, intensity));
                }
                foreach (var chain in sn3sn4)
                {
                    if (chain.DoubleBond.Count == 0 || chain.DoubleBond.UnDecidedCount > 0) continue;
                    if (chain.DoubleBond.Count <= 3) { intensity = intensity * 0.5; }
                    spectrum.AddRange(EidSpecificSpectrumGenerator.EidSpecificSpectrumGen(lipid, chain, adduct, sn1sn2mass+ nlMass, intensity));
                }
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
