using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public class OadDefaultSpectrumGenerator : ILipidSpectrumGenerator
    {
        private readonly ISpectrumPeakGenerator spectrumGenerator;
        public OadDefaultSpectrumGenerator()
        {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public OadDefaultSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+NH4]+" ||
                adduct.AdductIonName == "[M+H-H2O]+" ||
                adduct.AdductIonName == "[M-H2O+H]+")
            {
                return true;
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>() {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor }
            };
            //if (lipid.Chains is MolecularSpeciesLevelChains mlChains) {
            //    spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>().Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct));
            //}
            var nlMass = 0.0;
            var abundance = 100.0;
            var oadLipidSpectrumGenerator = new OadLipidSpectrumGenerator();

            spectrum.AddRange(oadLipidSpectrumGenerator.GetClassFragmentSpectrum(lipid, adduct));

            if (lipid.Chains is PositionLevelChains plChains)
            {
                foreach (var chain in plChains.Chains)
                {
                    if (chain is AcylChain acyl)
                    {
                        spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, abundance));
                    }
                    else if (chain is AlkylChain alkyl)
                    {
                        spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass, abundance));
                    }
                    if (chain is SphingoChain sphingo)
                    {
                        spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlMass, abundance));
                    }
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

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
