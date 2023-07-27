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
        public OadClassFragment GetClassFragmentSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlMass = 0.0;

            switch (lipid.LipidClass)
            {
                case LbmClass.PC:
                case LbmClass.EtherPC:
                case LbmClass.SM:
                    nlMass = 0.0;
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(C5H14NO, 50d, "C5H14NO") { SpectrumComment = SpectrumComment.metaboliteclass}
                        }
                    );
                    if (adduct.AdductIonName == "[M+H]+")
                    {
                        spectrum.AddRange
                        (
                            new[] {
                                new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true }}
                        );
                    }
                    break;
                case LbmClass.LPC:
                case LbmClass.EtherLPC:
                    nlMass = 0.0;
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 500d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 100d, "Precursor - H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                            new SpectrumPeak(C5H14NO, 50d, "C5H14NO") { SpectrumComment = SpectrumComment.metaboliteclass}
                        }
                    );
                    break;
                case LbmClass.PE:
                    nlMass = C2H8NO4P;
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 800d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                    );
                    break;
                case LbmClass.EtherPE:
                    nlMass = 0.0;
                    if (lipid.Chains is PositionLevelChains plChains)
                    {
                        (AlkylChain alkyl, AcylChain acyl) = lipid.Chains.Deconstruct<AlkylChain, AcylChain>();
                        if (alkyl != null && acyl != null && alkyl.DoubleBond.Bonds.Any(b => b.Position == 1))
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
                    nlMass = C2H8NO4P;
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C2H8NO4P), 100d, "Precursor -C2H8NO4P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 200d, "Precursor - H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                        }
                    );
                    break;
                case LbmClass.PG:
                    if (adduct.AdductIonName == "[M+NH4]+")
                    {
                        nlMass = C3H9O6P + NH3;
                    }
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 100d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(lipid.Mass - C3H9O6P +MassDiffDictionary.ProtonMass, 999d, "Precursor -C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                    );
                    break;
                case LbmClass.PS:
                    nlMass = C3H8NO6P;
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 200d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(lipid.Mass - C3H8NO6P+MassDiffDictionary.ProtonMass, 999d, "Precursor -C3H8NO6P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                    );
                    break;
                case LbmClass.PI:
                    if (adduct.AdductIonName == "[M+NH4]+")
                    {
                        nlMass = C6H13O9P + NH3;
                    }
                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 200d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                            new SpectrumPeak(lipid.Mass - C6H13O9P+MassDiffDictionary.ProtonMass, 999d, "Precursor -C6H13O9P") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true } }
                    );
                    break;

                case LbmClass.TG:
                    nlMass = 0.0;
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    if (lipid.Chains is PositionLevelChains tgChains)
                    {
                        foreach (var chain in lipid.Chains.GetDeterminedChains())
                        {
                            spectrum.Add(new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass, 200d, $"{chain} loss") { SpectrumComment = SpectrumComment.acylchain });
                        }
                    }
                    break;
                case LbmClass.DG:
                    if (adduct.AdductIonName == "[M+NH4]+")
                    {
                        nlMass = H2O + NH3;
                    }
                    else
                    {
                        nlMass = H2O;
                    }
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 100d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    spectrum.Add(new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 200d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass });
                    spectrum.Add(new SpectrumPeak(lipid.Mass - H2O + MassDiffDictionary.ProtonMass, 999d, "[M+H]+ -H2O") { SpectrumComment = SpectrumComment.metaboliteclass });
                    if (lipid.Chains is PositionLevelChains dgChains)
                    {
                        foreach (var chain in lipid.Chains.GetDeterminedChains())
                        {
                            spectrum.Add(new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass - Electron, 800d, $"{chain} loss") { SpectrumComment = SpectrumComment.acylchain });
                        }
                    }

                    break;
                default:
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    break;
            }
            return new OadClassFragment() { nlMass = nlMass, spectrum = spectrum };
        }

        private static readonly double Electron = 0.00054858026;

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double NH3 = new[]
        {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass,
        }.Sum();
        private static readonly double C5H14NO4P = new[] //PC SM Header
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C5H14NO = new[] //PC 104.107
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass,
            MassDiffDictionary.ProtonMass
        }.Sum();

        private static readonly double C2H8NO4P = new[] //PE Header
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C3H9O6P = new[] // PG Header
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C3H8NO6P = new[] // PS Header
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C6H13O9P = new[] // PI Header
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

    }
}
