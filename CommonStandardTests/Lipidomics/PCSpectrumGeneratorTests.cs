using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                503.337, // Sn1 -O
                507.368, // Sn2 -O
                520.340, // Sn1 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN1 acyl loss
                524.371, // Sn2 -C=O MspGenerator\GlyceroLipidFragmentation.cs SN2 acyl loss
                546.320, // Sn1  1 -H
                547.327, // Sn1  1
                548.335, // Sn1  1 +H
                550.351, // Sn2  1 -H
                551.358, // Sn2  1
                552.367, // Sn2  1 +H
                560.335, // Sn1  2 -H
                561.342, // Sn1  2
                562.351, // Sn1  2 +H
                564.367, // Sn2  2 -H
                565.374, // Sn2  2
                566.382, // Sn2  2 +H
                574.351, // Sn1  3 -H
                575.358, // Sn1  3
                576.367, // Sn1  3 +H
                578.382, // Sn2  3 -H
                579.389, // Sn2  3
                580.398, // Sn2  3 +H
                588.367, // Sn1  4 -H
                589.374, // Sn1  4
                590.382, // Sn1  4 +H
                592.398, // Sn2  4 -H
                593.405, // Sn2  4
                594.414, // Sn2  4 +H
                602.382, // Sn1  5 -H
                603.389, // Sn1  5
                604.398, // Sn1  5 +H
                606.414, // Sn2  5 -H
                607.421, // Sn2  5
                608.429, // Sn2  5 +H
                616.398, // Sn1  6 -H
                617.405, // Sn1  6
                618.414, // Sn1  6 +H
                620.429, // Sn2  6 -H
                621.436, // Sn2  6
                622.445, // Sn2  6 +H
                630.414, // Sn1  7 -H
                631.421, // Sn1  7
                632.429, // Sn1  7 +H
                634.445, // Sn2  7 -H
                635.452, // Sn2  7
                636.460, // Sn2  7 +H
                644.429, // Sn1  8 -H
                645.436, // Sn1  8
                646.445, // Sn1  8 +H
                648.460, // Sn2  8 -H
                649.468, // Sn2  8
                650.476, // Sn2  8 +H
                658.445, // Sn1  9 -H
                659.452, // Sn1  9
                660.460, // Sn1  9 +H
                661.468, // Sn2  9 -H
                662.476, // Sn2  9
                663.484, // Sn2  9 +H
                672.460, // Sn1 10 -H
                673.468, // Sn1 10
                674.476, // Sn1 10 +H Sn2 10 -H
                675.483, // Sn2 10
                676.492, // Sn2 10 +H
                686.476, // Sn1 11 -H
                687.483, // Sn1 11
                688.492, // Sn1 11 +H Sn2 11 -H
                689.499, // Sn2 11
                690.507, // Sn2 11 +H
                700.492, // Sn1 12 -H
                701.499, // Sn1 12 Sn2 12 -H
                702.507, // Sn1 12 +H Sn2 12
                703.515, // Sn2 12 +H
                714.507, // Sn1 13 Sn2 13 -H
                715.515, // Sn1 13 Sn2 13
                716.523, // Sn1 13 Sn2 13 +H
                728.523, // Sn1 14 Sn2 14 -H
                729.530, // Sn1 14 Sn2 14
                730.539, // Sn1 14 Sn2 14 +H
                742.539, // Sn1 15 Sn2 15 -H
                743.546, // Sn1 15 Sn2 15
                744.554, // Sn1 15 Sn2 15 +H
                756.554, // Sn1 16 Sn2 16 -H
                757.562, // Sn1 16 Sn2 16
                758.570, // Sn1 16 Sn2 16 +H
                770.570, // Sn1 17 Sn2 17 -H
                771.577, // Sn1 17 Sn2 17
                772.586, // Sn1 17 Sn2 17 +H
                786.601, // Sn1 18 Sn2 18 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Precursor Mass
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}