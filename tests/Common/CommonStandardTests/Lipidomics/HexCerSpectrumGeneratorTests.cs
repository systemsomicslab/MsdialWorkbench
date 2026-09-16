using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
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
                    //308.2584, // 24:0 C1-H
                    //309.2662, // 24:0 C1
                    //310.2741, // 24:0 C1+H
                    //322.2741, // 24:0 C2-H
                    //323.2819, // 24:0 C2
                    //324.2897, // 24:0 C2+H
                    //336.2897, // 24:0 C3-H
                    //337.2975, // 24:0 C3
                    //338.3054, // 24:0 C3+H
                    //350.3054, // 24:0 C4-H
                    //351.3132, // 24:0 C4
                    //352.321, // 24:0 C4+H
                    //364.321, // 24:0 C5-H
                    //365.3288, // 24:0 C5
                    //366.3367, // 24:0 C5+H
                    368.3887, // [FAA+H]+
                    //378.3367, // 24:0 C6-H
                    //379.3445, // 24:0 C6
                    //380.3523, // 24:0 C6+H
                    //392.3523, // 24:0 C7-H
                    //393.3601, // 24:0 C7
                    393.3965, // [FAA+C2H+H]+
                    //394.368, // 24:0 C7+H
                    //406.368, // 24:0 C8-H
                    //407.3758, // 24:0 C8
                    //408.3836, // 24:0 C8+H
                    410.3993, // [FAA+C2H2O+H]+
                    //420.3836, // 24:0 C9-H
                    //421.3914, // 24:0 C9
                    //422.3993, // 18:1(4);1OH3OH C3-H 24:0 C9+H
                    //423.4071, // 18:1(4);1OH3OH C3
                    //424.4149, // 18:1(4);1OH3OH C3+H
                    //434.3993, // 24:0 C10-H
                    //435.4071, // 18:1(4);1OH3OH C4-H 24:0 C10
                    //436.4149, // 18:1(4);1OH3OH C4 24:0 C10+H
                    //437.4227, // 18:1(4);1OH3OH C4+H
                    //448.4149, // 18:1(4);1OH3OH C5-H 24:0 C11-H
                    //449.4227, // 18:1(4);1OH3OH C5 24:0 C11
                    //450.4306, // 18:1(4);1OH3OH C5+H 24:0 C11+H
                    //462.4306, // 18:1(4);1OH3OH C6-H 24:0 C12-H
                    //463.4384, // 18:1(4);1OH3OH C6 24:0 C12
                    //464.4462, // 18:1(4);1OH3OH C6+H 24:0 C12+H
                    //476.4462, // 18:1(4);1OH3OH C7-H 24:0 C13-H
                    //477.454, // 18:1(4);1OH3OH C7 24:0 C13
                    //478.4619, // 18:1(4);1OH3OH C7+H 24:0 C13+H
                    //490.4619, // 18:1(4);1OH3OH C8-H 24:0 C14-H
                    //491.4697, // 18:1(4);1OH3OH C8 24:0 C14
                    //492.4775, // 18:1(4);1OH3OH C8+H 24:0 C14+H
                    //504.4775, // 18:1(4);1OH3OH C9-H 24:0 C15-H
                    //505.4853, // 18:1(4);1OH3OH C9 24:0 C15
                    //506.4932, // 18:1(4);1OH3OH C9+H 24:0 C15+H
                    //518.4932, // 18:1(4);1OH3OH C10-H 24:0 C16-H
                    //519.501, // 18:1(4);1OH3OH C10 24:0 C16
                    //520.5088, // 18:1(4);1OH3OH C10+H 24:0 C16+H
                    //532.5088, // 18:1(4);1OH3OH C11-H 24:0 C17-H
                    //533.5166, // 18:1(4);1OH3OH C11 24:0 C17
                    //534.5245, // 18:1(4);1OH3OH C11+H 24:0 C17+H
                    //546.5245, // 18:1(4);1OH3OH C12-H 24:0 C18-H
                    //547.5323, // 18:1(4);1OH3OH C12 24:0 C18
                    //548.5401, // 18:1(4);1OH3OH C12+H 24:0 C18+H
                    //560.5401, // 18:1(4);1OH3OH C13-H 24:0 C19-H
                    //561.5479, // 18:1(4);1OH3OH C13 24:0 C19
                    //562.5558, // 18:1(4);1OH3OH C13+H 24:0 C19+H
                    572.45207975, // [FAA+C2H4O+Hex+adduct]+
                    //574.5558, // 18:1(4);1OH3OH C14-H 24:0 C20-H
                    //575.5636, // 18:1(4);1OH3OH C14 24:0 C20
                    //576.5714, // 18:1(4);1OH3OH C14+H 24:0 C20+H
                    //588.5714, // 18:1(4);1OH3OH C15-H 24:0 C21-H
                    //589.5792, // 18:1(4);1OH3OH C15 24:0 C21
                    //590.5871, // 18:1(4);1OH3OH C15+H 24:0 C21+H
                    //602.5871, // 18:1(4);1OH3OH C16-H 24:0 C22-H
                    //603.5949, // 18:1(4);1OH3OH C16 24:0 C22
                    //604.6027, // 18:1(4);1OH3OH C16+H 24:0 C22+H
                    614.6234, // Precursor-Hex-2H2O
                    //616.6027, // 18:1(4);1OH3OH C17-H 24:0 C23-H
                    //617.6105, // 18:1(4);1OH3OH C17 24:0 C23
                    //618.6184, // 18:1(4);1OH3OH C17+H 24:0 C23+H
                    632.634, // Precursor-Hex-H2O
                    650.6446, // Precursor-Hex
                    794.6868, // Precursor-H2O
                    812.6974, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
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
                    //308.2584, // 24:0 C1-H
                    //309.2662, // 24:0 C1
                    //310.2741, // 24:0 C1+H
                    //322.2741, // 24:0 C2-H
                    //323.2819, // 24:0 C2
                    //324.2897, // 24:0 C2+H
                    //336.2897, // 24:0 C3-H
                    //337.2975, // 24:0 C3
                    //338.3054, // 24:0 C3+H
                    //350.3054, // 24:0 C4-H
                    //351.3132, // 24:0 C4
                    //352.321, // 24:0 C4+H
                    //364.321, // 24:0 C5-H
                    //365.3288, // 24:0 C5
                    //366.3367, // 24:0 C5+H
                    368.3887, // [FAA+H]+
                    //378.3367, // 24:0 C6-H
                    //379.3445, // 24:0 C6
                    //380.3523, // 24:0 C6+H
                    //392.3523, // 24:0 C7-H
                    //393.3601, // 24:0 C7
                    393.3965, // [FAA+C2H+H]+
                    //394.368, // 24:0 C7+H
                    //406.368, // 24:0 C8-H
                    //407.3758, // 24:0 C8
                    //408.3836, // 24:0 C8+H
                    //420.3836, // 24:0 C9-H
                    //421.3914, // 24:0 C9
                    //422.3993, // 18:1(4);1OH3OH C3-H 24:0 C9+H
                    //423.4071, // 18:1(4);1OH3OH C3
                    //424.4149, // 18:1(4);1OH3OH C3+H
                    //434.3993, // 24:0 C10-H
                    //435.4071, // 18:1(4);1OH3OH C4-H 24:0 C10
                    //436.4149, // 18:1(4);1OH3OH C4 24:0 C10+H
                    //437.4227, // 18:1(4);1OH3OH C4+H
                    //448.4149, // 18:1(4);1OH3OH C5-H 24:0 C11-H
                    //449.4227, // 18:1(4);1OH3OH C5 24:0 C11
                    //450.4306, // 18:1(4);1OH3OH C5+H 24:0 C11+H
                    //462.4306, // 18:1(4);1OH3OH C6-H 24:0 C12-H
                    //463.4384, // 18:1(4);1OH3OH C6 24:0 C12
                    //464.4462, // 18:1(4);1OH3OH C6+H 24:0 C12+H
                    //476.4462, // 18:1(4);1OH3OH C7-H 24:0 C13-H
                    //477.454, // 18:1(4);1OH3OH C7 24:0 C13
                    //478.4619, // 18:1(4);1OH3OH C7+H 24:0 C13+H
                    //490.4619, // 18:1(4);1OH3OH C8-H 24:0 C14-H
                    //491.4697, // 18:1(4);1OH3OH C8 24:0 C14
                    //492.4775, // 18:1(4);1OH3OH C8+H 24:0 C14+H
                    //504.4775, // 18:1(4);1OH3OH C9-H 24:0 C15-H
                    //505.4853, // 18:1(4);1OH3OH C9 24:0 C15
                    //506.4932, // 18:1(4);1OH3OH C9+H 24:0 C15+H
                    //518.4932, // 18:1(4);1OH3OH C10-H 24:0 C16-H
                    //519.501, // 18:1(4);1OH3OH C10 24:0 C16
                    //520.5088, // 18:1(4);1OH3OH C10+H 24:0 C16+H
                    //532.5088, // 18:1(4);1OH3OH C11-H 24:0 C17-H
                    //533.5166, // 18:1(4);1OH3OH C11 24:0 C17
                    //534.5245, // 18:1(4);1OH3OH C11+H 24:0 C17+H
                    //546.5245, // 18:1(4);1OH3OH C12-H 24:0 C18-H
                    //547.5323, // 18:1(4);1OH3OH C12 24:0 C18
                    //548.5401, // 18:1(4);1OH3OH C12+H 24:0 C18+H
                    554.441515, // [FAA+C2H4O+Hex+adduct]+
                    //560.5401, // 18:1(4);1OH3OH C13-H 24:0 C19-H
                    //561.5479, // 18:1(4);1OH3OH C13 24:0 C19
                    //562.5558, // 18:1(4);1OH3OH C13+H 24:0 C19+H
                    //574.5558, // 18:1(4);1OH3OH C14-H 24:0 C20-H
                    //575.5636, // 18:1(4);1OH3OH C14 24:0 C20
                    //576.5714, // 18:1(4);1OH3OH C14+H 24:0 C20+H
                    //588.5714, // 18:1(4);1OH3OH C15-H 24:0 C21-H
                    //589.5792, // 18:1(4);1OH3OH C15 24:0 C21
                    //590.5871, // 18:1(4);1OH3OH C15+H 24:0 C21+H
                    596.6129, // Precursor-Hex-2H2O
                    //602.5871, // 18:1(4);1OH3OH C16-H 24:0 C22-H
                    //603.5949, // 18:1(4);1OH3OH C16 24:0 C22
                    //604.6027, // 18:1(4);1OH3OH C16+H 24:0 C22+H
                    614.6234, // Precursor-Hex-H2O
                    //616.6027, // 18:1(4);1OH3OH C17-H 24:0 C23-H
                    //617.6105, // 18:1(4);1OH3OH C17 24:0 C23
                    //618.6184, // 18:1(4);1OH3OH C17+H 24:0 C23+H
                    632.634, // Precursor-Hex
                    776.6763, // Precursor-H2O
                    794.6868, // Precursor

            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
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
                156.0393,// [C5H9O4+adduct]+   
                201.03696,// [Hex+adduct-H]+   
                203.05261,// [Hex+adduct+H]+   
                244.07916,// Hex + C2H5NO 
                416.38629,// [FAA+C2H+Na]+   
                510.30374,// 24:0 C1-H  
                511.31156,// 24:0 C1  
                512.31939,// 24:0 C1+H  
                524.31939,// 24:0 C2-H  
                525.32721,// 24:0 C2  
                526.33504,// 24:0 C2+H  
                538.33504,// 24:0 C3-H  
                539.34286,// 24:0 C3  
                540.35069,// 24:0 C3+H  
                552.35069,// 24:0 C4-H  
                553.35851,// 24:0 C4  
                554.36634,// 24:0 C4+H  
                566.36634,// 24:0 C5-H  
                567.37416,// 24:0 C5  
                568.38199,// 24:0 C5+H  
                580.38199,// 24:0 C6-H  
                581.38981,// 24:0 C6  
                582.39764,// 24:0 C6+H  
                594.39764,// 24:0 C7-H  
                594.43402,// [FAA+C2H4O+Hex+adduct]+   
                595.40546,// 24:0 C7  
                596.41329,// 24:0 C7+H  
                608.41329,// 24:0 C8-H  
                609.42111,// 24:0 C8  
                610.42894,// 24:0 C8+H  
                622.42894,// 24:0 C9-H  
                623.43676,// 24:0 C9  
                624.44459,// 18:1(4)(1OH,3OH) C3-H, 24:0 C9+H
                625.45241,// 18:1(4)(1OH,3OH) C3  
                626.46024,// 18:1(4)(1OH,3OH) C3+H  
                636.44459,// 24:0 C10-H  
                637.45241,// 18:1(4)(1OH,3OH) C4-H, 24:0 C10
                638.46024,// 18:1(4)(1OH,3OH) C4, 24:0 C10+H
                639.46806,// 18:1(4)(1OH,3OH) C4+H  
                650.46024,// 18:1(4)(1OH,3OH) C5-H, 24:0 C11-H
                651.46806,// 18:1(4)(1OH,3OH) C5, 24:0 C11
                652.47589,// 18:1(4)(1OH,3OH) C5+H, 24:0 C11+H
                654.61595,// Precursor-Hex-H2O   
                664.47589,// 18:1(4)(1OH,3OH) C6-H, 24:0 C12-H
                665.48371,// 18:1(4)(1OH,3OH) C6, 24:0 C12
                666.49154,// 18:1(4)(1OH,3OH) C6+H, 24:0 C12+H
                678.49154,// 18:1(4)(1OH,3OH) C7-H, 24:0 C13-H
                679.49936,// 18:1(4)(1OH,3OH) C7, 24:0 C13
                680.50719,// 18:1(4)(1OH,3OH) C7+H, 24:0 C13+H
                692.50719,// 18:1(4)(1OH,3OH) C8-H, 24:0 C14-H
                693.51501,// 18:1(4)(1OH,3OH) C8, 24:0 C14
                694.52284,// 18:1(4)(1OH,3OH) C8+H, 24:0 C14+H
                700.62143,// Precursor-C5H10O4   
                706.52284,// 18:1(4)(1OH,3OH) C9-H, 24:0 C15-H
                707.53066,// 18:1(4)(1OH,3OH) C9, 24:0 C15
                708.53849,// 18:1(4)(1OH,3OH) C9+H, 24:0 C15+H
                720.53849,// 18:1(4)(1OH,3OH) C10-H, 24:0 C16-H
                721.54631,// 18:1(4)(1OH,3OH) C10, 24:0 C16
                722.55414,// 18:1(4)(1OH,3OH) C10+H, 24:0 C16+H
                734.55414,// 18:1(4)(1OH,3OH) C11-H, 24:0 C17-H
                735.56196,// 18:1(4)(1OH,3OH) C11, 24:0 C17
                736.56979,// 18:1(4)(1OH,3OH) C11+H, 24:0 C17+H
                748.56979,// 18:1(4)(1OH,3OH) C12-H, 24:0 C18-H
                749.57761,// 18:1(4)(1OH,3OH) C12, 24:0 C18
                750.58544,// 18:1(4)(1OH,3OH) C12+H, 24:0 C18+H
                762.58544,// 18:1(4)(1OH,3OH) C13-H, 24:0 C19-H
                763.59326,// 18:1(4)(1OH,3OH) C13, 24:0 C19
                764.60109,// 18:1(4)(1OH,3OH) C13+H, 24:0 C19+H
                776.60109,// 18:1(4)(1OH,3OH) C14-H, 24:0 C20-H
                777.60891,// 18:1(4)(1OH,3OH) C14, 24:0 C20
                778.61674,// 18:1(4)(1OH,3OH) C14+H, 24:0 C20+H
                790.61674,// 18:1(4)(1OH,3OH) C15-H, 24:0 C21-H
                791.62456,// 18:1(4)(1OH,3OH) C15, 24:0 C21
                792.63239,// 18:1(4)(1OH,3OH) C15+H, 24:0 C21+H
                804.63239,// 18:1(4)(1OH,3OH) C16-H, 24:0 C22-H
                805.64021,// 18:1(4)(1OH,3OH) C16, 24:0 C22
                806.64804,// 18:1(4)(1OH,3OH) C16+H, 24:0 C22+H
                818.64804,// 18:1(4)(1OH,3OH) C17-H, 24:0 C23-H
                819.65586,// 18:1(4)(1OH,3OH) C17, 24:0 C23
                820.66369,// 18:1(4)(1OH,3OH) C17+H, 24:0 C23+H
                834.67934,// Precursor   

            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}