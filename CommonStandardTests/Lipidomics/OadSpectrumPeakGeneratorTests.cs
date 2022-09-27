using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
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

            // PC 18:1(9)/24:1(15) O=C(OCC(OC(=O)CCCCCCCCCCCC=CCCCCCCCCCC)COP(=O)([O-])OCC[N+](C)(C)C)CCCCCCCC=CCCCCCCCC
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var lipid00 = new Lipid(LbmClass.PC,869.6873, new PositionLevelChains(acyl1, acyl1));

            var actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid00, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(775.5720f,10f),//OAD01
                new SpectrumPeak(774.5642f,10f),//OAD02
                new SpectrumPeak(773.5563f,10f),//OAD03
                new SpectrumPeak(757.5614f,10f),//OAD05
                new SpectrumPeak(756.5536f,10f),//OAD06
                new SpectrumPeak(755.5458f,10f),//OAD07
                new SpectrumPeak(760.5485f,10f),//OAD08
                new SpectrumPeak(746.5692f,10f),//OAD09
                new SpectrumPeak(745.5614f,10f),//OAD10
                new SpectrumPeak(744.5536f,10f),//OAD11
                new SpectrumPeak(746.5329f,10f),//OAD13
                new SpectrumPeak(733.5614f,10f),//OAD14
                new SpectrumPeak(732.5536f,10f),//OAD15
                new SpectrumPeak(731.5458f,10f),//OAD16
                new SpectrumPeak(730.5380f,10f),//OAD17
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
            // PC 18:1(9)/18:1(9)
            var lipid01 = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl1));

            actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(691.4782f,10f), // OAD01
                new SpectrumPeak(690.4704f,10f), // OAD02
                new SpectrumPeak(689.4626f,10f), // OAD03
                new SpectrumPeak(673.4677f,10f), // OAD05
                new SpectrumPeak(672.4599f,10f), // OAD06
                new SpectrumPeak(671.452f,10f), // OAD07
                new SpectrumPeak(676.4548f,10f), // OAD08
                new SpectrumPeak(662.4755f,10f), // OAD09
                new SpectrumPeak(661.4677f,10f), // OAD10
                new SpectrumPeak(660.4599f,10f), // OAD11
                new SpectrumPeak(662.4391f,10f), // OAD13
                new SpectrumPeak(649.4677f,10f), // OAD14
                new SpectrumPeak(648.4599f,10f), // OAD15
                new SpectrumPeak(647.452f,10f), // OAD16
                new SpectrumPeak(646.4442f,10f), // OAD17
            };

            actual.ToList().ForEach(n => System.Console.WriteLine($"Mass {n.Mass}, Intensity {n.Intensity}, Comment {n.Comment}"));

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC 18:3(9,12,15)/18:3(9,12,15)
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12, 15), new Oxidized(0));//18:3(9,12,15)
            var lipid02 = new Lipid(LbmClass.PC,777.5309, new PositionLevelChains(acyl2, acyl2));

            actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid02, acyl2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0);
            expected = new SpectrumPeak[]
            {
                    new SpectrumPeak(687.447f,10f), // OAD01
                    new SpectrumPeak(686.4391f,10f), // OAD02
                    new SpectrumPeak(685.4313f,10f), // OAD03
                    new SpectrumPeak(669.4364f,10f), // OAD05
                    new SpectrumPeak(668.4286f,10f), // OAD06
                    new SpectrumPeak(667.4208f,10f), // OAD07
                    new SpectrumPeak(672.4235f,10f), // OAD08
                    new SpectrumPeak(658.4442f,10f), // OAD09
                    new SpectrumPeak(657.4364f,10f), // OAD10
                    new SpectrumPeak(656.4286f,10f), // OAD11
                    new SpectrumPeak(658.4078f,10f), // OAD13
                    new SpectrumPeak(645.4364f,10f), // OAD14
                    new SpectrumPeak(644.4286f,10f), // OAD15
                    new SpectrumPeak(643.4208f,10f), // OAD16
                    new SpectrumPeak(642.4129f,10f), // OAD17
                    new SpectrumPeak(727.4783f,10f), // OAD01
                    new SpectrumPeak(726.4704f,10f), // OAD02
                    new SpectrumPeak(725.4626f,10f), // OAD03
                    new SpectrumPeak(709.4677f,10f), // OAD05
                    new SpectrumPeak(708.4599f,10f), // OAD06
                    new SpectrumPeak(707.4521f,10f), // OAD07
                    new SpectrumPeak(712.4548f,10f), // OAD08
                    new SpectrumPeak(698.4755f,10f), // OAD09
                    new SpectrumPeak(697.4677f,10f), // OAD10
                    new SpectrumPeak(696.4599f,10f), // OAD11
                    new SpectrumPeak(698.4391f,10f), // OAD13
                    new SpectrumPeak(685.4677f,10f), // OAD14
                    new SpectrumPeak(684.4599f,10f), // OAD15
                    new SpectrumPeak(683.4521f,10f), // OAD16
                    new SpectrumPeak(682.4442f,10f), // OAD17
                    new SpectrumPeak(767.5096f,10f), // OAD01
                    new SpectrumPeak(766.5017f,10f), // OAD02
                    new SpectrumPeak(765.4939f,10f), // OAD03
                    new SpectrumPeak(749.499f,10f), // OAD05
                    new SpectrumPeak(748.4912f,10f), // OAD06
                    new SpectrumPeak(747.4834f,10f), // OAD07
                    new SpectrumPeak(752.4861f,10f), // OAD08
                    new SpectrumPeak(738.5068f,10f), // OAD09
                    new SpectrumPeak(737.499f,10f), // OAD10
                    new SpectrumPeak(736.4912f,10f), // OAD11
                    new SpectrumPeak(738.4704f,10f), // OAD13
                    new SpectrumPeak(725.499f,10f), // OAD14
                    new SpectrumPeak(724.4912f,10f), // OAD15
                    new SpectrumPeak(723.4834f,10f), // OAD16
                    new SpectrumPeak(722.4755f,10f), // OAD17
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC O-18:1(9)/18:1(9)
            var alkyl1 = new AlkylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));// O-18:1(9)
            var lipid03 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl1, acyl1));

            actual = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid03, alkyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(677.499f,10f), // OAD01
                new SpectrumPeak(676.4912f,10f), // OAD02
                new SpectrumPeak(675.4834f,10f), // OAD03
                new SpectrumPeak(659.4884f,10f), // OAD05
                new SpectrumPeak(658.4806f,10f), // OAD06
                new SpectrumPeak(657.4728f,10f), // OAD07
                new SpectrumPeak(662.4755f,10f), // OAD08
                new SpectrumPeak(648.4963f,10f), // OAD09
                new SpectrumPeak(647.4884f,10f), // OAD10
                new SpectrumPeak(646.4806f,10f), // OAD11
                new SpectrumPeak(648.4599f,10f), // OAD13
                new SpectrumPeak(635.4884f,10f), // OAD14
                new SpectrumPeak(634.4806f,10f), // OAD15
                new SpectrumPeak(633.4728f,10f), // OAD16
                new SpectrumPeak(632.465f,10f), // OAD17
            };

            foreach ((var e, var a) in expected.Zip(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // PC P-18:0/18:1(9)
            var alkyl2 = new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0));// P-18:0 (O-18:1(1))
            var lipid04 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl2, acyl1));

            var alkylPeak = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid04, alkyl2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid04, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0).ToList();

            var actualList = alkylPeak;
            actualList.AddRange(acylPeak);

            expected = new SpectrumPeak[]
            {
                new SpectrumPeak(565.3738f,10f), // OAD01
                new SpectrumPeak(564.366f,10f), // OAD02
                new SpectrumPeak(563.3582f,10f), // OAD03
                new SpectrumPeak(677.499f,10f), // OAD01
                new SpectrumPeak(676.4912f,10f), // OAD02
                new SpectrumPeak(675.4834f,10f), // OAD03
                new SpectrumPeak(659.4884f,10f), // OAD05
                new SpectrumPeak(658.4806f,10f), // OAD06
                new SpectrumPeak(657.4728f,10f), // OAD07
                new SpectrumPeak(662.4755f,10f), // OAD08
                new SpectrumPeak(648.4963f,10f), // OAD09
                new SpectrumPeak(647.4884f,10f), // OAD10
                new SpectrumPeak(646.4806f,10f), // OAD11
                new SpectrumPeak(648.4599f,10f), // OAD13
                new SpectrumPeak(635.4884f,10f), // OAD14
                new SpectrumPeak(634.4806f,10f), // OAD15
                new SpectrumPeak(633.4728f,10f), // OAD16
                new SpectrumPeak(632.465f,10f), // OAD17
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

            var sphingoPeak = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid01, sphingo1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0).ToList();

            var actualList = sphingoPeak;
            actualList.AddRange(acylPeak);


            var expected = new SpectrumPeak[]
            {
                // sphingo
                new SpectrumPeak(519.3557f,10f), // OAD17
                new SpectrumPeak(520.3636f,10f), // OAD16
                //acyl
                new SpectrumPeak(634.468f, 10f), // OAD01
                new SpectrumPeak(633.4602f, 10f), // OAD02
                new SpectrumPeak(632.4524f, 10f), // OAD03
                new SpectrumPeak(616.4575f, 10f), // OAD05
                new SpectrumPeak(615.4496f, 10f), // OAD06
                new SpectrumPeak(614.4418f, 10f), // OAD07
                new SpectrumPeak(619.4446f, 10f), // OAD08
                new SpectrumPeak(605.4653f, 10f), // OAD09
                new SpectrumPeak(604.4575f, 10f), // OAD10
                new SpectrumPeak(603.4496f, 10f), // OAD11
                new SpectrumPeak(605.4289f, 10f), // OAD13
                new SpectrumPeak(592.4575f, 10f), // OAD14
                new SpectrumPeak(591.4496f, 10f), // OAD15
                new SpectrumPeak(590.4418f, 10f), // OAD16
                new SpectrumPeak(589.434f, 10f), // OAD17
            };

            foreach ((var e, var a) in expected.Zip(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

            // Cer 18:2(4,8);2O/24:0
            var acyl2 = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));// 24:0
            var sphingo2 = new SphingoChain(18, DoubleBond.CreateFromPosition(4,8), Oxidized.CreateFromPosition(1, 3));// sphingo 18:2(4,8)
            var lipid02 = new Lipid(LbmClass.Cer_NS, 647.6216, new PositionLevelChains(sphingo1, acyl2));

            var actual02 = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid02, sphingo2, AdductIonParser.GetAdductIonBean("[M+H]+"), 0.0, 100.0);

            var expected02 = new SpectrumPeak[]
            {
                //Δ4
                new SpectrumPeak(440.4098f,10f), 
                new SpectrumPeak(441.4176f,10f),
                //Δ8
                new SpectrumPeak(539.4908f,10f), // OAD01
                new SpectrumPeak(538.483f,10f), // OAD02
                new SpectrumPeak(537.4751f,10f), // OAD03
                new SpectrumPeak(521.4802f,10f), // OAD05
                new SpectrumPeak(520.4724f,10f), // OAD06
                new SpectrumPeak(519.4646f,10f), // OAD07
                new SpectrumPeak(524.4673f,10f), // OAD08
                new SpectrumPeak(510.488f,10f), // OAD09
                new SpectrumPeak(509.4802f,10f), // OAD10
                new SpectrumPeak(508.4724f,10f), // OAD11
                new SpectrumPeak(510.4517f,10f), // OAD13
                new SpectrumPeak(497.4802f,10f), // OAD14
                new SpectrumPeak(496.4724f,10f), // OAD15
                new SpectrumPeak(495.4646f,10f), // OAD16
                new SpectrumPeak(494.4567f,10f), // OAD17
            };

            foreach ((var e, var a) in expected02.Zip(actual02))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

        }

    }
}