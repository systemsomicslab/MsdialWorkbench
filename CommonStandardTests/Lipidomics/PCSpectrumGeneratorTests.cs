using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PCSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest() {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));

            var generator = new PCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                183.065 + MassDiffDictionary.ProtonMass, // HG + Proton MspGenerator\GlyceroLipidFragmentation.cs C5H15NO4P?
                224.105, // Gly-C
                226.083, // Gly-O
                489.321, // Sn1 -CH2
                //502.329, // MspGenerator\GlyceroLipidFragmentation.cs SN1-H2O //it is not the optimum value
                503.337, // Sn1 -O
                //506.360, // MspGenerator\GlyceroLipidFragmentation.cs SN2-H2O //it is not the optimum value
                507.368, // Sn2 -O
                520.340, // Sn1 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN1 acyl loss
                524.371, // Sn2 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN2 acyl loss
                547.327, // Sn1  1
                551.358, // Sn2  1
                561.342, // Sn1  2
                565.374, // Sn2  2
                575.358, // Sn1  3
                579.389, // Sn2  3
                589.374, // Sn1  4
                593.405, // Sn2  4
                603.389, // Sn1  5
                607.421, // Sn2  5
                617.405, // Sn1  6
                621.436, // Sn2  6
                631.421, // Sn1  7
                635.452, // Sn2  7
                645.436, // Sn1  8
                649.468, // Sn2  8
                659.452, // Sn1  9
                662.476, // Sn2  9
                673.468, // Sn1 10
                675.483, // Sn2 10
                687.483, // Sn1 11
                689.499, // Sn2 11
                701.499, // Sn1 12
                702.507, // Sn2 12
                715.515, // Sn1 13 Sn2 13
                729.530, // Sn1 14 Sn2 14
                743.546, // Sn1 15 Sn2 15
                757.562, // Sn1 16 Sn2 16
                771.577, // Sn1 17 Sn2 17
                786.601, // Sn1 18 Sn2 18 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Precursor Mass
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest2()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));

            var generator = new PCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                183.065 + MassDiffDictionary.ProtonMass, // HG + Proton MspGenerator\GlyceroLipidFragmentation.cs C5H15NO4P?
                224.105, // Gly-C
                226.083, // Gly-O
                489.321, // Sn1 -CH2
                //502.329, // MspGenerator\GlyceroLipidFragmentation.cs SN1-H2O //it is not the optimum value
                503.337, // Sn1 -O
                //506.360, // MspGenerator\GlyceroLipidFragmentation.cs SN2-H2O //it is not the optimum value
                507.368, // Sn2 -O
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
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }
}