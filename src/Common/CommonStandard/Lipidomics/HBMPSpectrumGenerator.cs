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
    public class HBMPSpectrumGenerator : ILipidSpectrumGenerator
    {
        //HBMP explain rule -> HBMP 1 chain(sn1)/2 chain(sn2,sn3)
        //HBMP sn1_sn2_sn3 (follow the rules of alignment) -- MolecularSpeciesLevelChains
        //HBMP sn1/sn2_sn3 -- MolecularSpeciesLevelChains <- cannot generate now
        //HBMP sn1/sn2/sn3 (sn4= 0:0)  -- MolecularSpeciesLevelChains
        //HBMP sn1/sn4(or sn4/sn1)/sn2/sn3 (sn4= 0:0)  -- PositionLevelChains <- cannot generate now

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

        public HBMPSpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public HBMPSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.HBMP)
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
            //var nlMass = adduct.AdductIonName == "[M+NH4]+" ? adduct.AdductIonAccurateMass + H2O : H2O;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetHBMPSpectrum(lipid, adduct));
            // GetChain(1) = lyso
            if (lipid.Description.Has(LipidDescription.Chain)) {
                lipid.Chains.ApplyToChain(1, chain => spectrum.AddRange(GetLysoAcylLevelSpectrum(lipid, chain, adduct)));
                lipid.Chains.ApplyToChain(2, chain => spectrum.AddRange(GetAcylLevelSpectrum(lipid, chain, adduct)));
                lipid.Chains.ApplyToChain(3, chain => spectrum.AddRange(GetAcylLevelSpectrum(lipid, chain, adduct)));
                //lipid.Chains.ApplyToChain(1, chain => spectrum.AddRange(GetAcylPositionSpectrum(lipid, chain, adduct)));
                //lipid.Chains.ApplyToChain(2, chain => spectrum.AddRange(GetAcylPositionSpectrum(lipid, chain, adduct)));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct, nlMass: 0.0));
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
        private SpectrumPeak[] GetHBMPSpectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(lipid.Mass + adductmass - H2O, 100d, "Precursor -H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                //new SpectrumPeak(C3H9O6P + adductmass, 50d, "C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(C3H9O6P -H2O + adductmass, 50d, "C3H9O6P - H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.Add(
                    new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 100d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass }
                );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            var spectrum = new List<SpectrumPeak>();
            var chains = acylChains.ToList();
            nlMass = chains[0].Mass + C3H9O6P - MassDiffDictionary.HydrogenMass + adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass;
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chains[1], adduct, nlMass, 10d));
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chains[2], adduct, nlMass, 10d));
            nlMass = chains[1].Mass + chains[2].Mass + C3H6O2 - MassDiffDictionary.HydrogenMass + adduct.AdductIonAccurateMass - MassDiffDictionary.ProtonMass;
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chains[0], adduct, nlMass, 10d));
            return spectrum.ToArray();
        }

        //private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct)
        //{
        //    return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct));
        //}

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;

            var spectrum = new List<SpectrumPeak>
            {
                //new SpectrumPeak(chainMass + C3H6O2 + adductmass, 100d, $"{acylChain}+C3H4O2+H"),
                new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass , 50d, $"{acylChain} Acyl+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.HydrogenMass + adductmass, 50d, $"-{acylChain}"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - chainMass - H2O + adductmass, 50d, $"-{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - chainMass - C3H9O6P + adductmass, 200d, $"-C3H9O6P -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(lipidMass - chainMass - C3H9O6P - H2O + adductmass, 50d, $"-C3H9O6P -{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
             };

            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetLysoAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(chainMass + C3H6O2 + adductmass, 100d, $"{acylChain}+C3H4O2+H"){ SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(chainMass + C3H6O2 - H2O + adductmass, 100d, $"{acylChain} + C3H4O2 -H2O +H"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass , 50d, $"{acylChain} Acyl+"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - chainMass - MassDiffDictionary.HydrogenMass + adductmass, 50d, $"-{acylChain}"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(lipidMass - chainMass - H2O + adductmass, 50d, $"-{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(lipidMass - chainMass - C3H9O6P + adductmass, 200d, $"-C3H9O6P -{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(lipidMass - chainMass - C3H9O6P - H2O + adductmass, 50d, $"-C3H9O6P -{acylChain}-H2O") { SpectrumComment = SpectrumComment.acylchain },
             };

            return spectrum.ToArray();
        }


        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;

            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(lipidMass - chainMass - H2O - CH2 , 50d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
            return spectrum.ToArray();
        }


        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
