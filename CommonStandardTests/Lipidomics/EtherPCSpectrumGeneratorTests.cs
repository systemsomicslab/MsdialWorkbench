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
    public class EtherPCSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateEtherPCPTest() {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPC, 789.5672409, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                183.065  + MassDiffDictionary.ProtonMass , // Header
                224.105 , // Gly-C
                226.083 , // Gly-O
                487.3421121 + MassDiffDictionary.ProtonMass,// -Sn2 -O
                //506.3605019 ,// -Sn2 acyl
                511.3057266 ,// -Sn1 -CH2
                525.3213767 ,// -Sn1 Ether
                532.3397664 ,//Sn2-1-H
                533.3475914 ,//Sn2-1
                534.3554165 ,//Sn2-1+H
                //541.3162913 ,//Sn1-0
                546.3554165 ,//Sn2-2-H
                547.3632415 ,//Sn2-2
                548.3710665 ,//Sn2-2+H
                553.3162913 ,//Sn1-3-H
                554.3241163 ,//Sn1-Δ1
                555.3319414 ,//Sn1-Δ1+H
                560.3710665 ,//Sn2-3-H
                561.3788916 ,//Sn2-3
                562.3867166 ,//Sn2-3+H
                566.3241163 ,//Sn1-4-H
                567.3319414 ,//Sn1-2
                568.3397664 ,//Sn1-2+H
                574.3867166 ,//Sn2-4-H
                575.3945416 ,//Sn2-4
                576.4023667 ,//Sn2-4+H
                580.3397664 ,//Sn1-Δ5-H
                581.3475914 ,//Sn1-3
                582.3554165 ,//Sn1-3+H
                587.3945416 ,//Sn2-Δ5-H
                588.4023667 ,//Sn2-Δ5
                589.4101917 ,//Sn2-Δ5+H
                594.3554165 ,//Sn1-6-H
                595.3632415 ,//Sn1-4
                596.3710665 ,//Sn1-4+H
                600.4023667 ,//Sn2-6-H
                601.4101917 ,//Sn2-6
                602.4180167 ,//Sn2-6+H
                608.3710665 ,//Sn1-7-H
                609.3788916 ,//Sn1-5
                610.3867166 ,//Sn1-5+H
                614.4180167 ,//Sn2-7-H
                615.4258418 ,//Sn2-7
                616.4336668 ,//Sn2-7+H
                622.3867166 ,//Sn1-Δ8-H
                623.3945416 ,//Sn1-6
                624.4023667 ,//Sn1-6+H
                627.4258418 ,//Sn2-Δ8-H
                628.4336668 ,//Sn2-Δ8
                629.4414918 ,//Sn2-Δ8+H
                636.4023667 ,//Sn1-9-H
                637.4101917 ,//Sn1-7
                638.4180167 ,//Sn1-7+H
                640.4336668 ,//Sn2-9-H
                641.4414918 ,//Sn2-9
                642.4493169 ,//Sn2-9+H
                650.4180167 ,//Sn1-10-H
                651.4258418 ,//Sn1-8
                652.4336668 ,//Sn1-8+H
                654.4493169 ,//Sn2-10-H
                655.4571419 ,//Sn2-10
                656.4649669 ,//Sn2-10+H
                664.4336668 ,//Sn1-Δ11-H
                665.4414918 ,//Sn1-9
                666.4493169 ,//Sn1-9+H
                667.4571419 ,//Sn2-Δ11-H
                668.4649669 ,//Sn2-Δ11
                669.472792  ,//Sn2-Δ11+H
                678.4493169 ,//Sn1-12-H
                679.4571419 ,//Sn1-10
                680.4649669 ,//Sn1-10+H,Sn2-12-H
                681.472792  ,//Sn2-12
                682.480617  ,//Sn2-12+H
                692.4649669 ,//Sn1-13-H
                693.472792  ,//Sn1-11
                694.480617  ,//Sn1-11+H,Sn2-13-H
                695.488442  ,//Sn2-13
                696.4962671 ,//Sn2-13+H
                705.472792  ,//Sn1-Δ14-H
                706.480617  ,//Sn1-Δ12
                707.488442  ,//Sn1-Δ12+H,Sn2-Δ14-H
                708.4962671 ,//Sn2-Δ14
                709.5040921 ,//Sn2-Δ14+H
                718.480617  ,//Sn1-15-H
                719.488442  ,//Sn1-13
                720.4962671 ,//Sn1-13+H,Sn2-15-H
                721.5040921 ,//Sn2-15
                722.5119171 ,//Sn2-15+H
                732.4962671 ,//Sn1-16-H
                733.5040921 ,//Sn1-14
                734.5119171 ,//Sn1-14+H,Sn2-16-H
                735.5197421 ,//Sn2-16
                736.5275672 ,//Sn2-16+H
                746.5119171 ,//Sn1-Δ17-H
                747.5197421 ,//Sn1-15,Sn2-Δ17-H
                748.5275672 ,//Sn1-15+H,Sn2-Δ17
                749.5353922 ,//Sn2-Δ17+H
                760.5275672 ,//Sn1-18-H,Sn2-18-H
                761.5353922 ,//Sn1-16,Sn2-18
                762.5432172 ,//Sn1-16+H,Sn2-18+H
                774.5432172 ,//Sn1-19-H,Sn2-19-H
                775.5510423 ,//Sn1-17,Sn2-19
                776.5588673 ,//Sn1-17+H,Sn2-19+H
                790.5745174 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateEtherPCOTest() {
            var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition( 9, 12), new Oxidized(0));
            var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.EtherPC, 789.5672409, new PositionLevelChains(alkyl, acyl));

            var generator = new EtherPCSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                183.065  + MassDiffDictionary.ProtonMass , // Header
                224.105 , // Gly-C
                226.083 , // Gly-O
                487.3421121 ,// -Sn2 -O
                506.3605019 ,// -Sn2 acyl
                511.3057266 ,// -Sn1 -CH2
                525.3213767 ,// -Sn1 Ether
                532.3397664 ,//Sn2-1-H
                533.3475914 ,//Sn2-1
                534.3554165 ,//Sn2-1+H
                541.3162913 ,//Sn1-0
                546.3554165 ,//Sn2-2-H
                547.3632415 ,//Sn2-2
                548.3710665 ,//Sn2-2+H
                554.3241163 ,//Sn1-3-H
                555.3319414 ,//Sn1-1
                556.3397664 ,//Sn1-1+H
                560.3710665 ,//Sn2-3-H
                561.3788916 ,//Sn2-3
                562.3867166 ,//Sn2-3+H
                568.3397664 ,//Sn1-4-H
                569.3475914 ,//Sn1-2
                570.3554165 ,//Sn1-2+H
                574.3867166 ,//Sn2-4-H
                575.3945416 ,//Sn2-4
                576.4023667 ,//Sn2-4+H
                582.3554165 ,//Sn1-Δ5-H
                583.3632415 ,//Sn1-3
                584.3710665 ,//Sn1-3+H
                587.3945416 ,//Sn2-Δ5-H
                588.4023667 ,//Sn2-Δ5
                589.4101917 ,//Sn2-Δ5+H
                596.3710665 ,//Sn1-6-H
                597.3788916 ,//Sn1-4
                598.3867166 ,//Sn1-4+H
                600.4023667 ,//Sn2-6-H
                601.4101917 ,//Sn2-6
                602.4180167 ,//Sn2-6+H
                610.3867166 ,//Sn1-7-H
                611.3945416 ,//Sn1-5
                612.4023667 ,//Sn1-5+H
                614.4180167 ,//Sn2-7-H
                615.4258418 ,//Sn2-7
                616.4336668 ,//Sn2-7+H
                624.4023667 ,//Sn1-Δ8-H
                625.4101917 ,//Sn1-6
                626.4180167 ,//Sn1-6+H
                627.4258418 ,//Sn2-Δ8-H
                628.4336668 ,//Sn2-Δ8
                629.4414918 ,//Sn2-Δ8+H
                638.4180167 ,//Sn1-9-H
                639.4258418 ,//Sn1-7
                640.4336668 ,//Sn1-7+H,Sn2-9-H
                641.4414918 ,//Sn2-9
                642.4493169 ,//Sn2-9+H
                652.4336668 ,//Sn1-10-H
                653.4414918 ,//Sn1-8
                654.4493169 ,//Sn1-8+H,Sn2-10-H
                655.4571419 ,//Sn2-10
                656.4649669 ,//Sn2-10+H
                665.4414918 ,//Sn1-Δ11-H
                666.4493169 ,//Sn1-Δ9
                667.4571419 ,//Sn1-Δ9+H,Sn2-Δ11-H
                668.4649669 ,//Sn2-Δ11
                669.472792  ,//Sn2-Δ11+H
                678.4493169 ,//Sn1-12-H
                679.4571419 ,//Sn1-10
                680.4649669 ,//Sn1-10+H,Sn2-12-H
                681.472792  ,//Sn2-12
                682.480617  ,//Sn2-12+H
                692.4649669 ,//Sn1-13-H
                693.472792  ,//Sn1-11
                694.480617  ,//Sn1-11+H,Sn2-13-H
                695.488442  ,//Sn2-13
                696.4962671 ,//Sn2-13+H
                705.472792  ,//Sn1-Δ14-H
                706.480617  ,//Sn1-Δ12
                707.488442  ,//Sn1-Δ12+H,Sn2-Δ14-H
                708.4962671 ,//Sn2-Δ14
                709.5040921 ,//Sn2-Δ14+H
                718.480617  ,//Sn1-15-H
                719.488442  ,//Sn1-13
                720.4962671 ,//Sn1-13+H,Sn2-15-H
                721.5040921 ,//Sn2-15
                722.5119171 ,//Sn2-15+H
                732.4962671 ,//Sn1-16-H
                733.5040921 ,//Sn1-14
                734.5119171 ,//Sn1-14+H,Sn2-16-H
                735.5197421 ,//Sn2-16
                736.5275672 ,//Sn2-16+H
                746.5119171 ,//Sn1-Δ17-H
                747.5197421 ,//Sn2-Δ17-H,Sn1-15
                748.5275672 ,//Sn1-15+H,Sn2-Δ17
                749.5353922 ,//Sn2-Δ17+H
                760.5275672 ,//Sn1-18-H,Sn2-18-H
                761.5353922 ,//Sn2-18,Sn1-16
                762.5432172 ,//Sn1-16+H,Sn2-18+H
                774.5432172 ,//Sn1-19-H,Sn2-19-H
                775.5510423 ,//Sn2-19,Sn1-17
                776.5588673 ,//Sn1-17+H,Sn2-19+H
                790.5745174 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass))) {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}