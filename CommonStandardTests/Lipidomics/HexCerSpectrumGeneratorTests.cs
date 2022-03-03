using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                222.09721, // Hex +C2H5NO, 
                252.26858, // [sph+H]+ -CH4O2, 
                264.26858, // [sph+H]+-2H2O, 
                282.27914, // [sph+H]+ -H2O, 
                368.38869, // [FAA+H]+, 
                393.39652, // [FAA+C2H+H]+, 
                410.39926, // [FAA+C2H2O+H]+, 
                470.31123, // Acyl(24:0)C1-H, 
                471.31905, // Acyl(24:0)C1, 
                472.32688, // Acyl(24:0)C1+H, 
                484.32688, // Acyl(24:0)C2-H, 
                485.3347, // Acyl(24:0)C2, 
                486.34253, // Acyl(24:0)C2+H, 
                498.34253, // Acyl(24:0)C3-H, 
                499.35035, // Acyl(24:0)C3, 
                500.35818, // Acyl(24:0)C3+H, 
                512.35818, // Acyl(24:0)C4-H, 
                513.366, // Acyl(24:0)C4, 
                514.37383, // Acyl(24:0)C4+H, 
                526.37383, // Acyl(24:0)C5-H, 
                527.38165, // Acyl(24:0)C5, 
                528.38948, // Acyl(24:0)C5+H, 
                540.38948, // Acyl(24:0)C6-H, 
                541.3973, // Acyl(24:0)C6, 
                542.40513, // Acyl(24:0)C6+H, 
                554.40513, // Acyl(24:0)C7-H, 
                555.41295, // Acyl(24:0)C7, 
                556.42078, // Acyl(24:0)C7+H, 
                568.42078, // Acyl(24:0)C8-H, 
                569.4286, // Acyl(24:0)C8, 
                570.43643, // Acyl(24:0)C8+H, 
                573.4599, // [FAA+C2H4NO+Hex+adduct]+, 
                582.43643, // Acyl(24:0)C9-H, 
                583.44425, // Acyl(24:0)C9, 
                584.45208, // Sph(18:1(4))C3-H, Acyl(24:0)C9+H
                585.4599, // Sph(18:1(4))C3, 
                586.46773, // Sph(18:1(4))C3+H, 
                596.45208, // Acyl(24:0)C10-H, 
                597.4599, // Acyl(24:0)C10, 
                598.46773, // Sph(18:1(4))C4-H, Acyl(24:0)C10+H
                599.47555, // Sph(18:1(4))C4, 
                600.48338, // Sph(18:1(4))C4+H, 
                610.46773, // Acyl(24:0)C11-H, 
                611.47555, // Acyl(24:0)C11, 
                612.48338, // Sph(18:1(4))C5-H, Acyl(24:0)C11+H
                613.4912, // Sph(18:1(4))C5, 
                614.49903, // Sph(18:1(4))C5+H, 
                614.62344, // Precursor-Hex-2H2O, 
                624.48338, // Acyl(24:0)C12-H, 
                625.4912, // Sph(18:1(4))C6-H, Acyl(24:0)C12
                626.49903, // Sph(18:1(4))C6, Acyl(24:0)C12+H
                627.50686, // Sph(18:1(4))C6+H, 
                632.63401, // Precursor-Hex-H2O, 
                638.49903, // Sph(18:1(4))C7-H, Acyl(24:0)C13-H
                639.50686, // Sph(18:1(4))C7, Acyl(24:0)C13
                640.51468, // Sph(18:1(4))C7+H, Acyl(24:0)C13+H
                650.64457, // Precursor-Hex, 
                652.51468, // Sph(18:1(4))C8-H, Acyl(24:0)C14-H
                653.52251, // Sph(18:1(4))C8, Acyl(24:0)C14
                654.53033, // Sph(18:1(4))C8+H, Acyl(24:0)C14+H
                666.53033, // Sph(18:1(4))C9-H, Acyl(24:0)C15-H
                667.53816, // Sph(18:1(4))C9, Acyl(24:0)C15
                668.54598, // Sph(18:1(4))C9+H, Acyl(24:0)C15+H
                680.54598, // Sph(18:1(4))C10-H, Acyl(24:0)C16-H
                681.55381, // Sph(18:1(4))C10, Acyl(24:0)C16
                682.56163, // Sph(18:1(4))C10+H, Acyl(24:0)C16+H
                694.56163, // Sph(18:1(4))C11-H, Acyl(24:0)C17-H
                695.56946, // Sph(18:1(4))C11, Acyl(24:0)C17
                696.57728, // Sph(18:1(4))C11+H, Acyl(24:0)C17+H
                708.57728, // Sph(18:1(4))C12-H, Acyl(24:0)C18-H
                709.58511, // Sph(18:1(4))C12, Acyl(24:0)C18
                710.59293, // Sph(18:1(4))C12+H, Acyl(24:0)C18+H
                722.59293, // Sph(18:1(4))C13-H, Acyl(24:0)C19-H
                723.60076, // Sph(18:1(4))C13, Acyl(24:0)C19
                724.60858, // Sph(18:1(4))C13+H, Acyl(24:0)C19+H
                736.60858, // Sph(18:1(4))C14-H, Acyl(24:0)C20-H
                737.61641, // Sph(18:1(4))C14, Acyl(24:0)C20
                738.62423, // Sph(18:1(4))C14+H, Acyl(24:0)C20+H
                750.62423, // Sph(18:1(4))C15-H, Acyl(24:0)C21-H
                751.63206, // Sph(18:1(4))C15, Acyl(24:0)C21
                752.63988, // Sph(18:1(4))C15+H, Acyl(24:0)C21+H
                764.63988, // Sph(18:1(4))C16-H, Acyl(24:0)C22-H
                765.64771, // Sph(18:1(4))C16, Acyl(24:0)C22
                766.65553, // Sph(18:1(4))C16+H, Acyl(24:0)C22+H
                778.65553, // Sph(18:1(4))C17-H, Acyl(24:0)C23-H
                779.66336, // Sph(18:1(4))C17, Acyl(24:0)C23
                780.67118, // Sph(18:1(4))C17+H, Acyl(24:0)C23+H
                794.68683, // Precursor-H2O, 
                812.6974, // Precursor, 
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
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H-H2O]+"));

            var expects = new[]
            {
                204.08665, // Hex +C2H5NO, 
                252.26858, // [sph+H]+ -CH4O2, 
                264.26858, // [sph+H]+-2H2O, 
                282.27914, // [sph+H]+ -H2O, 
                368.38869, // [FAA+H]+, 
                393.39652, // [FAA+C2H+H]+, 
                470.31123, // Acyl(24:0)C1-H, 
                471.31905, // Acyl(24:0)C1, 
                472.32688, // Acyl(24:0)C1+H, 
                484.32688, // Acyl(24:0)C2-H, 
                485.3347, // Acyl(24:0)C2, 
                486.34253, // Acyl(24:0)C2+H, 
                498.34253, // Acyl(24:0)C3-H, 
                499.35035, // Acyl(24:0)C3, 
                500.35818, // Acyl(24:0)C3+H, 
                512.35818, // Acyl(24:0)C4-H, 
                513.366, // Acyl(24:0)C4, 
                514.37383, // Acyl(24:0)C4+H, 
                526.37383, // Acyl(24:0)C5-H, 
                527.38165, // Acyl(24:0)C5, 
                528.38948, // Acyl(24:0)C5+H, 
                540.38948, // Acyl(24:0)C6-H, 
                541.3973, // Acyl(24:0)C6, 
                542.40513, // Acyl(24:0)C6+H, 
                554.40513, // Acyl(24:0)C7-H, 
                555.41295, // Acyl(24:0)C7, 
                555.44934, // [FAA+C2H4NO+Hex+adduct]+, 
                556.42078, // Acyl(24:0)C7+H, 
                568.42078, // Acyl(24:0)C8-H, 
                569.4286, // Acyl(24:0)C8, 
                570.43643, // Acyl(24:0)C8+H, 
                582.43643, // Acyl(24:0)C9-H, 
                583.44425, // Acyl(24:0)C9, 
                584.45208, // Sph(18:1(4))C3-H, Acyl(24:0)C9+H
                585.4599, // Sph(18:1(4))C3, 
                586.46773, // Sph(18:1(4))C3+H, 
                596.45208, // Acyl(24:0)C10-H, 
                596.61288, // Precursor-Hex-2H2O, 
                597.4599, // Acyl(24:0)C10, 
                598.46773, // Sph(18:1(4))C4-H, Acyl(24:0)C10+H
                599.47555, // Sph(18:1(4))C4, 
                600.48338, // Sph(18:1(4))C4+H, 
                610.46773, // Acyl(24:0)C11-H, 
                611.47555, // Acyl(24:0)C11, 
                612.48338, // Sph(18:1(4))C5-H, Acyl(24:0)C11+H
                613.4912, // Sph(18:1(4))C5, 
                614.49903, // Sph(18:1(4))C5+H, 
                614.62344, // Precursor-Hex-H2O, 
                624.48338, // Acyl(24:0)C12-H, 
                625.4912, // Sph(18:1(4))C6-H, Acyl(24:0)C12
                626.49903, // Sph(18:1(4))C6, Acyl(24:0)C12+H
                627.50686, // Sph(18:1(4))C6+H, 
                632.63401, // Precursor-Hex, 
                638.49903, // Sph(18:1(4))C7-H, Acyl(24:0)C13-H
                639.50686, // Sph(18:1(4))C7, Acyl(24:0)C13
                640.51468, // Sph(18:1(4))C7+H, Acyl(24:0)C13+H
                652.51468, // Sph(18:1(4))C8-H, Acyl(24:0)C14-H
                653.52251, // Sph(18:1(4))C8, Acyl(24:0)C14
                654.53033, // Sph(18:1(4))C8+H, Acyl(24:0)C14+H
                666.53033, // Sph(18:1(4))C9-H, Acyl(24:0)C15-H
                667.53816, // Sph(18:1(4))C9, Acyl(24:0)C15
                668.54598, // Sph(18:1(4))C9+H, Acyl(24:0)C15+H
                680.54598, // Sph(18:1(4))C10-H, Acyl(24:0)C16-H
                681.55381, // Sph(18:1(4))C10, Acyl(24:0)C16
                682.56163, // Sph(18:1(4))C10+H, Acyl(24:0)C16+H
                694.56163, // Sph(18:1(4))C11-H, Acyl(24:0)C17-H
                695.56946, // Sph(18:1(4))C11, Acyl(24:0)C17
                696.57728, // Sph(18:1(4))C11+H, Acyl(24:0)C17+H
                708.57728, // Sph(18:1(4))C12-H, Acyl(24:0)C18-H
                709.58511, // Sph(18:1(4))C12, Acyl(24:0)C18
                710.59293, // Sph(18:1(4))C12+H, Acyl(24:0)C18+H
                722.59293, // Sph(18:1(4))C13-H, Acyl(24:0)C19-H
                723.60076, // Sph(18:1(4))C13, Acyl(24:0)C19
                724.60858, // Sph(18:1(4))C13+H, Acyl(24:0)C19+H
                736.60858, // Sph(18:1(4))C14-H, Acyl(24:0)C20-H
                737.61641, // Sph(18:1(4))C14, Acyl(24:0)C20
                738.62423, // Sph(18:1(4))C14+H, Acyl(24:0)C20+H
                750.62423, // Sph(18:1(4))C15-H, Acyl(24:0)C21-H
                751.63206, // Sph(18:1(4))C15, Acyl(24:0)C21
                752.63988, // Sph(18:1(4))C15+H, Acyl(24:0)C21+H
                764.63988, // Sph(18:1(4))C16-H, Acyl(24:0)C22-H
                765.64771, // Sph(18:1(4))C16, Acyl(24:0)C22
                766.65553, // Sph(18:1(4))C16+H, Acyl(24:0)C22+H
                776.67627, // Precursor-H2O, 
                778.65553, // Sph(18:1(4))C17-H, Acyl(24:0)C23-H
                779.66336, // Sph(18:1(4))C17, Acyl(24:0)C23
                780.67118, // Sph(18:1(4))C17+H, Acyl(24:0)C23+H
                794.68683, // Precursor, 
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
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                156.0393, // [C5H9O4+adduct]+, 
                244.07916, // Hex +C2H5NO, 
                393.39652, // [FAA+C2H+H]+, 
                510.30374, // Acyl(24:0)C1-H, 
                511.31156, // Acyl(24:0)C1, 
                512.31939, // Acyl(24:0)C1+H, 
                524.31939, // Acyl(24:0)C2-H, 
                525.32721, // Acyl(24:0)C2, 
                526.33504, // Acyl(24:0)C2+H, 
                538.33504, // Acyl(24:0)C3-H, 
                539.34286, // Acyl(24:0)C3, 
                540.35069, // Acyl(24:0)C3+H, 
                552.35069, // Acyl(24:0)C4-H, 
                553.35851, // Acyl(24:0)C4, 
                554.36634, // Acyl(24:0)C4+H, 
                566.36634, // Acyl(24:0)C5-H, 
                567.37416, // Acyl(24:0)C5, 
                568.38199, // Acyl(24:0)C5+H, 
                580.38199, // Acyl(24:0)C6-H, 
                581.38981, // Acyl(24:0)C6, 
                582.39764, // Acyl(24:0)C6+H, 
                594.39764, // Acyl(24:0)C7-H, 
                595.40546, // Acyl(24:0)C7, 
                595.44185, // [FAA+C2H4NO+Hex+adduct]+, 
                596.41329, // Acyl(24:0)C7+H, 
                608.41329, // Acyl(24:0)C8-H, 
                609.42111, // Acyl(24:0)C8, 
                610.42894, // Acyl(24:0)C8+H, 
                622.42894, // Acyl(24:0)C9-H, 
                623.43676, // Acyl(24:0)C9, 
                624.44459, // Sph(18:1(4))C3-H, Acyl(24:0)C9+H
                625.45241, // Sph(18:1(4))C3, 
                626.46024, // Sph(18:1(4))C3+H, 
                636.44459, // Acyl(24:0)C10-H, 
                637.45241, // Acyl(24:0)C10, 
                638.46024, // Sph(18:1(4))C4-H, Acyl(24:0)C10+H
                639.46806, // Sph(18:1(4))C4, 
                640.47589, // Sph(18:1(4))C4+H, 
                650.46024, // Acyl(24:0)C11-H, 
                651.46806, // Acyl(24:0)C11, 
                652.47589, // Sph(18:1(4))C5-H, Acyl(24:0)C11+H
                653.48371, // Sph(18:1(4))C5, 
                654.49154, // Sph(18:1(4))C5+H, 
                654.61595, // Precursor-Hex-H2O, 
                664.47589, // Acyl(24:0)C12-H, 
                665.48371, // Sph(18:1(4))C6-H, Acyl(24:0)C12
                666.49154, // Sph(18:1(4))C6, Acyl(24:0)C12+H
                667.49936, // Sph(18:1(4))C6+H, 
                678.49154, // Sph(18:1(4))C7-H, Acyl(24:0)C13-H
                679.49936, // Sph(18:1(4))C7, Acyl(24:0)C13
                680.50719, // Sph(18:1(4))C7+H, Acyl(24:0)C13+H
                692.50719, // Sph(18:1(4))C8-H, Acyl(24:0)C14-H
                693.51501, // Sph(18:1(4))C8, Acyl(24:0)C14
                694.52284, // Sph(18:1(4))C8+H, Acyl(24:0)C14+H
                700.62143, // Precursor-C5H10O4, 
                706.52284, // Sph(18:1(4))C9-H, Acyl(24:0)C15-H
                707.53066, // Sph(18:1(4))C9, Acyl(24:0)C15
                708.53849, // Sph(18:1(4))C9+H, Acyl(24:0)C15+H
                720.53849, // Sph(18:1(4))C10-H, Acyl(24:0)C16-H
                721.54631, // Sph(18:1(4))C10, Acyl(24:0)C16
                722.55414, // Sph(18:1(4))C10+H, Acyl(24:0)C16+H
                734.55414, // Sph(18:1(4))C11-H, Acyl(24:0)C17-H
                735.56196, // Sph(18:1(4))C11, Acyl(24:0)C17
                736.56979, // Sph(18:1(4))C11+H, Acyl(24:0)C17+H
                748.56979, // Sph(18:1(4))C12-H, Acyl(24:0)C18-H
                749.57761, // Sph(18:1(4))C12, Acyl(24:0)C18
                750.58544, // Sph(18:1(4))C12+H, Acyl(24:0)C18+H
                762.58544, // Sph(18:1(4))C13-H, Acyl(24:0)C19-H
                763.59326, // Sph(18:1(4))C13, Acyl(24:0)C19
                764.60109, // Sph(18:1(4))C13+H, Acyl(24:0)C19+H
                776.60109, // Sph(18:1(4))C14-H, Acyl(24:0)C20-H
                777.60891, // Sph(18:1(4))C14, Acyl(24:0)C20
                778.61674, // Sph(18:1(4))C14+H, Acyl(24:0)C20+H
                790.61674, // Sph(18:1(4))C15-H, Acyl(24:0)C21-H
                791.62456, // Sph(18:1(4))C15, Acyl(24:0)C21
                792.63239, // Sph(18:1(4))C15+H, Acyl(24:0)C21+H
                804.63239, // Sph(18:1(4))C16-H, Acyl(24:0)C22-H
                805.64021, // Sph(18:1(4))C16, Acyl(24:0)C22
                806.64804, // Sph(18:1(4))C16+H, Acyl(24:0)C22+H
                818.64804, // Sph(18:1(4))C17-H, Acyl(24:0)C23-H
                819.65586, // Sph(18:1(4))C17, Acyl(24:0)C23
                820.66369, // Sph(18:1(4))C17+H, Acyl(24:0)C23+H
                834.67934, // Precursor, 
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}