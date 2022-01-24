using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Enum;
using System.Linq;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.Tests
{

    [TestClass()]
    public class LPISpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //LPI 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPI, 598.311814080, new PositionLevelChains(acyl1));

            var generator = new LPISpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                155.01093   ,//C3H9O6P-H2O
                173.0124997 ,//C3H9O6P
                261.0369865 ,//Header
                301.0709    ,//Gly-C
                303.0507    ,//Gly-O, -CH2(Sn1)
                303.0507    ,//Gly-O, -CH2(Sn1)
                317.0632197 ,//-Sn1-H2O
                335.0737844 ,//-Sn1
                339.2904785 ,// -Header
                361.0530489 ,//Sn1-1-H
                362.0608739 ,//Sn1-1
                363.068699  ,//Sn1-1+H
                375.068699  ,//Sn1-2-H
                376.076524  ,//Sn1-2
                377.084349  ,//Sn1-2+H
                389.084349  ,//Sn1-3-H
                390.0921741 ,//Sn1-3
                391.0999991 ,//Sn1-3+H
                403.0999991 ,//Sn1-4-H
                404.1078241 ,//Sn1-4
                405.1156492 ,//Sn1-4+H
                417.1156492 ,//Sn1-5-H
                418.1234742 ,//Sn1-5
                419.1312992 ,//Sn1-5+H
                419.25625 ,//Precursor -C6H12O6
                431.1312992 ,//Sn1-6-H
                432.1391243 ,//Sn1-6
                433.1469493 ,//Sn1-6+H
                437.26682 ,//Precursor -C6H10O5
                445.1469493 ,//Sn1-7-H
                446.1547743 ,//Sn1-7
                447.1625994 ,//Sn1-7+H
                459.1625994 ,//Sn1-8-H
                460.1704244 ,//Sn1-8
                461.1782494 ,//Sn1-8+H
                472.1704244 ,//Sn1-Δ9-H
                473.1782494 ,//Sn1-Δ9
                474.1860745 ,//Sn1-Δ9+H
                485.1782494 ,//Sn1-10-H
                486.1860745 ,//Sn1-10
                487.1938995 ,//Sn1-10+H
                499.1938995 ,//Sn1-11-H
                500.2017245 ,//Sn1-11
                501.2095496 ,//Sn1-11+H
                513.2095496 ,//Sn1-12-H
                514.2173746 ,//Sn1-12
                515.2251996 ,//Sn1-12+H
                527.2251996 ,//Sn1-13-H
                528.2330246 ,//Sn1-13
                529.2408497 ,//Sn1-13+H
                541.2408497 ,//Sn1-14-H
                542.2486747 ,//Sn1-14
                543.2564997 ,//Sn1-14+H
                555.2564997 ,//Sn1-15-H
                556.2643248 ,//Sn1-15
                557.2721498 ,//Sn1-15+H
                569.2721498 ,//Sn1-16-H
                570.2799748 ,//Sn1-16
                571.2877999 ,//Sn1-16+H
                581.30907   ,//precursor-H2O
                583.2877999 ,//Sn1-17-H
                584.2956249 ,//Sn1-17
                585.3034499 ,//Sn1-17+H
                599.31964   ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class LPSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //LPS 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPS, 523.291019063, new PositionLevelChains(acyl1));

            var generator = new LPSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                155.01093   ,//C3H9O6P-H2O
                173.0124997 ,//C3H9O6P
                186.01749   ,//Header
                226.05066   ,//Gly-C
                228.0253    ,//Gly-O
                228.0253    ,// -CH2(SN1)
                260.05353    ,// -acylChain
                286.0322489 ,//Sn1-1-H
                287.0400739 ,//Sn1-1
                288.047899  ,//Sn1-1+H
                300.047899  ,//Sn1-2-H
                301.055724  ,//Sn1-2
                302.063549  ,//Sn1-2+H
                314.063549  ,//Sn1-3-H
                315.0713741 ,//Sn1-3
                316.0791991 ,//Sn1-3+H
                328.0791991 ,//Sn1-4-H
                329.0870241 ,//Sn1-4
                330.0948492 ,//Sn1-4+H
                339.28992   ,//Precursor -C3H8NO6P
                342.0948492 ,//Sn1-5-H
                343.1026742 ,//Sn1-5
                344.1104992 ,//Sn1-5+H
                356.1104992 ,//Sn1-6-H
                357.1183243 ,//Sn1-6
                358.1261493 ,//Sn1-6+H
                370.1261493 ,//Sn1-7-H
                371.1339743 ,//Sn1-7
                372.1417994 ,//Sn1-7+H
                384.1417994 ,//Sn1-8-H
                385.1496244 ,//Sn1-8
                386.1574494 ,//Sn1-8+H
                397.1496244 ,//Sn1-Δ9-H
                398.1574494 ,//Sn1-Δ9
                399.1652745 ,//Sn1-Δ9+H
                410.1574494 ,//Sn1-10-H
                411.1652745 ,//Sn1-10
                412.1730995 ,//Sn1-10+H
                424.1730995 ,//Sn1-11-H
                425.1809245 ,//Sn1-11
                426.1887496 ,//Sn1-11+H
                438.1887496 ,//Sn1-12-H
                439.1965746 ,//Sn1-12
                440.2043996 ,//Sn1-12+H
                452.2043996 ,//Sn1-13-H
                453.2122246 ,//Sn1-13
                454.2200497 ,//Sn1-13+H
                466.2200497 ,//Sn1-14-H
                467.2278747 ,//Sn1-14
                468.2356997 ,//Sn1-14+H
                479.3012    ,//Precursor -CHO2
                480.2356997 ,//Sn1-15-H
                481.2435248 ,//Sn1-15
                482.2513498 ,//Sn1-15+H
                494.2513498 ,//Sn1-16-H
                495.2591748 ,//Sn1-16
                496.2669999 ,//Sn1-16+H
                506.287 ,//Precursor -H2O
                508.2669999 ,//Sn1-17-H
                509.2748249 ,//Sn1-17
                510.2826499 ,//Sn1-17+H
                524.2983    ,//Precursor

            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    class LPXSpectrumGeneratorTests
    {
    }
}
