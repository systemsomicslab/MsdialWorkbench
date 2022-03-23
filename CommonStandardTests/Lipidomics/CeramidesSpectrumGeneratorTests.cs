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
    [TestClass()]
    public class SHexCerSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateSHexCer_H()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //O=C(NC(COC1OC(CO)C(O)C(OS(=O)(=O)O)C1O)C(O)C=CCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            var acyl = new AcylChain(24, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.SHexCer,  891.646934, new PositionLevelChains(sphingo, acyl));

            var generator = new SHexCerSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                222.09721, //  [C8H16NO6+H]+ 
                252.26858, //  [sph-CH4O2+H]+  
                264.26858, //  [Sph-2H2O+H]+ 
                282.27914, //  [Sph-H2O+H]+ 
                302.05403, //  [C8H16NO9S+H]+ 
                368.38869, //  [FAA+H]+ 
                446.32381, //  [sph+Hex+H]+ 
                470.31123, // 24:0(acyl) C1-H 
                471.31905, // 24:0(acyl) C1 
                472.32688, // 24:0(acyl) C1+H 
                484.32688, // 24:0(acyl) C2-H 
                485.3347, // 24:0(acyl) C2 
                486.34253, // 24:0(acyl) C2+H 
                498.34253, // 24:0(acyl) C3-H 
                499.35035, // 24:0(acyl) C3 
                500.35818, // 24:0(acyl) C3+H 
                512.35818, // 24:0(acyl) C4-H 
                513.366, // 24:0(acyl) C4 
                514.37383, // 24:0(acyl) C4+H 
                526.37383, // 24:0(acyl) C5-H 
                527.38165, // 24:0(acyl) C5 
                528.38948, // 24:0(acyl) C5+H 
                540.38948, // 24:0(acyl) C6-H 
                541.3973, // 24:0(acyl) C6 
                542.40513, // 24:0(acyl) C6+H 
                554.40513, // 24:0(acyl) C7-H 
                555.41295, // 24:0(acyl) C7 
                556.42078, // 24:0(acyl) C7+H 
                568.42078, // 24:0(acyl) C8-H 
                569.4286, // 24:0(acyl) C8 
                570.43643, // 24:0(acyl) C8+H 
                582.43643, // 24:0(acyl) C9-H 
                583.44425, // 24:0(acyl) C9 
                584.45208, // 18:1(sphingo) C3-H, 24:0(acyl) C9+H
                585.4599, // 18:1(sphingo) C3
                586.46773, // 18:1(sphingo) C3+H
                596.45208, // 24:0(acyl) C10-H 
                597.4599, // 24:0(acyl) C10 
                598.46773, // 18:1(sphingo) C4-H, 24:0(acyl) C10+H
                599.47556, // 18:1(sphingo) C4
                600.48338, // 18:1(sphingo) C4+H
                610.46773, // 24:0(acyl) C11-H 
                611.47556, // 24:0(acyl) C11 
                612.48338, // 18:1(sphingo) C5-H, 24:0(acyl) C11+H
                613.49121, // 18:1(sphingo) C5
                614.49903, // 18:1(sphingo) C5+H
                614.62344, //  [M-C6H12O9S-H2O+H]+ 
                624.48338, // 24:0(acyl) C12-H 
                625.49121, // 18:1(sphingo) C6-H, 24:0(acyl) C12
                626.49903, // 18:1(sphingo) C6, 24:0(acyl) C12+H
                627.50686, // 18:1(sphingo) C6+H
                632.63401, //  [M-C6H12O9S+H]+ 
                638.49903, // 18:1(sphingo) C7-H, 24:0(acyl) C13-H
                639.50686, // 18:1(sphingo) C7, 24:0(acyl) C13
                640.51468, // 18:1(sphingo) C7+H, 24:0(acyl) C13+H
                650.64457, //  [M-C6H10O8S+H]+ 
                652.51468, // 18:1(sphingo) C8-H, 24:0(acyl) C14-H
                653.52251, // 18:1(sphingo) C8, 24:0(acyl) C14
                654.53033, // 18:1(sphingo) C8+H, 24:0(acyl) C14+H
                666.53033, // 18:1(sphingo) C9-H, 24:0(acyl) C15-H
                667.53816, // 18:1(sphingo) C9, 24:0(acyl) C15
                668.54598, // 18:1(sphingo) C9+H, 24:0(acyl) C15+H
                680.54598, // 18:1(sphingo) C10-H, 24:0(acyl) C16-H
                681.55381, // 18:1(sphingo) C10, 24:0(acyl) C16
                682.56163, // 18:1(sphingo) C10+H, 24:0(acyl) C16+H
                694.56163, // 18:1(sphingo) C11-H, 24:0(acyl) C17-H
                695.56946, // 18:1(sphingo) C11, 24:0(acyl) C17
                696.57728, // 18:1(sphingo) C11+H, 24:0(acyl) C17+H
                708.57728, // 18:1(sphingo) C12-H, 24:0(acyl) C18-H
                709.58511, // 18:1(sphingo) C12, 24:0(acyl) C18
                710.59293, // 18:1(sphingo) C12+H, 24:0(acyl) C18+H
                722.59293, // 18:1(sphingo) C13-H, 24:0(acyl) C19-H
                723.60076, // 18:1(sphingo) C13, 24:0(acyl) C19
                724.60858, // 18:1(sphingo) C13+H, 24:0(acyl) C19+H
                736.60858, // 18:1(sphingo) C14-H, 24:0(acyl) C20-H
                737.61641, // 18:1(sphingo) C14, 24:0(acyl) C20
                738.62423, // 18:1(sphingo) C14+H, 24:0(acyl) C20+H
                750.62423, // 18:1(sphingo) C15-H, 24:0(acyl) C21-H
                751.63206, // 18:1(sphingo) C15, 24:0(acyl) C21
                752.63988, // 18:1(sphingo) C15+H, 24:0(acyl) C21+H
                764.63988, // 18:1(sphingo) C16-H, 24:0(acyl) C22-H
                765.64771, // 18:1(sphingo) C16, 24:0(acyl) C22
                766.65553, // 18:1(sphingo) C16+H, 24:0(acyl) C22+H
                778.65553, // 18:1(sphingo) C17-H, 24:0(acyl) C23-H
                779.66336, // 18:1(sphingo) C17, 24:0(acyl) C23
                780.67118, // 18:1(sphingo) C17+H, 24:0(acyl) C23+H
                794.68683, //  [M-H2SO4+H]+ 
                812.6974, //  [M-SO3+H]+ 
                892.65421, //  Precursor 
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class GM3pectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateGM3_H()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //O=C(O)C1(OC2C(O)C(OC(CO)C2O)OC3C(O)C(O)C(OCC(NC(=O)CCCCCCCCCCCCCCCCC)C(O)C=CCCCCCCCCCCCCC)OC3CO)OC(C(O)C(O)CO)C(N=C(O)C)C(O)C1
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.GM3,1180.74446, new PositionLevelChains(sphingo, acyl));

            var generator = new GM3SpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                264.26858, //  [sph+H]+ -Header -H2O 
                274.09268, //  [C11H15NO7+H]+ 
                292.10324, //  [C11H17NO8+H]+ 
                454.15606, //  [C17H27NO13+H]+ 
                548.54066, //  [M-H2O-C23H37NO18+H]+ 
                566.55122, //  [M-H2O-C23H35NO17+H]+ 
                675.246, //  [C23H35NO17 +C2H3N +2H2O  +H]+ 
                710.59348, //  [M-H2O-C17H27NO13+H]+ 
                728.60405, //  [M-H2O-C17H25NO12+H]+ 
                872.6463, //  [M-H2O-C11H17NO8+H]+ 
                890.65687, //  [M-H2O-C11H15NO7+H]+ 
                941.47004, // 18:0(acyl) C1-H 
                942.47786, // 18:0(acyl) C1 
                943.48569, // 18:0(acyl) C1+H 
                955.48569, // 18:0(acyl) C2-H 
                956.49351, // 18:0(acyl) C2 
                957.50134, // 18:0(acyl) C2+H 
                969.50134, // 18:0(acyl) C3-H 
                970.50916, // 18:0(acyl) C3 
                971.51699, // 18:1(sphingo) C3-H, 18:0(acyl) C3+H
                972.52481, // 18:1(sphingo) C3
                973.53264, // 18:1(sphingo) C3+H
                983.51699, // 18:0(acyl) C4-H 
                984.52481, // 18:0(acyl) C4 
                985.53264, // 18:1(sphingo) C4-H, 18:0(acyl) C4+H
                986.54046, // 18:1(sphingo) C4
                987.54829, // 18:1(sphingo) C4+H
                997.53264, // 18:0(acyl) C5-H 
                998.54046, // 18:0(acyl) C5 
                999.54829, // 18:1(sphingo) C5-H, 18:0(acyl) C5+H
                1000.55611, // 18:1(sphingo) C5
                1001.56394, // 18:1(sphingo) C5+H
                1011.54829, // 18:0(acyl) C6-H 
                1012.55611, // 18:1(sphingo) C6-H, 18:0(acyl) C6
                1013.56394, // 18:1(sphingo) C6, 18:0(acyl) C6+H
                1014.57176, // 18:1(sphingo) C6+H
                1025.56394, // 18:1(sphingo) C7-H, 18:0(acyl) C7-H
                1026.57176, // 18:1(sphingo) C7, 18:0(acyl) C7
                1027.57959, // 18:1(sphingo) C7+H, 18:0(acyl) C7+H
                1039.57959, // 18:1(sphingo) C8-H, 18:0(acyl) C8-H
                1040.58741, // 18:1(sphingo) C8, 18:0(acyl) C8
                1041.59524, // 18:1(sphingo) C8+H, 18:0(acyl) C8+H
                1053.59524, // 18:1(sphingo) C9-H, 18:0(acyl) C9-H
                1054.60306, // 18:1(sphingo) C9, 18:0(acyl) C9
                1055.61089, // 18:1(sphingo) C9+H, 18:0(acyl) C9+H
                1067.61089, // 18:1(sphingo) C10-H, 18:0(acyl) C10-H
                1068.61871, // 18:1(sphingo) C10, 18:0(acyl) C10
                1069.62654, // 18:1(sphingo) C10+H, 18:0(acyl) C10+H
                1081.62654, // 18:1(sphingo) C11-H, 18:0(acyl) C11-H
                1082.63436, // 18:1(sphingo) C11, 18:0(acyl) C11
                1083.64219, // 18:1(sphingo) C11+H, 18:0(acyl) C11+H
                1095.64219, // 18:1(sphingo) C12-H, 18:0(acyl) C12-H
                1096.65001, // 18:1(sphingo) C12, 18:0(acyl) C12
                1097.65784, // 18:1(sphingo) C12+H, 18:0(acyl) C12+H
                1109.65784, // 18:1(sphingo) C13-H, 18:0(acyl) C13-H
                1110.66566, // 18:1(sphingo) C13, 18:0(acyl) C13
                1111.67349, // 18:1(sphingo) C13+H, 18:0(acyl) C13+H
                1123.67349, // 18:1(sphingo) C14-H, 18:0(acyl) C14-H
                1124.68131, // 18:1(sphingo) C14, 18:0(acyl) C14
                1125.68914, // 18:1(sphingo) C14+H, 18:0(acyl) C14+H
                1137.68914, // 18:1(sphingo) C15-H, 18:0(acyl) C15-H
                1138.69696, // 18:1(sphingo) C15, 18:0(acyl) C15
                1139.70479, // 18:1(sphingo) C15+H, 18:0(acyl) C15+H
                1151.70479, // 18:1(sphingo) C16-H, 18:0(acyl) C16-H
                1152.71261, // 18:1(sphingo) C16, 18:0(acyl) C16
                1153.72044, // 18:1(sphingo) C16+H, 18:0(acyl) C16+H
                1162.7339, //  [M-H2O-H+H]+ 
                1165.72044, // 18:1(sphingo) C17-H, 18:0(acyl) C17-H
                1166.72826, // 18:1(sphingo) C17, 18:0(acyl) C17
                1167.73609, // 18:1(sphingo) C17+H, 18:0(acyl) C17+H
                1181.75174, //  Precursor 
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateGM3_NH4()
        {
            var sphingo = new SphingoChain(18, DoubleBond.CreateFromPosition(4), Oxidized.CreateFromPosition(1, 3)); //O=C(O)C1(OC2C(O)C(OC(CO)C2O)OC3C(O)C(O)C(OCC(NC(=O)CCCCCCCCCCCCCCCCC)C(O)C=CCCCCCCCCCCCCC)OC3CO)OC(C(O)C(O)CO)C(N=C(O)C)C(O)C1
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var lipid = new Lipid(LbmClass.GM3, 1180.74446, new PositionLevelChains(sphingo, acyl));

            var generator = new GM3SpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+NH4]+"));

            var expects = new[]
            {
                274.09213, //  [C11H15NO7+H]+ 
                292.10269, //  [C11H17NO8+H]+ 
                454.15552, //  [C17H27NO13+H]+ 
                548.54011, //  [M-H2O-C23H37NO18+H]+ 
                566.55067, //  [M-H2O-C23H35NO17+H]+ 
                675.24545, //  [C23H35NO17 +C2H3N +2H2O  +H]+ 
                710.59293, //  [M-H2O-C17H27NO13+H]+ 
                728.6035, //  [M-H2O-C17H25NO12+H]+ 
                872.64576, //  [M-H2O-C11H17NO8+H]+ 
                890.65632, //  [M-H2O-C11H15NO7+H]+ 
                940.46221, // 18:0(acyl) C1-H 
                941.47004, // 18:0(acyl) C1 
                942.47786, // 18:0(acyl) C1+H 
                954.47786, // 18:0(acyl) C2-H 
                955.48569, // 18:0(acyl) C2 
                956.49351, // 18:0(acyl) C2+H 
                968.49351, // 18:0(acyl) C3-H 
                969.50134, // 18:0(acyl) C3 
                970.50916, // 18:1(sphingo) C3-H, 18:0(acyl) C3+H
                971.51699, // 18:1(sphingo) C3
                972.52481, // 18:1(sphingo) C3+H
                982.50916, // 18:0(acyl) C4-H 
                983.51699, // 18:0(acyl) C4 
                984.52481, // 18:1(sphingo) C4-H, 18:0(acyl) C4+H
                985.53264, // 18:1(sphingo) C4
                986.54046, // 18:1(sphingo) C4+H
                996.52481, // 18:0(acyl) C5-H 
                997.53264, // 18:0(acyl) C5 
                998.54046, // 18:1(sphingo) C5-H, 18:0(acyl) C5+H
                999.54829, // 18:1(sphingo) C5
                1000.55611, // 18:1(sphingo) C5+H
                1010.54046, // 18:0(acyl) C6-H 
                1011.54829, // 18:1(sphingo) C6-H, 18:0(acyl) C6
                1012.55611, // 18:1(sphingo) C6, 18:0(acyl) C6+H
                1013.56394, // 18:1(sphingo) C6+H
                1024.55611, // 18:1(sphingo) C7-H, 18:0(acyl) C7-H
                1025.56394, // 18:1(sphingo) C7, 18:0(acyl) C7
                1026.57176, // 18:1(sphingo) C7+H, 18:0(acyl) C7+H
                1038.57176, // 18:1(sphingo) C8-H, 18:0(acyl) C8-H
                1039.57959, // 18:1(sphingo) C8, 18:0(acyl) C8
                1040.58741, // 18:1(sphingo) C8+H, 18:0(acyl) C8+H
                1052.58741, // 18:1(sphingo) C9-H, 18:0(acyl) C9-H
                1053.59524, // 18:1(sphingo) C9, 18:0(acyl) C9
                1054.60306, // 18:1(sphingo) C9+H, 18:0(acyl) C9+H
                1066.60306, // 18:1(sphingo) C10-H, 18:0(acyl) C10-H
                1067.61089, // 18:1(sphingo) C10, 18:0(acyl) C10
                1068.61871, // 18:1(sphingo) C10+H, 18:0(acyl) C10+H
                1080.61871, // 18:1(sphingo) C11-H, 18:0(acyl) C11-H
                1081.62654, // 18:1(sphingo) C11, 18:0(acyl) C11
                1082.63436, // 18:1(sphingo) C11+H, 18:0(acyl) C11+H
                1094.63436, // 18:1(sphingo) C12-H, 18:0(acyl) C12-H
                1095.64219, // 18:1(sphingo) C12, 18:0(acyl) C12
                1096.65001, // 18:1(sphingo) C12+H, 18:0(acyl) C12+H
                1108.65001, // 18:1(sphingo) C13-H, 18:0(acyl) C13-H
                1109.65784, // 18:1(sphingo) C13, 18:0(acyl) C13
                1110.66566, // 18:1(sphingo) C13+H, 18:0(acyl) C13+H
                1122.66566, // 18:1(sphingo) C14-H, 18:0(acyl) C14-H
                1123.67349, // 18:1(sphingo) C14, 18:0(acyl) C14
                1124.68131, // 18:1(sphingo) C14+H, 18:0(acyl) C14+H
                1136.68131, // 18:1(sphingo) C15-H, 18:0(acyl) C15-H
                1137.68914, // 18:1(sphingo) C15, 18:0(acyl) C15
                1138.69696, // 18:1(sphingo) C15+H, 18:0(acyl) C15+H
                1150.69696, // 18:1(sphingo) C16-H, 18:0(acyl) C16-H
                1151.70479, // 18:1(sphingo) C16, 18:0(acyl) C16
                1152.71261, // 18:1(sphingo) C16+H, 18:0(acyl) C16+H
                1162.73335, //  [M-H2O-H+H]+ 
                1164.71261, // 18:1(sphingo) C17-H, 18:0(acyl) C17-H
                1165.72044, // 18:1(sphingo) C17, 18:0(acyl) C17
                1166.72826, // 18:1(sphingo) C17+H, 18:0(acyl) C17+H
                1181.75174, //  [M+H]+ 
                1198.77829, //  Precursor 
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}