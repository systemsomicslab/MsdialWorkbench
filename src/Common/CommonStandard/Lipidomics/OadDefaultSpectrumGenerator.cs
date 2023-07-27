using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public class OadDefaultSpectrumGenerator : ILipidSpectrumGenerator
    {
        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public OadDefaultSpectrumGenerator()
        {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public OadDefaultSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M+H]+" ||
                adduct.AdductIonName == "[M+Na]+" ||
                adduct.AdductIonName == "[M+NH4]+" ||
                adduct.AdductIonName == "[M+H-H2O]+" ||
                adduct.AdductIonName == "[M-H2O+H]+"||
                adduct.AdductIonName == "[M-H]-" ||
                adduct.AdductIonName == "[M+HCOO]-"||
                adduct.AdductIonName == "[M+CH3COO]-")
            {
                return true;
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var oadLipidSpectrumGenerator = new OadLipidSpectrumGenerator();
            var abundance = 40.0;

            var oadClassFragment = oadLipidSpectrumGenerator.GetClassFragmentSpectrum(lipid, adduct);
            var spectrum = new List<SpectrumPeak>(oadClassFragment.spectrum);
            var nlMass = oadClassFragment.nlMass;
            string[] oadId = new string[] {
                "OAD01",
                "OAD02",
                "OAD02+O",
                "OAD03",
                "OAD04",
                "OAD05",
                "OAD06",
                "OAD07",
                "OAD08",
                "OAD09",
                "OAD10",
                "OAD11",
                "OAD12",
                "OAD13",
                "OAD14",
                "OAD15",
                "OAD15+O",
                "OAD16",
                "OAD17",
                "OAD12+O",
                "OAD12+O+H",
                "OAD12+O+2H",
                "OAD01+H"
            };


            if (lipid.Chains is PositionLevelChains plChains)
            {
                foreach (var chain in lipid.Chains.GetDeterminedChains())
                {
                    if (chain is AcylChain acyl)
                    {
                        spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, abundance, oadId));
                    }
                    else if (chain is AlkylChain alkyl)
                    {
                        spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass, abundance, oadId));
                    }
                    if (chain is SphingoChain sphingo)
                    {
                        spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlMass, abundance, oadId));
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

        private readonly Dictionary<LbmClass, List<ILipidSpectrumGenerator>> map = new Dictionary<LbmClass, List<ILipidSpectrumGenerator>>();
        public void Add(LbmClass lipidClass, ILipidSpectrumGenerator generator)
        {
            if (!map.ContainsKey(lipidClass))
            {
                map.Add(lipidClass, new List<ILipidSpectrumGenerator>());
            }
            map[lipidClass].Add(generator);
        }

    }
    public class OadClassFragment
    {
        public double nlMass { get; set; }
        public List<SpectrumPeak> spectrum { get; set; }
    }

}

