using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PCSpectrumGeneratorTests
    {
        #region
        //[TestMethod()]
        //public void GenerateTest() {
        //    var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
        //    var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
        //    var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));

        //    var generator = new PCSpectrumGenerator();
        //    var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

        //    var expects = new[]
        //    {
        //        183.065 + MassDiffDictionary.ProtonMass, // HG + Proton MspGenerator\GlyceroLipidFragmentation.cs C5H15NO4P?
        //        224.105, // Gly-C
        //        226.083, // Gly-O
        //        489.321, // Sn1 -CH2
        //        503.337, // Sn1 -O
        //        507.368, // Sn2 -O
        //        520.340, // Sn1 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN1 acyl loss
        //        524.371, // Sn2 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN2 acyl loss
        //        546.320, // Sn1  1 -H
        //        547.327, // Sn1  1
        //        548.335, // Sn1  1 +H
        //        550.351, // Sn2  1 -H
        //        551.358, // Sn2  1
        //        552.367, // Sn2  1 +H
        //        560.335, // Sn1  2 -H
        //        561.342, // Sn1  2
        //        562.351, // Sn1  2 +H
        //        564.367, // Sn2  2 -H
        //        565.374, // Sn2  2
        //        566.382, // Sn2  2 +H
        //        574.351, // Sn1  3 -H
        //        575.358, // Sn1  3
        //        576.367, // Sn1  3 +H
        //        578.382, // Sn2  3 -H
        //        579.389, // Sn2  3
        //        580.398, // Sn2  3 +H
        //        588.367, // Sn1  4 -H
        //        589.374, // Sn1  4
        //        590.382, // Sn1  4 +H
        //        592.398, // Sn2  4 -H
        //        593.405, // Sn2  4
        //        594.414, // Sn2  4 +H
        //        602.382, // Sn1  5 -H
        //        603.389, // Sn1  5
        //        604.398, // Sn1  5 +H
        //        606.414, // Sn2  5 -H
        //        607.421, // Sn2  5
        //        608.429, // Sn2  5 +H
        //        616.398, // Sn1  6 -H
        //        617.405, // Sn1  6
        //        618.414, // Sn1  6 +H
        //        620.429, // Sn2  6 -H
        //        621.436, // Sn2  6
        //        622.445, // Sn2  6 +H
        //        630.414, // Sn1  7 -H
        //        631.421, // Sn1  7
        //        632.429, // Sn1  7 +H
        //        634.445, // Sn2  7 -H
        //        635.452, // Sn2  7
        //        636.460, // Sn2  7 +H
        //        644.429, // Sn1  8 -H
        //        645.436, // Sn1  8
        //        646.445, // Sn1  8 +H
        //        648.460, // Sn2  8 -H
        //        649.468, // Sn2  8
        //        650.476, // Sn2  8 +H
        //        658.445, // Sn1  9 -H
        //        659.452, // Sn1  9
        //        660.460, // Sn1  9 +H
        //        661.468, // Sn2  9 -H
        //        662.476, // Sn2  9
        //        663.484, // Sn2  9 +H
        //        672.460, // Sn1 10 -H
        //        673.468, // Sn1 10
        //        674.476, // Sn1 10 +H Sn2 10 -H
        //        675.483, // Sn2 10
        //        676.492, // Sn2 10 +H
        //        686.476, // Sn1 11 -H
        //        687.483, // Sn1 11
        //        688.492, // Sn1 11 +H Sn2 11 -H
        //        689.499, // Sn2 11
        //        690.507, // Sn2 11 +H
        //        700.492, // Sn1 12 -H
        //        701.499, // Sn1 12 Sn2 12 -H
        //        702.507, // Sn1 12 +H Sn2 12
        //        703.515, // Sn2 12 +H
        //        714.507, // Sn1 13 Sn2 13 -H
        //        715.515, // Sn1 13 Sn2 13
        //        716.523, // Sn1 13 Sn2 13 +H
        //        728.523, // Sn1 14 Sn2 14 -H
        //        729.530, // Sn1 14 Sn2 14
        //        730.539, // Sn1 14 Sn2 14 +H
        //        742.539, // Sn1 15 Sn2 15 -H
        //        743.546, // Sn1 15 Sn2 15
        //        744.554, // Sn1 15 Sn2 15 +H
        //        756.554, // Sn1 16 Sn2 16 -H
        //        757.562, // Sn1 16 Sn2 16
        //        758.570, // Sn1 16 Sn2 16 +H
        //        770.570, // Sn1 17 Sn2 17 -H
        //        771.577, // Sn1 17 Sn2 17
        //        772.586, // Sn1 17 Sn2 17 +H
        //        786.601, // Sn1 18 Sn2 18 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Precursor Mass
        //    };

        //    scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
        //    foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
        //        Assert.AreEqual(expect, actual, 0.01d);
        //    }
        //}
        #endregion

        [TestMethod()]
        public void PCGenerateTest2_H()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));//PC 18:0_18:2(9,12)
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2)); 

            var generator = new PCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                183.065 + MassDiffDictionary.ProtonMass, // HG + Proton MspGenerator\GlyceroLipidFragmentation.cs C5H15NO4P?
                224.105, // Gly-C
                226.083, // Gly-O
                393.300388, //[Precursor]2+
                489.321, // Sn1 -CH2
                //502.329, // MspGenerator\GlyceroLipidFragmentation.cs SN1-H2O //it is not the optimum value
                502.329246, // Sn1 -O
                //506.360, // MspGenerator\GlyceroLipidFragmentation.cs SN2-H2O //it is not the optimum value
                506.360546, // Sn2 -O
                520.340, // Sn1 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN1 acyl loss
                524.371, // Sn2 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN2 acyl loss
                546.3192989 ,//Sn1-1-H
                547.3271239 ,//Sn1-1
                548.3349489 ,//Sn1-1+H
                550.350599  ,//Sn2-1-H
                551.358424  ,//Sn2-1
                552.366249  ,//Sn2-1+H
                560.3349489 ,//Sn1-2-H
                561.3427739 ,//Sn1-2
                562.350599  ,//Sn1-2+H
                564.366249  ,//Sn2-2-H
                565.3740741 ,//Sn2-2
                566.3818991 ,//Sn2-2+H
                574.350599  ,//Sn1-3-H
                575.358424  ,//Sn1-3
                576.366249  ,//Sn1-3+H
                578.3818991 ,//Sn2-3-H
                579.3897241 ,//Sn2-3
                580.3975492 ,//Sn2-3+H
                588.366249  ,//Sn1-4-H
                589.3740741 ,//Sn1-4
                590.3818991 ,//Sn1-4+H
                592.3975492 ,//Sn2-4-H
                593.4053742 ,//Sn2-4
                594.4131992 ,//Sn2-4+H
                601.5190 ,//Precursor-Header
                602.3818991 ,//Sn1-5-H
                603.3897241 ,//Sn1-5
                604.3975492 ,//Sn1-5+H
                606.4131992 ,//Sn2-5-H
                607.4210243 ,//Sn2-5
                608.4288493 ,//Sn2-5+H
                616.3975492 ,//Sn1-6-H
                617.4053742 ,//Sn1-6
                618.4131992 ,//Sn1-6+H
                620.4288493 ,//Sn2-6-H
                621.4366743 ,//Sn2-6
                622.4444994 ,//Sn2-6+H
                630.4131992 ,//Sn1-7-H
                631.4210243 ,//Sn1-7
                632.4288493 ,//Sn1-7+H
                634.4444994 ,//Sn2-7-H
                635.4523244 ,//Sn2-7
                636.4601494 ,//Sn2-7+H
                644.4288493 ,//Sn1-8-H
                645.4366743 ,//Sn1-8
                646.4444994 ,//Sn1-8+H
                648.4601494 ,//Sn2-8-H
                649.4679745 ,//Sn2-8
                650.4757995 ,//Sn2-8+H
                658.4444994 ,//Sn1-Δ9-H
                659.4523244 ,//Sn1-9
                660.4601494 ,//Sn1-9+H
                661.4679745 ,//Sn2-Δ9-H
                662.4757995 ,//Sn2-Δ9
                663.4836245 ,//Sn2-Δ9+H
                672.4601494 ,//Sn1-10-H
                673.4679745 ,//Sn1-10
                674.4757995 ,//Sn1-10+H,//Sn2-10-H
                675.4836245 ,//Sn2-10
                676.4914496 ,//Sn2-10+H
                686.4757995 ,//Sn1-11-H
                687.4836245 ,//Sn1-11
                688.4914496 ,//Sn1-11+H,//Sn2-11-H
                689.4992746 ,//Sn2-11
                690.5070996 ,//Sn2-11+H
                700.4914496 ,//Sn1-Δ12-H
                701.4992746 ,//Sn1-12,//Sn2-Δ12-H
                702.5070996 ,//Sn2-Δ12,//Sn1-12+H
                703.5149246 ,//Sn2-Δ12+H
                714.5070996 ,//Sn-13-H
                715.5149246 ,//Sn-13
                716.5227497 ,//Sn-13+H
                728.5227497 ,//Sn-14-H
                729.5305747 ,//Sn-14
                730.5383997 ,//Sn-14+H
                742.5383997 ,//Sn-15-H
                743.5462248 ,//Sn-15
                744.5540498 ,//Sn-15+H
                756.5540498 ,//Sn-16-H
                757.5618748 ,//Sn-16
                758.5696999 ,//Sn-16+H
                770.5696999 ,//Sn-17-H
                771.5775249 ,//Sn-17
                772.5853499 ,//Sn-17+H
                786.601, // Sn1 18 Sn2 18 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Precursor Mass
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void PCGenerateTest2_Na()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));

            var generator = new PCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                206.0542207 , // Header
                246.0869442 , // Gly-C
                248.0649442 , // Gly-O
                404.291360, //[Precursor]2+
                511.3035888 , // Sn1 -CH2
                524.31119 , // Sn1 -O
                528.3424904  , // Sn2 -O
                542.3219785 , //SN1 acyl loss
                546.3532787 , //SN2 acyl loss
                568.3012431 ,//Sn1-1-H
                569.3090681 ,//Sn1-1
                570.3168931 ,//Sn1-1+H
                572.3325432 ,//Sn2-1-H
                573.3403682 ,//Sn2-1
                574.3481933 ,//Sn2-1+H
                582.3168931 ,//Sn1-2-H
                583.3247182 ,//Sn1-2
                584.3325432 ,//Sn1-2+H
                586.3481933 ,//Sn2-2-H
                587.3560183 ,//Sn2-2
                588.3638433 ,//Sn2-2+H
                596.3325432 ,//Sn1-3-H
                597.3403682 ,//Sn1-3
                598.3481933 ,//Sn1-3+H
                600.3638433 ,//Sn2-3-H
                601.3716684 ,//Sn2-3
                602.3794934 ,//Sn2-3+H
                610.3481933 ,//Sn1-4-H
                611.3560183 ,//Sn1-4
                612.3638433 ,//Sn1-4+H
                614.3794934 ,//Sn2-4-H
                615.3873184 ,//Sn2-4
                616.3951435 ,//Sn2-4+H
                623.501026 ,//Precursor-Header
                624.3638433 ,//Sn1-5-H
                625.3716684 ,//Sn1-5
                625.5179442 ,// precursor - header
                626.3794934 ,//Sn1-5+H
                628.3951435 ,//Sn2-5-H
                629.4029685 ,//Sn2-5
                630.4107935 ,//Sn2-5+H
                638.3794934 ,//Sn1-6-H
                639.3873184 ,//Sn1-6
                640.3951435 ,//Sn1-6+H
                642.4107935 ,//Sn2-6-H
                643.4186186 ,//Sn2-6
                644.4264436 ,//Sn2-6+H
                652.3951435 ,//Sn1-7-H
                653.4029685 ,//Sn1-7
                654.4107935 ,//Sn1-7+H
                656.4264436 ,//Sn2-7-H
                657.4342686 ,//Sn2-7
                658.4420937 ,//Sn2-7+H
                666.4107935 ,//Sn1-8-H
                667.4186186 ,//Sn1-8
                668.4264436 ,//Sn1-8+H
                670.4420937 ,//Sn2-8-H
                671.4499187 ,//Sn2-8
                672.4577437 ,//Sn2-8+H
                680.4264436 ,//Sn1-Δ9-H
                681.4342686 ,//Sn1-9
                682.4420937 ,//Sn1-9+H
                683.4499187 ,//Sn2-Δ9-H
                684.4577437 ,//Sn2-Δ9
                685.4655688 ,//Sn2-Δ9+H
                694.4420937 ,//Sn1-10-H
                695.4499187 ,//Sn1-10
                696.4577437 ,//Sn1-10+H,//Sn2-10-H
                697.4655688 ,//Sn2-10
                698.4733938 ,//Sn2-10+H
                708.4577437 ,//Sn1-11-H
                709.4655688 ,//Sn1-11
                710.4733938 ,//Sn1-11+H,//Sn2-11-H
                711.4812188 ,//Sn2-11
                712.4890439 ,//Sn2-11+H
                722.4733938 ,//Sn1-Δ12-H
                723.4812188 ,//Sn1-12,//Sn2-Δ12-H
                724.4890439 ,//Sn2-Δ12,//Sn1-12+H
                725.4968689 ,//Sn2-Δ12+H
                736.4890439 ,//Sn-13-H
                737.4968689 ,//Sn-13
                738.5046939 ,//Sn-13+H
                749.5094449 ,// precursor - C3H9N
                750.5046939 ,//Sn-14-H
                751.5125189 ,//Sn-14
                752.520344  ,//Sn-14+H
                764.520344  ,//Sn-15-H
                765.528169  ,//Sn-15
                766.535994  ,//Sn-15+H
                778.535994  ,//Sn-16-H
                779.5438191 ,//Sn-16
                780.5516441 ,//Sn-16+H
                792.5516441 ,//Sn-17-H
                793.5594691 ,//Sn-17
                794.5672942 ,//Sn-17+H
                808.5829442 ,// precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }
}