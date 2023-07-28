using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class CerNSd7SpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double CH6O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double CH4O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C2H3NO = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double NH = new[]
        {
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.HydrogenMass * 1,
        }.Sum();
        private static readonly double CH3O = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();
        private static readonly double sphD7MassBalance = new[]
        {       
            MassDiffDictionary.Hydrogen2Mass * 7,
            - MassDiffDictionary.HydrogenMass * 7,
        }.Sum();
        private static readonly double CD2 = new[]
        {
            MassDiffDictionary.Hydrogen2Mass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public CerNSd7SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public CerNSd7SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.Cer_NS_d7)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+H-H2O]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlmass = adduct.AdductIonName == "[M+H]+" ? H2O : 0.0;
            spectrum.AddRange(GetCerNSd7Spectrum(lipid, adduct));
            if (lipid.Chains.GetChainByPosition(1) is SphingoChain sphingo)
            {
                spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                spectrum.AddRange(GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlmass, 100d));
            }
            if (lipid.Chains.GetChainByPosition(2) is AcylChain acyl)
            {
                spectrum.AddRange(GetAcylSpectrum(lipid, acyl, adduct));
                spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlmass - sphD7MassBalance, 30d));
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
                PrecursorMz = adduct.ConvertToMz(lipid.Mass + sphD7MassBalance),
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

        private SpectrumPeak[] GetCerNSd7Spectrum(ILipid lipid, AdductIon adduct)
        {
            var lipidD7mass = lipid.Mass + sphD7MassBalance;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipidD7mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - CH3O, 150d, "Precursor-CH3O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O, 200d, "Precursor-H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        new SpectrumPeak(lipidD7mass + MassDiffDictionary.ProtonMass - CH6O2, 100d, "[M+H]+ -CH6O2") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
                if (adduct.AdductIonName == "[M+H]+")
                {
                    spectrum.AddRange
                    (
                         new[]
                         {
                             new SpectrumPeak(adduct.ConvertToMz(lipidD7mass) - H2O*2, 100d, "Precursor-2H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                         }
                    );
                }
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct)
        {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass + sphD7MassBalance;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName != "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O, 200d, "[sph+H]+ -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - H2O*2, 500d, "[sph+H]+ -2H2O"){ SpectrumComment = SpectrumComment.acylchain },
                        new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH4O2, 150d, "[sph+H]+ -CH4O2"){ SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }
        public IEnumerable<SpectrumPeak> GetSphingoDoubleBondSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct, double nlMass, double abundance)
        {
            var sphingoD7mass = sphingo.Mass + sphD7MassBalance;
            if (sphingo.DoubleBond.UnDecidedCount != 0 || sphingo.CarbonCount == 0 || sphingo.Oxidized.UnDecidedCount != 0)
            {
                return Enumerable.Empty<SpectrumPeak>();
            }

            var chainLoss = (lipid.Mass + sphD7MassBalance) - sphingoD7mass - nlMass
                + MassDiffDictionary.NitrogenMass
                + 12 * 2
                //+ MassDiffDictionary.OxygenMass * (sphingo.OxidizedCount > 2 ? 2 : sphingo.OxidizedCount)
                + MassDiffDictionary.OxygenMass * 1
                + MassDiffDictionary.HydrogenMass * 5;
            var diffs = new double[sphingo.CarbonCount];
            for (int i = 0; i < sphingo.CarbonCount; i++)
            {
                diffs[i] = CH2;
            }
            for (int i = sphingo.CarbonCount - 2; i < sphingo.CarbonCount; i++)
            {
                diffs[i] = CD2;
            }
            if (sphingo.Oxidized != null)
            {
                foreach (var ox in sphingo.Oxidized.Oxidises)
                {
                    if (ox > 2)
                    {
                        diffs[ox - 1] = diffs[ox - 1] + MassDiffDictionary.OxygenMass;
                    }
                }
            }

            foreach (var bond in sphingo.DoubleBond.Bonds)
            {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 3; i < sphingo.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            var bondPositions = new List<int>();
            for (int i = 2; i < sphingo.CarbonCount - 1; i++)
            {
                var speccomment = SpectrumComment.doublebond;
                var factor = 1.0;
                var factorHLoss = 0.5;
                var factorHGain = 0.2;

                if (bondPositions.Contains(i - 1))
                { // in the case of 18:2(9,12), Radical is big, and H loss is next
                    factor = 4.0;
                    factorHLoss = 2.0;
                    speccomment |= SpectrumComment.doublebond_high;
                }
                else if (bondPositions.Contains(i))
                {
                    // now no modification
                }
                else if (bondPositions.Contains(i + 1))
                {
                    factor = 0.25;
                    factorHLoss = 0.5;
                    factorHGain = 0.05;
                    speccomment |= SpectrumComment.doublebond_low;
                }
                else if (bondPositions.Contains(i + 2))
                {
                    // now no modification
                }
                else if (bondPositions.Contains(i + 3))
                {
                    if (bondPositions.Contains(i))
                    {
                        factor = 4.0;
                        factorHLoss = 0.5;
                        factorHGain = 2.0;
                    }
                    else
                    {
                        factorHLoss = 4.0;
                        speccomment |= SpectrumComment.doublebond_high;
                    }
                    speccomment |= SpectrumComment.doublebond_high;
                }

                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), factorHLoss * abundance, $"{sphingo} C{i + 1}-H") { SpectrumComment = speccomment });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), factor * abundance, $"{sphingo} C{i + 1}") { SpectrumComment = speccomment });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), factorHGain * abundance, $"{sphingo} C{i + 1}+H") { SpectrumComment = speccomment });

                //    for (int i = 2; i < sphingo.CarbonCount - 1; i++)
                //{
                //    peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{sphingo} C{i + 1}-H") { SpectrumComment = SpectrumComment.doublebond });
                //    peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), abundance, $"{sphingo} C{i + 1}") { SpectrumComment = SpectrumComment.doublebond });
                //    peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{sphingo} C{i + 1}+H") { SpectrumComment = SpectrumComment.doublebond });
                //}
            }
            return peaks;
        }

        private SpectrumPeak[] GetAcylSpectrum(ILipid lipid, AcylChain acyl, AdductIon adduct)
        {
            var chainMass = acyl.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(adduct.ConvertToMz(chainMass) +C2H3NO + MassDiffDictionary.HydrogenMass, 150d, "[FAA+C2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            else
            {
                spectrum.AddRange
                (
                     new[]
                     {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +NH, 200d, "[FAA+H]+") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
                if (adduct.AdductIonName == "[M+H]+")
                {
                    spectrum.AddRange
                    (
                         new[]
                         {
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO, 150d, "[FAA+C2H2O+H]+") { SpectrumComment = SpectrumComment.acylchain },
                             new SpectrumPeak(chainMass+ MassDiffDictionary.ProtonMass +C2H3NO - MassDiffDictionary.HydrogenMass -MassDiffDictionary.OxygenMass, 200d, "[FAA+C2H+H]+") { SpectrumComment = SpectrumComment.acylchain },
                         }
                    );
                }
            }
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
