using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LPCSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LPC 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPC, 521.348140016, new PositionLevelChains(acyl1));

            var generator = new LPCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                104.1069905 ,//C5H14NO+
                166.0627577 ,//Header - H2O
                184.0733224 ,//Header
                224.1046225 ,//Gly-C
                226.0838871 ,//Gly-O -CH2(Sn1)
                240.0995197 ,//-Sn1-O
                258.1100844 ,//-Sn1
                265.25259 ,//18:1(9) acyl+
                284.0893489 ,//Sn1-1-H
                285.0971739 ,//Sn1-1
                286.104999  ,//Sn1-1+H
                298.104999  ,//Sn1-2-H
                299.112824  ,//Sn1-2
                300.120649  ,//Sn1-2+H
                312.120649  ,//Sn1-3-H
                313.1284741 ,//Sn1-3
                314.1362991 ,//Sn1-3+H
                326.1362991 ,//Sn1-4-H
                327.1441241 ,//Sn1-4
                328.1519492 ,//Sn1-4+H
                340.1519492 ,//Sn1-5-H
                341.1597742 ,//Sn1-5
                342.1675992 ,//Sn1-5+H
                354.1675992 ,//Sn1-6-H
                355.1754243 ,//Sn1-6
                356.1832493 ,//Sn1-6+H
                368.1832493 ,//Sn1-7-H
                369.1910743 ,//Sn1-7
                370.1988994 ,//Sn1-7+H
                382.1988994 ,//Sn1-8-H
                383.2067244 ,//Sn1-8
                384.2145494 ,//Sn1-8+H
                395.2067244 ,//Sn1-Δ9-H
                396.2145494 ,//Sn1-Δ9
                397.2223745 ,//Sn1-Δ9+H
                408.2145494 ,//Sn1-10-H
                409.2223745 ,//Sn1-10
                410.2301995 ,//Sn1-10+H
                422.2301995 ,//Sn1-11-H
                423.2380245 ,//Sn1-11
                424.2458496 ,//Sn1-11+H
                436.2458496 ,//Sn1-12-H
                437.2536746 ,//Sn1-12
                438.2614996 ,//Sn1-12+H
                450.2614996 ,//Sn1-13-H
                451.2693246 ,//Sn1-13
                452.2771497 ,//Sn1-13+H
                464.2771497 ,//Sn1-14-H
                465.2849747 ,//Sn1-14
                466.2927997 ,//Sn1-14+H
                478.2927997 ,//Sn1-15-H
                479.3006248 ,//Sn1-15
                480.3084498 ,//Sn1-15+H
                492.3084498 ,//Sn1-16-H
                493.3162748 ,//Sn1-16
                494.3240999 ,//Sn1-16+H
                504.3448353 ,//Precursor-H2O
                506.3240999 ,//Sn1-17-H
                507.3319249 ,//Sn1-17
                508.3397499 ,//Sn1-17+H
                522.3554    ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //LPC 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPC, 521.348140016, new PositionLevelChains(acyl1));

            var generator = new LPCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                206.0552666 ,//Header
                246.0865668 ,//Gly-C
                248.0658313 ,//Gly-O
                262.0814639 ,//-Sn1-O
                265.25259 ,//18:1(9) acyl+
                280.0920286 ,//-Sn1
                306.0712931 ,//Sn1-1-H
                307.0791182 ,//Sn1-1
                308.0869432 ,//Sn1-1+H
                320.0869432 ,//Sn1-2-H
                321.0947682 ,//Sn1-2
                322.1025933 ,//Sn1-2+H
                334.1025933 ,//Sn1-3-H
                335.1104183 ,//Sn1-3
                336.1182433 ,//Sn1-3+H
                348.1182433 ,//Sn1-4-H
                349.1260684 ,//Sn1-4
                350.1338934 ,//Sn1-4+H
                362.1338934 ,//Sn1-5-H
                363.1417184 ,//Sn1-5
                364.1495435 ,//Sn1-5+H
                376.1495435 ,//Sn1-6-H
                377.1573685 ,//Sn1-6
                378.1651935 ,//Sn1-6+H
                390.1651935 ,//Sn1-7-H
                391.1730186 ,//Sn1-7
                392.1808436 ,//Sn1-7+H
                404.1808436 ,//Sn1-8-H
                405.1886686 ,//Sn1-8
                406.1964937 ,//Sn1-8+H
                417.1886686 ,//Sn1-Δ9-H
                418.1964937 ,//Sn1-Δ9
                419.2043187 ,//Sn1-Δ9+H
                430.1964937 ,//Sn1-10-H
                431.2043187 ,//Sn1-10
                432.2121437 ,//Sn1-10+H
                444.2121437 ,//Sn1-11-H
                445.2199688 ,//Sn1-11
                446.2277938 ,//Sn1-11+H
                458.2277938 ,//Sn1-12-H
                459.2356188 ,//Sn1-12
                459.2487599 ,//Precursor -C5H11N
                460.2434439 ,//Sn1-12+H
                472.2434439 ,//Sn1-13-H
                473.2512689 ,//Sn1-13
                474.2590939 ,//Sn1-13+H
                485.26441   ,//Precursor -C3H9N
                486.2590939 ,//Sn1-14-H
                487.2669189 ,//Sn1-14
                488.274744  ,//Sn1-14+H
                500.274744  ,//Sn1-15-H
                501.282569  ,//Sn1-15
                502.290394  ,//Sn1-15+H
                514.290394  ,//Sn1-16-H
                515.2982191 ,//Sn1-16
                516.3060441 ,//Sn1-16+H
                528.3060441 ,//Sn1-17-H
                529.3138691 ,//Sn1-17
                530.3216942 ,//Sn1-17+H
                544.3373442 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }

    [TestClass()]
    public class LPESpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LPE 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPE, 479.3011898, new PositionLevelChains(acyl1));

            var generator = new LPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                142.0252765 ,//Header
                155.010935  ,//C3H9O6P - H2O
                166.0269194 ,//Gly-O - H2O
                173.0214997 ,//C3H9O6P
                182.058 ,//Gly-C
                184.0369696 ,//-CH2(Sn1),Gly-O
                198.052586 ,//-acylChain-O
                216.0631844 ,//-acylChain
                242.0424489 ,//Sn1-1-H
                243.0502739 ,//Sn1-1
                244.058099  ,//Sn1-1+H
                256.058099  ,//Sn1-2-H
                257.065924  ,//Sn1-2
                258.073749  ,//Sn1-2+H
                265.25259 ,//18:1(9) acyl+
                270.073749  ,//Sn1-3-H
                271.0815741 ,//Sn1-3
                272.0893991 ,//Sn1-3+H
                284.0893991 ,//Sn1-4-H
                285.0972241 ,//Sn1-4
                286.1050492 ,//Sn1-4+H
                298.1050492 ,//Sn1-5-H
                299.1128742 ,//Sn1-5
                300.1206992 ,//Sn1-5+H
                312.1206992 ,//Sn1-6-H
                313.1285243 ,//Sn1-6
                314.1363493 ,//Sn1-6+H
                326.1363493 ,//Sn1-7-H
                327.1441743 ,//Sn1-7
                328.1519994 ,//Sn1-7+H
                339.2910485 ,//-Header
                340.1519994 ,//Sn1-8-H
                341.1598244 ,//Sn1-8
                342.1676494 ,//Sn1-8+H
                353.1598244 ,//Sn1-Δ9-H
                354.1676494 ,//Sn1-Δ9
                355.1754745 ,//Sn1-Δ9+H
                366.1676494 ,//Sn1-10-H
                367.1754745 ,//Sn1-10
                368.1832995 ,//Sn1-10+H
                380.1832995 ,//Sn1-11-H
                381.1911245 ,//Sn1-11
                382.1989496 ,//Sn1-11+H
                394.1989496 ,//Sn1-12-H
                395.2067746 ,//Sn1-12
                396.2145996 ,//Sn1-12+H
                408.2145996 ,//Sn1-13-H
                409.2224246 ,//Sn1-13
                410.2302497 ,//Sn1-13+H
                422.2302497 ,//Sn1-14-H
                423.2380747 ,//Sn1-14
                424.2458997 ,//Sn1-14+H
                436.2458997 ,//Sn1-15-H
                437.2537248 ,//Sn1-15
                438.2615498 ,//Sn1-15+H
                450.2615498 ,//Sn1-16-H
                451.2693748 ,//Sn1-16
                452.2771999 ,//Sn1-16+H
                462.29790 ,//Precursor - H2O
                464.2771999 ,//Sn1-17-H
                465.2850249 ,//Sn1-17
                466.2928499 ,//Sn1-17+H
                480.3085    ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //LPE 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPE, 479.3011898, new PositionLevelChains(acyl1));

            var generator = new LPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                164.0072207 ,//Header
                204.0399442 ,//Gly-C
                206.0189138 ,//-CH2(Sn1),Gly-O
                220.034530, // -18:1(9) - O
                238.0451286 ,//-acylChain
                264.0243931 ,//Sn1-1-H
                265.0322182 ,//Sn1-1
                265.25259 ,//18:1(9) acyl+
                266.0400432 ,//Sn1-1+H
                278.0400432 ,//Sn1-2-H
                279.0478682 ,//Sn1-2
                280.0556933 ,//Sn1-2+H
                292.0556933 ,//Sn1-3-H
                293.0635183 ,//Sn1-3
                294.0713433 ,//Sn1-3+H
                306.0713433 ,//Sn1-4-H
                307.0791684 ,//Sn1-4
                308.0869934 ,//Sn1-4+H
                320.0869934 ,//Sn1-5-H
                321.0948184 ,//Sn1-5
                322.1026435 ,//Sn1-5+H
                334.1026435 ,//Sn1-6-H
                335.1104685 ,//Sn1-6
                336.1182935 ,//Sn1-6+H
                339.2893719 ,//Precursor -C2H8NO4P
                348.1182935 ,//Sn1-7-H
                349.1261186 ,//Sn1-7
                350.1339436 ,//Sn1-7+H
                362.1339436 ,//Sn1-8-H
                363.1417686 ,//Sn1-8
                364.1495937 ,//Sn1-8+H
                375.1417686 ,//Sn1-Δ9-H
                376.1495937 ,//Sn1-Δ9
                377.1574187 ,//Sn1-Δ9+H
                388.1495937 ,//Sn1-10-H
                389.1574187 ,//Sn1-10
                390.1652437 ,//Sn1-10+H
                402.1652437 ,//Sn1-11-H
                403.1730688 ,//Sn1-11
                404.1808938 ,//Sn1-11+H
                416.1808938 ,//Sn1-12-H
                417.1887188 ,//Sn1-12
                418.1965439 ,//Sn1-12+H
                430.1965439 ,//Sn1-13-H
                431.2043689 ,//Sn1-13
                432.2121939 ,//Sn1-13+H
                444.2121939 ,//Sn1-14-H
                445.2200189 ,//Sn1-14
                446.227844  ,//Sn1-14+H
                458.227844  ,//Sn1-15-H
                459.235669  ,//Sn1-15
                459.2487599 ,//Precursor -C2H5N
                460.243494  ,//Sn1-15+H
                472.243494  ,//Sn1-16-H
                473.2513191 ,//Sn1-16
                474.2591441 ,//Sn1-16+H
                484.27985 ,//Precursor - H2O
                486.2591441 ,//Sn1-17-H
                487.2669691 ,//Sn1-17
                488.2747942 ,//Sn1-17+H
                502.2904442 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }

    [TestClass()]
    public class LPGSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LPG 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPG, 510.2957701, new PositionLevelChains(acyl1));

            var generator = new LPGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                    155.010386 ,//Header - H2O
                    173.0209525 ,//Header
                    197.020951, // -H2O -CH2(Sn1)
                    213.0522526 ,//Gly-C
                    215.0315172 ,//Gly-O
                    229.0480197 ,//-Sn1-H2O
                    247.0585844 ,//-Sn1
                    265.25259 ,//18:1(9) acyl+
                    273.0378489 ,//Sn1-1-H
                    274.0456739 ,//Sn1-1
                    275.053499  ,//Sn1-1+H
                    287.053499  ,//Sn1-2-H
                    288.061324  ,//Sn1-2
                    289.069149  ,//Sn1-2+H
                    301.069149  ,//Sn1-3-H
                    302.0769741 ,//Sn1-3
                    303.0847991 ,//Sn1-3+H
                    315.0847991 ,//Sn1-4-H
                    316.0926241 ,//Sn1-4
                    317.1004492 ,//Sn1-4+H
                    329.1004492 ,//Sn1-5-H
                    330.1082742 ,//Sn1-5
                    331.1160992 ,//Sn1-5+H
                    339.290225  ,// -Header
                    343.1160992 ,//Sn1-6-H
                    344.1239243 ,//Sn1-6
                    345.1317493 ,//Sn1-6+H
                    357.1317493 ,//Sn1-7-H
                    358.1395743 ,//Sn1-7
                    359.1473994 ,//Sn1-7+H
                    371.1473994 ,//Sn1-8-H
                    372.1552244 ,//Sn1-8
                    373.1630494 ,//Sn1-8+H
                    384.1552244 ,//Sn1-Δ9-H
                    385.1630494 ,//Sn1-Δ9
                    386.1708745 ,//Sn1-Δ9+H
                    397.1630494 ,//Sn1-10-H
                    398.1708745 ,//Sn1-10
                    399.1786995 ,//Sn1-10+H
                    411.1786995 ,//Sn1-11-H
                    412.1865245 ,//Sn1-11
                    413.1943496 ,//Sn1-11+H
                    419.2565559 ,//Precursor -C3H6O2 -H2O
                    425.1943496 ,//Sn1-12-H
                    426.2021746 ,//Sn1-12
                    427.2099996 ,//Sn1-12+H
                    437.2671206 ,//Precursor -C3H6O2
                    439.2099996 ,//Sn1-13-H
                    440.2178246 ,//Sn1-13
                    441.2256497 ,//Sn1-13+H
                    453.2256497 ,//Sn1-14-H
                    454.2334747 ,//Sn1-14
                    455.2412997 ,//Sn1-14+H
                    467.2412997 ,//Sn1-15-H
                    468.2491248 ,//Sn1-15
                    469.2569498 ,//Sn1-15+H
                    475.2827706 ,//precursor-2H2O
                    481.2569498 ,//Sn1-16-H
                    482.2647748 ,//Sn1-16
                    483.2725999 ,//Sn1-16+H
                    493.2933353 ,//precursor-H2O
                    495.2725999 ,//Sn1-17-H
                    496.2804249 ,//Sn1-17
                    497.2882499 ,//Sn1-17+H
                    511.3039    ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_Na()
        {
            //LPG 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPG, 510.2957701, new PositionLevelChains(acyl1));

            var generator = new LPGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                176.99233 ,//Header - H2O
                195.0028967 ,//Header
                219.002896, // -H2O -CH2(Sn1)
                235.0341968 ,//Gly-C
                237.0134614 ,//Gly-O
                251.0299639 ,//-Sn1-H2O
                265.25259 ,//18:1(9) acyl+
                269.0405286 ,//-Sn1
                295.0197931 ,//Sn1-1-H
                296.0276182 ,//Sn1-1
                297.0354432 ,//Sn1-1+H
                309.0354432 ,//Sn1-2-H
                310.0432682 ,//Sn1-2
                311.0510933 ,//Sn1-2+H
                323.0510933 ,//Sn1-3-H
                324.0589183 ,//Sn1-3
                325.0667433 ,//Sn1-3+H
                337.0667433 ,//Sn1-4-H
                338.0745684 ,//Sn1-4
                339.0823934 ,//Sn1-4+H
                351.0823934 ,//Sn1-5-H
                352.0902184 ,//Sn1-5
                353.0980435 ,//Sn1-5+H
                365.0980435 ,//Sn1-6-H
                366.1058685 ,//Sn1-6
                367.1136935 ,//Sn1-6+H
                379.1136935 ,//Sn1-7-H
                380.1215186 ,//Sn1-7
                381.1293436 ,//Sn1-7+H
                393.1293436 ,//Sn1-8-H
                394.1371686 ,//Sn1-8
                395.1449937 ,//Sn1-8+H
                406.1371686 ,//Sn1-Δ9-H
                407.1449937 ,//Sn1-Δ9
                408.1528187 ,//Sn1-Δ9+H
                419.1449937 ,//Sn1-10-H
                420.1528187 ,//Sn1-10
                421.1606437 ,//Sn1-10+H
                433.1606437 ,//Sn1-11-H
                434.1684688 ,//Sn1-11
                435.1762938 ,//Sn1-11+H
                447.1762938 ,//Sn1-12-H
                448.1841188 ,//Sn1-12
                449.1919439 ,//Sn1-12+H
                //459.2490648 ,//Precursor -C3H6O2
                461.1919439 ,//Sn1-13-H
                462.1997689 ,//Sn1-13
                463.2075939 ,//Sn1-13+H
                475.2075939 ,//Sn1-14-H
                476.2154189 ,//Sn1-14
                477.223244  ,//Sn1-14+H
                489.223244  ,//Sn1-15-H
                490.231069  ,//Sn1-15
                491.238894  ,//Sn1-15+H
                503.238894  ,//Sn1-16-H
                504.2467191 ,//Sn1-16
                505.2545441 ,//Sn1-16+H
                517.2545441 ,//Sn1-17-H
                518.2623691 ,//Sn1-17
                519.2701942 ,//Sn1-17+H
                533.2858442 ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class LPISpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LPI 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPI, 598.311814080, new PositionLevelChains(acyl1));

            var generator = new LPISpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                155.0109,// C3H9O6P-H2O
                173.0215,// C3H9O6P
                243.0270,// Header -H2O
                261.0375,// Header
                265.2526,// 18:1(9)acyl+
                301.0688,// Gly-C
                303.0481,// -CH2(Sn1)
                303.0481,// Gly-O
                317.0638,// -18:1(9)-H2O
                335.0743,// -18:1(9)
                339.2899,// [M+H]+-Header
                343.0419,// 18:1(9)C1-H
                344.0498,// 18:1(9)C1
                345.0576,// 18:1(9)C1+H
                357.0576,// 18:1(9)C2-H
                358.0654,// 18:1(9)C2
                359.0732,// 18:1(9)C2+H
                371.0732,// 18:1(9)C3-H
                372.0811,// 18:1(9)C3
                373.0889,// 18:1(9)C3+H
                385.0889,// 18:1(9)C4-H
                386.0967,// 18:1(9)C4
                387.1045,// 18:1(9)C4+H
                399.1045,// 18:1(9)C5-H
                400.1124,// 18:1(9)C5
                401.1202,// 18:1(9)C5+H
                413.1202,// 18:1(9)C6-H
                414.128,// 18:1(9)C6
                415.1358,// 18:1(9)C6+H
                419.2563,// [M+H]+-C6H12O6
                427.1358,// 18:1(9)C7-H
                428.1437,// 18:1(9)C7
                429.1515,// 18:1(9)C7+H
                437.2668,// [M+H]+-C6H10O5
                441.1515,// 18:1(9)C8-H
                442.1593,// 18:1(9)C8
                443.1671,// 18:1(9)C8+H
                454.1593,// 18:1(9)C9-H
                455.1671,// 18:1(9)C9
                456.175,// 18:1(9)C9+H
                467.1671,// 18:1(9)C10-H
                468.175,// 18:1(9)C10
                469.1828,// 18:1(9)C10+H
                481.1828,// 18:1(9)C11-H
                482.1906,// 18:1(9)C11
                483.1984,// 18:1(9)C11+H
                495.1984,// 18:1(9)C12-H
                496.2063,// 18:1(9)C12
                497.2141,// 18:1(9)C12+H
                509.2141,// 18:1(9)C13-H
                510.2219,// 18:1(9)C13
                511.2297,// 18:1(9)C13+H
                523.2297,// 18:1(9)C14-H
                524.2376,// 18:1(9)C14
                525.2454,// 18:1(9)C14+H
                537.2454,// 18:1(9)C15-H
                538.2532,// 18:1(9)C15
                539.261,// 18:1(9)C15+H
                551.261,// 18:1(9)C16-H
                552.2689,// 18:1(9)C16
                553.2767,// 18:1(9)C16+H
                565.2767,// 18:1(9)C17-H
                566.2845,// 18:1(9)C17
                567.2923,// 18:1(9)C17+H
                581.3091,// [M+H]+-H2O
                599.3191,// Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class LPSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LPS 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPS, 523.291019063, new PositionLevelChains(acyl1));

            var generator = new LPSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                155.01093   ,//C3H9O6P-H2O
                173.0124997 ,//C3H9O6P
                186.01749   ,//Header
                226.05066   ,//Gly-C
                228.0253    ,//Gly-O, -CH2(SN1)
                242.042415 , // -18:1(9)-O
                260.05353    ,// -acylChain
                265.25259 ,//18:1(9) acyl+
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
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_Na()
        {
            //LPS 18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LPS, 523.291019063, new PositionLevelChains(acyl1));

            var generator = new LPSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                207.9994342 ,//Header
                248.0326042 ,//Gly-C
                250.0072442 ,//Gly-O
                264.0243594345,// -18:1(9) - O
                265.25259 ,//18:1(9) acyl+
                282.0349286 ,// -acylChain
                308.0141931 ,//Sn1-1-H
                309.0220182 ,//Sn1-1
                310.0298432 ,//Sn1-1+H
                322.0298432 ,//Sn1-2-H
                323.0376682 ,//Sn1-2
                324.0454933 ,//Sn1-2+H
                336.0454933 ,//Sn1-3-H
                337.0533183 ,//Sn1-3
                338.0611433 ,//Sn1-3+H
                350.0611433 ,//Sn1-4-H
                351.0689684 ,//Sn1-4
                352.0767934 ,//Sn1-4+H
                364.0767934 ,//Sn1-5-H
                365.0846184 ,//Sn1-5
                366.0924435 ,//Sn1-5+H
                378.0924435 ,//Sn1-6-H
                379.1002685 ,//Sn1-6
                380.1080935 ,//Sn1-6+H
                392.1080935 ,//Sn1-7-H
                393.1159186 ,//Sn1-7
                394.1237436 ,//Sn1-7+H
                406.1237436 ,//Sn1-8-H
                407.1315686 ,//Sn1-8
                408.1393937 ,//Sn1-8+H
                419.1315686 ,//Sn1-Δ9-H
                420.1393937 ,//Sn1-Δ9
                421.1472187 ,//Sn1-Δ9+H
                432.1393937 ,//Sn1-10-H
                433.1472187 ,//Sn1-10
                434.1550437 ,//Sn1-10+H
                446.1550437 ,//Sn1-11-H
                447.1628688 ,//Sn1-11
                448.1706938 ,//Sn1-11+H
                459.2487599 ,// Precursor-C3H5NO2
                460.1706938 ,//Sn1-12-H
                461.1785188 ,//Sn1-12
                462.1863439 ,//Sn1-12+H
                474.1863439 ,//Sn1-13-H
                475.1941689 ,//Sn1-13
                476.2019939 ,//Sn1-13+H
                488.2019939 ,//Sn1-14-H
                489.2098189 ,//Sn1-14
                490.217644  ,//Sn1-14+H
                502.217644  ,//Sn1-15-H
                503.225469  ,//Sn1-15
                504.233294  ,//Sn1-15+H
                516.233294  ,//Sn1-16-H
                517.2411191 ,//Sn1-16
                518.2489441 ,//Sn1-16+H
                530.2489441 ,//Sn1-17-H
                531.2567691 ,//Sn1-17
                532.2645942 ,//Sn1-17+H
                546.2802442 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }
}
