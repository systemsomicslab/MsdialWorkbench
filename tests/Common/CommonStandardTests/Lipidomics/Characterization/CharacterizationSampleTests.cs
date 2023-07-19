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
                new FragmentsExistCondition<PCCandidate>(_ => new[]
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
            var search = new IdentitySearchSpace<PCCandidate>((m, scan) => new PCCandidate(m, m.Sn1CarbonCount, m.Sn1DoubleBondCount), conditions, scorer);
            var expected = LipidEieioMsmsCharacterization.JudgeIfPhosphatidylcholine(scan, .02d, lipid.Mz, lipid.TotalCarbonCount, lipid.TotalDoubleBondCount, lipid.Sn1CarbonCount, lipid.TotalCarbonCount - lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount, lipid.TotalDoubleBondCount - lipid.Sn1DoubleBondCount, lipid.Adduct);
            if (!preConditions.Satisfy(lipid)) {
                Assert.IsNull(expected);
                return;
            }
            var candidates = search.RetrieveAll(lipid, scan).ToArray();
            var actual = pcType.Convert(lipid, candidates);
            if (expected is null) {
                Assert.IsNull(actual);
                return;
            }
            Assert.AreEqual(expected.LipidName, actual?.LipidName);
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
    }
}
