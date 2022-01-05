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
    }
}