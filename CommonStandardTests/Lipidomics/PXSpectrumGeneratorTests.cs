using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PESpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PE, 743.5465235, new PositionLevelChains(acyl1, acyl2));

            var generator = new PESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                142.0252765   ,	//	Header
                182.058   ,	//	Gly-C
                184.037    ,	//	Gly-O
                308.2721   ,	//	"-Header -CH2(Sn1) calc
                321.2797   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                449.2900946   ,	//	"-CH2(Sn1)
                463.3057447   ,	//	" -acylChain -O
                480.3103   ,	//	" -acylChain
                507.2955739    ,	//	Sn1 ,Sn2 1 calc
                521.311224     ,	//	Sn1 ,Sn2 2 calc
                535.3268741    ,	//	Sn1 ,Sn2 3 calc
                549.3425241    ,	//	Sn1 ,Sn2 4 calc
                563.3581742     ,	//	Sn1 ,Sn2 5 calc
                577.3738243    ,	//	Sn1 ,Sn2 6 calc
                591.3894743    ,	//	Sn1 ,Sn2 7 calc
                603.5382    ,	//	-Header
                605.4051244    ,	//	Sn1 ,Sn2 8 calc
                618.4129494    ,	//	Sn1 ,Sn2 9 calc
                631.4207745    ,	//	Sn1 ,Sn2 10 calc
                645.4364245    ,	//	Sn1 ,Sn2 11 calc
                659.4520746    ,	//	Sn1 ,Sn2 12 calc
                673.4677246    ,	//	Sn1 ,Sn2 13 calc
                687.4833747    ,	//	Sn1 ,Sn2 14 calc
                701.4990248    ,	//	Sn1 ,Sn2 15 calc
                715.5146748    ,	//	Sn1 ,Sn2 16 calc
                729.5303249    ,	//	Sn1 ,Sn2 17 calc
                744.5538   ,	//	Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest2()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PE, 743.5465235, new PositionLevelChains(acyl1, acyl2));

            var generator = new PESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                142.0252765   ,	//	Header
                182.058   ,	//	Gly-C
                184.037    ,	//	Gly-O
                308.2721   ,	//	"-Header -CH2(Sn1) calc
                321.2797   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                449.2900946   ,	//	"-CH2(Sn1)
                463.3057447   ,	//	" -acylChain -O
                480.3103   ,	//	" -acylChain
                507.2955739    ,	//	Sn1 ,Sn2 1 calc
                521.311224     ,	//	Sn1 ,Sn2 2 calc
                535.3268741    ,	//	Sn1 ,Sn2 3 calc
                549.3425241    ,	//	Sn1 ,Sn2 4 calc
                563.3581742     ,	//	Sn1 ,Sn2 5 calc
                577.3738243    ,	//	Sn1 ,Sn2 6 calc
                591.3894743    ,	//	Sn1 ,Sn2 7 calc
                603.5382    ,	//	-Header
                605.4051244    ,	//	Sn1 ,Sn2 8 calc
                618.4129494    ,	//	Sn1 9 calc
                619.4213    ,	//	Sn2 9 calc
                631.4207745    ,	//	Sn1 10 calc
                633.4370    ,	//	Sn2 10 calc
                645.4364245    ,	//	Sn1 11 calc
                647.4526    ,	//	Sn2 11 calc
                659.4520746    ,	//	Sn1 12 calc
                660.4604   ,	    //	Sn2 12 calc
                673.4677246    ,	//	Sn1 ,Sn2 13 calc
                687.4833747    ,	//	Sn1 ,Sn2 14 calc
                701.4990248    ,	//	Sn1 ,Sn2 15 calc
                715.5146748    ,	//	Sn1 ,Sn2 16 calc
                729.5303249    ,	//	Sn1 ,Sn2 17 calc
                744.5538   ,	//	Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class PSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PS, 787.53633471, new PositionLevelChains(acyl1, acyl2));

            var generator = new PSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                186.01749   ,//Header
                226.05066   ,//Gly-C
                228.0253    ,//Gly-O
                308.2637    ,//-Header -CH2(SN1) calc
                322.27924   ,//-Header -acylChain -O
                339.29147   ,//-Header -acylChain
                365.2686489 ,//Sn1-1-H
                366.2764739 ,//Sn1-1
                367.284299  ,//Sn1-1+H
                379.284299  ,//Sn1-2-H
                380.292124  ,//Sn1-2
                381.299949  ,//Sn1-2+H
                393.299949  ,//Sn1-3-H
                394.3077741 ,//Sn1-3
                395.3155991 ,//Sn1-3+H
                407.3155991 ,//Sn1-4-H
                408.3234241 ,//Sn1-4
                409.3312492 ,//Sn1-4+H
                421.3312492 ,//Sn1-5-H
                422.3390742 ,//Sn1-5
                423.3468992 ,//Sn1-5+H
                435.3468992 ,//Sn1-6-H
                436.3547243 ,//Sn1-6
                437.3625493 ,//Sn1-6+H
                449.3625493 ,//Sn1-7-H
                450.3703743 ,//Sn1-7
                451.3781994 ,//Sn1-7+H
                463.3781994 ,//Sn1-8-H
                464.3860244 ,//Sn1-8
                465.3938494 ,//Sn1-8+H
                476.3860244 ,//Sn1-Δ9-H
                477.3938494 ,//Sn1-Δ9
                478.4016745 ,//Sn1-Δ9+H
                489.3938494 ,//Sn1-10-H
                490.4016745 ,//Sn1-10
                491.4094995 ,//Sn1-10+H
                493.27316   ,//-CH2(SN1)
                503.4094995 ,//Sn1-11-H
                504.4173245 ,//Sn1-11
                505.4251496 ,//Sn1-11+H
                507.30017   ,// -acylChain -O
                517.4251496 ,//Sn1-12-H
                518.4329746 ,//Sn1-12
                519.4407996 ,//Sn1-12+H
                524.29663   ,// -acylChain
                531.4407996 ,//Sn1-13-H
                532.4486246 ,//Sn1-13
                533.4564497 ,//Sn1-13+H
                545.4564497 ,//Sn1-14-H
                546.4642747 ,//Sn1-14
                547.4720997 ,//Sn1-14+H
                559.4720997 ,//Sn1-15-H
                560.4799248 ,//Sn1-15
                561.4877498 ,//Sn1-15+H
                573.4877498 ,//Sn1-16-H
                574.4955748 ,//Sn1-16
                575.5033999 ,//Sn1-16+H
                587.5033999 ,//Sn1-17-H
                588.5112249 ,//Sn1-17
                589.5190499 ,//Sn1-17+H
                603.54462   ,//Precursor -C3H8NO6P
                743.54816   ,//Precursor -CHO2
                770.53375   ,//Precursor -H2O
                788.54663   ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class PGSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //PG 18:1(9)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PG, 820.525435677, new PositionLevelChains(acyl1, acyl2));

            var generator = new PGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                173.0209525   ,	//	Header
                213.0522526   ,	//	Gly-C
                215.0315172    ,	//	Gly-O
                321.2793554435, //-Header -sn2-O
                339.2899201296, //-Header -sn2
                353.2480553159, //-Header -CH2(Sn1)
                367.2637053797, //-Header -sn1-O
                385.2742700658, //-Header -sn1
                493.2930304435, //-sn2-O
                507.251165, // -H2O -CH2(Sn1)
                511.3035951296, //-sn2
                525.261730315901, //-CH2(Sn1)
                538.290684720001, //sn2-1
                539.277380379701, //-sn1 -O
                552.3063347838, //sn2-2
                557.2879450658, //-sn1
                566.3219848476, //sn2-3
                579.3298098795, //sn2-Δ4
                584.275034656201, //sn1-1
                592.3376349114, //sn2-5
                598.290684720001, //sn1-2
                606.3532849752, //sn2-6
                612.3063347838, //sn1-3
                619.3611100071, //sn2-Δ7
                626.3219848476, //sn1-4
                632.368935039, //sn2-8
                640.3376349114, //sn1-5
                646.3845851028, //sn2-9
                649.5195857 , //header loss
                654.3532849752, //sn1-6
                659.3924101347, //sn2-Δ10
                668.368935039, //sn1-7
                672.4002351666, //sn2-11
                682.3845851028, //sn1-8
                686.4158852304, //sn2-12
                695.3924101347, //sn1-Δ9
                699.4237102623, //sn2-Δ13
                708.4002351666, //sn1-10
                712.4315352942, //sn2-14
                722.4158852304, //sn1-11
                726.447185358, //sn2-15
                736.4315352942, //sn1-12
                739.4550103899, //sn2-Δ16
                747.4964812775299, // Precursor -C3H6O2
                750.447185358, //sn1-13
                752.4628354218, //sn2-17
                764.4628354218, //sn1-14
                766.4784854856, //sn2-18
                778.4784854856, //sn1-15
                779.4863105175, //sn2-Δ19
                792.4941355494, //sn1-16,sn2-20
                803.52269602537, //precursor -H2O
                806.5097856132, //sn1-17,sn2-21
                821.5332607089, //precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class PISpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //PI 18:0_20:4(5,8,11,14)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PI, 886.557129731, new PositionLevelChains(acyl1, acyl2));

            var generator = new PISpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                    261.0369865 ,//Header
                    301.0709    ,//Gly-C
                    303.0507    ,//Gly-O
                    329.2475095 ,//-Header -CH2(Sn1)
                    341.3050244 ,//-Header -sn2
                    361.2737243 ,//-Header -sn1
                    367.284289  ,//Sn2-1-H
                    368.292114  ,//Sn2-1
                    369.299939  ,//Sn2-1+H
                    381.299939  ,//Sn2-2-H
                    382.3077641 ,//Sn2-2
                    383.3155891 ,//Sn2-2+H
                    387.2529889 ,//Sn1-1-H
                    388.2608139 ,//Sn1-1
                    389.2686389 ,//Sn1-1+H
                    395.3155891 ,//Sn2-3-H
                    396.3234141 ,//Sn2-3
                    397.3312392 ,//Sn2-3+H
                    401.2686389 ,//Sn1-2-H
                    402.2764639 ,//Sn1-2
                    403.284289  ,//Sn1-2+H
                    409.3312392 ,//Sn2-4-H
                    410.3390642 ,//Sn2-4
                    411.3468892 ,//Sn2-4+H
                    415.284289  ,//Sn1-3-H
                    416.292114  ,//Sn1-3
                    417.299939  ,//Sn1-3+H
                    422.3390642 ,//Sn2-Δ5-H
                    423.3468892 ,//Sn2-Δ5
                    424.3547143 ,//Sn2-Δ5+H
                    429.299939  ,//Sn1-4-H
                    430.3077641 ,//Sn1-4
                    431.3155891 ,//Sn1-4+H
                    435.3468892 ,//Sn2-6-H
                    436.3547143 ,//Sn2-6
                    437.3625393 ,//Sn2-6+H
                    443.3155891 ,//Sn1-5-H
                    444.3234141 ,//Sn1-5
                    445.3312392 ,//Sn1-5+H
                    449.3625393 ,//Sn2-7-H
                    450.3703643 ,//Sn2-7
                    451.3781894 ,//Sn2-7+H
                    457.3312392 ,//Sn1-6-H
                    458.3390642 ,//Sn1-6
                    459.3468892 ,//Sn1-6+H
                    462.3703643 ,//Sn2-Δ8-H
                    463.3781894 ,//Sn2-Δ8
                    464.3860144 ,//Sn2-Δ8+H
                    471.3468892 ,//Sn1-7-H
                    472.3547143 ,//Sn1-7
                    473.3625393 ,//Sn1-7+H
                    475.3781894 ,//Sn2-9-H
                    476.3860144 ,//Sn2-9
                    477.3938394 ,//Sn2-9+H
                    485.3625393 ,//Sn1-8-H
                    486.3703643 ,//Sn1-8
                    487.3781894 ,//Sn1-8+H
                    489.3938394 ,//Sn2-10-H
                    490.4016645 ,//Sn2-10
                    491.4094895 ,//Sn2-10+H
                    499.3781894 ,//Sn1-9-H
                    500.3860144 ,//Sn1-9
                    501.3938394 ,//Sn1-9+H
                    502.4016645 ,//Sn2-Δ11-H
                    503.4094895 ,//Sn2-Δ11
                    504.4173145 ,//Sn2-Δ11+H
                    513.3938394 ,//Sn1-10-H
                    514.4016645 ,//Sn1-10
                    515.4094895 ,//Sn1-10+H,//Sn2-12-H
                    516.4173145 ,//Sn2-12
                    517.4251396 ,//Sn2-12+H
                    527.4094895 ,//Sn1-11-H
                    528.4173145 ,//Sn1-11
                    529.4251396 ,//Sn1-11+H,//Sn2-13-H
                    530.4329646 ,//Sn2-13
                    531.4407896 ,//Sn2-13+H
                    541.4251396 ,//Sn1-12-H
                    542.4329646 ,//Sn1-12,//Sn2-Δ14-H
                    543.4407896 ,//Sn2-Δ14,//Sn1-12+H
                    544.4486146 ,//Sn2-Δ14+H
                    555.4407896 ,//Sn1-13-H,//Sn2-15-H
                    556.4486146 ,//Sn1-13,//Sn2-15
                    557.4564397 ,//Sn1-13+H,//Sn2-15+H
                    569.4564397 ,//Sn1-14-H,//Sn2-16-H
                    570.4642647 ,//Sn1-14,//Sn2-16
                    571.4720897 ,//Sn1-14+H,//Sn2-16+H
                    //583.3241697 ,//-sn2-O
                    583.4720897 ,//Sn1-15-H,//Sn2-17-H
                    584.4799148 ,//Sn1-15,//Sn2-17
                    585.4877398 ,//Sn1-15+H,//Sn2-17+H
                    589.2772195 ,//-CH2(Sn1)
                    597.4877398 ,//Sn1-16-H,//Sn2-18-H
                    598.4955648 ,//Sn1-16,//Sn2-18
                    599.5033899 ,//Sn1-16+H,//Sn2-18+H
                    601.3347344 ,//-sn2
                    //603.2928696 ,//-sn1 -O
                    611.5033899 ,//Sn1-17-H,//Sn2-19-H
                    612.5112149 ,//Sn1-17,//Sn2-19
                    613.5190399 ,//Sn1-17+H,//Sn2-19+H
                    621.3034343 ,//-SN1
                    627.5351    ,// -Header
                    725.5121    ,//precursor-C6H10O5
                    887.5644    ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class PASpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //standard PA 18:1(n-9)/18:1(n-9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PA, 700.504306309, new PositionLevelChains(acyl1, acyl2));

            var generator = new PASpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                    139.01358   ,//Gly-C
                    140.993 ,//Gly-O
                    173.0124997 ,//C3H9O6P
                    307.2632696 ,// Header -CH2(SN1) calc
                    321.27924   ,// -Header -acylChain -O
                    339.29147   ,// -Header -acylChain
                    365.2687489 ,//Sn1-1-H
                    366.2765739 ,//Sn1-1
                    367.284399  ,//Sn1-1+H
                    379.284399  ,//Sn1-2-H
                    380.292224  ,//Sn1-2
                    381.300049  ,//Sn1-2+H
                    393.300049  ,//Sn1-3-H
                    394.3078741 ,//Sn1-3
                    395.3156991 ,//Sn1-3+H
                    405.2400696 ,// -CH2(SN1) calc
                    407.3156991 ,//Sn1-4-H
                    408.3235241 ,//Sn1-4
                    409.3313492 ,//Sn1-4+H
                    419.2556    ,// -acylChain -O
                    421.3313492 ,//Sn1-5-H
                    422.3391742 ,//Sn1-5
                    423.3469992 ,//Sn1-5+H
                    435.3469992 ,//Sn1-6-H
                    436.3548243 ,//Sn1-6
                    437.2662844 ,// -acylChain calc
                    437.3626493 ,//Sn1-6+H
                    449.3626493 ,//Sn1-7-H
                    450.3704743 ,//Sn1-7
                    451.3782994 ,//Sn1-7+H
                    463.3782994 ,//Sn1-8-H
                    464.3861244 ,//Sn1-8
                    465.3939494 ,//Sn1-8+H
                    476.3861244 ,//Sn1-Δ9-H
                    477.3939494 ,//Sn1-Δ9
                    478.4017745 ,//Sn1-Δ9+H
                    489.3939494 ,//Sn1-10-H
                    490.4017745 ,//Sn1-10
                    491.4095995 ,//Sn1-10+H
                    503.4095995 ,//Sn1-11-H
                    504.4174245 ,//Sn1-11
                    505.4252496 ,//Sn1-11+H
                    517.4252496 ,//Sn1-12-H
                    518.4330746 ,//Sn1-12
                    519.4408996 ,//Sn1-12+H
                    531.4408996 ,//Sn1-13-H
                    532.4487246 ,//Sn1-13
                    533.4565497 ,//Sn1-13+H
                    545.4565497 ,//Sn1-14-H
                    546.4643747 ,//Sn1-14
                    547.4721997 ,//Sn1-14+H
                    559.4721997 ,//Sn1-15-H
                    560.4800248 ,//Sn1-15
                    561.4878498 ,//Sn1-15+H
                    573.4878498 ,//Sn1-16-H
                    574.4956748 ,//Sn1-16
                    575.5034999 ,//Sn1-16+H
                    587.5034999 ,//Sn1-17-H
                    588.5113249 ,//Sn1-17
                    589.5191499 ,//Sn1-17+H
                    603.5349    ,//Header loss
                    701.5116    ,//[M+H]+
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

}