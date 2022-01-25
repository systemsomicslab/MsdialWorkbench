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
                    142.0252765 ,//Header
                    182.0576723 ,//Gly-C
                    184.0369369 ,//Gly-O
                    308.2709989 ,//-Header -CH2(Sn1) calc
                    321.278823919  ,//-Header -acylChain -O
                    339.2893886 ,//-Header -acylChain
                    365.2686532 ,//Sn-1-H
                    366.2764782 ,//Sn-1
                    367.2843032 ,//Sn-1+H
                    379.2843032 ,//Sn-2-H
                    380.2921283 ,//Sn-2
                    381.2999533 ,//Sn-2+H
                    393.2999533 ,//Sn-3-H
                    394.3077783 ,//Sn-3
                    395.3156034 ,//Sn-3+H
                    407.3156034 ,//Sn-4-H
                    408.3234284 ,//Sn-4
                    409.3312534 ,//Sn-4+H
                    421.3312534 ,//Sn-5-H
                    422.3390785 ,//Sn-5
                    423.3469035 ,//Sn-5+H
                    435.3469035 ,//Sn-6-H
                    436.3547285 ,//Sn-6
                    437.3625535 ,//Sn-6+H
                    449.2900946 ,//-CH2(Sn1)
                    449.3625535 ,//Sn-7-H
                    450.3703786 ,//Sn-7
                    451.3782036 ,//Sn-7+H
                    463.3057447 ,// -acylChain -O
                    463.3782036 ,//Sn-8-H
                    464.3860286 ,//Sn-8
                    465.3938537 ,//Sn-8+H
                    476.3860286 ,//Sn-Δ9-H
                    477.3938537 ,//Sn-Δ9
                    478.4016787 ,//Sn-Δ9+H
                    480.3084844 ,//-acylChain
                    489.3938537 ,//Sn-10-H
                    490.4016787 ,//Sn-10
                    491.4095037 ,//Sn-10+H
                    503.4095037 ,//Sn-11-H
                    504.4173288 ,//Sn-11
                    505.4251538 ,//Sn-11+H
                    506.2877489 ,//Sn-1-H
                    507.2955739 ,//Sn-1
                    508.303399  ,//Sn-1+H
                    517.4251538 ,//Sn-12-H
                    518.4329788 ,//Sn-12
                    519.4408039 ,//Sn-12+H
                    520.303399  ,//Sn-2-H
                    521.311224  ,//Sn-2
                    522.319049  ,//Sn-2+H
                    531.4408039 ,//Sn-13-H
                    532.4486289 ,//Sn-13
                    533.4564539 ,//Sn-13+H
                    534.319049  ,//Sn-3-H
                    535.3268741 ,//Sn-3
                    536.3346991 ,//Sn-3+H
                    545.4564539 ,//Sn-14-H
                    546.464279  ,//Sn-14
                    547.472104  ,//Sn-14+H
                    548.3346991 ,//Sn-4-H
                    549.3425241 ,//Sn-4
                    550.3503492 ,//Sn-4+H
                    559.472104  ,//Sn-15-H
                    560.479929  ,//Sn-15
                    561.4877541 ,//Sn-15+H
                    562.3503492 ,//Sn-5-H
                    563.3581742 ,//Sn-5
                    564.3659992 ,//Sn-5+H
                    573.4877541 ,//Sn-16-H
                    574.4955791 ,//Sn-16
                    575.5034041 ,//Sn-16+H
                    576.3659992 ,//Sn-6-H
                    577.3738243 ,//Sn-6
                    578.3816493 ,//Sn-6+H
                    587.5034041 ,//Sn-17-H
                    588.5112292 ,//Sn-17
                    589.5190542 ,//Sn-17+H
                    590.3816493 ,//Sn-7-H
                    591.3894743 ,//Sn-7
                    592.3972994 ,//Sn-7+H
                    603.5347042 ,//-Header
                    604.3972994 ,//Sn-8-H
                    605.4051244 ,//Sn-8
                    606.4129494 ,//Sn-8+H
                    617.4051244 ,//Sn-Δ9-H
                    618.4129494 ,//Sn-Δ9
                    619.4207745 ,//Sn-Δ9+H
                    630.4129494 ,//Sn-10-H
                    631.4207745 ,//Sn-10
                    632.4285995 ,//Sn-10+H
                    644.4285995 ,//Sn-11-H
                    645.4364245 ,//Sn-11
                    646.4442496 ,//Sn-11+H
                    658.4442496 ,//Sn-12-H
                    659.4520746 ,//Sn-12
                    660.4598996 ,//Sn-12+H
                    672.4598996 ,//Sn-13-H
                    673.4677246 ,//Sn-13
                    674.4755497 ,//Sn-13+H
                    686.4755497 ,//Sn-14-H
                    687.4833747 ,//Sn-14
                    688.4911997 ,//Sn-14+H
                    700.4911997 ,//Sn-15-H
                    701.4990248 ,//Sn-15
                    702.5068498 ,//Sn-15+H
                    714.5068498 ,//Sn-16-H
                    715.5146748 ,//Sn-16
                    716.5224999 ,//Sn-16+H
                    728.5224999 ,//Sn-17-H
                    729.5303249 ,//Sn-17
                    730.5381499 ,//Sn-17+H
                    744.5538    ,//Precursor
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
                    142.0252765 ,//Header
                    182.0576723 ,//Gly-C
                    184.0369369 ,//Gly-O
                    308.2709989 ,//-Header -CH2(Sn1) calc
                    321.2788239 ,//-Header -acylChain -O
                    339.2893886 ,//-Header -acylChain
                    365.2686532 ,//Sn1-1-H,//Sn2-1-H,//Sn2-1-H
                    366.2764782 ,//Sn1-1,//Sn2-1
                    367.2843032 ,//Sn1-1+H,//Sn2-1+H
                    379.2843032 ,//Sn1-2-H,//Sn2-2-H
                    380.2921283 ,//Sn1-2,//Sn2-2
                    381.2999533 ,//Sn1-2+H,//Sn2-2+H
                    393.2999533 ,//Sn1-3-H,//Sn2-3-H
                    394.3077783 ,//Sn1-3,//Sn2-3
                    395.3156034 ,//Sn1-3+H,//Sn2-3+H
                    407.3156034 ,//Sn1-4-H,//Sn2-4-H
                    408.3234284 ,//Sn1-4,//Sn2-4
                    409.3312534 ,//Sn1-4+H,//Sn2-4+H
                    421.3312534 ,//Sn1-5-H,//Sn2-5-H
                    422.3390785 ,//Sn1-5,//Sn2-5
                    423.3469035 ,//Sn1-5+H,//Sn2-5+H
                    435.3469035 ,//Sn1-6-H,//Sn2-6-H
                    436.3547285 ,//Sn1-6,//Sn2-6
                    437.3625535 ,//Sn1-6+H,//Sn2-6+H
                    449.2900946 ,//-CH2(Sn1)
                    449.3625535 ,//Sn1-7-H,//Sn2-7-H
                    450.3703786 ,//Sn1-7,//Sn2-7
                    451.3782036 ,//Sn1-7+H,//Sn2-7+H
                    463.3057447 ,// -acylChain -O
                    463.3782036 ,//Sn1-8-H,//Sn2-8-H
                    464.3860286 ,//Sn1-8,//Sn2-8
                    465.3938537 ,//Sn1-8+H,//Sn2-8+H
                    476.3860286 ,//Sn1-Δ9-H
                    477.3938537 ,//Sn1-Δ9,//Sn2-9-H
                    478.4016787 ,//Sn1-Δ9+H,//Sn2-9
                    479.4095037 ,//Sn2-9+H
                    480.3084844 ,//-acylChain
                    489.3938537 ,//Sn1-10-H
                    490.4016787 ,//Sn1-10
                    491.4095037 ,//Sn1-10+H,//Sn2-10-H
                    492.4173288 ,//Sn2-10
                    493.4251538 ,//Sn2-10+H
                    503.4095037 ,//Sn1-11-H
                    504.4173288 ,//Sn1-11
                    505.4251538 ,//Sn1-11+H,//Sn2-11-H
                    506.2877489 ,//Sn1-1-H,//Sn2-1-H
                    506.4329788 ,//Sn2-11
                    507.2955739 ,//Sn1-1,//Sn2-1
                    507.4408039 ,//Sn2-11+H
                    508.303399  ,//Sn1-1+H,//Sn2-1+H
                    517.4251538 ,//Sn1-12-H
                    518.4329788 ,//Sn1-12,//Sn2-Δ12-H
                    519.4408039 ,//Sn1-12+H,//Sn2-Δ12
                    520.303399  ,//Sn1-2-H,//Sn2-2-H
                    520.4486289 ,//Sn2-Δ12+H
                    521.311224  ,//Sn1-2,//Sn2-2
                    522.319049  ,//Sn1-2+H,//Sn2-2+H
                    531.4408039 ,//Sn1-13-H,//Sn2-13-H
                    532.4486289 ,//Sn1-13,//Sn2-13
                    533.4564539 ,//Sn1-13+H,//Sn2-13+H
                    534.319049  ,//Sn1-3-H,//Sn2-3-H
                    535.3268741 ,//Sn1-3,//Sn2-3
                    536.3346991 ,//Sn1-3+H,//Sn2-3+H
                    545.4564539 ,//Sn1-14-H,//Sn2-14-H
                    546.464279  ,//Sn1-14,//Sn2-14
                    547.472104  ,//Sn1-14+H,//Sn2-14+H
                    548.3346991 ,//Sn1-4-H,//Sn2-4-H
                    549.3425241 ,//Sn1-4,//Sn2-4
                    550.3503492 ,//Sn1-4+H,//Sn2-4+H
                    559.472104  ,//Sn1-15-H,//Sn2-15-H
                    560.479929  ,//Sn1-15,//Sn2-15
                    561.4877541 ,//Sn1-15+H,//Sn2-15+H
                    562.3503492 ,//Sn1-5-H,//Sn2-5-H
                    563.3581742 ,//Sn1-5,//Sn2-5
                    564.3659992 ,//Sn1-5+H,//Sn2-5+H
                    573.4877541 ,//Sn1-16-H,//Sn2-16-H
                    574.4955791 ,//Sn1-16,//Sn2-16
                    575.5034041 ,//Sn1-16+H,//Sn2-16+H
                    576.3659992 ,//Sn1-6-H,//Sn2-6-H
                    577.3738243 ,//Sn1-6,//Sn2-6
                    578.3816493 ,//Sn1-6+H,//Sn2-6+H
                    587.5034041 ,//Sn1-17-H,//Sn2-17-H
                    588.5112292 ,//Sn1-17,//Sn2-17
                    589.5190542 ,//Sn1-17+H,//Sn2-17+H
                    590.3816493 ,//Sn1-7-H,//Sn2-7-H
                    591.3894743 ,//Sn1-7,//Sn2-7
                    592.3972994 ,//Sn1-7+H,//Sn2-7+H
                    603.5347042 ,//-Header
                    604.3972994 ,//Sn1-8-H,//Sn2-8-H
                    605.4051244 ,//Sn1-8,//Sn2-8
                    606.4129494 ,//Sn1-8+H,//Sn2-8+H
                    617.4051244 ,//Sn1-Δ9-H
                    618.4129494 ,//Sn1-Δ9,//Sn2-9-H
                    619.4207745 ,//Sn1-Δ9+H,//Sn2-9
                    620.4285995 ,//Sn2-9+H
                    630.4129494 ,//Sn1-10-H
                    631.4207745 ,//Sn1-10
                    632.4285995 ,//Sn1-10+H,//Sn2-10-H
                    633.4364245 ,//Sn2-10
                    634.4442496 ,//Sn2-10+H
                    644.4285995 ,//Sn1-11-H
                    645.4364245 ,//Sn1-11
                    646.4442496 ,//Sn1-11+H,//Sn2-11-H
                    647.4520746 ,//Sn2-11
                    648.4598996 ,//Sn2-11+H
                    658.4442496 ,//Sn1-12-H
                    659.4520746 ,//Sn1-12,//Sn2-Δ12-H
                    660.4598996 ,//Sn1-12+H,//Sn2-Δ12
                    661.4677246 ,//Sn2-Δ12+H
                    672.4598996 ,//Sn1-13-H,//Sn2-13-H
                    673.4677246 ,//Sn1-13,//Sn2-13
                    674.4755497 ,//Sn1-13+H,//Sn2-13+H
                    686.4755497 ,//Sn1-14-H,//Sn2-14-H
                    687.4833747 ,//Sn1-14,//Sn2-14
                    688.4911997 ,//Sn1-14+H,//Sn2-14+H
                    700.4911997 ,//Sn1-15-H,//Sn2-15-H
                    701.4990248 ,//Sn1-15,//Sn2-15
                    702.5068498 ,//Sn1-15+H,//Sn2-15+H
                    714.5068498 ,//Sn1-16-H,//Sn2-16-H
                    715.5146748 ,//Sn1-16,//Sn2-16
                    716.5224999 ,//Sn1-16+H,//Sn2-16+H
                    728.5224999 ,//Sn1-17-H,//Sn2-17-H
                    729.5303249 ,//Sn1-17,//Sn2-17
                    730.5381499 ,//Sn1-17+H,//Sn2-17+H
                    744.5538    ,//Precursor
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
                    365.2691847 ,//sn2-1-H
                    366.27700972, //sn2-1
                    367.2637053797, //-Header -sn1-O
                    367.2848348 ,//sn2-1+H
                    379.2848348 ,//sn2-2-H
                    380.2926597838, //sn2-2
                    381.3004848 ,//sn2-2+H
                    385.2742700658, //-Header -sn1
                    393.3004848157,//sn2-3-H
                    394.3083098476, //sn2-3
                    395.3161348795,//sn2-3+H
                    406.3083098476,//sn2-Δ4-H
                    407.3161348795, //sn2-Δ4
                    408.3239599114,//sn2-Δ4+H
                    411.2535346243,//sn1-1-H
                    412.2613596562, //sn1-1
                    413.2691846881,//sn1-1+H
                    419.3161348795,//sn2-5-H
                    420.3239599114, //sn2-5
                    421.3317849433,//sn2-5+H
                    425.2691846881,//sn1-2-H
                    426.27700972, //sn1-2
                    427.2848347519,//sn1-2+H
                    433.3317849433,//sn2-6-H
                    434.3396099752, //sn2-6
                    435.3474350071,//sn2-6+H
                    439.2848347519,//sn1-3-H
                    440.2926597838, //sn1-3
                    441.3004848157,//sn1-3+H
                    446.3396099752,//sn2-Δ7-H
                    447.3474350071, //sn2-Δ7
                    448.355260039,//sn2-Δ7+H
                    453.3004848157,//sn1-4-H
                    454.3083098476, //sn1-4
                    455.3161348795,//sn1-4+H
                    459.3474350071,//sn2-8-H
                    460.355260039, //sn2-8
                    461.3630850709,//sn2-8+H
                    467.3161348795,//sn1-5-H
                    468.3239599114, //sn1-5
                    469.3317849433,//sn1-5+H
                    473.3630850709,//sn2-9-H
                    474.3709101028, //sn2-9
                    475.3787351347,//sn2-9+H
                    481.3317849433,//sn1-6-H
                    482.3396099752, //sn1-6
                    483.3474350071,//sn1-6+H
                    486.3709101028,//sn2-Δ10-H
                    487.3787351347, //sn2-Δ10
                    488.3865601666,//sn2-Δ10+H
                    493.2930304435, //-sn2-O
                    495.3474350071,//sn1-7-H
                    496.355260039, //sn1-7
                    497.3630850709,//sn1-7+H
                    499.3787351347,//sn2-11-H
                    500.3865601666, //sn2-11
                    501.3943851985,//sn2-11+H
                    507.251165, // -H2O -CH2(Sn1)
                    509.3630850709,//sn1-8-H
                    510.3709101028, //sn1-8
                    511.3035951296, //-sn2
                    511.3787351347,//sn1-8+H
                    513.3943851985,//sn2-12-H
                    514.4022102304, //sn2-12
                    515.4100352623,//sn2-12+H
                    522.3709101028,//sn1-Δ9-H
                    523.3787351347, //sn1-Δ9
                    524.3865601666,//sn1-Δ9+H
                    525.261730315901, //-CH2(Sn1)
                    526.4022102304,//sn2-Δ13-H
                    527.4100352623, //sn2-Δ13
                    528.4178602942,//sn2-Δ13+H
                    535.3787351347,//sn1-10-H
                    536.3865601666, //sn1-10
                    537.3943851985,//sn1-10+H
                    539.277380379701, //-sn1 -O
                    539.4100352623,//sn2-14-H
                    540.4178602942, //sn2-14
                    541.4256853261,//sn2-14+H
                    549.3943851985,//sn1-11-H
                    550.4022102304, //sn1-11
                    551.4100352623,//sn1-11+H
                    553.4256853261,//sn2-15-H
                    554.433510358, //sn2-15
                    555.4413353899,//sn2-15+H
                    557.2879450658, //-sn1
                    563.4100352623,//sn1-12-H
                    564.4178602942, //sn1-12
                    565.4256853261,//sn1-12+H
                    566.433510358,//sn2-Δ16-H
                    567.4413353899, //sn2-Δ16
                    568.4491604218,//sn2-Δ16+H
                    577.4256853261,//sn1-13-H
                    578.433510358, //sn1-13
                    579.4413353899,//sn2-17-H,sn1-13+H
                    580.4491604218, //sn2-17
                    581.4569854537,//sn2-17+H
                    591.4413353899,//sn1-14-H
                    592.4491604218, //sn1-14
                    593.4569854537,//sn2-18-H,sn1-14+H
                    594.4648104856, //sn2-18
                    595.4726355175,//sn2-18+H
                    605.4569854537,//sn1-15-H
                    606.4648104856,//sn1-15,sn2-Δ19-H
                    607.4726355175, //sn1-15+H,sn2-Δ19
                    608.4804605494,//sn2-Δ19+H
                    619.4726355175,//sn1-16-H,sn2-20-H
                    620.4804605494, //sn1-16,sn2-20
                    621.4882855813,//sn1-16+H,sn2-20+H
                    633.4882855813,//sn1-17-H,sn2-21-H
                    634.4961106132, //sn1-17,sn2-21
                    635.5039356451,//sn1-17+H,sn2-21+H
                    649.5195857 , //header loss
                    747.4964812775299, // Precursor -C3H6O2
                    803.52269602537, //precursor -H2O
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