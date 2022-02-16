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
                142.027, // Header
                182.058, // Gly-C
                184.037, // Gly-O
                292.299, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ SN1 ether +C2H8NO3P-H3PO4
                359.258, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ NL of C2H8NO4P+SN1
                390.277, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Sn1ether+C2H8NO3P
                446.303, // Sn2 -O
                464.314, // Sn2 -C=O
                470.267, // Sn1 -CH2
                483.275, // Sn1 -O
                490.293, // Sn2  1 -H
                491.301, // Sn2  1
                492.309, // Sn2  1 +H
                500.277, // Sn1 -C
                504.309, // Sn2  2 -H
                505.316, // Sn2  2
                506.324, // Sn2  2 +H
                512.277, // Sn1  1
                518.324, // Sn2  3 -H
                519.332, // Sn2  3
                520.340, // Sn2  3 +H
                525.285, // Sn1  2
                532.340, // Sn2  4 -H
                533.348, // Sn2  4
                534.356, // Sn2  4 +H
                539.301, // Sn1  3
                545.348, // Sn2  5 -H
                546.356, // Sn2  5
                547.363, // Sn2  5 +H
                553.316, // Sn1  4
                558.356, // Sn2  6 -H
                559.363, // Sn2  6
                560.371, // Sn2  6 +H
                567.332, // Sn1  5
                572.371, // Sn2  7 -H
                573.379, // Sn2  7
                574.387, // Sn2  7 +H
                581.348, // Sn1  6
                585.379, // Sn2  8 -H
                586.387, // Sn2  8
                587.395, // Sn2  8 +H
                595.363, // Sn1  7
                598.387, // Sn2  9 -H
                599.395, // Sn2  9
                600.403, // Sn2  9 +H
                607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -C2H8NO4P
                609.378, // Sn1  8
                612.403, // Sn2 10 -H
                613.410, // Sn2 10
                614.418, // Sn2 10 +H
                623.395, // Sn1  9
                625.410, // Sn2 11 -H
                626.418, // Sn2 12
                627.426, // Sn2 11 +H
                637.410, // Sn1 10
                638.418, // Sn2 12 -H
                639.426, // Sn2 12
                640.434, // Sn2 12 +H
                651.426, // Sn1 11
                652.434, // Sn2 13 -H
                653.442, // Sn2 13
                654.449, // Sn2 13 +H
                664.434, // Sn1 12
                665.442, // Sn2 14 -H
                666.449, // Sn2 14
                667.457, // Sn2 14 +H
                677.442, // Sn1 13
                678.449, // Sn2 15 -H
                679.457, // Sn2 15
                680.465, // Sn2 15 +H
                691.457, // Sn1 14
                692.465, // Sn2 16 -H
                693.473, // Sn2 16
                694.481, // Sn2 16 +H
                705.473, // Sn1 15 Sn2 17 -H
                706.481, // Sn2 17
                707.489, // Sn2 17 +H
                718.481, // Sn2 18 -H
                719.489, // Sn1 16 Sn2 18
                720.496, // Sn2 18 +H
                732.496, // Sn2 19 -H
                733.504, // Sn1 17 Sn2 19
                734.512, // Sn2 19 +H
                748.528, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct
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
                142.027, // Header
                182.058, // Gly-C
                184.037, // Gly-O
                445.295, // Sn2 - O
                464.314, // Sn2 -C=O
                470.267, // Sn1 -CH2
                483.275, // Sn1 -O
                490.293, // Sn2  1 -H
                491.300, // Sn2  1
                492.309, // Sn2  1 +H
                498.262, // Sn1 -C
                504.308, // Sn2  2 -H
                505.316, // Sn2  2
                506.324, // Sn2  2 +H
                513.285, // Sn1  1
                518.324, // Sn2  3 -H
                519.332, // Sn2  3
                520.340, // Sn2  3 +H
                527.300, // Sn1  2
                532.340, // Sn2  4 -H
                533.347, // Sn2  4
                534.356, // Sn2  4 +H
                541.316, // Sn1  3
                545.348, // Sn2  5 -H
                546.356, // Sn2  5
                547.363, // Sn2  5 +H
                555.332, // Sn1  4
                558.356, // Sn2  6 -H
                559.363, // Sn2  6
                560.371, // Sn2  6 +H
                569.347, // Sn1  5
                572.371, // Sn2  7 -H
                573.379, // Sn2  7
                574.386, // Sn2  7 +H
                583.363, // Sn1  6
                585.379, // Sn2  8 -H
                586.386, // Sn2  8
                587.395, // Sn2  8 +H
                597.379, // Sn1  7
                598.387, // Sn2  9 -H
                599.395, // Sn2  9
                600.403, // Sn2  9 +H
                607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -141
                611.395, // Sn1  8
                612.403, // Sn2 10 -H
                613.410, // Sn2 10
                614.418, // Sn2 10 +H
                624.403, // Sn1  9
                625.410, // Sn2 11 -H
                626.418, // Sn2 12
                627.426, // Sn2 11 +H
                637.410, // Sn1 10
                638.418, // Sn2 12 -H
                639.426, // Sn2 12
                640.434, // Sn2 12 +H
                651.426, // Sn1 11
                652.434, // Sn2 13 -H
                653.442, // Sn2 13
                654.449, // Sn2 13 +H
                664.434, // Sn1 12
                665.442, // Sn2 14 -H
                666.449, // Sn2 14
                667.457, // Sn2 14 +H
                677.442, // Sn1 13
                678.449, // Sn2 15 -H
                679.457, // Sn2 15
                680.465, // Sn2 15 +H
                691.457, // Sn1 14
                692.465, // Sn2 16 -H
                693.473, // Sn2 16
                694.481, // Sn2 16 +H
                705.473, // Sn1 15
                706.481, // Sn2 17
                707.489, // Sn2 17 +H
                718.481, // Sn2 18 -H
                719.489, // Sn1 16 Sn2 18
                720.496, // Sn2 18 +H
                732.496, // Sn2 19 -H
                733.504, // Sn1 17 Sn2 19
                734.512, // Sn2 19 +H
                748.528, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}