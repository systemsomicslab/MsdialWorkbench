using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public class OadLipidSpectrumGenerator
    {
        public SpectrumPeak[] GetClassFragmentSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>();

            switch (lipid.LipidClass)
            {
                case LbmClass.PC:
                case LbmClass.EtherPC:
                case LbmClass.SM:
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 500d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true }}
                    );
                    break;
                case LbmClass.LPC:
                case LbmClass.EtherLPC:
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                            //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 500d, "Precursor - H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        }
                    );
                    break;
                case LbmClass.PE:
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 800d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                    );
                    break;
                case LbmClass.EtherPE:
                    if (lipid.Chains is PositionLevelChains plChains)
                    {
                        AlkylChain alkyl;
                        AcylChain acyl;

                        if (plChains.Chains[0] is AlkylChain)
                        {
                            alkyl = (AlkylChain)plChains.Chains[0];
                            acyl = (AcylChain)plChains.Chains[1];
                        }
                        else
                        {
                            alkyl = (AlkylChain)plChains.Chains[1];
                            acyl = (AcylChain)plChains.Chains[0];
                        }

                        if (alkyl.DoubleBond.Bonds.Any(b => b.Position == 1))
                        {
                            spectrum.AddRange
                            (
                                new[] {
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                                    new SpectrumPeak(adduct.ConvertToMz(alkyl.Mass + C2H8NO4P-MassDiffDictionary.HydrogenMass), 500d, "Precursor") { SpectrumComment = SpectrumComment.acylchain },
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 100d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                            );
                        }
                        else
                        {
                            spectrum.AddRange
                            (
                                new[] {
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                                    new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 100d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                            );
                        }
                    }
                    break;
                case LbmClass.LPE:
                case LbmClass.EtherLPE:
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 100d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                            //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 200d, "Precursor - H2O") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                        }
                    );
                    break;
                case LbmClass.TG:
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    if (lipid.Chains is PositionLevelChains tgChains)
                    {
                        foreach (var chain in tgChains.Chains)
                        {
                            spectrum.Add(new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass, 200d, $"{chain} loss") { SpectrumComment = SpectrumComment.acylchain });
                        }
                    }
                    break;
                case LbmClass.DG:
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 100d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    spectrum.Add(new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 200d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass });
                    spectrum.Add(new SpectrumPeak(lipid.Mass -H2O+ MassDiffDictionary.ProtonMass, 999d, "[M+H]+ -H2O") { SpectrumComment = SpectrumComment.metaboliteclass });
                    if (lipid.Chains is PositionLevelChains dgChains)
                    {
                        foreach (var chain in dgChains.Chains)
                        {
                            spectrum.Add(new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass - Electron, 800d, $"{chain} loss") { SpectrumComment = SpectrumComment.acylchain });
                        }
                    }
                    break;
                default:
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    break;
            }

            return spectrum.ToArray();
        }

        private static readonly double Electron = 0.00054858026;

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double C5H14NO4P = new[] //PC SM Header
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C2H8NO4P = new[] //PE Header
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

    }
}
