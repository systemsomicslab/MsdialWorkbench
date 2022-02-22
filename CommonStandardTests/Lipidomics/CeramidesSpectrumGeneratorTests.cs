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
    public class SMSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateSM_H()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //[H][C@](/C=C/CCCCCCCCCCCCC)(O)[C@@]([H])(NC(CCCCCCC/C=C\CCCCCCCC)=O)COP([O-])(OCC[N+](C)(C)C)=O
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.SM, 728.583225334, new PositionLevelChains(sphingo, acyl));

            var generator = new SMSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                184.07332, // C5H14NO4P(Header)
                225.09987, // C7H18N2O4P(Header+C2H3N)
                253.09478, // Header+C3H3NO
                264.26858, // [sph+H]+ -Header -H2O
                307.28697, // [FAA+C2H+adduct]+
                489.34519, // [FAA+C2H+Header+adduct]+
                491.32445, // Acyl(18:1)C1-H, 
                492.33228, // Acyl(18:1)C1, 
                493.3401, // Acyl(18:1)C1+H, 
                505.3401, // Acyl(18:1)C2-H, 
                506.34793, // Acyl(18:1)C2, 
                507.35575, // Acyl(18:1)C2+H, 
                519.35575, // Sph(18:1)C3-H, Acyl(18:1)C3-H
                520.36358, // Sph(18:1)C3, Acyl(18:1)C3
                521.3714, // Sph(18:1)C3+H, Acyl(18:1)C3+H
                533.3714, // Sph(18:1)C4-H, Acyl(18:1)C4-H
                534.37923, // Sph(18:1)C4, Acyl(18:1)C4
                535.38705, // Sph(18:1)C4+H, Acyl(18:1)C4+H
                547.38705, // Sph(18:1)C5-H, Acyl(18:1)C5-H
                548.39488, // Sph(18:1)C5, Acyl(18:1)C5
                549.4027, // Sph(18:1)C5+H, Acyl(18:1)C5+H
                560.39488, // Sph(18:1)C6-H, 
                561.4027, // Sph(18:1)C6, Acyl(18:1)C6-H
                562.41053, // Sph(18:1)C6+H, Acyl(18:1)C6
                563.41835, // Acyl(18:1)C6+H, 
                573.4027, // Sph(18:1)C7-H, 
                574.41053, // Sph(18:1)C7, 
                575.41835, // Sph(18:1)C7+H, Acyl(18:1)C7-H
                576.42618, // Acyl(18:1)C7, 
                577.434, // Acyl(18:1)C7+H, 
                587.41835, // Sph(18:1)C8-H, 
                588.42618, // Sph(18:1)C8, 
                589.434, // Sph(18:1)C8+H, Acyl(18:1)C8-H
                590.44183, // Acyl(18:1)C8, 
                591.44965, // Acyl(18:1)C8+H, 
                601.434, // Sph(18:1)C9-H, 
                602.44183, // Sph(18:1)C9, Acyl(18:1)C9-H
                603.44965, // Sph(18:1)C9+H, Acyl(18:1)C9
                604.45748, // Acyl(18:1)C9+H, 
                615.44965, // Sph(18:1)C10-H, Acyl(18:1)C10-H
                616.45748, // Sph(18:1)C10, Acyl(18:1)C10
                617.4653, // Sph(18:1)C10+H, Acyl(18:1)C10+H
                629.4653, // Sph(18:1)C11-H, Acyl(18:1)C11-H
                630.47313, // Sph(18:1)C11, Acyl(18:1)C11
                631.48095, // Sph(18:1)C11+H, Acyl(18:1)C11+H
                643.48095, // Sph(18:1)C12-H, Acyl(18:1)C12-H
                644.48878, // Sph(18:1)C12, Acyl(18:1)C12
                645.4966, // Sph(18:1)C12+H, Acyl(18:1)C12+H
                657.4966, // Sph(18:1)C13-H, Acyl(18:1)C13-H
                658.50443, // Sph(18:1)C13, Acyl(18:1)C13
                659.51225, // Sph(18:1)C13+H, Acyl(18:1)C13+H
                671.51225, // Sph(18:1)C14-H, Acyl(18:1)C14-H
                672.52008, // Sph(18:1)C14, Acyl(18:1)C14
                673.5279, // Sph(18:1)C14+H, Acyl(18:1)C14+H
                685.5279, // Sph(18:1)C15-H, Acyl(18:1)C15-H
                686.53573, // Sph(18:1)C15, Acyl(18:1)C15
                687.54355, // Sph(18:1)C15+H, Acyl(18:1)C15+H
                699.54355, // Sph(18:1)C16-H, Acyl(18:1)C16-H
                700.55138, // Sph(18:1)C16, Acyl(18:1)C16
                701.5592, // Sph(18:1)C16+H, Acyl(18:1)C16+H
                713.5592, // Sph(18:1)C17-H, Acyl(18:1)C17-H
                714.56703, // Sph(18:1)C17, Acyl(18:1)C17
                715.57485, // Sph(18:1)C17+H, Acyl(18:1)C17+H
                729.5905, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateSM_Na()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //[H][C@](/C=C/CCCCCCCCCCCCC)(O)[C@@]([H])(NC(CCCCCCC/C=C\CCCCCCCC)=O)COP([O-])(OCC[N+](C)(C)C)=O
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.SM, 751.57244603, new PositionLevelChains(sphingo, acyl));

            var generator = new SMSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                206.05527, // C5H14NO4P(Header), 
                247.08181, // C7H18N2O4P(Header+C2H3N), 
                329.26891, // [FAA+C2H+adduct]+, 
                511.32713, // [FAA+C2H+Header+adduct]+, 
                536.29562, // Acyl(18:1)C1-H, 
                537.30344, // Acyl(18:1)C1, 
                538.31127, // Acyl(18:1)C1+H, 
                550.31127, // Acyl(18:1)C2-H, 
                551.31909, // Acyl(18:1)C2, 
                552.32692, // Acyl(18:1)C2+H, 
                564.32692, // Sph(18:1)C3-H, Acyl(18:1)C3-H
                565.33474, // Sph(18:1)C3, Acyl(18:1)C3
                566.34257, // Sph(18:1)C3+H, Acyl(18:1)C3+H
                578.34257, // Sph(18:1)C4-H, Acyl(18:1)C4-H
                579.35039, // Sph(18:1)C4, Acyl(18:1)C4
                580.35822, // Sph(18:1)C4+H, Acyl(18:1)C4+H
                591.49562, // Precursor-Header, 
                592.35822, // Sph(18:1)C5-H, Acyl(18:1)C5-H
                593.36604, // Sph(18:1)C5, Acyl(18:1)C5
                594.37387, // Sph(18:1)C5+H, Acyl(18:1)C5+H
                605.36604, // Sph(18:1)C6-H, 
                606.37387, // Sph(18:1)C6, Acyl(18:1)C6-H
                607.38169, // Sph(18:1)C6+H, Acyl(18:1)C6
                608.38952, // Acyl(18:1)C6+H, 
                618.37387, // Sph(18:1)C7-H, 
                619.38169, // Sph(18:1)C7, 
                620.38952, // Sph(18:1)C7+H, Acyl(18:1)C7-H
                621.39734, // Acyl(18:1)C7, 
                622.40517, // Acyl(18:1)C7+H, 
                632.38952, // Sph(18:1)C8-H, 
                633.39734, // Sph(18:1)C8, 
                634.40517, // Sph(18:1)C8+H, Acyl(18:1)C8-H
                635.41299, // Acyl(18:1)C8, 
                636.42082, // Acyl(18:1)C8+H, 
                646.40517, // Sph(18:1)C9-H, 
                647.41299, // Sph(18:1)C9, Acyl(18:1)C9-H
                648.42082, // Sph(18:1)C9+H, Acyl(18:1)C9
                649.42864, // Acyl(18:1)C9+H, 
                660.42082, // Sph(18:1)C10-H, Acyl(18:1)C10-H
                661.42864, // Sph(18:1)C10, Acyl(18:1)C10
                662.43647, // Sph(18:1)C10+H, Acyl(18:1)C10+H
                674.43647, // Sph(18:1)C11-H, Acyl(18:1)C11-H
                675.44429, // Sph(18:1)C11, Acyl(18:1)C11
                676.45212, // Sph(18:1)C11+H, Acyl(18:1)C11+H
                688.45212, // Sph(18:1)C12-H, Acyl(18:1)C12-H
                689.45994, // Sph(18:1)C12, Acyl(18:1)C12
                690.46777, // Sph(18:1)C12+H, Acyl(18:1)C12+H
                702.46777, // Sph(18:1)C13-H, Acyl(18:1)C13-H
                703.47559, // Sph(18:1)C13, Acyl(18:1)C13
                704.48342, // Sph(18:1)C13+H, Acyl(18:1)C13+H
                715.48817, // Precursor-C3H9N, 
                716.48342, // Sph(18:1)C14-H, Acyl(18:1)C14-H
                717.49124, // Sph(18:1)C14, Acyl(18:1)C14
                718.49907, // Sph(18:1)C14+H, Acyl(18:1)C14+H
                730.49907, // Sph(18:1)C15-H, Acyl(18:1)C15-H
                731.50689, // Sph(18:1)C15, Acyl(18:1)C15
                732.51472, // Sph(18:1)C15+H, Acyl(18:1)C15+H
                744.51472, // Sph(18:1)C16-H, Acyl(18:1)C16-H
                745.52254, // Sph(18:1)C16, Acyl(18:1)C16
                746.53037, // Sph(18:1)C16+H, Acyl(18:1)C16+H
                758.53037, // Sph(18:1)C17-H, Acyl(18:1)C17-H
                759.53819, // Sph(18:1)C17, Acyl(18:1)C17
                760.54602, // Sph(18:1)C17+H, Acyl(18:1)C17+H
                774.56167, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class CerNSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateCerNS_H()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4,8), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCC=CCCCCCCCC(=O)NC(CO)C(O)C=CCCC=CCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.Cer_NS, 645.6059954, new PositionLevelChains(sphingo, acyl));

            var generator = new CeramideSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                250.25293, // [sph+H]+ -CH4O2, 
                262.25293, // [sph+H]+-2H2O, 
                280.26349, // [sph+H]+ -H2O, 
                306.24276, // Acyl(24:1(9))C1-H, 
                307.25058, // Acyl(24:1(9))C1, 
                308.25841, // Acyl(24:1(9))C1+H, 
                320.25841, // Acyl(24:1(9))C2-H, 
                321.26623, // Acyl(24:1(9))C2, 
                322.27406, // Acyl(24:1(9))C2+H, 
                334.27406, // Acyl(24:1(9))C3-H, 
                335.28188, // Acyl(24:1(9))C3, 
                336.28971, // Acyl(24:1(9))C3+H, 
                348.28971, // Acyl(24:1(9))C4-H, 
                349.29753, // Acyl(24:1(9))C4, 
                350.30536, // Acyl(24:1(9))C4+H, 
                362.30536, // Acyl(24:1(9))C5-H, 
                363.31318, // Acyl(24:1(9))C5, 
                364.32101, // Acyl(24:1(9))C5+H, 
                366.37304, // [FAA+H]+, 
                376.32101, // Acyl(24:1(9))C6-H, 
                377.32883, // Acyl(24:1(9))C6, 
                378.33666, // Acyl(24:1(9))C6+H, 
                390.33666, // Acyl(24:1(9))C7-H, 
                391.34448, // Acyl(24:1(9))C7, 
                391.38087, // [FAA+C2H+H]+, 
                392.35231, // Acyl(24:1(9))C7+H, 
                404.35231, // Acyl(24:1(9))C8-H, 
                405.36013, // Acyl(24:1(9))C8, 
                406.36796, // Acyl(24:1(9))C8+H, 
                408.38361, // [FAA+C2H2O+H]+, 
                417.36013, // Acyl(24:1(9))C9-H, 
                418.36796, // Acyl(24:1(9))C9, 
                419.37578, // Acyl(24:1(9))C9+H, 
                420.38361, // Sph(18:2(4,8))C3-H, 
                421.39143, // Sph(18:2(4,8))C3, 
                422.39926, // Sph(18:2(4,8))C3+H, 
                430.36796, // Acyl(24:1(9))C10-H, 
                431.37578, // Acyl(24:1(9))C10, 
                432.38361, // Acyl(24:1(9))C10+H, 
                434.39926, // Sph(18:2(4,8))C4-H, 
                435.40708, // Sph(18:2(4,8))C4, 
                436.41491, // Sph(18:2(4,8))C4+H, 
                444.38361, // Acyl(24:1(9))C11-H, 
                445.39143, // Acyl(24:1(9))C11, 
                446.39926, // Acyl(24:1(9))C11+H, 
                448.41491, // Sph(18:2(4,8))C5-H, 
                449.42273, // Sph(18:2(4,8))C5, 
                450.43056, // Sph(18:2(4,8))C5+H, 
                458.39926, // Acyl(24:1(9))C12-H, 
                459.40708, // Acyl(24:1(9))C12, 
                460.41491, // Acyl(24:1(9))C12+H, 
                461.42273, // Sph(18:2(4,8))C6-H, 
                462.43056, // Sph(18:2(4,8))C6, 
                463.43838, // Sph(18:2(4,8))C6+H, 
                472.41491, // Acyl(24:1(9))C13-H, 
                473.42273, // Acyl(24:1(9))C13, 
                474.43056, // Sph(18:2(4,8))C7-H, Acyl(24:1(9))C13+H
                475.43838, // Sph(18:2(4,8))C7, 
                476.44621, // Sph(18:2(4,8))C7+H, 
                486.43056, // Acyl(24:1(9))C14-H, 
                487.43838, // Acyl(24:1(9))C14, 
                488.44621, // Sph(18:2(4,8))C8-H, Acyl(24:1(9))C14+H
                489.45403, // Sph(18:2(4,8))C8, 
                490.46186, // Sph(18:2(4,8))C8+H, 
                500.44621, // Acyl(24:1(9))C15-H, 
                501.45403, // Acyl(24:1(9))C15, 
                502.46186, // Sph(18:2(4,8))C9-H, Acyl(24:1(9))C15+H
                503.46968, // Sph(18:2(4,8))C9, 
                504.47751, // Sph(18:2(4,8))C9+H, 
                514.46186, // Acyl(24:1(9))C16-H, 
                515.46968, // Sph(18:2(4,8))C10-H, Acyl(24:1(9))C16
                516.47751, // Sph(18:2(4,8))C10, Acyl(24:1(9))C16+H
                517.48533, // Sph(18:2(4,8))C10+H, 
                528.47751, // Sph(18:2(4,8))C11-H, Acyl(24:1(9))C17-H
                529.48533, // Sph(18:2(4,8))C11, Acyl(24:1(9))C17
                530.49316, // Sph(18:2(4,8))C11+H, Acyl(24:1(9))C17+H
                542.49316, // Sph(18:2(4,8))C12-H, Acyl(24:1(9))C18-H
                543.50098, // Sph(18:2(4,8))C12, Acyl(24:1(9))C18
                544.50881, // Sph(18:2(4,8))C12+H, Acyl(24:1(9))C18+H
                556.50881, // Sph(18:2(4,8))C13-H, Acyl(24:1(9))C19-H
                557.51663, // Sph(18:2(4,8))C13, Acyl(24:1(9))C19
                558.52446, // Sph(18:2(4,8))C13+H, Acyl(24:1(9))C19+H
                570.52446, // Sph(18:2(4,8))C14-H, Acyl(24:1(9))C20-H
                571.53228, // Sph(18:2(4,8))C14, Acyl(24:1(9))C20
                572.54011, // Sph(18:2(4,8))C14+H, Acyl(24:1(9))C20+H
                584.54011, // Sph(18:2(4,8))C15-H, Acyl(24:1(9))C21-H
                585.54793, // Sph(18:2(4,8))C15, Acyl(24:1(9))C21
                586.55576, // Sph(18:2(4,8))C15+H, Acyl(24:1(9))C21+H
                596.57649, // [M+H]+ -CH6O2, 
                598.55576, // Sph(18:2(4,8))C16-H, Acyl(24:1(9))C22-H
                599.56358, // Sph(18:2(4,8))C16, Acyl(24:1(9))C22
                600.57141, // Sph(18:2(4,8))C16+H, Acyl(24:1(9))C22+H
                610.59214, // Precursor-2H2O, 
                612.57141, // Sph(18:2(4,8))C17-H, Acyl(24:1(9))C23-H
                613.57923, // Sph(18:2(4,8))C17, Acyl(24:1(9))C23
                614.58706, // Sph(18:2(4,8))C17+H, Acyl(24:1(9))C23+H
                628.60271, // Precursor-H2O, 
                646.61327, // Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateCerNS_H2O()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4, 8), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCC=CCCCCCCCC(=O)NC(CO)C(O)C=CCCC=CCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.Cer_NS, 645.6059954, new PositionLevelChains(sphingo, acyl));//Cer 18:2(4,8);2O/24:1(9)

            var generator = new CeramideSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H-H2O]+"));

            var expects = new[]
            {
                250.25293, // [sph+H]+ -CH4O2, 
                262.25293, // [sph+H]+-2H2O, 
                280.26349, // [sph+H]+ -H2O, 
                306.24276, // Acyl(24:1(9))C1-H, 
                307.25058, // Acyl(24:1(9))C1, 
                308.25841, // Acyl(24:1(9))C1+H, 
                320.25841, // Acyl(24:1(9))C2-H, 
                321.26623, // Acyl(24:1(9))C2, 
                322.27406, // Acyl(24:1(9))C2+H, 
                334.27406, // Acyl(24:1(9))C3-H, 
                335.28188, // Acyl(24:1(9))C3, 
                336.28971, // Acyl(24:1(9))C3+H, 
                348.28971, // Acyl(24:1(9))C4-H, 
                349.29753, // Acyl(24:1(9))C4, 
                350.30536, // Acyl(24:1(9))C4+H, 
                362.30536, // Acyl(24:1(9))C5-H, 
                363.31318, // Acyl(24:1(9))C5, 
                364.32101, // Acyl(24:1(9))C5+H, 
                366.37304, // [FAA+H]+, 
                376.32101, // Acyl(24:1(9))C6-H, 
                377.32883, // Acyl(24:1(9))C6, 
                378.33666, // Acyl(24:1(9))C6+H, 
                390.33666, // Acyl(24:1(9))C7-H, 
                391.34448, // Acyl(24:1(9))C7, 
                //391.38087, // [FAA+C2H+H]+, 
                392.35231, // Acyl(24:1(9))C7+H, 
                404.35231, // Acyl(24:1(9))C8-H, 
                405.36013, // Acyl(24:1(9))C8, 
                406.36796, // Acyl(24:1(9))C8+H, 
                //408.38361, // [FAA+C2H2O+H]+, 
                417.36013, // Acyl(24:1(9))C9-H, 
                418.36796, // Acyl(24:1(9))C9, 
                419.37578, // Acyl(24:1(9))C9+H, 
                420.38361, // Sph(18:2(4,8))C3-H, 
                421.39143, // Sph(18:2(4,8))C3, 
                422.39926, // Sph(18:2(4,8))C3+H, 
                430.36796, // Acyl(24:1(9))C10-H, 
                431.37578, // Acyl(24:1(9))C10, 
                432.38361, // Acyl(24:1(9))C10+H, 
                434.39926, // Sph(18:2(4,8))C4-H, 
                435.40708, // Sph(18:2(4,8))C4, 
                436.41491, // Sph(18:2(4,8))C4+H, 
                444.38361, // Acyl(24:1(9))C11-H, 
                445.39143, // Acyl(24:1(9))C11, 
                446.39926, // Acyl(24:1(9))C11+H, 
                448.41491, // Sph(18:2(4,8))C5-H, 
                449.42273, // Sph(18:2(4,8))C5, 
                450.43056, // Sph(18:2(4,8))C5+H, 
                458.39926, // Acyl(24:1(9))C12-H, 
                459.40708, // Acyl(24:1(9))C12, 
                460.41491, // Acyl(24:1(9))C12+H, 
                461.42273, // Sph(18:2(4,8))C6-H, 
                462.43056, // Sph(18:2(4,8))C6, 
                463.43838, // Sph(18:2(4,8))C6+H, 
                472.41491, // Acyl(24:1(9))C13-H, 
                473.42273, // Acyl(24:1(9))C13, 
                474.43056, // Sph(18:2(4,8))C7-H, Acyl(24:1(9))C13+H
                475.43838, // Sph(18:2(4,8))C7, 
                476.44621, // Sph(18:2(4,8))C7+H, 
                486.43056, // Acyl(24:1(9))C14-H, 
                487.43838, // Acyl(24:1(9))C14, 
                488.44621, // Sph(18:2(4,8))C8-H, Acyl(24:1(9))C14+H
                489.45403, // Sph(18:2(4,8))C8, 
                490.46186, // Sph(18:2(4,8))C8+H, 
                500.44621, // Acyl(24:1(9))C15-H, 
                501.45403, // Acyl(24:1(9))C15, 
                502.46186, // Sph(18:2(4,8))C9-H, Acyl(24:1(9))C15+H
                503.46968, // Sph(18:2(4,8))C9, 
                504.47751, // Sph(18:2(4,8))C9+H, 
                514.46186, // Acyl(24:1(9))C16-H, 
                515.46968, // Sph(18:2(4,8))C10-H, Acyl(24:1(9))C16
                516.47751, // Sph(18:2(4,8))C10, Acyl(24:1(9))C16+H
                517.48533, // Sph(18:2(4,8))C10+H, 
                528.47751, // Sph(18:2(4,8))C11-H, Acyl(24:1(9))C17-H
                529.48533, // Sph(18:2(4,8))C11, Acyl(24:1(9))C17
                530.49316, // Sph(18:2(4,8))C11+H, Acyl(24:1(9))C17+H
                542.49316, // Sph(18:2(4,8))C12-H, Acyl(24:1(9))C18-H
                543.50098, // Sph(18:2(4,8))C12, Acyl(24:1(9))C18
                544.50881, // Sph(18:2(4,8))C12+H, Acyl(24:1(9))C18+H
                556.50881, // Sph(18:2(4,8))C13-H, Acyl(24:1(9))C19-H
                557.51663, // Sph(18:2(4,8))C13, Acyl(24:1(9))C19
                558.52446, // Sph(18:2(4,8))C13+H, Acyl(24:1(9))C19+H
                570.52446, // Sph(18:2(4,8))C14-H, Acyl(24:1(9))C20-H
                571.53228, // Sph(18:2(4,8))C14, Acyl(24:1(9))C20
                572.54011, // Sph(18:2(4,8))C14+H, Acyl(24:1(9))C20+H
                584.54011, // Sph(18:2(4,8))C15-H, Acyl(24:1(9))C21-H
                585.54793, // Sph(18:2(4,8))C15, Acyl(24:1(9))C21
                586.55576, // Sph(18:2(4,8))C15+H, Acyl(24:1(9))C21+H
                596.57649, // [M+H]+ -CH6O2, 
                598.55576, // Sph(18:2(4,8))C16-H, Acyl(24:1(9))C22-H
                599.56358, // Sph(18:2(4,8))C16, Acyl(24:1(9))C22
                600.57141, // Sph(18:2(4,8))C16+H, Acyl(24:1(9))C22+H
                610.59214, // Precursor-H2O, 
                612.57141, // Sph(18:2(4,8))C17-H, Acyl(24:1(9))C23-H
                613.57923, // Sph(18:2(4,8))C17, Acyl(24:1(9))C23
                614.58706, // Sph(18:2(4,8))C17+H, Acyl(24:1(9))C23+H
                628.60271, // Precursor, 
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateCerNS_Na()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4, 8), Oxidized.CreateFromPosition(1, 3)); //CCCCCCCCCCCCCCC=CCCCCCCCC(=O)NC(CO)C(O)C=CCCC=CCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.Cer_NS, 645.6059954, new PositionLevelChains(sphingo, acyl));

            var generator = new CeramideSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+Na]+"));

            var expects = new[]
            {
                346.23526, // Acyl(24:1(9))C1-H, 
                347.24309, // Acyl(24:1(9))C1, 
                348.25091, // Acyl(24:1(9))C1+H, 
                360.25091, // Acyl(24:1(9))C2-H, 
                361.25874, // Acyl(24:1(9))C2, 
                362.26656, // Acyl(24:1(9))C2+H, 
                374.26656, // Acyl(24:1(9))C3-H, 
                375.27439, // Acyl(24:1(9))C3, 
                376.28221, // Acyl(24:1(9))C3+H, 
                388.28221, // Acyl(24:1(9))C4-H, 
                389.29004, // Acyl(24:1(9))C4, 
                390.29786, // Acyl(24:1(9))C4+H, 
                402.29786, // Acyl(24:1(9))C5-H, 
                403.30569, // Acyl(24:1(9))C5, 
                404.31351, // Acyl(24:1(9))C5+H, 
                416.31351, // Acyl(24:1(9))C6-H, 
                417.32134, // Acyl(24:1(9))C6, 
                418.32917, // Acyl(24:1(9))C6+H, 
                430.32917, // Acyl(24:1(9))C7-H, 
                431.33699, // Acyl(24:1(9))C7, 
                431.37338, // [FAA+C2H2O+H]+, 
                432.34482, // Acyl(24:1(9))C7+H, 
                444.34482, // Acyl(24:1(9))C8-H, 
                445.35264, // Acyl(24:1(9))C8, 
                446.36047, // Acyl(24:1(9))C8+H, 
                457.35264, // Acyl(24:1(9))C9-H, 
                458.36047, // Acyl(24:1(9))C9, 
                459.36829, // Acyl(24:1(9))C9+H, 
                460.37612, // Sph(18:2(4,8))C3-H, 
                461.38394, // Sph(18:2(4,8))C3, 
                462.39177, // Sph(18:2(4,8))C3+H, 
                470.36047, // Acyl(24:1(9))C10-H, 
                471.36829, // Acyl(24:1(9))C10, 
                472.37612, // Acyl(24:1(9))C10+H, 
                474.39177, // Sph(18:2(4,8))C4-H, 
                475.39959, // Sph(18:2(4,8))C4, 
                476.40742, // Sph(18:2(4,8))C4+H, 
                484.37612, // Acyl(24:1(9))C11-H, 
                485.38394, // Acyl(24:1(9))C11, 
                486.39177, // Acyl(24:1(9))C11+H, 
                488.40742, // Sph(18:2(4,8))C5-H, 
                489.41524, // Sph(18:2(4,8))C5, 
                490.42307, // Sph(18:2(4,8))C5+H, 
                498.39177, // Acyl(24:1(9))C12-H, 
                499.39959, // Acyl(24:1(9))C12, 
                500.40742, // Acyl(24:1(9))C12+H, 
                501.41524, // Sph(18:2(4,8))C6-H, 
                502.42307, // Sph(18:2(4,8))C6, 
                503.43089, // Sph(18:2(4,8))C6+H, 
                512.40742, // Acyl(24:1(9))C13-H, 
                513.41524, // Acyl(24:1(9))C13, 
                514.42307, // Sph(18:2(4,8))C7-H, Acyl(24:1(9))C13+H
                515.43089, // Sph(18:2(4,8))C7, 
                516.43872, // Sph(18:2(4,8))C7+H, 
                526.42307, // Acyl(24:1(9))C14-H, 
                527.43089, // Acyl(24:1(9))C14, 
                528.43872, // Sph(18:2(4,8))C8-H, Acyl(24:1(9))C14+H
                529.44654, // Sph(18:2(4,8))C8, 
                530.45437, // Sph(18:2(4,8))C8+H, 
                540.43872, // Acyl(24:1(9))C15-H, 
                541.44654, // Acyl(24:1(9))C15, 
                542.45437, // Sph(18:2(4,8))C9-H, Acyl(24:1(9))C15+H
                543.46219, // Sph(18:2(4,8))C9, 
                544.47002, // Sph(18:2(4,8))C9+H, 
                554.45437, // Acyl(24:1(9))C16-H, 
                555.46219, // Sph(18:2(4,8))C10-H, Acyl(24:1(9))C16
                556.47002, // Sph(18:2(4,8))C10, Acyl(24:1(9))C16+H
                557.47784, // Sph(18:2(4,8))C10+H, 
                568.47002, // Sph(18:2(4,8))C11-H, Acyl(24:1(9))C17-H
                569.47784, // Sph(18:2(4,8))C11, Acyl(24:1(9))C17
                570.48567, // Sph(18:2(4,8))C11+H, Acyl(24:1(9))C17+H
                582.48567, // Sph(18:2(4,8))C12-H, Acyl(24:1(9))C18-H
                583.49349, // Sph(18:2(4,8))C12, Acyl(24:1(9))C18
                584.50132, // Sph(18:2(4,8))C12+H, Acyl(24:1(9))C18+H
                596.50132, // Sph(18:2(4,8))C13-H, Acyl(24:1(9))C19-H
                597.50914, // Sph(18:2(4,8))C13, Acyl(24:1(9))C19
                598.51697, // Sph(18:2(4,8))C13+H, Acyl(24:1(9))C19+H
                610.51697, // Sph(18:2(4,8))C14-H, Acyl(24:1(9))C20-H
                611.52479, // Sph(18:2(4,8))C14, Acyl(24:1(9))C20
                612.53262, // Sph(18:2(4,8))C14+H, Acyl(24:1(9))C20+H
                624.53262, // Sph(18:2(4,8))C15-H, Acyl(24:1(9))C21-H
                625.54044, // Sph(18:2(4,8))C15, Acyl(24:1(9))C21
                626.54827, // Sph(18:2(4,8))C15+H, Acyl(24:1(9))C21+H
                637.57683, // Precursor-CH3O, 
                638.54827, // Sph(18:2(4,8))C16-H, Acyl(24:1(9))C22-H
                639.55609, // Sph(18:2(4,8))C16, Acyl(24:1(9))C22
                640.56392, // Sph(18:2(4,8))C16+H, Acyl(24:1(9))C22+H
                652.56392, // Sph(18:2(4,8))C17-H, Acyl(24:1(9))C23-H
                653.57174, // Sph(18:2(4,8))C17, Acyl(24:1(9))C23
                654.57957, // Sph(18:2(4,8))C17+H, Acyl(24:1(9))C23+H
                668.59522, // Precursor, 
                            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}