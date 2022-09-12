using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class OadSpectrumPeakGeneratorTests
    {
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // PC 18:1(9)/18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var lipid01 = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl1));

            var actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(689.4625f,10f),
                new SpectrumPeak(690.4704f,10f),
                new SpectrumPeak(691.4782f,10f),
                new SpectrumPeak(647.4520f,10f),
                new SpectrumPeak(648.4599f,10f),
                new SpectrumPeak(649.4677f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC 18:3(9,12,15)/18:3(9,12,15)
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12, 15), new Oxidized(0));//18:3(9,12,15)
            var lipid02 = new Lipid(LbmClass.PC,777.5309, new PositionLevelChains(acyl2, acyl2));

            actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid02, acyl2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0);
            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(685.4318f,10f),
                new SpectrumPeak(686.4396f,10f),
                new SpectrumPeak(687.4475f,10f),
                new SpectrumPeak(643.4211f,10f),
                new SpectrumPeak(644.4289f,10f),
                new SpectrumPeak(645.4368f,10f),
                new SpectrumPeak(725.4634f,10f),
                new SpectrumPeak(726.4712f,10f),
                new SpectrumPeak(727.4791f,10f),
                new SpectrumPeak(683.4522f,10f),
                new SpectrumPeak(684.46f,10f),
                new SpectrumPeak(685.4679f,10f),
                new SpectrumPeak(765.4931f,10f),
                new SpectrumPeak(766.5009f,10f),
                new SpectrumPeak(767.5088f,10f),
                new SpectrumPeak(723.4834f,10f),
                new SpectrumPeak(724.4912f,10f),
                new SpectrumPeak(725.4991f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC O-18:1(9)/18:1(9)
            var alkyl1 = new AlkylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));// O-18:1(9)
            var lipid03 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl1, acyl1));

            actual = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid03, alkyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(675.4833f,10f),
                new SpectrumPeak(676.4911f,10f),
                new SpectrumPeak(677.499f,10f),
                new SpectrumPeak(633.4728f,10f),
                new SpectrumPeak(634.4806f,10f),
                new SpectrumPeak(635.4885f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC P-18:0/18:1(9)
            var alkyl2 = new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0));// P-18:0 (O-18:1(1))
            var lipid04 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl2, acyl1));

            var alkylPeak = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid04, alkyl2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid04, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0).ToList();

            var actualList = alkylPeak;
            actualList.AddRange(acylPeak);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(563.3587f,10f),
                new SpectrumPeak(564.3665f,10f),
                new SpectrumPeak(675.4833f,10f),
                new SpectrumPeak(676.4911f,10f),
                new SpectrumPeak(677.499f,10f),
                new SpectrumPeak(633.4728f,10f),
                new SpectrumPeak(634.4806f,10f),
                new SpectrumPeak(635.4885f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

        }

        [TestMethod()]
        public void OadSphingoSpectrumPeakGeneratorTest()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // SM 18:1(4);2O/18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var sphingo1 = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3));// sphingo 18:1(4)
            var lipid01 = new Lipid(LbmClass.SM, 728.5832, new PositionLevelChains(sphingo1, acyl1));

            var sphingoPeak = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid01, sphingo1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0).ToList();

            var actualList = sphingoPeak;
            actualList.AddRange(acylPeak);


            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(521.3714f,10f),
                new SpectrumPeak(522.3792f,10f),
                new SpectrumPeak(632.4524f,10f),
                new SpectrumPeak(633.4602f,10f),
                new SpectrumPeak(634.468f,10f),
                new SpectrumPeak(590.4418f,10f),
                new SpectrumPeak(591.4497f,10f),
                new SpectrumPeak(592.4575f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // Cer 18:2(4,8);2O/24:0
            var acyl2 = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));// 24:0
            var sphingo2 = new SphingoChain(18, DoubleBond.CreateFromPosition(4,8), Oxidized.CreateFromPosition(1, 3));// sphingo 18:2(4,8)
            var lipid02 = new Lipid(LbmClass.Cer_NS, 647.6216, new PositionLevelChains(sphingo1, acyl2));

            var actual = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid02, sphingo2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 0.0);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(440.4098f,10f), //Δ4
                new SpectrumPeak(441.4176f,10f),
                new SpectrumPeak(537.478f,10f), //Δ8
                new SpectrumPeak(538.4858f,10f),
                new SpectrumPeak(539.4937f,10f),
                new SpectrumPeak(495.4681f,10f),
                new SpectrumPeak(496.4759f,10f),
                new SpectrumPeak(497.4838f,10f),
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

        }

    }
}