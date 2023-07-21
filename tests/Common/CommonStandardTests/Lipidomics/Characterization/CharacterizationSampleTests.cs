using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization.Tests
{
    [TestClass()]
    public class CharacterizationSampleTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(PCCharacterizationTestData))]
        public void PCCharacterizationTest(LipidMolecule lipid, IMSScanProperty scan) {
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 8,
                MassDiffDictionary.HydrogenMass * 18,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.ProtonMass,
                
            }.Sum();
            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 7,
                MassDiffDictionary.HydrogenMass * 16,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 5,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.ProtonMass,
            }.Sum();
            double C5H15NO4P = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 14,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.ProtonMass,
            }.Sum();

            var pcType = new PCLipidType();
            var preConditions = new MergePreCondition(
                new IonModeCondition(IonMode.Positive),
                new AdductCondition("[M+H]+"));
            var conditions = new MergeCondition<PCCandidate>(
                new LipidChainCondition<PCCandidate>(c => c.Sn1Carbon >= 10 && c.Sn1DoubleBond <= 6),
                new FragmentsExistAllCondition<PCCandidate>(_ => new[]
                {
                    (C5H15NO4P, 3d),
                    (Gly_C, 3d),
                    (Gly_O, 3d),
                }, .02d),
                new FragmentsNotExistCondition<PCCandidate>(c => new[]
                {
                    (c.Lipid.Mz - 59.0735, 10d),
                    (c.Lipid.Mz - 141.019094261, 5d),
                }, .02d));
            var scorer = new LipidScoring<PCCandidate>(c => {
                var theoreticalMz = c.Lipid.Mz;
                var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(c.Sn1Carbon, c.Sn1DoubleBond) + MassDiffDictionary.HydrogenMass;
                var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.HydrogenMass * 2 - MassDiffDictionary.OxygenMass;

                var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(c.Sn2Carbon, c.Sn2DoubleBond) + MassDiffDictionary.HydrogenMass;
                var nl_NS2_H2O = nl_SN2 - MassDiffDictionary.HydrogenMass * 2 - MassDiffDictionary.OxygenMass;
                return new[]
                {
                    (nl_SN1, 0.1),
                    (nl_SN1_H2O, 0.1),
                    (nl_SN2, 0.1),
                    (nl_NS2_H2O, 0.1),
                };
            }, .02d);
            var search = new IdentitySearchSpace<PCCandidate>(pcType, conditions, scorer);
            var characterizer = new LipidCharacterization<PCCandidate>(pcType, preConditions, _ => search);
            var expected = LipidEieioMsmsCharacterization.JudgeIfPhosphatidylcholine(scan, .02d, lipid.Mz, lipid.TotalCarbonCount, lipid.TotalDoubleBondCount, lipid.Sn1CarbonCount, lipid.TotalCarbonCount - lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount, lipid.TotalDoubleBondCount - lipid.Sn1DoubleBondCount, lipid.Adduct);
            var actual = characterizer.Apply(lipid, scan, .02d);
            Assert.AreEqual(expected?.LipidName, actual?.LipidName);
        }

        public static IEnumerable<object[]> PCCharacterizationTestData {
            get {
                var lipid = new LipidMolecule
                {
                    TotalCarbonCount = 34,
                    TotalDoubleBondCount = 1,
                    Sn1CarbonCount = 16,
                    Sn2CarbonCount = 18,
                    Sn1DoubleBondCount = 0,
                    Sn2DoubleBondCount = 1,
                    Adduct = AdductIon.GetAdductIon("[M+H]+"),
                    Mz = 760.58508f,
                    IonMode = IonMode.Positive,
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 184.07332, Intensity = 300d, },
                            new SpectrumPeak { Mass = 224.10462, Intensity = 150d, },
                            new SpectrumPeak { Mass = 226.08388, Intensity = 100d, },
                            new SpectrumPeak { Mass = 478.32920, Intensity = 50d, },
                            new SpectrumPeak { Mass = 496.33977, Intensity = 50d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 224.10462, Intensity = 150d, },
                            new SpectrumPeak { Mass = 226.08388, Intensity = 100d, },
                            new SpectrumPeak { Mass = 478.32920, Intensity = 50d, },
                            new SpectrumPeak { Mass = 496.33977, Intensity = 50d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 184.07332, Intensity = 300d, },
                            new SpectrumPeak { Mass = 224.10462, Intensity = 150d, },
                            new SpectrumPeak { Mass = 226.08388, Intensity = 100d, },
                            new SpectrumPeak { Mass = 478.32920, Intensity = 50d, },
                            new SpectrumPeak { Mass = 496.33977, Intensity = 50d, },
                            new SpectrumPeak { Mass = 701.51158, Intensity = 20d, }
                        }
                    },
                };
            }
        }

        [DataTestMethod()]
        [DynamicData(nameof(PCCharacterizationTest2Data))]
        public void PCCharacterizationTest2(LipidMolecule lipid, IMSScanProperty scan) {
            var pcType = new PCLipidType();
            var preConditions = new MergePreCondition(
                new IonModeCondition(IonMode.Positive),
                new AdductCondition("[M+Na]+"));
            var conditions = new MergeCondition<PCCandidate>(
                new LipidChainCondition<PCCandidate>(c => c.Sn1DoubleBond <= 6),
                new FragmentsExistAnyCondition<PCCandidate>(c => {
                    //// seek 184.07332 (C5H15NO4P)
                    //var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = c.Lipid.Mz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = c.Lipid.Mz - 59.0735;
                    return new[] {
                        (diagnosticMz2, 3d),
                        (diagnosticMz3, 3d),
                    };
                }, .02d));
            var scorer = new LipidScoring<PCCandidate>(c => {
                var theoreticalMz = c.Lipid.Mz;
                var sn1CarbonNum = c.Sn1Carbon;
                var sn1DoubleNum = c.Sn1DoubleBond;
                var sn2CarbonNum = c.Sn2Carbon;
                var sn2DoubleNum = c.Sn2DoubleBond;
                var H2O = MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.OxygenMass;

                var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum);
                var nl_SN1_H2O = nl_SN1 - H2O;
                var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum);
                var nl_SN2_H2O = nl_SN2 - H2O;

                var query = new List<SpectrumPeak>
                {
                };
                return new[]
                {
                    (nl_SN1, 0.01),
                    (nl_SN1_H2O, 0.01),
                    (nl_SN2, 0.01),
                    (nl_SN2_H2O, 0.01)
                };
            }, .02d);
            var search = new PCSearchSpace(6, 7, conditions, scorer);
            var characterizer = new LipidCharacterization<PCCandidate>(pcType, preConditions, _ => search);
            var expected = LipidEieioMsmsCharacterization.JudgeIfPhosphatidylcholine(scan, .02d, lipid.Mz, lipid.TotalCarbonCount, lipid.TotalDoubleBondCount, lipid.Sn1CarbonCount, lipid.TotalCarbonCount - lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount, lipid.TotalDoubleBondCount - lipid.Sn1DoubleBondCount, lipid.Adduct);
            var actual = characterizer.Apply(lipid, scan, .02d);
            Assert.AreEqual(expected?.LipidName, actual?.LipidName);
        }

        public static IEnumerable<object[]> PCCharacterizationTest2Data {
            get {
                var lipid = new LipidMolecule
                {
                    TotalCarbonCount = 34,
                    TotalDoubleBondCount = 1,
                    Sn1CarbonCount = 16,
                    Sn2CarbonCount = 18,
                    Sn1DoubleBondCount = 0,
                    Sn2DoubleBondCount = 1,
                    Adduct = AdductIon.GetAdductIon("[M+Na]+"),
                    Mz = 782.5670f,
                    IonMode = IonMode.Positive,
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 500.31115, Intensity = 50d, },
                            new SpectrumPeak { Mass = 518.32171, Intensity = 50d, },
                            new SpectrumPeak { Mass = 526.32680, Intensity = 50d, },
                            new SpectrumPeak { Mass = 544.33736, Intensity = 50d, },
                            new SpectrumPeak { Mass = 599.50098, Intensity = 300d, },
                            new SpectrumPeak { Mass = 723.49353, Intensity = 200d, },
                            new SpectrumPeak { Mass = 782.56703, Intensity = 999d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 500.31115, Intensity = 50d, },
                            new SpectrumPeak { Mass = 518.32171, Intensity = 50d, },
                            new SpectrumPeak { Mass = 526.32680, Intensity = 50d, },
                            new SpectrumPeak { Mass = 544.33736, Intensity = 50d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 500.31115, Intensity = 50d, },
                            new SpectrumPeak { Mass = 518.32171, Intensity = 50d, },
                            new SpectrumPeak { Mass = 526.32680, Intensity = 50d, },
                            new SpectrumPeak { Mass = 544.33736, Intensity = 50d, },
                        }
                    },
                };
            }
        }

        [DataTestMethod()]
        [DynamicData(nameof(SHexCerCharacterizationTestData))]
        public void SHexCerCharacterizationTest(LipidMolecule lipid, IMSScanProperty scan) {
            var electron = 0.00054858026;
            var shexcerType = new SHexCerLipidType();
            var preConditions = new MergePreCondition(
                new IonModeCondition(IonMode.Positive),
                new AdductCondition("[M+H]+", "[M+NH4]+"));
            var conditions = new FragmentsExistAnyCondition<SHexCerCandidate>(c => {
                var diagnosticMz = c.Lipid.Adduct.AdductIonName == "[M+NH4]+" ?  c.Lipid.Mz - (MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass * 3) : c.Lipid.Mz;
                // seek [M-SO3-H2O+H]+
                var diagnosticMz1 = diagnosticMz - MassDiffDictionary.SulfurMass - 3 * MassDiffDictionary.OxygenMass - (MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.OxygenMass) - electron;
                // seek [M-H2O-SO3-C6H10O5+H]+
                var diagnosticMz2 = diagnosticMz1 - 162.052833;
                return new[] {
                    (diagnosticMz1, 1d),
                    (diagnosticMz2, 1d),
                };
            }, .02d);
            var scorer = new LipidScoring<SHexCerCandidate>(c => {
                var diagnosticMz = c.Lipid.Adduct.AdductIonName == "[M+NH4]+" ?  c.Lipid.Mz - (MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass * 3) : c.Lipid.Mz;
                // seek [M-SO3-H2O+H]+
                var diagnosticMz1 = diagnosticMz - MassDiffDictionary.SulfurMass - 3 * MassDiffDictionary.OxygenMass - (MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.OxygenMass) - electron;
                // seek [M-H2O-SO3-C6H10O5+H]+
                var diagnosticMz2 = diagnosticMz1 - 162.052833;

                var sph1 = diagnosticMz2 - LipidMsmsCharacterizationUtility.acylCainMass(c.Sn2Carbon, c.Sn2DoubleBond) + MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass * c.Sn2Oxidized;
                var sph2 = sph1 - (MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.OxygenMass);
                var sph3 = sph2 - 12;
                return new[] {
                    (sph1, 1d),
                    (sph2, 1d),
                    (sph3, 1d),
                };
            }, .02d);
            var search = new IdentitySearchSpace<SHexCerCandidate>((m, scan) => new SHexCerCandidate(m, m.Sn1CarbonCount, m.Sn1DoubleBondCount), conditions, scorer);
            var characterizer = new LipidCharacterization<SHexCerCandidate>(shexcerType, preConditions, _ => search);
            var expected = LipidEieioMsmsCharacterization.JudgeIfShexcer(scan, .02d, lipid.Mz, lipid.TotalCarbonCount, lipid.TotalDoubleBondCount, lipid.Sn1CarbonCount, lipid.TotalCarbonCount - lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount, lipid.TotalDoubleBondCount - lipid.Sn1DoubleBondCount, lipid.Adduct, lipid.TotalOxidizedCount);
            var actual = characterizer.Apply(lipid, scan, .02d);
            Assert.AreEqual(expected?.LipidName, actual?.LipidName);
        }

        public static IEnumerable<object[]> SHexCerCharacterizationTestData {
            get {
                var lipid1 = new LipidMolecule
                {
                    TotalCarbonCount = 38,
                    TotalDoubleBondCount = 1,
                    TotalOxidizedCount = 2,
                    Sn1CarbonCount = 18,
                    Sn2CarbonCount = 20,
                    Sn1DoubleBondCount = 1,
                    Sn2DoubleBondCount = 0,
                    Sn1Oxidizedount = 2,
                    Sn2Oxidizedount = 0,
                    Adduct = AdductIon.GetAdductIon("[M+H]+"),
                    Mz = 836.5916f,
                    IonMode = IonMode.Positive,
                };
                yield return new object[] {
                    lipid1,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 252.26858, Intensity = 80d, },
                            new SpectrumPeak { Mass = 264.26858, Intensity = 200d, },
                            new SpectrumPeak { Mass = 282.27914, Intensity = 100d, },
                            new SpectrumPeak { Mass = 576.57141, Intensity = 250d, },
                            new SpectrumPeak { Mass = 738.62423, Intensity = 200d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid1,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 252.26858, Intensity = 80d, },
                            new SpectrumPeak { Mass = 264.26858, Intensity = 200d, },
                            new SpectrumPeak { Mass = 282.27914, Intensity = 100d, },
                        }
                    },
                };
                yield return new object[] {
                    lipid1,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 576.57141, Intensity = 250d, },
                            new SpectrumPeak { Mass = 738.62423, Intensity = 200d, },
                        }
                    },
                };
                var lipid2 = new LipidMolecule
                {
                    TotalCarbonCount = 38,
                    TotalDoubleBondCount = 1,
                    TotalOxidizedCount = 2,
                    Sn1CarbonCount = 18,
                    Sn2CarbonCount = 20,
                    Sn1DoubleBondCount = 1,
                    Sn2DoubleBondCount = 0,
                    Sn1Oxidizedount = 2,
                    Sn2Oxidizedount = 0,
                    Adduct = AdductIon.GetAdductIon("[M+NH4]+"),
                    Mz = (float)(836.5916f + MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass * 3),
                    IonMode = IonMode.Positive,
                };
                yield return new object[] {
                    lipid2,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 252.26858, Intensity = 80d, },
                            new SpectrumPeak { Mass = 264.26858, Intensity = 200d, },
                            new SpectrumPeak { Mass = 282.27914, Intensity = 100d, },
                            new SpectrumPeak { Mass = 576.57141, Intensity = 250d, },
                            new SpectrumPeak { Mass = 738.62423, Intensity = 200d, },
                        }
                    },
                };
                var lipid3 = new LipidMolecule
                {
                    TotalCarbonCount = 38,
                    TotalDoubleBondCount = 1,
                    TotalOxidizedCount = 3,
                    Sn1CarbonCount = 18,
                    Sn2CarbonCount = 20,
                    Sn1DoubleBondCount = 1,
                    Sn2DoubleBondCount = 0,
                    Sn1Oxidizedount = 2,
                    Sn2Oxidizedount = 0,
                    Adduct = AdductIon.GetAdductIon("[M+H]+"),
                    Mz = 852.5865f,
                    IonMode = IonMode.Positive,
                };
                yield return new object[] {
                    lipid3,
                    new MSScanProperty
                    {
                        Spectrum = new List<SpectrumPeak>
                        {
                            new SpectrumPeak { Mass = 252.26858, Intensity = 80d, },
                            new SpectrumPeak { Mass = 264.26858, Intensity = 200d, },
                            new SpectrumPeak { Mass = 282.27914, Intensity = 100d, },
                            new SpectrumPeak { Mass = 592.56632, Intensity = 250d, },
                            new SpectrumPeak { Mass = 754.61915, Intensity = 200d, },
                        }
                    },
                };
            }
        }
    }
}
