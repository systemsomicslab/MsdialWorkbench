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
    public class EtherPESpectrumGeneratorTests
    {
        #region
        //[TestMethod()]
        //public void GenerateEtherPEPTest() {
        //    var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
        //    var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
        //    var lipid = new Lipid(LbmClass.EtherPE, 747.5199, new PositionLevelChains(alkyl, acyl));

        //    var generator = new EtherPESpectrumGenerator();
        //    var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

        //    var expects = new[]
        //    {
        //        142.027, // Header
        //        182.058, // Gly-C
        //        184.037, // Gly-O
        //        292.299, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ SN1 ether +C2H8NO3P-H3PO4
        //        359.258, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ NL of C2H8NO4P+SN1
        //        390.277, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Sn1ether+C2H8NO3P
        //        446.303, // Sn2 -O
        //        464.314, // Sn2 -C=O
        //        470.267, // Sn1 -CH2
        //        483.275, // Sn1 -O
        //        490.293, // Sn2  1 -H
        //        491.301, // Sn2  1
        //        492.309, // Sn2  1 +H
        //        500.277, // Sn1 -C
        //        504.309, // Sn2  2 -H
        //        505.316, // Sn2  2
        //        506.324, // Sn2  2 +H
        //        512.277, // Sn1  1
        //        518.324, // Sn2  3 -H
        //        519.332, // Sn2  3
        //        520.340, // Sn2  3 +H
        //        525.285, // Sn1  2
        //        532.340, // Sn2  4 -H
        //        533.348, // Sn2  4
        //        534.356, // Sn2  4 +H
        //        539.301, // Sn1  3
        //        545.348, // Sn2  5 -H
        //        546.356, // Sn2  5
        //        547.363, // Sn2  5 +H
        //        553.316, // Sn1  4
        //        558.356, // Sn2  6 -H
        //        559.363, // Sn2  6
        //        560.371, // Sn2  6 +H
        //        567.332, // Sn1  5
        //        572.371, // Sn2  7 -H
        //        573.379, // Sn2  7
        //        574.387, // Sn2  7 +H
        //        581.348, // Sn1  6
        //        585.379, // Sn2  8 -H
        //        586.387, // Sn2  8
        //        587.395, // Sn2  8 +H
        //        595.363, // Sn1  7
        //        598.387, // Sn2  9 -H
        //        599.395, // Sn2  9
        //        600.403, // Sn2  9 +H
        //        607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -C2H8NO4P
        //        609.378, // Sn1  8
        //        612.403, // Sn2 10 -H
        //        613.410, // Sn2 10
        //        614.418, // Sn2 10 +H
        //        623.395, // Sn1  9
        //        625.410, // Sn2 11 -H
        //        626.418, // Sn2 12
        //        627.426, // Sn2 11 +H
        //        637.410, // Sn1 10
        //        638.418, // Sn2 12 -H
        //        639.426, // Sn2 12
        //        640.434, // Sn2 12 +H
        //        651.426, // Sn1 11
        //        652.434, // Sn2 13 -H
        //        653.442, // Sn2 13
        //        654.449, // Sn2 13 +H
        //        664.434, // Sn1 12
        //        665.442, // Sn2 14 -H
        //        666.449, // Sn2 14
        //        667.457, // Sn2 14 +H
        //        677.442, // Sn1 13
        //        678.449, // Sn2 15 -H
        //        679.457, // Sn2 15
        //        680.465, // Sn2 15 +H
        //        691.457, // Sn1 14
        //        692.465, // Sn2 16 -H
        //        693.473, // Sn2 16
        //        694.481, // Sn2 16 +H
        //        705.473, // Sn1 15 Sn2 17 -H
        //        706.481, // Sn2 17
        //        707.489, // Sn2 17 +H
        //        718.481, // Sn2 18 -H
        //        719.489, // Sn1 16 Sn2 18
        //        720.496, // Sn2 18 +H
        //        732.496, // Sn2 19 -H
        //        733.504, // Sn1 17 Sn2 19
        //        734.512, // Sn2 19 +H
        //        748.528, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct
        //    };

        //    scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
        //    foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
        //        Assert.AreEqual(expect, actual, 0.01d);
        //    }
        //}

        //[TestMethod()]
        //public void GenerateEtherPEOTest() {
        //    var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0));
        //    var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
        //    var lipid = new Lipid(LbmClass.EtherPE, 747.5199, new PositionLevelChains(alkyl, acyl));

        //    var generator = new EtherPESpectrumGenerator();
        //    var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

        //    var expects = new[]
        //    {
        //        142.027, // Header
        //        182.058, // Gly-C
        //        184.037, // Gly-O
        //        445.295, // Sn2 - O
        //        464.314, // Sn2 -C=O
        //        470.267, // Sn1 -CH2
        //        483.275, // Sn1 -O
        //        490.293, // Sn2  1 -H
        //        491.300, // Sn2  1
        //        492.309, // Sn2  1 +H
        //        498.262, // Sn1 -C
        //        504.308, // Sn2  2 -H
        //        505.316, // Sn2  2
        //        506.324, // Sn2  2 +H
        //        513.285, // Sn1  1
        //        518.324, // Sn2  3 -H
        //        519.332, // Sn2  3
        //        520.340, // Sn2  3 +H
        //        527.300, // Sn1  2
        //        532.340, // Sn2  4 -H
        //        533.347, // Sn2  4
        //        534.356, // Sn2  4 +H
        //        541.316, // Sn1  3
        //        545.348, // Sn2  5 -H
        //        546.356, // Sn2  5
        //        547.363, // Sn2  5 +H
        //        555.332, // Sn1  4
        //        558.356, // Sn2  6 -H
        //        559.363, // Sn2  6
        //        560.371, // Sn2  6 +H
        //        569.347, // Sn1  5
        //        572.371, // Sn2  7 -H
        //        573.379, // Sn2  7
        //        574.386, // Sn2  7 +H
        //        583.363, // Sn1  6
        //        585.379, // Sn2  8 -H
        //        586.386, // Sn2  8
        //        587.395, // Sn2  8 +H
        //        597.379, // Sn1  7
        //        598.387, // Sn2  9 -H
        //        599.395, // Sn2  9
        //        600.403, // Sn2  9 +H
        //        607.508, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ PreCursor -141
        //        611.395, // Sn1  8
        //        612.403, // Sn2 10 -H
        //        613.410, // Sn2 10
        //        614.418, // Sn2 10 +H
        //        624.403, // Sn1  9
        //        625.410, // Sn2 11 -H
        //        626.418, // Sn2 12
        //        627.426, // Sn2 11 +H
        //        637.410, // Sn1 10
        //        638.418, // Sn2 12 -H
        //        639.426, // Sn2 12
        //        640.434, // Sn2 12 +H
        //        651.426, // Sn1 11
        //        652.434, // Sn2 13 -H
        //        653.442, // Sn2 13
        //        654.449, // Sn2 13 +H
        //        664.434, // Sn1 12
        //        665.442, // Sn2 14 -H
        //        666.449, // Sn2 14
        //        667.457, // Sn2 14 +H
        //        677.442, // Sn1 13
        //        678.449, // Sn2 15 -H
        //        679.457, // Sn2 15
        //        680.465, // Sn2 15 +H
        //        691.457, // Sn1 14
        //        692.465, // Sn2 16 -H
        //        693.473, // Sn2 16
        //        694.481, // Sn2 16 +H
        //        705.473, // Sn1 15
        //        706.481, // Sn2 17
        //        707.489, // Sn2 17 +H
        //        718.481, // Sn2 18 -H
        //        719.489, // Sn1 16 Sn2 18
        //        720.496, // Sn2 18 +H
        //        732.496, // Sn2 19 -H
        //        733.504, // Sn1 17 Sn2 19
        //        734.512, // Sn2 19 +H
        //        748.528, // Sn1 18 Sn2 20 MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ adduct
        //    };

        //    scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
        //    foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
        //        Assert.AreEqual(expect, actual, 0.01d);
        //    }
        //}
        #endregion
        [TestMethod()]
        public void GenerateEtherPEPTest2_H()//PE O-18:1(1)_18:1(9)
        {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0));
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 729.5672409, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                142.027, // Header
                182.058, // Gly-C
                184.037, // Gly-O
                294.3155, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ SN1 ether +C2H8NO3P-H3PO4
                //308.2788, // -Header -CH2(Sn1)
                339.28992, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ NL of C2H8NO4P+SN1
                365.287259, // [Precursor]2+
                392.29242, // MspGenerator\GlyceroLipidFragmentation.cs [M+H]+ Sn1ether+C2H8NO3P
                447.3107946 ,//Sn2 FA loss
                449.2900592 ,//-CH2
                463.3057093 ,//Sn1 Ether loss
                465.3213593 ,//Sn2 Acyl loss
                480.3084489 ,//Sn1 chain loss
                491.3084489 ,//Sn1-Δ1-H
                492.3084489 ,//Sn2-1-H
                493.3162739 ,//Sn1-Δ1,//Sn2-1
                494.324099  ,//Sn1-Δ1+H,//Sn2-1+H
                504.3084489 ,//Sn1-2-H
                505.3162739 ,//Sn1-2
                506.324099  ,//Sn2-2-H
                507.331924  ,//Sn1-2+H,//Sn2-2
                508.339749  ,//Sn2-2+H
                518.324099  ,//Sn1-3-H
                519.331924  ,//Sn1-3
                520.339749  ,//Sn1-3+H,//Sn2-3-H
                521.3475741 ,//Sn2-3
                522.3553991 ,//Sn2-3+H
                532.339749  ,//Sn1-4-H
                533.3475741 ,//Sn1-4
                534.3553991 ,//Sn1-4+H,//Sn2-4-H
                535.3632241 ,//Sn2-4
                536.3710492 ,//Sn2-4+H
                546.3553991 ,//Sn1-5-H
                547.3632241 ,//Sn1-5
                548.3710492 ,//Sn1-5+H,//Sn2-5-H
                549.3788742 ,//Sn2-5
                550.3866992 ,//Sn2-5+H
                560.3710492 ,//Sn1-6-H
                561.3788742 ,//Sn1-6
                562.3866992 ,//Sn1-6+H,//Sn2-6-H
                563.3945243 ,//Sn2-6
                564.4023493 ,//Sn2-6+H
                574.3866992 ,//Sn1-7-H
                575.3945243 ,//Sn1-7
                576.4023493 ,//Sn1-7+H,//Sn2-7-H
                577.4101743 ,//Sn2-7
                578.4179994 ,//Sn2-7+H
                588.4023493 ,//Sn1-8-H
                589.4101743 ,//Sn1-8
                589.555423  ,//Precursor -C2H8NO4P
                590.4179994 ,//Sn1-8+H,//Sn2-8-H
                591.4258244 ,//Sn2-8
                592.4336494 ,//Sn2-8+H
                602.4179994 ,//Sn1-9-H
                603.4258244 ,//Sn1-9,//Sn2-Δ9-H
                604.4336494 ,//Sn2-Δ9,//Sn1-9+H
                605.4414745 ,//Sn2-Δ9+H
                616.4336494 ,//Sn1-10-H,Sn2-10-H
                617.4414745 ,//Sn1-10,Sn2-10
                618.4492995 ,//Sn1-10+H,Sn2-10+H
                630.4492995 ,//Sn1-11-H,Sn2-11-H
                631.4571245 ,//Sn1-11,Sn2-11
                632.4649496 ,//Sn1-11+H,Sn2-11+H
                644.4649496 ,//Sn1-12-H,Sn2-12-H
                645.4727746 ,//Sn1-12,Sn2-12
                646.4805996 ,//Sn1-12+H,Sn2-12+H
                658.4805996 ,//Sn1-13-H,Sn2-13-H
                659.4884246 ,//Sn1-13,Sn2-13
                660.4962497 ,//Sn1-13+H,Sn2-13+H
                672.4962497 ,//Sn1-14-H,Sn2-14-H
                673.5040747 ,//Sn1-14,Sn2-14
                674.5118997 ,//Sn1-14+H,Sn2-14+H
                686.5118997 ,//Sn1-15-H,Sn2-15-H
                687.5197248 ,//Sn1-15,Sn2-15
                688.5275498 ,//Sn1-15+H,Sn2-15+H
                700.5275498 ,//Sn1-16-H,Sn2-16-H
                701.5353748 ,//Sn1-16,Sn2-16
                702.5431999 ,//Sn1-16+H,Sn2-16+H
                714.5431999 ,//Sn1-17-H,Sn2-17-H
                715.5510249 ,//Sn1-17,Sn2-17
                716.5588499 ,//Sn1-17+H,Sn2-17+H
                730.57452, // precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateEtherPEPTest2_Na()//PE O-18:1(1)_18:1(9)
        {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1), new Oxidized(0));
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 729.5672409, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                164.0089442 ,//Header
                204.0399442 ,//Gly-C
                206.0189442 ,//Gly-O
                //330.2540034 ,//- Header -CH2(Sn1)
                376.2782308, // [Precursor]2+
                469.2927389 ,//Sn2 FA loss
                471.2720034 ,//-CH2
                485.2876535 ,//Sn1 Ether loss
                487.3033036 ,//Sn2 Acyl loss
                502.2903931 ,//Sn1 chain loss
                513.2903931 ,//Sn1-Δ1-H
                514.2903931 ,//Sn2-1-H
                515.2982182 ,//Sn1-Δ1,//Sn2-1
                516.3060432 ,//Sn1-Δ1+H,//Sn2-1+H
                526.2903931 ,//Sn1-2-H
                527.2982182 ,//Sn1-2
                528.3060432 ,//Sn1-2,//Sn2-2-H
                529.3138682 ,//Sn1-2+H,//Sn2-2
                530.3216933 ,//Sn2-2+H
                540.3060432 ,//Sn1-3-H
                541.3138682 ,//Sn1-3
                542.3216933 ,//Sn1-3+H,//Sn2-3-H
                543.3295183 ,//Sn2-3
                544.3373433 ,//Sn2-3+H
                554.3216933 ,//Sn1-4-H
                555.3295183 ,//Sn1-4
                556.3373433 ,//Sn1-4+H,//Sn2-4-H
                557.3451684 ,//Sn2-4
                558.3529934 ,//Sn2-4+H
                568.3373433 ,//Sn1-5-H
                569.3451684 ,//Sn1-5
                570.3529934 ,//Sn1-5+H,//Sn2-5-H
                571.3608184 ,//Sn2-5
                572.3686435 ,//Sn2-5+H
                582.3529934 ,//Sn1-6-H
                583.3608184 ,//Sn1-6
                584.3686435 ,//Sn1-6+H,//Sn2-6-H
                585.3764685 ,//Sn2-6
                586.3842935 ,//Sn2-6+H
                596.3686435 ,//Sn1-7-H
                597.3764685 ,//Sn1-7
                598.3842935 ,//Sn1-7+H,//Sn2-7-H
                599.3921186 ,//Sn2-7
                600.3999436 ,//Sn2-7+H
                610.3842935 ,//Sn1-8-H
                611.3921186 ,//Sn1-8
                612.3999436 ,//Sn1-8+H,//Sn2-8-H
                613.4077686 ,//Sn2-8
                614.4155937 ,//Sn2-8+H
                624.3999436 ,//Sn1-9-H
                625.4077686 ,//Sn1-9,//Sn2-Δ9-H
                626.4155937 ,//Sn2-Δ9,//Sn1-9+H
                627.4234187 ,//Sn2-Δ9+H
                638.4155937 ,//Sn1-10-H,Sn2-10-H
                639.4234187 ,//Sn1-10,Sn2-10
                640.4312437 ,//Sn1-10+H,Sn2-10+H
                652.4312437 ,//Sn1-11-H,Sn2-11-H
                653.4390688 ,//Sn1-11,Sn2-11
                654.4468938 ,//Sn1-11+H,Sn2-11+H
                666.4468938 ,//Sn1-12-H,Sn2-12-H
                667.4547188 ,//Sn1-12,Sn2-12
                668.4625439 ,//Sn1-12+H,Sn2-12+H
                680.4625439 ,//Sn1-13-H,Sn2-13-H
                681.4703689 ,//Sn1-13,Sn2-13
                682.4781939 ,//Sn1-13+H,Sn2-13+H
                //692.5120713 ,//Precursor -C2H6NO
                694.4781939 ,//Sn1-14-H,Sn2-14-H
                695.4860189 ,//Sn1-14,Sn2-14
                696.493844  ,//Sn1-14+H,Sn2-14+H
                708.493844  ,//Sn1-15-H,Sn2-15-H
                709.501669  ,//Sn1-15,Sn2-15
                709.514811  ,//Precursor -C2H5N
                710.509494  ,//Sn1-15+H,Sn2-15+H
                722.509494  ,//Sn1-16-H,Sn2-16-H
                723.5173191 ,//Sn1-16,Sn2-16
                724.5251441 ,//Sn1-16+H,Sn2-16+H
                736.5251441 ,//Sn1-17-H,Sn2-17-H
                737.5329691 ,//Sn1-17,Sn2-17
                738.5407942 ,//Sn1-17+H,Sn2-17+H
                752.5564642 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }


        [TestMethod()]
        public void GenerateEtherPEOTest2_H()//PE O-16:0_18:1(9)
        {
            var alkyl = new AlkylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 703.5515909, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                142.027, // Header
                182.058, // Gly-C
                184.037, // Gly-O
                //280.2771946389 ,// -Header and FA loss
                //299.2955844 ,// -Header and Acyl loss
                //308.2721093 ,// -Header and -CH2
                //321.2799343 ,// -Header and Ether loss
                //339.290499  ,// -Header and Sn1 Chain loss
                352.2794337, // [Precursor]2+
                421.2951946 ,//FA loss
                440.3135844 ,//Acyl loss
                449.2901093 ,//-CH2
                463.3057593 ,//Ether loss
                466.2928489 ,//Sn2-1-H
                467.3006739 ,//Sn2-1
                468.308499  ,//Sn2-1+H
                480.308499  ,//Sn1 Chain loss ,//Sn2-2-H
                481.316324  ,//Sn2-2
                482.324149  ,//Sn2-2+H
                492.308499  ,//Sn1-1-H
                493.316324  ,//Sn1-1
                494.324149  ,//Sn1-1+H,//Sn2-3-H
                495.3319741 ,//Sn2-3
                496.3397991 ,//Sn2-3+H
                506.324149  ,//Sn1-2-H
                507.3319741 ,//Sn1-2
                508.3397991 ,//Sn1-2+H,//Sn2-4-H
                509.3476241 ,//Sn2-4
                510.3554492 ,//Sn2-4+H
                520.3397991 ,//Sn1-3-H
                521.3476241 ,//Sn1-3
                522.3554492 ,//Sn1-3+H,//Sn2-5-H
                523.3632742 ,//Sn2-5
                524.3710992 ,//Sn2-5+H
                534.3554492 ,//Sn1-4-H
                535.3632742 ,//Sn1-4
                536.3710992 ,//Sn1-4+H,//Sn2-6-H
                537.3789243 ,//Sn2-6
                538.3867493 ,//Sn2-6+H
                548.3710992 ,//Sn1-5-H
                549.3789243 ,//Sn1-5
                550.3867493 ,//Sn1-5+H,//Sn2-7-H
                551.3945743 ,//Sn2-7
                552.4023994 ,//Sn2-7+H
                562.3867493 ,//Sn1-6-H
                563.3945743 ,//Sn1-6
                563.539773   ,//Precursor -C2H8NO4P
                564.4023994 ,//Sn1-6+H,//Sn2-8-H
                565.4102244 ,//Sn2-8
                566.4180494 ,//Sn2-8+H
                576.4023994 ,//Sn1-7-H
                577.4102244 ,//Sn1-7,//Sn2-Δ9-H
                578.4180494 ,//Sn2-Δ9,//Sn1-7+H
                579.4258745 ,//Sn2-Δ9+H
                590.4180494 ,//Sn1-8-H,//Sn2-10-H
                591.4258745 ,//Sn1-8,//Sn2-10
                592.4336995 ,//Sn1-8+H,//Sn2-10+H
                604.4336995 ,//Sn1-9-H,//Sn2-11-H
                605.4415245 ,//Sn1-9,//Sn2-11
                606.4493496 ,//Sn1-9+H,//Sn2-11+H
                618.4493496 ,//Sn1-10-H,//Sn2-12-H
                619.4571746 ,//Sn1-10,//Sn2-12
                620.4649996 ,//Sn1-10+H,//Sn2-12+H
                632.4649996 ,//Sn1-11-H,//Sn2-13-H
                633.4728246 ,//Sn1-11,//Sn2-13
                634.4806497 ,//Sn1-11+H,//Sn2-13+H
                646.4806497 ,//Sn1-12-H,//Sn2-14-H
                647.4884747 ,//Sn1-12,//Sn2-14
                648.4962997 ,//Sn1-12+H,//Sn2-14+H
                660.4962997 ,//Sn1-13-H,//Sn2-15-H
                661.5041248 ,//Sn1-13,//Sn2-15
                662.5119498 ,//Sn1-13+H,//Sn2-15+H
                674.5119498 ,//Sn1-14-H,//Sn2-16-H
                675.5197748 ,//Sn1-14,//Sn2-16
                676.5275999 ,//Sn1-14+H,//Sn2-16+H
                688.5275999 ,//Sn1-15-H,//Sn2-17-H
                689.5354249 ,//Sn1-15,//Sn2-17
                690.5432499 ,//Sn1-15+H,//Sn2-17+H
                704.5589    , // precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateEtherPEOTest2_Na()//PE O-16:0_18:1(9)
        {
            var alkyl = new AlkylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPE, 703.5515909, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPESpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                164.0089442 ,//Header
                204.0399442 ,//Gly-C
                206.0189442 ,//Gly-O
                //302.2591389 ,// -Header and FA loss
                //321.2775286 ,// -Header and Acyl loss
                //330.2540535 ,// -Header and -CH2
                //343.2618785 ,// -Header and Ether loss
                //361.2724432 ,// -Header and Sn1 Chain loss
                363.270405, // [Precursor]2+
                443.2771389 ,//FA loss
                462.2955286 ,//Acyl loss
                471.2720535 ,//-CH2
                485.2877036 ,//Ether loss
                488.2747931 ,//Sn2-1-H
                489.2826182 ,//Sn2-1
                490.2904432 ,//Sn2-1+H
                502.2904432 ,//Sn1 Chain loss ,//Sn2-2-H
                503.2982682 ,//Sn2-2
                504.3060933 ,//Sn2-2+H
                514.2904432 ,//Sn1-1-H
                515.2982682 ,//Sn1-1
                516.3060933 ,//Sn1-1+H,//Sn2-3-H
                517.3139183 ,//Sn2-3
                518.3217433 ,//Sn2-3+H
                528.3060933 ,//Sn1-2-H
                529.3139183 ,//Sn1-2
                530.3217433 ,//Sn1-2+H,//Sn2-4-H
                531.3295684 ,//Sn2-4
                532.3373934 ,//Sn2-4+H
                542.3217433 ,//Sn1-3-H
                543.3295684 ,//Sn1-3
                544.3373934 ,//Sn1-3+H,//Sn2-5-H
                545.3452184 ,//Sn2-5
                546.3530435 ,//Sn2-5+H
                556.3373934 ,//Sn1-4-H
                557.3452184 ,//Sn1-4
                558.3530435 ,//Sn1-4+H,//Sn2-6-H
                559.3608685 ,//Sn2-6
                560.3686935 ,//Sn2-6+H
                570.3530435 ,//Sn1-5-H
                571.3608685 ,//Sn1-5
                572.3686935 ,//Sn1-5+H,//Sn2-7-H
                573.3765186 ,//Sn2-7
                574.3843436 ,//Sn2-7+H
                584.3686935 ,//Sn1-6-H
                585.3765186 ,//Sn1-6
                586.3843436 ,//Sn1-6+H,//Sn2-8-H
                587.3921686 ,//Sn2-8
                588.3999937 ,//Sn2-8+H
                598.3843436 ,//Sn1-7-H
                599.3921686 ,//Sn1-7,//Sn2-Δ9-H
                600.3999937 ,//Sn2-Δ9,//Sn1-7+H
                601.4078187 ,//Sn2-Δ9+H
                612.3999937 ,//Sn1-8-H,//Sn2-10-H
                613.4078187 ,//Sn1-8,//Sn2-10
                614.4156437 ,//Sn1-8+H,//Sn2-10+H
                626.4156437 ,//Sn1-9-H,//Sn2-11-H
                627.4234688 ,//Sn1-9,//Sn2-11
                628.4312938 ,//Sn1-9+H,//Sn2-11+Hdb.Generate(lipid, adduct, reference);
                640.4312938 ,//Sn1-10-H,//Sn2-12-H
                641.4391188 ,//Sn1-10,//Sn2-12
                642.4469439 ,//Sn1-10+H,//Sn2-12+H
                654.4469439 ,//Sn1-11-H,//Sn2-13-H
                655.4547689 ,//Sn1-11,//Sn2-13
                656.4625939 ,//Sn1-11+H,//Sn2-13+H
                666.49587   ,//Precursor -C2H6NO
                668.4625939 ,//Sn1-12-H,//Sn2-14-H
                669.4704189 ,//Sn1-12,//Sn2-14
                670.478244  ,//Sn1-12+H,//Sn2-14+H
                682.478244  ,//Sn1-13-H,//Sn2-15-H
                683.486069  ,//Sn1-13,//Sn2-15
                683.49916   ,//Precursor -C2H5N
                684.493894  ,//Sn1-13+H,//Sn2-15+H
                696.493894  ,//Sn1-14-H,//Sn2-16-H
                697.5017191 ,//Sn1-14,//Sn2-16
                698.5095441 ,//Sn1-14+H,//Sn2-16+H
                710.5095441 ,//Sn1-15-H,//Sn2-17-H
                711.5173691 ,//Sn1-15,//Sn2-17
                712.5251942 ,//Sn1-15+H,//Sn2-17+H
                726.5408442 , // precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

    }
}