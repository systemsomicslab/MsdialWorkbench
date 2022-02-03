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
        public void GenerateTest_Na()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PE, 743.5465235, new PositionLevelChains(acyl1, acyl2));

            var generator = new PESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                164.0072207 ,//Header
                204.0396166 ,//Gly-C
                206.0188811 ,//Gly-O
                //330.2529431 ,//-Header -CH2(Sn1) calc
                //343.2607682 ,//-Header -acylChain -O
                //361.2713328 ,//-Header -acylChain
                387.2505974 ,//Sn-1-H
                388.2584224 ,//Sn-1
                389.2662475 ,//Sn-1+H
                401.2662475 ,//Sn-2-H
                402.2740725 ,//Sn-2
                403.2818975 ,//Sn-2+H
                415.2818975 ,//Sn-3-H
                416.2897226 ,//Sn-3
                417.2975476 ,//Sn-3+H
                429.2975476 ,//Sn-4-H
                430.3053726 ,//Sn-4
                431.3131977 ,//Sn-4+H
                443.3131977 ,//Sn-5-H
                444.3210227 ,//Sn-5
                445.3288477 ,//Sn-5+H
                457.3288477 ,//Sn-6-H
                458.3366727 ,//Sn-6
                459.3444978 ,//Sn-6+H
                471.2720389 ,//-CH2(Sn1)
                471.3444978 ,//Sn-7-H
                472.3523228 ,//Sn-7
                473.3601478 ,//Sn-7+H
                484.2798639 ,// -acylChain -O
                485.3601478 ,//Sn-8-H
                486.3679729 ,//Sn-8
                487.3757979 ,//Sn-8+H
                498.3679729 ,//Sn-Δ9-H
                499.3757979 ,//Sn-Δ9
                500.2747785 ,//-acylChain
                500.3836229 ,//Sn-Δ9+H
                511.3757979 ,//Sn-10-H
                512.3836229 ,//Sn-10
                513.391448  ,//Sn-10+H
                525.391448  ,//Sn-11-H
                526.399273  ,//Sn-11
                527.407098  ,//Sn-11+H
                528.2696931 ,//Sn-1-H
                529.2775182 ,//Sn-1
                530.2853432 ,//Sn-1+H
                539.407098  ,//Sn-12-H
                540.4149231 ,//Sn-12
                541.4227481 ,//Sn-12+H
                542.2853432 ,//Sn-2-H
                543.2931682 ,//Sn-2
                544.3009933 ,//Sn-2+H
                553.4227481 ,//Sn-13-H
                554.4305731 ,//Sn-13
                555.4383982 ,//Sn-13+H
                556.3009933 ,//Sn-3-H
                557.3088183 ,//Sn-3
                558.3166433 ,//Sn-3+H
                567.4383982 ,//Sn-14-H
                568.4462232 ,//Sn-14
                569.4540482 ,//Sn-14+H
                570.3166433 ,//Sn-4-H
                571.3244684 ,//Sn-4
                572.3322934 ,//Sn-4+H
                581.4540482 ,//Sn-15-H
                582.4618733 ,//Sn-15
                583.4696983 ,//Sn-15+H
                584.3322934 ,//Sn-5-H
                585.3401184 ,//Sn-5
                586.3479435 ,//Sn-5+H
                595.4696983 ,//Sn-16-H
                596.4775233 ,//Sn-16
                597.4853484 ,//Sn-16+H
                598.3479435 ,//Sn-6-H
                599.3557685 ,//Sn-6
                600.3635935 ,//Sn-6+H
                603.5347042 ,//-Header[M+H]+
                609.4853484 ,//Sn-17-H
                610.4931734 ,//Sn-17
                611.5009984 ,//Sn-17+H
                612.3635935 ,//Sn-7-H
                613.3714186 ,//Sn-7
                614.3792436 ,//Sn-7+H
                625.5166485 ,//-Header
                626.3792436 ,//Sn-8-H
                627.3870686 ,//Sn-8
                628.3948937 ,//Sn-8+H
                639.3870686 ,//Sn-Δ9-H
                640.3948937 ,//Sn-Δ9
                641.4027187 ,//Sn-Δ9+H
                652.3948937 ,//Sn-10-H
                653.4027187 ,//Sn-10
                654.4105437 ,//Sn-10+H
                666.4105437 ,//Sn-11-H
                667.4183688 ,//Sn-11
                668.4261938 ,//Sn-11+H
                680.4261938 ,//Sn-12-H
                681.4340188 ,//Sn-12
                682.4418439 ,//Sn-12+H
                694.4418439 ,//Sn-13-H
                695.4496689 ,//Sn-13
                696.4574939 ,//Sn-13+H
                708.4574939 ,//Sn-14-H
                709.4653189 ,//Sn-14
                710.473144  ,//Sn-14+H
                721.4784436 ,// Precursor -CH5N -CH2
                722.473144  ,//Sn-15-H
                723.480969  ,//Sn-15
                724.488794  ,//Sn-15+H
                735.4940936 ,//Precursor -CH5N
                736.488794  ,//Sn-16-H
                737.4966191 ,//Sn-16
                738.5044441 ,//Sn-16+H
                750.5044441 ,//Sn-17-H
                751.5122691 ,//Sn-17
                752.5200942 ,//Sn-17+H
                766.5357442 ,//Precursor
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
        public void GenerateTest_H()
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

        [TestMethod()]
        public void GenerateTest_Na()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PS, 787.53633471, new PositionLevelChains(acyl1, acyl2));

            var generator = new PSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                207.9994342 ,//Header
                248.0326042 ,//Gly-C
                250.0072442 ,//Gly-O
                330.2456442 ,//-Header -CH2(SN1) calc
                344.2611842 ,//-Header -acylChain -O
                361.2734142 ,//-Header -acylChain
                387.2505931 ,//Sn1-1-H
                388.2584182 ,//Sn1-1
                389.2662432 ,//Sn1-1+H
                401.2662432 ,//Sn1-2-H
                402.2740682 ,//Sn1-2
                403.2818933 ,//Sn1-2+H
                415.2818933 ,//Sn1-3-H
                416.2897183 ,//Sn1-3
                417.2975433 ,//Sn1-3+H
                429.2975433 ,//Sn1-4-H
                430.3053684 ,//Sn1-4
                431.3131934 ,//Sn1-4+H
                443.3131934 ,//Sn1-5-H
                444.3210184 ,//Sn1-5
                445.3288435 ,//Sn1-5+H
                457.3288435 ,//Sn1-6-H
                458.3366685 ,//Sn1-6
                459.3444935 ,//Sn1-6+H
                471.3444935 ,//Sn1-7-H
                472.3523186 ,//Sn1-7
                473.3601436 ,//Sn1-7+H
                485.3601436 ,//Sn1-8-H
                486.3679686 ,//Sn1-8
                487.3757937 ,//Sn1-8+H
                498.3679686 ,//Sn1-Δ9-H
                499.3757937 ,//Sn1-Δ9
                500.3836187 ,//Sn1-Δ9+H
                511.3757937 ,//Sn1-10-H
                512.3836187 ,//Sn1-10
                513.3914437 ,//Sn1-10+H
                515.2551042 ,//-CH2(SN1)
                525.3914437 ,//Sn1-11-H
                526.3992688 ,//Sn1-11
                527.4070938 ,//Sn1-11+H
                529.2821142 ,// -acylChain -O
                539.4070938 ,//Sn1-12-H
                540.4149188 ,//Sn1-12
                541.4227439 ,//Sn1-12+H
                546.2785742 ,// -acylChain
                553.4227439 ,//Sn1-13-H
                554.4305689 ,//Sn1-13
                555.4383939 ,//Sn1-13+H
                567.4383939 ,//Sn1-14-H
                568.4462189 ,//Sn1-14
                569.454044  ,//Sn1-14+H
                581.454044  ,//Sn1-15-H
                582.461869  ,//Sn1-15
                583.469694  ,//Sn1-15+H
                595.469694  ,//Sn1-16-H
                596.4775191 ,//Sn1-16
                597.4853441 ,//Sn1-16+H
                609.4853441 ,//Sn1-17-H
                610.4931691 ,//Sn1-17
                611.5009942 ,//Sn1-17+H
                625.5265642 ,//Precursor -C3H8NO6P
                723.4940756 ,//Precursor -C3H5NO2
                765.5301042 ,//Precursor -CHO2
                792.5156942 ,//Precursor -H2O
                810.5285742 ,//Precursor
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
        public void GenerateTest_H()
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
        [TestMethod()]
        public void GenerateTest_NH4()
        {
            //PG 18:1(9)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PG, 820.525435677, new PositionLevelChains(acyl1, acyl2));

            var generator = new PGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+NH4]+"));

            var expects = new[]
            {
                173.0209525 ,// Header
                213.0522526 ,// Gly-C
                215.0315172 ,// Gly-O
                321.2793554 ,//-Header  -SN2-O
                339.2899201 ,//-Header  -SN2
                353.2480553 ,//-Header  -CH2(SN1)
                365.2691847 ,//sn2-1-H 
                366.2770097 ,//sn2-1 
                367.2637054 ,//-Header  -SN1-O
                367.2848348 ,//sn2-1+H 
                379.2848348 ,//sn2-2-H 
                380.2926598 ,//sn2-2 
                381.3004848 ,//sn2-2+H 
                385.2742701 ,//-Header  -SN1
                393.3004848 ,//sn2-3-H 
                394.3083098 ,//sn2-3 
                395.3161349 ,//sn2-3+H 
                406.3083098 ,//sn2-Δ4-H 
                407.3161349 ,//sn2-Δ4 
                408.3239599 ,//sn2-Δ4+H 
                411.2535346 ,//sn1-1-H 
                412.2613597 ,//sn1-1 
                413.2691847 ,//sn1-1+H 
                419.3161349 ,//sn2-5-H 
                420.3239599 ,//sn2-5 
                421.3317849 ,//sn2-5+H 
                425.2691847 ,//sn1-2-H 
                426.2770097 ,//sn1-2 
                427.2848348 ,//sn1-2+H 
                433.3317849 ,//sn2-6-H 
                434.33961   ,//sn2-6 
                435.347435  ,//sn2-6+H 
                439.2848348 ,//sn1-3-H 
                440.2926598 ,//sn1-3 
                441.3004848 ,//sn1-3+H 
                446.33961   ,//sn2-Δ7-H 
                447.347435  ,//sn2-Δ7 
                448.35526   ,//sn2-Δ7+H 
                453.3004848 ,//sn1-4-H 
                454.3083098 ,//sn1-4 
                455.3161349 ,//sn1-4+H 
                459.347435  ,//sn2-8-H 
                460.35526   ,//sn2-8 
                461.3630851 ,//sn2-8+H 
                467.3161349 ,//sn1-5-H 
                468.3239599 ,//sn1-5 
                469.3317849 ,//sn1-5+H 
                473.3630851 ,//sn2-9-H 
                474.3709101 ,//sn2-9 
                475.3787351 ,//sn2-9+H 
                481.3317849 ,//sn1-6-H 
                482.33961   ,//sn1-6 
                483.347435  ,//sn1-6+H 
                486.3709101 ,//sn2-Δ10-H 
                487.3787351 ,//sn2-Δ10 
                488.3865602 ,//sn2-Δ10+H 
                493.2930304 ,//-sn2-O 
                495.347435  ,//sn1-7-H 
                496.35526   ,//sn1-7 
                497.3630851 ,//sn1-7+H 
                499.3787351 ,//sn2-11-H 
                500.3865602 ,//sn2-11 
                501.3943852 ,//sn2-11+H 
                507.251165  ,//  -H2O -CH2(Sn1)
                509.3630851 ,//sn1-8-H 
                510.3709101 ,//sn1-8 
                511.3035951 ,//-sn2 
                511.3787351 ,//sn1-8+H 
                513.3943852 ,//sn2-12-H 
                514.4022102 ,//sn2-12 
                515.4100353 ,//sn2-12+H 
                522.3709101 ,//sn1-Δ9-H 
                523.3787351 ,//sn1-Δ9 
                524.3865602 ,//sn1-Δ9+H 
                525.2617303 ,//-CH2(Sn1) 
                526.4022102 ,//sn2-Δ13-H 
                527.4100353 ,//sn2-Δ13 
                528.4178603 ,//sn2-Δ13+H 
                535.3787351 ,//sn1-10-H 
                536.3865602 ,//sn1-10 
                537.3943852 ,//sn1-10+H 
                539.2773804 ,//-sn1  -O
                539.4100353 ,//sn2-14-H 
                540.4178603 ,//sn2-14 
                541.4256853 ,//sn2-14+H 
                549.3943852 ,//sn1-11-H 
                550.4022102 ,//sn1-11 
                551.4100353 ,//sn1-11+H 
                553.4256853 ,//sn2-15-H 
                554.4335104 ,//sn2-15 
                555.4413354 ,//sn2-15+H 
                557.2879451 ,//-sn1 
                563.4100353 ,//sn1-12-H 
                564.4178603 ,//sn1-12 
                565.4256853 ,//sn1-12+H 
                566.4335104 ,//sn2-Δ16-H 
                567.4413354 ,//sn2-Δ16 
                568.4491604 ,//sn2-Δ16+H 
                577.4256853 ,//sn1-13-H 
                578.4335104 ,//sn1-13 
                579.4413354 ,//sn2-17-H sn1-13+H
                580.4491604 ,//sn2-17 
                581.4569855 ,//sn2-17+H 
                591.4413354 ,//sn1-14-H 
                592.4491604 ,//sn1-14 
                593.4569855 ,//sn2-18-H sn1-14+H
                594.4648105 ,//sn2-18 
                595.4726355 ,//sn2-18+H 
                605.4569855 ,//sn1-15-H 
                606.4648105 ,//sn1-15 sn2-Δ19-H
                607.4726355 ,//sn1-15+H sn2-Δ19
                608.4804605 ,//sn2-Δ19+H 
                619.4726355 ,//sn1-16-H sn2-20-H
                620.4804605 ,//sn1-16 sn2-20
                621.4882856 ,//sn1-16+H sn2-20+H
                633.4882856 ,//sn1-17-H sn2-21-H
                634.4961106 ,//sn1-17 sn2-21
                635.5039356 ,//sn1-17+H sn2-21+H
                649.51903   ,//Precursor -C3H9O6P
                764.5230304 ,// Precursor -C3H6O2
                820.5492451 ,//precursor  -H2O
                821.5332607 ,//[M+H]+
                838.5598098 ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //PG 18:1(9)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PG, 820.525435677, new PositionLevelChains(acyl1, acyl2));

            var generator = new PGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                195.0023482 ,// Header
                235.0336483 ,// Gly-C
                237.0129129 ,// Gly-O
                343.2607511 ,//-Header  -SN2-O
                361.2713158 ,//-Header  -SN2
                375.229451  ,//-Header  -CH2(SN1)
                387.2505804 ,//sn2-1-H 
                388.2584054 ,//sn2-1 
                389.245101  ,//-Header  -SN1-O
                389.2662305 ,//sn2-1+H 
                401.2662305 ,//sn2-2-H 
                402.2740555 ,//sn2-2 
                403.2818805 ,//sn2-2+H 
                407.2556657 ,//-Header  -SN1
                415.2818805 ,//sn2-3-H 
                416.2897055 ,//sn2-3 
                417.2975305 ,//sn2-3+H 
                428.2897055 ,//sn2-Δ4-H 
                429.2975305 ,//sn2-Δ4 
                430.3053556 ,//sn2-Δ4+H 
                433.2349303 ,//sn1-1-H 
                434.2427553 ,//sn1-1 
                435.2505804 ,//sn1-1+H 
                441.2975305 ,//sn2-5-H 
                442.3053556 ,//sn2-5 
                443.3131806 ,//sn2-5+H 
                447.2505804 ,//sn1-2-H 
                448.2584054 ,//sn1-2 
                449.2662304 ,//sn1-2+H 
                455.3131806 ,//sn2-6-H 
                456.3210056 ,//sn2-6 
                457.3288307 ,//sn2-6+H 
                461.2662304 ,//sn1-3-H 
                462.2740555 ,//sn1-3 
                463.2818805 ,//sn1-3+H 
                468.3210056 ,//sn2-Δ7-H 
                469.3288307 ,//sn2-Δ7 
                470.3366557 ,//sn2-Δ7+H 
                475.2818805 ,//sn1-4-H 
                476.2897055 ,//sn1-4 
                477.2975305 ,//sn1-4+H 
                481.3288307 ,//sn2-8-H 
                482.3366557 ,//sn2-8 
                483.3444807 ,//sn2-8+H 
                489.2975305 ,//sn1-5-H 
                490.3053556 ,//sn1-5 
                491.3131806 ,//sn1-5+H 
                495.3444807 ,//sn2-9-H 
                496.3523058 ,//sn2-9 
                497.3601308 ,//sn2-9+H 
                503.3131806 ,//sn1-6-H 
                504.3210056 ,//sn1-6 
                505.3288307 ,//sn1-6+H 
                508.3523058 ,//sn2-Δ10-H 
                509.3601308 ,//sn2-Δ10 
                510.3679558 ,//sn2-Δ10+H 
                515.2744261 ,//-sn2-O 
                517.3288307 ,//sn1-7-H 
                518.3366557 ,//sn1-7 
                519.3444807 ,//sn1-7+H 
                521.3601308 ,//sn2-11-H 
                522.3679558 ,//sn2-11 
                523.3757809 ,//sn2-11+H 
                529.2325607 ,//  -H2O -CH2(Sn1)
                531.3444807 ,//sn1-8-H 
                532.3523058 ,//sn1-8 
                533.2849908 ,//-sn2 
                533.3601308 ,//sn1-8+H 
                535.3757809 ,//sn2-12-H 
                536.3836059 ,//sn2-12 
                537.3914309 ,//sn2-12+H 
                544.3523058 ,//sn1-Δ9-H 
                545.3601308 ,//sn1-Δ9 
                546.3679558 ,//sn1-Δ9+H 
                547.243126  ,//-CH2(Sn1) 
                548.3836059 ,//sn2-Δ13-H 
                549.3914309 ,//sn2-Δ13 
                550.399256  ,//sn2-Δ13+H 
                557.3601308 ,//sn1-10-H 
                558.3679558 ,//sn1-10 
                559.3757809 ,//sn1-10+H 
                561.258776  ,//-sn1  -O
                561.3914309 ,//sn2-14-H 
                562.399256  ,//sn2-14 
                563.407081  ,//sn2-14+H 
                571.3757809 ,//sn1-11-H 
                572.3836059 ,//sn1-11 
                573.3914309 ,//sn1-11+H 
                575.407081  ,//sn2-15-H 
                576.414906  ,//sn2-15 
                577.4227311 ,//sn2-15+H 
                579.2693407 ,//-sn1 
                585.3914309 ,//sn1-12-H 
                586.399256  ,//sn1-12 
                587.407081  ,//sn1-12+H 
                588.414906  ,//sn2-Δ16-H 
                589.4227311 ,//sn2-Δ16 
                590.4305561 ,//sn2-Δ16+H 
                599.407081  ,//sn1-13-H 
                600.414906  ,//sn1-13 
                601.4227311 ,//sn2-17-H sn1-13+H
                602.4305561 ,//sn2-17 
                603.4383811 ,//sn2-17+H 
                613.4227311 ,//sn1-14-H 
                614.4305561 ,//sn1-14 
                615.4383811 ,//sn2-18-H sn1-14+H
                616.4462062 ,//sn2-18 
                617.4540312 ,//sn2-18+H 
                627.4383811 ,//sn1-15-H 
                628.4462062 ,//sn1-15 sn2-Δ19-H
                629.4540312 ,//sn1-15+H sn2-Δ19
                630.4618562 ,//sn2-Δ19+H 
                641.4540312 ,//sn1-16-H sn2-20-H
                642.4618562 ,//sn1-16 sn2-20
                643.4696812 ,//sn1-16+H sn2-20+H
                655.4696812 ,//sn1-17-H sn2-21-H
                656.4775063 ,//sn1-17 sn2-21
                657.4853313 ,//sn1-17+H sn2-21+H
                671.5009814 ,//header loss
                769.4778769 ,// Precursor -C3H6O2
                825.5040917 ,//precursor  -H2O
                843.5146564 ,//precursor 
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
        public void GenerateTest_H()
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

        [TestMethod()]
        public void GenerateTest_NH4()
        {
            //PI 18:0_20:4(5,8,11,14)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PI, 886.557129731, new PositionLevelChains(acyl1, acyl2));

            var generator = new PISpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+NH4]+"));

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
                    742.5386491 ,//precursor-C6H10O5
                    887.5644    ,//[M+H]+
                    904.5909491 ,//precursor
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