using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class DMEDFAHFAOadSpectrumGenerator : ILipidSpectrumGenerator {

        private static readonly double CH2 = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double C2NH7 = new[] {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.HydrogenMass * 7,
        }.Sum();

        private static readonly double C4N2H10_O = new[] {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 10,
            - MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C4N2H10 = new[] {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 10,
        }.Sum();

        private static readonly double H2O = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public DMEDFAHFAOadSpectrumGenerator() {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public DMEDFAHFAOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }
        private readonly IOadSpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return adduct.AdductIonName == "[M+H]+";
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var abundance = 30;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetDMEDFAHFASpectrum(lipid, adduct, nlMass));
            string[] oadId =
                new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                "OAD03",
                "OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                //"OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                "OAD13",
                "OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                "OAD17",
                "OAD12+O",
                "OAD12+O+H",
                "OAD12+O+2H",
                "OAD01+H" };


            if (lipid.Description.Has(LipidDescription.Chain)) {
                lipid.Chains.ApplyToChain<AcylChain>(2, acylChain => spectrum.AddRange(GetAcylLevelSpectrum(lipid, acylChain, adduct)));
                //lipid.Chains.ApplyToChain<AcylChain>(2, acylChain => spectrum.AddRange(GetOxPositionSpectrum(lipid, acylChain, adduct)));

                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, lipid.Chains.GetTypedChains<AcylChain>(), adduct, nlMass, abundance, oadId));
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference {
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

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct) {
            var adductmass = adduct.AdductIonAccurateMass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass + C4N2H10_O;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange
            (
                 new[] {
                        //new SpectrumPeak(chainMass + MassDiffDictionary.OxygenMass + MassDiffDictionary.ProtonMass, 50d, $"{acylChain}(+DMED)") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 10d, $"{acylChain}(+DMED) -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        //new SpectrumPeak(chainMass - C2NH7 + MassDiffDictionary.ProtonMass, 700d, $"{acylChain}(+DMED) -H2O -C2NH7") { SpectrumComment = SpectrumComment.acylchain, IsAbsolutelyRequiredFragmentForAnnotation = true },
                 }
            );
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetOxPositionSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct) {
            var adductmass = adduct.AdductIonAccurateMass;
            var lipidMass = lipid.Mass + adductmass;
            var spectrum = new List<SpectrumPeak>();
            if (acylChain.Oxidized.UnDecidedCount > 0 || acylChain.Oxidized.Count > 1) { return spectrum.ToArray(); }
            var separatedChainMass = CH2 * (acylChain.Oxidized.Oxidises[0] - 2) - MassDiffDictionary.HydrogenMass*2*(acylChain.DoubleBond.Bonds.Where(n => n.Position < acylChain.Oxidized.Oxidises[0]).Count());
            spectrum.AddRange
            (
                 new[] {
                        new SpectrumPeak(adduct.ConvertToMz(C4N2H10 + MassDiffDictionary.HydrogenMass * 2 + 12 * 2 + MassDiffDictionary.OxygenMass * 2 + separatedChainMass), 300d, $"{acylChain} O position") { SpectrumComment = SpectrumComment.snposition, IsAbsolutelyRequiredFragmentForAnnotation = true  },
                 }
            );
            return spectrum.ToArray();
        }
        private SpectrumPeak[] GetDMEDFAHFASpectrum(ILipid lipid, AdductIon adduct,double nlmass = 0.0) {
            var spectrum = new List<SpectrumPeak> {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor(DMED derv.)") { SpectrumComment = SpectrumComment.precursor },
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2NH7), 50d, "Precursor - C2NH7") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass, double abundance, string[] oadId) {
            nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            var acylChain = acylChains.ToList();
            spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain[0], adduct, nlMass, abundance, oadId));

            //// Dehydroxy HFA chain spectrum 
            if (acylChain[1].Oxidized.Oxidises.Count == 1) {
                //nlMass = acylChain[0].Mass + H2O * acylChain[1].Oxidized.Count - MassDiffDictionary.HydrogenMass;
                var hfaOx = acylChain[1].Oxidized.Oxidises;
                var HfaDb = acylChain[1].DoubleBond;
                //foreach (var ox in hfaOx)
                //{
                //    if (ox == acylChain[1].CarbonCount)
                //    {
                //        HfaDb = HfaDb.Add(DoubleBondInfo.Create(ox - 1));
                //    }
                //    else
                //    {
                //        HfaDb = HfaDb.Add(DoubleBondInfo.Create(ox));
                //    }
                //}
                var HfaNoDBChain = new AcylChain(acylChain[1].CarbonCount, HfaDb, new Oxidized(0));
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, HfaNoDBChain, adduct, nlMass, abundance, oadId));
            }
            return spectrum.ToArray();
        }
        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
