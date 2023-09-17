using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class HexCerNsSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateHexCerNS_H()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCCCCCCCCCCC(=O)NC(COC1OC(CO)C(O)C(O)C1O)C(O)C=CCCCCCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.HexCer_NS, 811.690119, new PositionLevelChains(sphingo, acyl));

            var generator = new HexCerSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                    222.0972, // Hex + C2H5NO
                    252.2686, // [sph+H]+ -CH4O2
                    264.2686, // [sph+H]+ -2H2O
                    282.2791, // [sph+H]+ -H2O
                    308.2584, // 24:0 C1-H
                    309.2662, // 24:0 C1
                    310.2741, // 24:0 C1+H
                    322.2741, // 24:0 C2-H
                    323.2819, // 24:0 C2
                    324.2897, // 24:0 C2+H
                    336.2897, // 24:0 C3-H
                    337.2975, // 24:0 C3
                    338.3054, // 24:0 C3+H
                    350.3054, // 24:0 C4-H
                    351.3132, // 24:0 C4
                    352.321, // 24:0 C4+H
                    364.321, // 24:0 C5-H
                    365.3288, // 24:0 C5
                    366.3367, // 24:0 C5+H
                    368.3887, // [FAA+H]+
                    378.3367, // 24:0 C6-H
                    379.3445, // 24:0 C6
                    380.3523, // 24:0 C6+H
                    392.3523, // 24:0 C7-H
                    393.3601, // 24:0 C7
                    393.3965, // [FAA+C2H+H]+
                    394.368, // 24:0 C7+H
                    406.368, // 24:0 C8-H
                    407.3758, // 24:0 C8
                    408.3836, // 24:0 C8+H
                    410.3993, // [FAA+C2H2O+H]+
                    420.3836, // 24:0 C9-H
                    421.3914, // 24:0 C9
                    422.3993, // 18:1(4);1OH3OH C3-H 24:0 C9+H
                    423.4071, // 18:1(4);1OH3OH C3
                    424.4149, // 18:1(4);1OH3OH C3+H
                    434.3993, // 24:0 C10-H
                    435.4071, // 18:1(4);1OH3OH C4-H 24:0 C10
                    436.4149, // 18:1(4);1OH3OH C4 24:0 C10+H
                    437.4227, // 18:1(4);1OH3OH C4+H
                    448.4149, // 18:1(4);1OH3OH C5-H 24:0 C11-H
                    449.4227, // 18:1(4);1OH3OH C5 24:0 C11
                    450.4306, // 18:1(4);1OH3OH C5+H 24:0 C11+H
                    462.4306, // 18:1(4);1OH3OH C6-H 24:0 C12-H
                    463.4384, // 18:1(4);1OH3OH C6 24:0 C12
                    464.4462, // 18:1(4);1OH3OH C6+H 24:0 C12+H
                    476.4462, // 18:1(4);1OH3OH C7-H 24:0 C13-H
                    477.454, // 18:1(4);1OH3OH C7 24:0 C13
                    478.4619, // 18:1(4);1OH3OH C7+H 24:0 C13+H
                    490.4619, // 18:1(4);1OH3OH C8-H 24:0 C14-H
                    491.4697, // 18:1(4);1OH3OH C8 24:0 C14
                    492.4775, // 18:1(4);1OH3OH C8+H 24:0 C14+H
                    504.4775, // 18:1(4);1OH3OH C9-H 24:0 C15-H
                    505.4853, // 18:1(4);1OH3OH C9 24:0 C15
                    506.4932, // 18:1(4);1OH3OH C9+H 24:0 C15+H
                    518.4932, // 18:1(4);1OH3OH C10-H 24:0 C16-H
                    519.501, // 18:1(4);1OH3OH C10 24:0 C16
                    520.5088, // 18:1(4);1OH3OH C10+H 24:0 C16+H
                    532.5088, // 18:1(4);1OH3OH C11-H 24:0 C17-H
                    533.5166, // 18:1(4);1OH3OH C11 24:0 C17
                    534.5245, // 18:1(4);1OH3OH C11+H 24:0 C17+H
                    546.5245, // 18:1(4);1OH3OH C12-H 24:0 C18-H
                    547.5323, // 18:1(4);1OH3OH C12 24:0 C18
                    548.5401, // 18:1(4);1OH3OH C12+H 24:0 C18+H
                    560.5401, // 18:1(4);1OH3OH C13-H 24:0 C19-H
                    561.5479, // 18:1(4);1OH3OH C13 24:0 C19
                    562.5558, // 18:1(4);1OH3OH C13+H 24:0 C19+H
                    573.4599, // [FAA+C2H4O+Hex+adduct]+
                    574.5558, // 18:1(4);1OH3OH C14-H 24:0 C20-H
                    575.5636, // 18:1(4);1OH3OH C14 24:0 C20
                    576.5714, // 18:1(4);1OH3OH C14+H 24:0 C20+H
                    588.5714, // 18:1(4);1OH3OH C15-H 24:0 C21-H
                    589.5792, // 18:1(4);1OH3OH C15 24:0 C21
                    590.5871, // 18:1(4);1OH3OH C15+H 24:0 C21+H
                    602.5871, // 18:1(4);1OH3OH C16-H 24:0 C22-H
                    603.5949, // 18:1(4);1OH3OH C16 24:0 C22
                    604.6027, // 18:1(4);1OH3OH C16+H 24:0 C22+H
                    614.6234, // Precursor-Hex-2H2O
                    616.6027, // 18:1(4);1OH3OH C17-H 24:0 C23-H
                    617.6105, // 18:1(4);1OH3OH C17 24:0 C23
                    618.6184, // 18:1(4);1OH3OH C17+H 24:0 C23+H
                    632.634, // Precursor-Hex-H2O
                    650.6446, // Precursor-Hex
                    794.6868, // Precursor-H2O
                    812.6974, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateHexCerNS_H2O()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCCCCCCCCCCC(=O)NC(COC1OC(CO)C(O)C(O)C1O)C(O)C=CCCCCCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.HexCer_NS, 811.690119, new PositionLevelChains(sphingo, acyl));

            var generator = new HexCerSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H-H2O]+"));

            var expects = new[]
            {
                    204.0866, // Hex + C2H5NO
                    252.2686, // [sph+H]+ -CH4O2
                    264.2686, // [sph+H]+ -2H2O
                    282.2791, // [sph+H]+ -H2O
                    308.2584, // 24:0 C1-H
                    309.2662, // 24:0 C1
                    310.2741, // 24:0 C1+H
                    322.2741, // 24:0 C2-H
                    323.2819, // 24:0 C2
                    324.2897, // 24:0 C2+H
                    336.2897, // 24:0 C3-H
                    337.2975, // 24:0 C3
                    338.3054, // 24:0 C3+H
                    350.3054, // 24:0 C4-H
                    351.3132, // 24:0 C4
                    352.321, // 24:0 C4+H
                    364.321, // 24:0 C5-H
                    365.3288, // 24:0 C5
                    366.3367, // 24:0 C5+H
                    368.3887, // [FAA+H]+
                    378.3367, // 24:0 C6-H
                    379.3445, // 24:0 C6
                    380.3523, // 24:0 C6+H
                    392.3523, // 24:0 C7-H
                    393.3601, // 24:0 C7
                    393.3965, // [FAA+C2H+H]+
                    394.368, // 24:0 C7+H
                    406.368, // 24:0 C8-H
                    407.3758, // 24:0 C8
                    408.3836, // 24:0 C8+H
                    420.3836, // 24:0 C9-H
                    421.3914, // 24:0 C9
                    422.3993, // 18:1(4);1OH3OH C3-H 24:0 C9+H
                    423.4071, // 18:1(4);1OH3OH C3
                    424.4149, // 18:1(4);1OH3OH C3+H
                    434.3993, // 24:0 C10-H
                    435.4071, // 18:1(4);1OH3OH C4-H 24:0 C10
                    436.4149, // 18:1(4);1OH3OH C4 24:0 C10+H
                    437.4227, // 18:1(4);1OH3OH C4+H
                    448.4149, // 18:1(4);1OH3OH C5-H 24:0 C11-H
                    449.4227, // 18:1(4);1OH3OH C5 24:0 C11
                    450.4306, // 18:1(4);1OH3OH C5+H 24:0 C11+H
                    462.4306, // 18:1(4);1OH3OH C6-H 24:0 C12-H
                    463.4384, // 18:1(4);1OH3OH C6 24:0 C12
                    464.4462, // 18:1(4);1OH3OH C6+H 24:0 C12+H
                    476.4462, // 18:1(4);1OH3OH C7-H 24:0 C13-H
                    477.454, // 18:1(4);1OH3OH C7 24:0 C13
                    478.4619, // 18:1(4);1OH3OH C7+H 24:0 C13+H
                    490.4619, // 18:1(4);1OH3OH C8-H 24:0 C14-H
                    491.4697, // 18:1(4);1OH3OH C8 24:0 C14
                    492.4775, // 18:1(4);1OH3OH C8+H 24:0 C14+H
                    504.4775, // 18:1(4);1OH3OH C9-H 24:0 C15-H
                    505.4853, // 18:1(4);1OH3OH C9 24:0 C15
                    506.4932, // 18:1(4);1OH3OH C9+H 24:0 C15+H
                    518.4932, // 18:1(4);1OH3OH C10-H 24:0 C16-H
                    519.501, // 18:1(4);1OH3OH C10 24:0 C16
                    520.5088, // 18:1(4);1OH3OH C10+H 24:0 C16+H
                    532.5088, // 18:1(4);1OH3OH C11-H 24:0 C17-H
                    533.5166, // 18:1(4);1OH3OH C11 24:0 C17
                    534.5245, // 18:1(4);1OH3OH C11+H 24:0 C17+H
                    546.5245, // 18:1(4);1OH3OH C12-H 24:0 C18-H
                    547.5323, // 18:1(4);1OH3OH C12 24:0 C18
                    548.5401, // 18:1(4);1OH3OH C12+H 24:0 C18+H
                    555.4493, // [FAA+C2H4O+Hex+adduct]+
                    560.5401, // 18:1(4);1OH3OH C13-H 24:0 C19-H
                    561.5479, // 18:1(4);1OH3OH C13 24:0 C19
                    562.5558, // 18:1(4);1OH3OH C13+H 24:0 C19+H
                    574.5558, // 18:1(4);1OH3OH C14-H 24:0 C20-H
                    575.5636, // 18:1(4);1OH3OH C14 24:0 C20
                    576.5714, // 18:1(4);1OH3OH C14+H 24:0 C20+H
                    588.5714, // 18:1(4);1OH3OH C15-H 24:0 C21-H
                    589.5792, // 18:1(4);1OH3OH C15 24:0 C21
                    590.5871, // 18:1(4);1OH3OH C15+H 24:0 C21+H
                    596.6129, // Precursor-Hex-2H2O
                    602.5871, // 18:1(4);1OH3OH C16-H 24:0 C22-H
                    603.5949, // 18:1(4);1OH3OH C16 24:0 C22
                    604.6027, // 18:1(4);1OH3OH C16+H 24:0 C22+H
                    614.6234, // Precursor-Hex-H2O
                    616.6027, // 18:1(4);1OH3OH C17-H 24:0 C23-H
                    617.6105, // 18:1(4);1OH3OH C17 24:0 C23
                    618.6184, // 18:1(4);1OH3OH C17+H 24:0 C23+H
                    632.634, // Precursor-Hex
                    776.6763, // Precursor-H2O
                    794.6868, // Precursor

            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateHexCerNS_Na()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCCCCCCCCCCC(=O)NC(COC1OC(CO)C(O)C(O)C1O)C(O)C=CCCCCCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.HexCer_NS, 811.690119, new PositionLevelChains(sphingo, acyl));

            var generator = new HexCerSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                    156.0393, // [C5H9O4+adduct]+
                    244.0792, // Hex + C2H5NO
                    348.2509, // 24:0 C1-H
                    349.2587, // 24:0 C1
                    350.2666, // 24:0 C1+H
                    362.2666, // 24:0 C2-H
                    363.2744, // 24:0 C2
                    364.2822, // 24:0 C2+H
                    376.2822, // 24:0 C3-H
                    377.29, // 24:0 C3
                    378.2979, // 24:0 C3+H
                    390.2979, // 24:0 C4-H
                    391.3057, // 24:0 C4
                    392.3135, // 24:0 C4+H
                    393.3965, // [FAA+C2H+H]+
                    404.3135, // 24:0 C5-H
                    405.3213, // 24:0 C5
                    406.3292, // 24:0 C5+H
                    418.3292, // 24:0 C6-H
                    419.337, // 24:0 C6
                    420.3448, // 24:0 C6+H
                    432.3448, // 24:0 C7-H
                    433.3526, // 24:0 C7
                    434.3605, // 24:0 C7+H
                    446.3605, // 24:0 C8-H
                    447.3683, // 24:0 C8
                    448.3761, // 24:0 C8+H
                    460.3761, // 24:0 C9-H
                    461.3839, // 24:0 C9
                    462.3918, // 18:1(4);1OH3OH C3-H 24:0 C9+H
                    463.3996, // 18:1(4);1OH3OH C3
                    464.4074, // 18:1(4);1OH3OH C3+H
                    474.3918, // 24:0 C10-H
                    475.3996, // 18:1(4);1OH3OH C4-H 24:0 C10
                    476.4074, // 18:1(4);1OH3OH C4 24:0 C10+H
                    477.4152, // 18:1(4);1OH3OH C4+H
                    488.4074, // 18:1(4);1OH3OH C5-H 24:0 C11-H
                    489.4152, // 18:1(4);1OH3OH C5 24:0 C11
                    490.4231, // 18:1(4);1OH3OH C5+H 24:0 C11+H
                    502.4231, // 18:1(4);1OH3OH C6-H 24:0 C12-H
                    503.4309, // 18:1(4);1OH3OH C6 24:0 C12
                    504.4387, // 18:1(4);1OH3OH C6+H 24:0 C12+H
                    516.4387, // 18:1(4);1OH3OH C7-H 24:0 C13-H
                    517.4465, // 18:1(4);1OH3OH C7 24:0 C13
                    518.4544, // 18:1(4);1OH3OH C7+H 24:0 C13+H
                    530.4544, // 18:1(4);1OH3OH C8-H 24:0 C14-H
                    531.4622, // 18:1(4);1OH3OH C8 24:0 C14
                    532.47, // 18:1(4);1OH3OH C8+H 24:0 C14+H
                    544.47, // 18:1(4);1OH3OH C9-H 24:0 C15-H
                    545.4778, // 18:1(4);1OH3OH C9 24:0 C15
                    546.4857, // 18:1(4);1OH3OH C9+H 24:0 C15+H
                    558.4857, // 18:1(4);1OH3OH C10-H 24:0 C16-H
                    559.4935, // 18:1(4);1OH3OH C10 24:0 C16
                    560.5013, // 18:1(4);1OH3OH C10+H 24:0 C16+H
                    572.5013, // 18:1(4);1OH3OH C11-H 24:0 C17-H
                    573.5091, // 18:1(4);1OH3OH C11 24:0 C17
                    574.517, // 18:1(4);1OH3OH C11+H 24:0 C17+H
                    586.517, // 18:1(4);1OH3OH C12-H 24:0 C18-H
                    587.5248, // 18:1(4);1OH3OH C12 24:0 C18
                    588.5326, // 18:1(4);1OH3OH C12+H 24:0 C18+H
                    595.4418, // [FAA+C2H4O+Hex+adduct]+
                    600.5326, // 18:1(4);1OH3OH C13-H 24:0 C19-H
                    601.5404, // 18:1(4);1OH3OH C13 24:0 C19
                    602.5483, // 18:1(4);1OH3OH C13+H 24:0 C19+H
                    614.5483, // 18:1(4);1OH3OH C14-H 24:0 C20-H
                    615.5561, // 18:1(4);1OH3OH C14 24:0 C20
                    616.5639, // 18:1(4);1OH3OH C14+H 24:0 C20+H
                    628.5639, // 18:1(4);1OH3OH C15-H 24:0 C21-H
                    629.5717, // 18:1(4);1OH3OH C15 24:0 C21
                    630.5796, // 18:1(4);1OH3OH C15+H 24:0 C21+H
                    642.5796, // 18:1(4);1OH3OH C16-H 24:0 C22-H
                    643.5874, // 18:1(4);1OH3OH C16 24:0 C22
                    644.5952, // 18:1(4);1OH3OH C16+H 24:0 C22+H
                    654.616, // Precursor-Hex-H2O
                    656.5952, // 18:1(4);1OH3OH C17-H 24:0 C23-H
                    657.603, // 18:1(4);1OH3OH C17 24:0 C23
                    658.6109, // 18:1(4);1OH3OH C17+H 24:0 C23+H
                    700.6214, // Precursor-C5H10O4
                    834.6793, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}