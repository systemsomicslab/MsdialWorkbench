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
        public void GenerateTest2() {
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
                186.01749   ,	//	Header
                226.05066   ,	//	Gly-C
                228.0253    ,	//	Gly-O
                308.2637   ,	//	"-Header -CH2(Sn1) calc
                322.27924   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                493.27316   ,	//	"-CH2(Sn1)
                507.30017   ,	//	" -acylChain -O
                524.29663   ,	//	" -acylChain
                551.2854    ,	//	Sn1 ,Sn2 1 calc
                565.301     ,	//	Sn1 ,Sn2 2 calc
                579.3167    ,	//	Sn1 ,Sn2 3 calc
                593.3323    ,	//	Sn1 ,Sn2 4 calc
                603.54462   ,	//	Precursor -C3H8NO6P
                607.348     ,	//	Sn1 ,Sn2 5 calc
                621.3636    ,	//	Sn1 ,Sn2 6 calc
                635.3793    ,	//	Sn1 ,Sn2 7 calc
                649.3949    ,	//	Sn1 ,Sn2 8 calc
                662.4027    ,	//	Sn1 ,Sn2 9 calc
                675.4106    ,	//	Sn1 ,Sn2 10 calc
                689.4262    ,	//	Sn1 ,Sn2 11 calc
                703.4419    ,	//	Sn1 ,Sn2 12 calc
                717.4575    ,	//	Sn1 ,Sn2 13 calc
                731.4732    ,	//	Sn1 ,Sn2 14 calc
                743.54816   ,	//	Precursor -CHO2
                745.4888    ,	//	Sn1 ,Sn2 15 calc
                759.5045    ,	//	Sn1 ,Sn2 16 calc
                770.53375   ,	//	Precursor -H2O
                773.5201    ,	//	Sn1 ,Sn2 17 calc
                788.54663   ,	//	Precursor
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
                261.0369865   ,	//	Header
                301.0709   ,	//	Gly-C
                303.0507    ,	//	Gly-O
                323.2944597, //-Header -sn2-O
                329.2475095432, //-Header -CH2(Sn1)
                341.3050244207, //-Header -sn2
                343.263159607, //-Header -sn1-O
                361.2737242931, //-Header -sn1
                583.3241697346, //-sn2-O
                589.277219543201, //-CH2(Sn1)
                601.3347344207, //-sn2
                603.292869607001, //-sn1 -O
                621.303434293101, //-SN1
                627.5351, // -Header
                628.3218240111, //Sn2-1
                642.3374740749, //Sn2-2
                648.290523883501, //Sn1-1
                656.3531241387, //Sn2-3
                662.306173947301, //Sn1-2
                670.3687742025, //Sn2-4
                676.3218240111, //Sn1-3
                683.3765992344, //Sn2-Δ5
                690.3374740749, //Sn1-4
                696.3844242663, //Sn2-6
                704.3531241387, //Sn1-5
                710.4000743301, //Sn2-7
                718.3687742025, //Sn1-6
                723.407899362, //Sn2-Δ8
                732.3844242663, //Sn1-7
                736.4157243939, //Sn2-9
                746.4000743301, //Sn1-8
                750.4313744577, //Sn2-10
                760.4157243939, //Sn1-9
                763.4391994896, //Sn2-Δ11
                774.4313744577, //Sn1-10
                776.4470245215, //Sn2-12
                788.4470245215, //Sn1-11
                790.4626745853, //Sn2-13
                802.4626745853, //Sn1-12
                803.4704996172, //Sn2-Δ14
                816.4783246491, //Sn1-13,Sn2-15
                830.4939747129, //Sn1-14,Sn2-16
                844.5096247767, //Sn1-15,Sn2-17
                858.5252748405, //Sn1-16,Sn2-18
                872.5409249043, //Sn1-17,Sn2-19
                887.5644,  //precursor
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
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+NH4]+"));

            var expects = new[]
            {
                97.98036   ,	//	Header
                139.01358   ,	//	Gly-C
                140.993    ,	//	Gly-O
                307.2632696   ,	//	"-Header -CH2(Sn1) calc
                321.27924   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                405.2400696   ,	//	"-CH2(Sn1) calc
                419.2556   ,	//	" -acylChain -O
                437.2662844   ,	//	" -acylChain calc
                464.2534   ,	//	Sn1 ,Sn2 1 calc
                478.269 ,	//	Sn1 ,Sn2 2 calc
                492.2847    ,	//	Sn1 ,Sn2 3 calc
                506.3003    ,	//	Sn1 ,Sn2 4 calc
                520.316 ,	//	Sn1 ,Sn2 5 calc
                534.3316    ,	//	Sn1 ,Sn2 6 calc
                548.3473    ,	//	Sn1 ,Sn2 7 calc
                562.3629    ,	//	Sn1 ,Sn2 8 calc
                575.3707    ,	//	Sn1 ,Sn2 9 calc
                588.3786    ,	//	Sn1 ,Sn2 10 calc
                602.3942    ,	//	Sn1 ,Sn2 11 calc
                603.5349    ,	//	Header loss
                616.4099    ,	//	Sn1 ,Sn2 12 calc
                630.4255    ,	//	Sn1 ,Sn2 13 calc
                644.4412    ,	//	Sn1 ,Sn2 14 calc
                658.4568    ,	//	Sn1 ,Sn2 15 calc
                672.4725    ,	//	Sn1 ,Sn2 16 calc
                686.4881    ,	//	Sn1 ,Sn2 17 calc
                701.5116    ,	//	[M+H]+
                718.5339   ,	//	Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

}