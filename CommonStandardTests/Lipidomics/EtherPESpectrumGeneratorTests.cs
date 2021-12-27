using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class EtherPESpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateEtherPEPTest() {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 747.5199, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                292.300, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ SN1 ether +C2H8NO3P-H3PO4
                359.258, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ NL of C2H8NO4P+SN1
                390.277, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Sn1ether+C2H8NO3P
                447.311, // Sn2 -O
                463.305, // Sn2 -C=O
                469.258, // Sn1 -CH2
                483.274, // Sn1 -O
                491.300, // Sn2  1
                499.269, // Sn1 -C
                505.316, // Sn2  2
                512.276, // Sn1  1
                519.332, // Sn2  3
                525.284, // Sn1  2
                533.347, // Sn2  4
                539.300, // Sn1  3
                546.355, // Sn2  5
                553.316, // Sn1  4
                559.363, // Sn2  6
                567.331, // Sn1  5
                573.379, // Sn2  7
                581.347, // Sn1  6
                586.386, // Sn2  8
                595.362, // Sn1  7
                599.394, // Sn2  9
                607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -C2H8NO4P
                609.378, // Sn1  8
                613.410, // Sn2 10
                623.394, // Sn1  9
                626.418, // Sn2 12
                637.409, // Sn1 10
                639.425, // Sn2 12
                651.425, // Sn1 11
                653.441, // Sn2 13
                664.433, // Sn1 12
                666.448, // Sn2 14
                677.441, // Sn1 13
                679.456, // Sn2 15
                691.457, // Sn1 14
                693.472, // Sn2 16
                705.472, // Sn1 15
                706.480, // Sn2 17
                719.488, // Sn1 16 Sn2 18
                733.504, // Sn1 17 Sn2 19
                748.527, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateEtherPEOTest() {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 747.5199, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                285.221, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ SN2 acyl
                447.311, // Sn2 -O
                463.305, // Sn2 -C=O
                469.258, // Sn1 -CH2
                483.274, // Sn1 -O
                491.300, // Sn2  1
                499.269, // Sn1 -C
                505.316, // Sn2  2
                513.285, // Sn1  1
                519.332, // Sn2  3
                527.300, // Sn1  2
                533.347, // Sn2  4
                541.316, // Sn1  3
                546.355, // Sn2  5
                555.332, // Sn1  4
                559.363, // Sn2  6
                569.347, // Sn1  5
                573.379, // Sn2  7
                583.363, // Sn1  6
                586.386, // Sn2  8
                597.379, // Sn1  7
                599.394, // Sn2  9
                607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -141
                611.394, // Sn1  8
                613.410, // Sn2 10
                624.402, // Sn1  9
                626.418, // Sn2 12
                637.409, // Sn1 10
                639.425, // Sn2 12
                651.425, // Sn1 11
                653.441, // Sn2 13
                664.433, // Sn1 12
                666.448, // Sn2 14
                677.441, // Sn1 13
                679.456, // Sn2 15
                691.457, // Sn1 14
                693.472, // Sn2 16
                705.472, // Sn1 15
                706.480, // Sn2 17
                719.488, // Sn1 16 Sn2 18
                733.504, // Sn1 17 Sn2 19
                748.527, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct

            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}