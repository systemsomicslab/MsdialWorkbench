using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class OadSpectrumPeakGeneratorTests
    {
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest01()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();

            // PC 18:1(9)/24:1(15) O=C(OCC(OC(=O)CCCCCCCCCCCC=CCCCCCCCCCC)COP(=O)([O-])OCC[N+](C)(C)C)CCCCCCCC=CCCCCCCCC
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var acyl0 = new AcylChain(24, DoubleBond.CreateFromPosition(15), new Oxidized(0));//24:1(15)
            var lipid00 = new Lipid(LbmClass.PC, 869.6873, new PositionLevelChains(acyl1, acyl0));

            var actual01 = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid00, acyl1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();
            var actual02 = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid00, acyl0, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();
            var actualList = actual01;
            actualList.AddRange(actual02);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(775.5721f,50f),  // 18:1(9)C9+C+O+H OAD01
                new SpectrumPeak(774.5643f,100f),  // 18:1(9)C9+O OAD02
                new SpectrumPeak(790.5592f,20f),  // 18:1(9)C9+O OAD02+O
                new SpectrumPeak(773.5565f,20f),  // 18:1(9)C9+C+O-H OAD03
                new SpectrumPeak(772.5486f,30f),  // 18:1(9)C9+C+O-2H OAD04
                new SpectrumPeak(760.5486f,20f),  // 18:1(9)C9+O OAD08
                new SpectrumPeak(746.5694f,20f),  // 18:1(9)C9 OAD09
                new SpectrumPeak(733.5616f,20f),  // 18:1(9)C9-C+H OAD14
                new SpectrumPeak(732.5537f,80f),  // 18:1(9)C9-C OAD15
                new SpectrumPeak(748.5486f,20f),  // 18:1(9)C9-C OAD15+O
                new SpectrumPeak(731.5459f,10f),  // 18:1(9)C9-C-H OAD16
                new SpectrumPeak(730.5381f,20f),  // 18:1(9)C9-C-2H OAD17
                new SpectrumPeak(775.5721f,50f),  // 24:1(15)C9+C+O+H OAD01
                new SpectrumPeak(774.5643f,100f),  // 24:1(15)C9+O OAD02
                new SpectrumPeak(790.5592f,20f),  // 24:1(15)C9+O OAD02+O
                new SpectrumPeak(773.5565f,20f),  // 24:1(15)C9+C+O-H OAD03
                new SpectrumPeak(772.5486f,30f),  // 24:1(15)C9+C+O-2H OAD04
                new SpectrumPeak(760.5486f,20f),  // 24:1(15)C9+O OAD08
                new SpectrumPeak(746.5694f,20f),  // 24:1(15)C9 OAD09
                new SpectrumPeak(733.5616f,20f),  // 24:1(15)C9-C+H OAD14
                new SpectrumPeak(732.5537f,80f),  // 24:1(15)C9-C OAD15
                new SpectrumPeak(748.5486f,20f),  // 24:1(15)C9-C OAD15+O
                new SpectrumPeak(731.5459f,10f),  // 24:1(15)C9-C-H OAD16
                new SpectrumPeak(730.5381f,20f),  // 24:1(15)C9-C-2H OAD17
            };
            foreach (var item in actualList)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
        }
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest02()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // PC 18:1(9)/18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var lipid01 = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl1));

            var actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(691.4783f,50f),  // 18:1(9)C9+C+O+H OAD01
                new SpectrumPeak(690.4705f,100f),  // 18:1(9)C9+O OAD02
                new SpectrumPeak(706.4654f,20f),  // 18:1(9)C9+O OAD02+O
                new SpectrumPeak(689.4627f,20f),  // 18:1(9)C9+C+O-H OAD03
                new SpectrumPeak(688.4548f,30f),  // 18:1(9)C9+C+O-2H OAD04
                new SpectrumPeak(676.4548f,20f),  // 18:1(9)C9+O OAD08
                new SpectrumPeak(662.4756f,20f),  // 18:1(9)C9 OAD09
                new SpectrumPeak(649.4678f,20f),  // 18:1(9)C9-C+H OAD14
                new SpectrumPeak(648.4599f,80f),  // 18:1(9)C9-C OAD15
                new SpectrumPeak(664.4548f,20f),  // 18:1(9)C9-C OAD15+O
                new SpectrumPeak(647.4521f,10f),  // 18:1(9)C9-C-H OAD16
                new SpectrumPeak(646.4443f,20f),  // 18:1(9)C9-C-2H OAD17
            };

            foreach (var item in actual)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
        }
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest03()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // PC 18:3(9,12,15)/18:3(9,12,15)
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12, 15), new Oxidized(0));//18:3(9,12,15)
            var lipid02 = new Lipid(LbmClass.PC, 777.5309, new PositionLevelChains(acyl2, acyl2));

            var actual = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid02, acyl2, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId);
            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(687.447f,50f),  // 18:3(9,12,15)C9+C+O+H OAD01
                new SpectrumPeak(686.4392f,100f),  // 18:3(9,12,15)C9+O OAD02
                new SpectrumPeak(702.4341f,20f),  // 18:3(9,12,15)C9+O OAD02+O
                new SpectrumPeak(685.4314f,20f),  // 18:3(9,12,15)C9+C+O-H OAD03
                new SpectrumPeak(684.4235f,30f),  // 18:3(9,12,15)C9+C+O-2H OAD04
                new SpectrumPeak(672.4235f,20f),  // 18:3(9,12,15)C9+O OAD08
                new SpectrumPeak(658.4443f,20f),  // 18:3(9,12,15)C9 OAD09
                new SpectrumPeak(645.4365f,20f),  // 18:3(9,12,15)C9-C+H OAD14
                new SpectrumPeak(644.4286f,80f),  // 18:3(9,12,15)C9-C OAD15
                new SpectrumPeak(660.4235f,20f),  // 18:3(9,12,15)C9-C OAD15+O
                new SpectrumPeak(643.4208f,10f),  // 18:3(9,12,15)C9-C-H OAD16
                new SpectrumPeak(642.413f,20f),  // 18:3(9,12,15)C9-C-2H OAD17
                new SpectrumPeak(727.4783f,50f),  // 18:3(9,12,15)C12+C+O+H OAD01
                new SpectrumPeak(726.4705f,100f),  // 18:3(9,12,15)C12+O OAD02
                new SpectrumPeak(742.4654f,20f),  // 18:3(9,12,15)C12+O OAD02+O
                new SpectrumPeak(725.4627f,20f),  // 18:3(9,12,15)C12+C+O-H OAD03
                new SpectrumPeak(724.4548f,30f),  // 18:3(9,12,15)C12+C+O-2H OAD04
                new SpectrumPeak(712.4548f,20f),  // 18:3(9,12,15)C12+O OAD08
                new SpectrumPeak(698.4756f,20f),  // 18:3(9,12,15)C12 OAD09
                new SpectrumPeak(685.4678f,20f),  // 18:3(9,12,15)C12-C+H OAD14
                new SpectrumPeak(684.4599f,80f),  // 18:3(9,12,15)C12-C OAD15
                new SpectrumPeak(700.4548f,20f),  // 18:3(9,12,15)C12-C OAD15+O
                new SpectrumPeak(683.4521f,10f),  // 18:3(9,12,15)C12-C-H OAD16
                new SpectrumPeak(682.4443f,20f),  // 18:3(9,12,15)C12-C-2H OAD17
                new SpectrumPeak(767.5096f,50f),  // 18:3(9,12,15)C15+C+O+H OAD01
                new SpectrumPeak(766.5018f,100f),  // 18:3(9,12,15)C15+O OAD02
                new SpectrumPeak(782.4967f,20f),  // 18:3(9,12,15)C15+O OAD02+O
                new SpectrumPeak(765.494f,20f),  // 18:3(9,12,15)C15+C+O-H OAD03
                new SpectrumPeak(764.4861f,30f),  // 18:3(9,12,15)C15+C+O-2H OAD04
                new SpectrumPeak(752.4861f,20f),  // 18:3(9,12,15)C15+O OAD08
                new SpectrumPeak(738.5069f,20f),  // 18:3(9,12,15)C15 OAD09
                new SpectrumPeak(725.4991f,20f),  // 18:3(9,12,15)C15-C+H OAD14
                new SpectrumPeak(724.4912f,80f),  // 18:3(9,12,15)C15-C OAD15
                new SpectrumPeak(740.4861f,20f),  // 18:3(9,12,15)C15-C OAD15+O
                new SpectrumPeak(723.4834f,10f),  // 18:3(9,12,15)C15-C-H OAD16
                new SpectrumPeak(722.4756f,20f),  // 18:3(9,12,15)C15-C-2H OAD17
            };
            foreach (var item in actual)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
        }
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest04()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // PC O-18:1(9)/18:1(9)
            var alkyl1 = new AlkylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));// O-18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var lipid03 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl1, acyl1));

            var actual = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid03, alkyl1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(677.499f,50f),  // O-18:1(9)C9+C+O+H OAD01
                new SpectrumPeak(676.4912f,100f),  // O-18:1(9)C9+O OAD02
                new SpectrumPeak(692.4861f,20f),  // O-18:1(9)C9+O OAD02+O
                new SpectrumPeak(675.4834f,20f),  // O-18:1(9)C9+C+O-H OAD03
                new SpectrumPeak(674.4755f,30f),  // O-18:1(9)C9+C+O-2H OAD04
                new SpectrumPeak(662.4755f,20f),  // O-18:1(9)C9+O OAD08
                new SpectrumPeak(648.4963f,20f),  // O-18:1(9)C9 OAD09
                new SpectrumPeak(635.4885f,20f),  // O-18:1(9)C9-C+H OAD14
                new SpectrumPeak(634.4806f,80f),  // O-18:1(9)C9-C OAD15
                new SpectrumPeak(650.4755f,20f),  // O-18:1(9)C9-C OAD15+O
                new SpectrumPeak(633.4728f,10f),  // O-18:1(9)C9-C-H OAD16
                new SpectrumPeak(632.465f,20f),  // O-18:1(9)C9-C-2H OAD17
            };
            foreach (var item in actual)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actual))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
        }
        [TestMethod()]
        public void OadSpectrumPeakGeneratorTest05()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // PC P-18:0/18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var alkyl2 = new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0));// P-18:0 (O-18:1(1))
            var lipid04 = new Lipid(LbmClass.EtherPC, 771.6142, new PositionLevelChains(alkyl2, acyl1));

            var alkylPeak = OadSpectrumPeakGenerator.GetAlkylDoubleBondSpectrum(lipid04, alkyl2, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid04, acyl1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();

            var actualList = alkylPeak;
            actualList.AddRange(acylPeak);

            var expected = new SpectrumPeak[]
            {
                new SpectrumPeak(565.3738f,20f),  // P-18:0C1+C+O+H OAD01
                new SpectrumPeak(564.366f,50f),  // P-18:0C1+O OAD02
                //new SpectrumPeak(563.3582f,100f),  // P-18:0C1+C+O-H OAD03
                new SpectrumPeak(677.499f,50f),  // 18:1(9)C9+C+O+H OAD01
                new SpectrumPeak(676.4912f,100f),  // 18:1(9)C9+O OAD02
                new SpectrumPeak(692.4861f,20f),  // 18:1(9)C9+O OAD02+O
                new SpectrumPeak(675.4834f,20f),  // 18:1(9)C9+C+O-H OAD03
                new SpectrumPeak(674.4755f,30f),  // 18:1(9)C9+C+O-2H OAD04
                new SpectrumPeak(662.4755f,20f),  // 18:1(9)C9+O OAD08
                new SpectrumPeak(648.4963f,20f),  // 18:1(9)C9 OAD09
                new SpectrumPeak(635.4885f,20f),  // 18:1(9)C9-C+H OAD14
                new SpectrumPeak(634.4806f,80f),  // 18:1(9)C9-C OAD15
                new SpectrumPeak(650.4755f,20f),  // 18:1(9)C9-C OAD15+O
                new SpectrumPeak(633.4728f,10f),  // 18:1(9)C9-C-H OAD16
                new SpectrumPeak(632.465f,20f),  // 18:1(9)C9-C-2H OAD17
            };
            foreach (var item in actualList)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

        }

        [TestMethod()]
        public void OadSphingoSpectrumPeakGeneratorTest01()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // SM 18:1(4);2O/18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));//18:1(9)
            var sphingo1 = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3));// sphingo 18:1(4)
            var lipid01 = new Lipid(LbmClass.SM, 728.5832, new PositionLevelChains(sphingo1, acyl1));

            var sphingoPeak = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid01, sphingo1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();
            var acylPeak = OadSpectrumPeakGenerator.GetAcylDoubleBondSpectrum(lipid01, acyl1, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId).ToList();

            var actualList = sphingoPeak;
            actualList.AddRange(acylPeak);


            var expected = new SpectrumPeak[]
            {
                // sphingo
                new SpectrumPeak(519.3563f,100f),  // 18:1(4)(1OH,3OH)C4DB
                new SpectrumPeak(520.3636f,50f),  // 18:1(4)(1OH,3OH)C4DB+H
                //acyl
                new SpectrumPeak(634.468f,50f),  // 18:1(9)C9+C+O+H OAD01
                new SpectrumPeak(633.4602f,100f),  // 18:1(9)C9+O OAD02
                new SpectrumPeak(649.4551f,20f),  // 18:1(9)C9+O OAD02+O
                new SpectrumPeak(632.4524f,20f),  // 18:1(9)C9+C+O-H OAD03
                new SpectrumPeak(631.4445f,30f),  // 18:1(9)C9+C+O-2H OAD04
                new SpectrumPeak(619.4445f,20f),  // 18:1(9)C9+O OAD08
                new SpectrumPeak(605.4653f,20f),  // 18:1(9)C9 OAD09
                new SpectrumPeak(592.4575f,20f),  // 18:1(9)C9-C+H OAD14
                new SpectrumPeak(591.4496f,80f),  // 18:1(9)C9-C OAD15
                new SpectrumPeak(607.4445f,20f),  // 18:1(9)C9-C OAD15+O
                new SpectrumPeak(590.4418f,10f),  // 18:1(9)C9-C-H OAD16
                new SpectrumPeak(589.434f,20f),  // 18:1(9)C9-C-2H OAD17
            };
            foreach (var item in actualList)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected.ZipInternal(actualList))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }
        }
        [TestMethod()]
        public void OadSphingoSpectrumPeakGeneratorTest02()
        {
            var OadSpectrumPeakGenerator = new OadSpectrumPeakGenerator();
            // Cer 18:2(4,8);2O/24:0
            var acyl2 = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));// 24:0
            var sphingo2 = new SphingoChain(18, DoubleBond.CreateFromPosition(4, 8), Oxidized.CreateFromPosition(1, 3));// sphingo 18:2(4,8)
            var lipid02 = new Lipid(LbmClass.Cer_NS, 647.6216, new PositionLevelChains(sphingo2, acyl2));

            var actual02 = OadSpectrumPeakGenerator.GetSphingoDoubleBondSpectrum(lipid02, sphingo2, AdductIon.GetAdductIon("[M+H]+"), 0.0, 100.0, oadId);

            var expected02 = new SpectrumPeak[]
            {
                //delta 4
                new SpectrumPeak(440.4103f,100f),  // 18:2(4,8)(1OH,3OH)C4DB
                new SpectrumPeak(441.4176f,50f),  // 18:2(4,8)(1OH,3OH)C4DB+H
                //delta 8
                new SpectrumPeak(539.4908f,50f),  // 18:2(4,8)(1OH,3OH)C8+C+O+H OAD01
                new SpectrumPeak(538.4829f,100f),  // 18:2(4,8)(1OH,3OH)C8+O OAD02
                new SpectrumPeak(554.4779f,20f),  // 18:2(4,8)(1OH,3OH)C8+O OAD02+O
                new SpectrumPeak(537.4751f,20f),  // 18:2(4,8)(1OH,3OH)C8+C+O-H OAD03
                new SpectrumPeak(536.4673f,30f),  // 18:2(4,8)(1OH,3OH)C8+C+O-2H OAD04
                new SpectrumPeak(524.4673f,20f),  // 18:2(4,8)(1OH,3OH)C8+O OAD08
                new SpectrumPeak(510.488f,20f),  // 18:2(4,8)(1OH,3OH)C8 OAD09
                new SpectrumPeak(497.4802f,20f),  // 18:2(4,8)(1OH,3OH)C8-C+H OAD14
                new SpectrumPeak(496.4724f,80f),  // 18:2(4,8)(1OH,3OH)C8-C OAD15
                new SpectrumPeak(512.4673f,20f),  // 18:2(4,8)(1OH,3OH)C8-C OAD15+O
                new SpectrumPeak(495.4646f,10f),  // 18:2(4,8)(1OH,3OH)C8-C-H OAD16
                new SpectrumPeak(494.4567f,20f),  // 18:2(4,8)(1OH,3OH)C8-C-2H OAD17
            };
            foreach (var item in actual02)
            {
                Console.WriteLine($"Mass {item.Mass}, Intensity {item.Intensity}, Comment {item.Comment}");
            }

            foreach ((var e, var a) in expected02.ZipInternal(actual02))
            {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
            }

        }
        string[] oadId = new string[] {
                "OAD01",
                "OAD02",
                "OAD02+O",
                "OAD03",
                "OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                "OAD08",
                "OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                "OAD14",
                "OAD15",
                "OAD15+O",
                "OAD16",
                "OAD17",
                "SphOAD",
                "SphOAD+H",
                //"SphOAD+2H",
            };

    }
}