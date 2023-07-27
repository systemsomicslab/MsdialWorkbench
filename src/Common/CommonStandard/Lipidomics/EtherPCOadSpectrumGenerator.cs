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
    public class EtherPCOadSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H14NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH3 = new[]
{
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.CarbonMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public EtherPCOadSpectrumGenerator()
        {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public EtherPCOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M+H]+")
            {
                return true;
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var abundance = 40.0;
            var nlMass = 0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetEtherPCOadSpectrum(lipid, adduct));
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
                "OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                //"OAD14",
                //"OAD15",
                //"OAD15+O",
                //"OAD16",
                //"OAD17",
                "OAD12+O",
                "OAD12+O+H",
                "OAD12+O+2H",
                //"OAD01+H"
                } ;
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
                spectrum.AddRange(spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkyl, adduct, nlMass, abundance, oadId));
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlMass, abundance, oadId));
            }

            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private SpectrumPeak[] GetEtherPCOadSpectrum(Lipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 500d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 999d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                    }
                );
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

        private SpectrumPeak[] GetEtherPCPSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            return new[]
            {
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass), 100d, $"-{alkylChain}"),
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass), 100d, $"-{acylChain}"),
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass-MassDiffDictionary.HydrogenMass), 50d, $"-{alkylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - MassDiffDictionary.OxygenMass), 200d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
            };
        }

        private SpectrumPeak[] GetEtherPCOSpectrum(ILipid lipid, IChain alkylChain, IChain acylChain, AdductIon adduct)
        {
            return new[]
            {
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass), 100d, $"-{alkylChain}"){ SpectrumComment = SpectrumComment.acylchain },
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - alkylChain.Mass - MassDiffDictionary.OxygenMass), 100d, $"-{alkylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass + MassDiffDictionary.HydrogenMass), 100d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass + MassDiffDictionary.HydrogenMass*2), 50d, $"-{acylChain} +H") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - H2O+ MassDiffDictionary.HydrogenMass), 80d, $"-{acylChain}-O"){ SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - acylChain.Mass - H2O+ MassDiffDictionary.HydrogenMass*2), 50d, $"-{acylChain}-O +H"){ SpectrumComment = SpectrumComment.acylchain },
            };
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

