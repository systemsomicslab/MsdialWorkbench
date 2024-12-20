using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class DGSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //DG 18:1(11)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(11), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.DG, 666.5223253, new PositionLevelChains(acyl1, acyl2));

            var generator = new DGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                265.25259    ,// 18:1(11) acyl+
                311.23694    ,// 22:6(4,7,10,13,16,19) acyl+
                339.2893715    ,//Sn2 FA loss
                357.2999362 ,//Sn2 Acyl loss
                365.2686361 ,//Sn2-1-H
                366.2764611 ,//Sn2-1
                367.2842862 ,//Sn2-1+H
                371.2580714 ,// -CH2
                379.2842862 ,//Sn2-2-H
                380.2921112 ,//Sn2-2
                381.2999362 ,//Sn2-2+H
                385.2737215 ,//Sn1 FA loss
                393.2999362 ,//Sn2-3-H
                394.3077612 ,//Sn2-3
                395.3155863 ,//Sn2-3+H
                403.2842862 ,//Sn1 Acyl loss
                406.3077612 ,//Sn2-Δ4-H
                407.3155863 ,//Sn2-Δ4
                408.3234113 ,//Sn2-Δ4+H
                411.252986  ,//Sn1-5-H
                412.2608111 ,//Sn1-1
                413.2686361 ,//Sn1-1+H
                419.3155863 ,//Sn2-5-H
                420.3234113 ,//Sn2-5
                421.3312363 ,//Sn2-5+H
                425.2686361 ,//Sn1-6-H
                426.2764611 ,//Sn1-2
                427.2842862 ,//Sn1-2+H
                433.3312363 ,//Sn2-6-H
                434.3390614 ,//Sn2-6
                435.3468864 ,//Sn2-6+H
                439.2842862 ,//Sn1-Δ7-H
                440.2921112 ,//Sn1-3
                441.2999362 ,//Sn1-3+H
                446.3390614 ,//Sn2-Δ7-H
                447.3468864 ,//Sn2-Δ7
                448.3547114 ,//Sn2-Δ7+H
                453.2999362 ,//Sn1-8-H
                454.3077612 ,//Sn1-4
                455.3155863 ,//Sn1-4+H
                459.3468864 ,//Sn2-8-H
                460.3547114 ,//Sn2-8
                461.3625365 ,//Sn2-8+H
                467.3155863 ,//Sn1-9-H
                468.3234113 ,//Sn1-5
                469.3312363 ,//Sn1-5+H
                473.3625365 ,//Sn2-9-H
                474.3703615 ,//Sn2-9
                475.3781865 ,//Sn2-9+H
                481.3312363 ,//Sn1-Δ10-H
                482.3390614 ,//Sn1-6
                483.3468864 ,//Sn1-6+H
                486.3703615 ,//Sn2-Δ10-H
                487.3781865 ,//Sn2-Δ10
                488.3860116 ,//Sn2-Δ10+H
                495.3468864 ,//Sn1-11-H
                496.3547114 ,//Sn1-7
                497.3625365 ,//Sn1-7+H
                499.3781865 ,//Sn2-11-H
                500.3860116 ,//Sn2-11
                501.3938366 ,//Sn2-11+H
                509.3625365 ,//Sn1-12-H
                510.3703615 ,//Sn1-8
                511.3781865 ,//Sn1-8+H
                513.3938366 ,//Sn2-12-H
                514.4016616 ,//Sn2-12
                515.4094867 ,//Sn2-12+H
                523.3781865 ,//Sn1-Δ13-H
                524.3860116 ,//Sn1-9
                525.3938366 ,//Sn1-9+H
                526.4016616 ,//Sn2-Δ13-H
                527.4094867 ,//Sn2-Δ13
                528.4173117 ,//Sn2-Δ13+H
                537.3938366 ,//Sn1-14-H
                538.4016616 ,//Sn1-10
                539.4094867 ,//Sn1-10+H,//Sn2-14-H
                540.4173117 ,//Sn2-14
                541.4251367 ,//Sn2-14+H
                550.4016616 ,//Sn1-15-H
                551.4094867 ,//Sn1-Δ11
                552.4173117 ,//Sn1-Δ11+H
                553.4251367 ,//Sn2-15-H
                554.4329618 ,//Sn2-15
                555.4407868 ,//Sn2-15+H
                563.4094867 ,//Sn1-Δ16-H
                564.4173117 ,//Sn1-12
                565.4251367 ,//Sn1-12+H
                566.4329618 ,//Sn2-Δ16-H
                567.4407868 ,//Sn2-Δ16
                568.4486118 ,//Sn2-Δ16+H
                577.4251367 ,//Sn1-17-H
                578.4329618 ,//Sn1-13
                579.4407868 ,//Sn1-13+H,//Sn2-17-H
                580.4486118 ,//Sn2-17
                581.4564369 ,//Sn2-17+H
                591.4407868 ,//Sn1-18-H
                592.4486118 ,//Sn1-14
                593.4564369 ,//Sn1-14+H,//Sn2-18-H
                594.4642619 ,//Sn2-18
                595.4720869 ,//Sn2-18+H
                605.4564369 ,//Sn1-Δ19-H
                606.4642619 ,//Sn1-15,//Sn2-Δ19-H
                607.4720869 ,//Sn2-Δ19,//Sn1-15+H
                608.479912  ,//Sn2-Δ19+H
                619.4720869 ,//Sn1-20-H,//Sn2-20-H
                620.479912  ,//Sn2-20,//Sn1-16
                621.487737  ,//Sn1-16+H,//Sn2-20+H
                633.487737  ,//Sn1-21-H,//Sn2-21-H
                634.495562  ,//Sn2-21,//Sn1-17
                635.503387  ,//Sn1-17+H,//Sn2-21+H
                649.5190371 ,//Precursor - H2O
                667.5296018 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_NH4()
        {
            //DG 18:1(11)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(11), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.DG, 666.5223253, new PositionLevelChains(acyl1, acyl2));

            var generator = new DGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+NH4]+"));

            var expects = new[]
            {
                265.25259    ,// 18:1(11) acyl+
                311.23694    ,// 22:6(4,7,10,13,16,19) acyl+
                339.2893715    ,//Sn2 FA loss
                357.2999362 ,//Sn2 Acyl loss
                365.2686361 ,//Sn2-1-H
                366.2764611 ,//Sn2-1
                367.2842862 ,//Sn2-1+H
                371.2580714 ,// -CH2
                379.2842862 ,//Sn2-2-H
                380.2921112 ,//Sn2-2
                381.2999362 ,//Sn2-2+H
                385.2737215 ,//Sn1 FA loss
                393.2999362 ,//Sn2-3-H
                394.3077612 ,//Sn2-3
                395.3155863 ,//Sn2-3+H
                403.2842862 ,//Sn1 Acyl loss
                406.3077612 ,//Sn2-Δ4-H
                407.3155863 ,//Sn2-Δ4
                408.3234113 ,//Sn2-Δ4+H
                411.252986  ,//Sn1-5-H
                412.2608111 ,//Sn1-1
                413.2686361 ,//Sn1-1+H
                419.3155863 ,//Sn2-5-H
                420.3234113 ,//Sn2-5
                421.3312363 ,//Sn2-5+H
                425.2686361 ,//Sn1-6-H
                426.2764611 ,//Sn1-2
                427.2842862 ,//Sn1-2+H
                433.3312363 ,//Sn2-6-H
                434.3390614 ,//Sn2-6
                435.3468864 ,//Sn2-6+H
                439.2842862 ,//Sn1-Δ7-H
                440.2921112 ,//Sn1-3
                441.2999362 ,//Sn1-3+H
                446.3390614 ,//Sn2-Δ7-H
                447.3468864 ,//Sn2-Δ7
                448.3547114 ,//Sn2-Δ7+H
                453.2999362 ,//Sn1-8-H
                454.3077612 ,//Sn1-4
                455.3155863 ,//Sn1-4+H
                459.3468864 ,//Sn2-8-H
                460.3547114 ,//Sn2-8
                461.3625365 ,//Sn2-8+H
                467.3155863 ,//Sn1-9-H
                468.3234113 ,//Sn1-5
                469.3312363 ,//Sn1-5+H
                473.3625365 ,//Sn2-9-H
                474.3703615 ,//Sn2-9
                475.3781865 ,//Sn2-9+H
                481.3312363 ,//Sn1-Δ10-H
                482.3390614 ,//Sn1-6
                483.3468864 ,//Sn1-6+H
                486.3703615 ,//Sn2-Δ10-H
                487.3781865 ,//Sn2-Δ10
                488.3860116 ,//Sn2-Δ10+H
                495.3468864 ,//Sn1-11-H
                496.3547114 ,//Sn1-7
                497.3625365 ,//Sn1-7+H
                499.3781865 ,//Sn2-11-H
                500.3860116 ,//Sn2-11
                501.3938366 ,//Sn2-11+H
                509.3625365 ,//Sn1-12-H
                510.3703615 ,//Sn1-8
                511.3781865 ,//Sn1-8+H
                513.3938366 ,//Sn2-12-H
                514.4016616 ,//Sn2-12
                515.4094867 ,//Sn2-12+H
                523.3781865 ,//Sn1-Δ13-H
                524.3860116 ,//Sn1-9
                525.3938366 ,//Sn1-9+H
                526.4016616 ,//Sn2-Δ13-H
                527.4094867 ,//Sn2-Δ13
                528.4173117 ,//Sn2-Δ13+H
                537.3938366 ,//Sn1-14-H
                538.4016616 ,//Sn1-10
                539.4094867 ,//Sn1-10+H,//Sn2-14-H
                540.4173117 ,//Sn2-14
                541.4251367 ,//Sn2-14+H
                550.4016616 ,//Sn1-15-H
                551.4094867 ,//Sn1-Δ11
                552.4173117 ,//Sn1-Δ11+H
                553.4251367 ,//Sn2-15-H
                554.4329618 ,//Sn2-15
                555.4407868 ,//Sn2-15+H
                563.4094867 ,//Sn1-Δ16-H
                564.4173117 ,//Sn1-12
                565.4251367 ,//Sn1-12+H
                566.4329618 ,//Sn2-Δ16-H
                567.4407868 ,//Sn2-Δ16
                568.4486118 ,//Sn2-Δ16+H
                577.4251367 ,//Sn1-17-H
                578.4329618 ,//Sn1-13
                579.4407868 ,//Sn1-13+H,//Sn2-17-H
                580.4486118 ,//Sn2-17
                581.4564369 ,//Sn2-17+H
                591.4407868 ,//Sn1-18-H
                592.4486118 ,//Sn1-14
                593.4564369 ,//Sn1-14+H,//Sn2-18-H
                594.4642619 ,//Sn2-18
                595.4720869 ,//Sn2-18+H
                605.4564369 ,//Sn1-Δ19-H
                606.4642619 ,//Sn1-15,//Sn2-Δ19-H
                607.4720869 ,//Sn2-Δ19,//Sn1-15+H
                608.479912  ,//Sn2-Δ19+H
                619.4720869 ,//Sn1-20-H,//Sn2-20-H
                620.479912  ,//Sn2-20,//Sn1-16
                621.487737  ,//Sn1-16+H,//Sn2-20+H
                633.487737  ,//Sn1-21-H,//Sn2-21-H
                634.495562  ,//Sn2-21,//Sn1-17
                635.503387  ,//Sn1-17+H,//Sn2-21+H
                649.5190371 ,//[M+H]+ - H2O
                666.5455862 ,//Precursor - H2O
                667.5296018 ,//[M+H]+
                684.5561509 ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //DG 18:1(11)_22:6(4,7,10,13,16,19)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(11), new Oxidized(0));
            var acyl2 = new AcylChain(22, DoubleBond.CreateFromPosition(4, 7, 10, 13, 16, 19), new Oxidized(0));
            var lipid = new Lipid(LbmClass.DG, 666.5223253, new PositionLevelChains(acyl1, acyl2));

            var generator = new DGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                305.24510103050005,//18:1(11) + O
                351.22945096636005, // 22:6(4, 7, 10, 13, 16, 19) + O
                362.2791408 ,//Sn2 FA loss
                377.2662304 ,//Sn2 Acyl loss
                393.2400156 ,// -CH2
                405.261145  ,//Sn2-1-H
                406.26897   ,//Sn2-1
                407.2767951 ,//Sn2-1+H
                408.2634907 ,//Sn1 FA loss
                419.2767951 ,//Sn2-2-H
                420.2846201 ,//Sn2-2
                421.2924451 ,//Sn2-2+H
                423.2505803 ,//Sn1 Acyl loss
                433.2924451 ,//Sn2-3-H
                434.3002702 ,//Sn2-3
                435.3080952 ,//Sn2-3+H
                446.3002702 ,//Sn2-Δ4-H
                447.3080952 ,//Sn2-Δ4
                448.3159202 ,//Sn2-Δ4+H
                451.2454949 ,//Sn1-5-H
                452.25332   ,//Sn1-1
                453.261145  ,//Sn1-1+H
                459.3080952 ,//Sn2-5-H
                460.3159202 ,//Sn2-5
                461.3237453 ,//Sn2-5+H
                465.261145  ,//Sn1-6-H
                466.26897   ,//Sn1-2
                467.2767951 ,//Sn1-2+H
                473.3237453 ,//Sn2-6-H
                474.3315703 ,//Sn2-6
                475.3393953 ,//Sn2-6+H
                479.2767951 ,//Sn1-Δ7-H
                480.2846201 ,//Sn1-3
                481.2924451 ,//Sn1-3+H
                486.3315703 ,//Sn2-Δ7-H
                487.3393953 ,//Sn2-Δ7
                488.3472204 ,//Sn2-Δ7+H
                493.2924451 ,//Sn1-8-H
                494.3002702 ,//Sn1-4
                495.3080952 ,//Sn1-4+H
                499.3393953 ,//Sn2-8-H
                500.3472204 ,//Sn2-8
                501.3550454 ,//Sn2-8+H
                507.3080952 ,//Sn1-9-H
                508.3159202 ,//Sn1-5
                509.3237453 ,//Sn1-5+H
                513.3550454 ,//Sn2-9-H
                514.3628704 ,//Sn2-9
                515.3706955 ,//Sn2-9+H
                521.3237453 ,//Sn1-Δ10-H
                522.3315703 ,//Sn1-6
                523.3393953 ,//Sn1-6+H
                526.3628704 ,//Sn2-Δ10-H
                527.3706955 ,//Sn2-Δ10
                528.3785205 ,//Sn2-Δ10+H
                535.3393953 ,//Sn1-11-H
                536.3472204 ,//Sn1-7
                537.3550454 ,//Sn1-7+H
                539.3706955 ,//Sn2-11-H
                540.3785205 ,//Sn2-11
                541.3863455 ,//Sn2-11+H
                549.3550454 ,//Sn1-12-H
                550.3628704 ,//Sn1-8
                551.3706955 ,//Sn1-8+H
                553.3863455 ,//Sn2-12-H
                554.3941706 ,//Sn2-12
                555.4019956 ,//Sn2-12+H
                563.3706955 ,//Sn1-Δ13-H
                564.3785205 ,//Sn1-9
                565.3863455 ,//Sn1-9+H
                566.3941706 ,//Sn2-Δ13-H
                567.4019956 ,//Sn2-Δ13
                568.4098206 ,//Sn2-Δ13+H
                577.3863455 ,//Sn1-14-H
                578.3941706 ,//Sn1-10
                579.4019956 ,//Sn1-10+H,//Sn2-14-H
                580.4098206 ,//Sn2-14
                581.4176456 ,//Sn2-14+H
                590.3941706 ,//Sn1-15-H
                591.4019956 ,//Sn1-Δ11
                592.4098206 ,//Sn1-Δ11+H
                593.4176456 ,//Sn2-15-H
                594.4254707 ,//Sn2-15
                595.4332957 ,//Sn2-15+H
                603.4019956 ,//Sn1-Δ16-H
                604.4098206 ,//Sn1-12
                605.4176456 ,//Sn1-12+H
                606.4254707 ,//Sn2-Δ16-H
                607.4332957 ,//Sn2-Δ16
                608.4411207 ,//Sn2-Δ16+H
                617.4176456 ,//Sn1-17-H
                618.4254707 ,//Sn1-13
                619.4332957 ,//Sn1-13+H,//Sn2-17-H
                620.4411207 ,//Sn2-17
                621.4489458 ,//Sn2-17+H
                631.4332957 ,//Sn1-18-H
                632.4411207 ,//Sn1-14
                633.4489458 ,//Sn1-14+H,//Sn2-18-H
                634.4567708 ,//Sn2-18
                635.4645958 ,//Sn2-18+H
                645.4489458 ,//Sn1-Δ19-H
                646.4567708 ,//Sn1-15,//Sn2-Δ19-H
                647.4645958 ,//Sn2-Δ19,//Sn1-15+H
                648.4724209 ,//Sn2-Δ19+H
                659.4645958 ,//Sn1-20-H,//Sn2-20-H
                660.4724209 ,//Sn1-16,//Sn2-20
                661.4802459 ,//Sn1-16+H,//Sn2-20+H
                673.4802459 ,//Sn1-21-H,//Sn2-21-H
                674.4880709 ,//Sn1-17,//Sn2-21
                675.495896  ,//Sn1-17+H,//Sn2-21+H
                689.511546  ,//Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class TGSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //TG 16:0_18:1(9)_18:2(9,12)
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl3 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.TG, 856.7519909, new PositionLevelChains(acyl1, acyl2, acyl3));

            var generator = new TGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                239.2374906 ,//Sn1 Acyl
                263.2374906 ,//Sn3 Acyl
                265.2531407 ,//Sn2 Acyl
                295.2631568 ,//[Sn1 Acyl + C3H3O+H]+
                309.27935542571,  //Sn2 diagnostics
                313.2737215 ,//[Sn1 Acyl + C3H5O2+H]+
                319.2631568 ,//[Sn3 Acyl + C3H3O+H]+
                321.2788069 ,//[Sn2 Acyl + C3H3O+H]+
                337.2737215 ,//[Sn3 Acyl + C3H5O2+H]+
                339.2893715 ,//[Sn2 Acyl + C3H5O2+H]+
                428.879634, // [Precursor]2+
                575.503387  ,//Sn2 FA loss
                577.5190371 ,//Sn3 FA loss
                593.5139517 ,//Sn2 Acyl loss
                595.5296018 ,//Sn3 Acyl loss
                601.5190371 ,//Sn1 FA loss
                619.4932163 ,//Sn2-1-H
                619.5296018    ,//Sn1 Acyl loss
                620.5010413 ,//Sn2-1
                621.5088664 ,//Sn2-1+H,//Sn3-1-H
                622.5166914 ,//Sn3-1
                623.5245164 ,//Sn3-1+H
                633.5088664 ,//Sn2-2-H
                634.5166914 ,//Sn2-2
                635.5245164 ,//Sn2-2+H,//Sn3-2-H
                636.5323415 ,//Sn3-2
                637.5401665 ,//Sn3-2+H
                645.5088664 ,//Sn1-1-H
                646.5166914 ,//Sn1-1
                647.5245164 ,//Sn1-1+H,//Sn2-3-H
                648.5323415 ,//Sn2-3
                649.5401665 ,//Sn2-3+H,//Sn3-3-H
                650.5479915 ,//Sn3-3
                651.5558165 ,//Sn3-3+H
                659.5245164 ,//Sn1-2-H
                660.5323415 ,//Sn1-2
                661.5401665 ,//Sn1-2+H,//Sn2-4-H
                662.5479915 ,//Sn2-4
                663.5558165 ,//Sn2-4+H,//Sn3-4-H
                664.5636416 ,//Sn3-4
                665.5714666 ,//Sn3-4+H
                673.5401665 ,//Sn1-3-H
                674.5479915 ,//Sn1-3
                675.5558165 ,//Sn1-3+H,//Sn2-5-H
                676.5636416 ,//Sn2-5
                677.5714666 ,//Sn2-5+H,//Sn3-5-H
                678.5792916 ,//Sn3-5
                679.5871167 ,//Sn3-5+H
                687.5558165 ,//Sn1-4-H
                688.5636416 ,//Sn1-4
                689.5714666 ,//Sn1-4+H,//Sn2-6-H
                690.5792916 ,//Sn2-6
                691.5871167 ,//Sn2-6+H,//Sn3-6-H
                692.5949417 ,//Sn3-6
                693.6027667 ,//Sn3-6+H
                701.5714666 ,//Sn1-5-H
                702.5792916 ,//Sn1-5
                703.5871167 ,//Sn1-5+H,//Sn2-7-H
                704.5949417 ,//Sn2-7
                705.6027667 ,//Sn2-7+H,//Sn3-7-H
                706.6105918 ,//Sn3-7
                707.6184168 ,//Sn3-7+H
                715.5871167 ,//Sn1-6-H
                716.5949417 ,//Sn1-6
                717.6027667 ,//Sn1-6+H,//Sn2-8-H
                718.6105918 ,//Sn2-8
                719.6184168 ,//Sn2-8+H,//Sn3-8-H
                720.6262418 ,//Sn3-8
                721.6340669 ,//Sn3-8+H
                729.6027667 ,//Sn1-7-H
                730.6105918 ,//Sn2-Δ9-H,//Sn1-7
                731.6184168 ,//Sn2-Δ9,//Sn1-7+H
                732.6262418 ,//Sn2-Δ9+H,//Sn3-Δ9-H
                733.6340669 ,//Sn3-Δ9
                734.6418919 ,//Sn3-Δ9+H
                743.6184168 ,//Sn1-8-H,//Sn2-10-H
                744.6262418 ,//Sn2-10,//Sn1-8
                745.6340669 ,//Sn1-8+H,//Sn2-10+H,//Sn3-10-H
                746.6418919 ,//Sn3-10
                747.6497169 ,//Sn3-10+H
                757.6340669 ,//Sn1-9-H,//Sn2-11-H
                758.6418919 ,//Sn2-11,//Sn1-9
                759.6497169 ,//Sn1-9+H,//Sn2-11+H,//Sn3-11-H
                760.657542  ,//Sn3-11
                761.665367  ,//Sn3-11+H
                771.6497169 ,//Sn1-10-H,//Sn2-Δ12-H
                772.657542  ,//Sn2-12,//Sn3-Δ12-H,//Sn1-10
                773.665367  ,//Sn3-Δ12,//Sn1-10+H,//Sn2-12+H
                774.673192  ,//Sn3-Δ12+H
                785.665367  ,//Sn1-11-H,//Sn2-13-H,//Sn3-13-H
                786.673192  ,//Sn2-13,//Sn3-13,//Sn1-11
                787.6810171 ,//Sn1-11+H,//Sn2-13+H,//Sn3-13+H
                799.6810171 ,//Sn1-12-H,//Sn2-14-H,//Sn3-14-H
                800.6888421 ,//Sn2-14,//Sn3-14,//Sn1-12
                801.6966671 ,//Sn1-12+H,//Sn2-14+H,//Sn3-14+H
                813.6966671 ,//Sn1-13-H,//Sn2-15-H,//Sn3-15-H
                814.7044922 ,//Sn2-15,//Sn3-15,//Sn1-13
                815.7123172 ,//Sn1-13+H,//Sn2-15+H,//Sn3-15+H
                827.7123172 ,//Sn1-14-H,//Sn2-16-H,//Sn3-16-H
                828.7201422 ,//Sn2-16,//Sn3-16,//Sn1-14
                829.7279672 ,//Sn1-14+H,//Sn2-16+H,//Sn3-16+H
                839.7487027 ,//precursor-H2O
                841.7279672 ,//Sn1-15-H,//Sn2-17-H,//Sn3-17-H
                842.7357923 ,//Sn2-17,//Sn3-17,//Sn1-15
                843.7436173 ,//Sn1-15+H,//Sn2-17+H,//Sn3-17+H
                857.7592674 ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_NH4()
        {
            //TG 16:0_18:1(9)_18:2(9,12)
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl3 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.TG, 856.7519909, new PositionLevelChains(acyl1, acyl2, acyl3));

            var generator = new TGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+NH4]+"));

            var expects = new[]
            {
                239.2374906 ,//Sn1 Acyl
                263.2374906 ,//Sn3 Acyl
                265.2531407 ,//Sn2 Acyl
                295.2631568 ,//[Sn1 Acyl + C3H3O+H]+
                309.278806860, // Sn2 diagnostics
                313.2737215 ,//[Sn1 Acyl + C3H5O2+H]+
                319.2631568 ,//[Sn3 Acyl + C3H3O+H]+
                321.2788069 ,//[Sn2 Acyl + C3H3O+H]+
                337.2737215 ,//[Sn3 Acyl + C3H5O2+H]+
                339.2893715 ,//[Sn2 Acyl + C3H5O2+H]+
                437.3929082, // [Precursor]2+
                575.503387  ,//Sn2 FA loss
                577.5190371 ,//Sn3 FA loss
                593.5139517 ,//Sn2 Acyl loss
                595.5296018 ,//Sn3 Acyl loss
                601.5190371 ,//Sn1 FA loss
                619.5296018 ,//Sn1 Acyl loss
                636.5197654 ,//Sn2-1-H
                637.5275904 ,//Sn2-1
                638.5354155 ,//Sn2-1+H,//Sn3-1-H
                639.5432405 ,//Sn3-1
                640.5510655 ,//Sn3-1+H
                650.5354155 ,//Sn2-2-H
                651.5432405 ,//Sn2-2
                652.5510655 ,//Sn2-2+H,//Sn3-2-H
                653.5588906 ,//Sn3-2
                654.5667156 ,//Sn3-2+H
                662.5354155 ,//Sn1-1-H
                663.5432405 ,//Sn1-1
                664.5510655 ,//Sn1-1+H,//Sn2-3-H
                665.5588906 ,//Sn2-3
                666.5667156 ,//Sn2-3+H,//Sn3-3-H
                667.5745406 ,//Sn3-3
                668.5823657 ,//Sn3-3+H
                676.5510655 ,//Sn1-2-H
                677.5588906 ,//Sn1-2
                678.5667156 ,//Sn1-2+H,//Sn2-4-H
                679.5745406 ,//Sn2-4
                680.5823657 ,//Sn2-4+H,//Sn3-4-H
                681.5901907 ,//Sn3-4
                682.5980157 ,//Sn3-4+H
                690.5667156 ,//Sn1-3-H
                691.5745406 ,//Sn1-3
                692.5823657 ,//Sn1-3+H,//Sn2-5-H
                693.5901907 ,//Sn2-5
                694.5980157 ,//Sn2-5+H,//Sn3-5-H
                695.6058407 ,//Sn3-5
                696.6136658 ,//Sn3-5+H
                704.5823657 ,//Sn1-4-H
                705.5901907 ,//Sn1-4
                706.5980157 ,//Sn1-4+H,//Sn2-6-H
                707.6058407 ,//Sn2-6
                708.6136658 ,//Sn2-6+H,//Sn3-6-H
                709.6214908 ,//Sn3-6
                710.6293158 ,//Sn3-6+H
                718.5980157 ,//Sn1-5-H
                719.6058407 ,//Sn1-5
                720.6136658 ,//Sn1-5+H,//Sn2-7-H
                721.6214908 ,//Sn2-7
                722.6293158 ,//Sn2-7+H,//Sn3-7-H
                723.6371409 ,//Sn3-7
                724.6449659 ,//Sn3-7+H
                732.6136658 ,//Sn1-6-H
                733.6214908 ,//Sn1-6
                734.6293158 ,//Sn1-6+H,//Sn2-8-H
                735.6371409 ,//Sn2-8
                736.6449659 ,//Sn2-8+H,//Sn3-8-H
                737.6527909 ,//Sn3-8
                738.660616  ,//Sn3-8+H
                746.6293158 ,//Sn1-7-H
                747.6371409 ,//Sn2-Δ9-H,//Sn1-7
                748.6449659 ,//Sn2-Δ9,//Sn1-7+H
                749.6527909 ,//Sn2-Δ9+H,//Sn3-Δ9-H
                750.660616  ,//Sn3-Δ9
                751.668441  ,//Sn3-Δ9+H
                760.6449659 ,//Sn1-8-H,//Sn2-10-H
                761.6527909 ,//Sn2-10,//Sn1-8
                762.660616  ,//Sn1-8+H,//Sn2-10+H,//Sn3-10-H
                763.668441  ,//Sn3-10
                764.676266  ,//Sn3-10+H
                774.660616  ,//Sn1-9-H,//Sn2-11-H
                775.668441  ,//Sn2-11,//Sn1-9
                776.676266  ,//Sn1-9+H,//Sn2-11+H,//Sn3-11-H
                777.6840911 ,//Sn3-11
                778.6919161 ,//Sn3-11+H
                788.676266  ,//Sn1-10-H,//Sn2-Δ12-H
                789.6840911 ,//Sn2-12,//Sn3-Δ12-H,//Sn1-10
                790.6919161 ,//Sn3-Δ12,//Sn1-10+H,//Sn2-12+H
                791.6997411 ,//Sn3-Δ12+H
                802.6919161 ,//Sn1-11-H,//Sn2-13-H,//Sn3-13-H
                803.6997411 ,//Sn2-13,//Sn3-13,//Sn1-11
                804.7075662 ,//Sn1-11+H,//Sn2-13+H,//Sn3-13+H
                816.7075662 ,//Sn1-12-H,//Sn2-14-H,//Sn3-14-H
                817.7153912 ,//Sn2-14,//Sn3-14,//Sn1-12
                818.7232162 ,//Sn1-12+H,//Sn2-14+H,//Sn3-14+H
                830.7232162 ,//Sn1-13-H,//Sn2-15-H,//Sn3-15-H
                831.7310413 ,//Sn2-15,//Sn3-15,//Sn1-13
                832.7388663 ,//Sn1-13+H,//Sn2-15+H,//Sn3-15+H
                839.7487027 ,//[M+H]+ -H2O
                844.7388663 ,//Sn1-14-H,//Sn2-16-H,//Sn3-16-H
                845.7466913 ,//Sn2-16,//Sn3-16,//Sn1-14
                846.7545164 ,//Sn1-14+H,//Sn2-16+H,//Sn3-16+H
                856.7752518 ,//precursor-H2O
                857.7592674 ,//[M+H]+
                858.7545164 ,//Sn1-15-H,//Sn2-17-H,//Sn3-17-H
                859.7623414 ,//Sn2-17,//Sn3-17,//Sn1-15
                860.7701664 ,//Sn1-15+H,//Sn2-17+H,//Sn3-17+H
                874.7858165 ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //TG 16:0_18:1(9)_18:2(9,12)
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl3 = new AcylChain(18, DoubleBond.CreateFromPosition(9,12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.TG, 856.7519909, new PositionLevelChains(acyl1, acyl2, acyl3));

            var generator = new TGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                239.2374906 ,//Sn1 Acyl
                263.2374906 ,//Sn3 Acyl
                265.2531407 ,//Sn2 Acyl
                319.2607511 ,//[Sn1 Acyl + C3H5O + Na]+
                321.2400157 ,//[Sn1 Acyl + C2H3O2 + Na]+
                331.26129967454006, // Sn2 diagnostics
                343.26129967  ,//[Sn3 Acyl + C3H5O + Na]+
                345.2400157 ,//[Sn3 Acyl + C2H3O2 + Na]+
                439.87060580049996, //[Precursor]2+
                //575.5039356 ,//Sn2 FA loss
                599.50152991 ,//Sn3 FA loss
                615.495896  ,//Sn3 Acyl loss
                623.50152991 ,//Sn1 FA loss
                639.495896  ,//Sn1 Acyl loss
                641.4751605 ,//Sn2-1-H
                642.4829856 ,//Sn2-1
                643.4908106 ,//Sn2-1+H,//Sn3-1-H
                644.4986356 ,//Sn3-1
                645.5064607 ,//Sn3-1+H
                655.4908106 ,//Sn2-2-H
                656.4986356 ,//Sn2-2
                657.5064607 ,//Sn2-2+H,//Sn3-2-H
                658.5142857 ,//Sn3-2
                659.5221107 ,//Sn3-2+H
                667.4908106 ,//Sn1-1-H
                668.4986356 ,//Sn1-1
                669.5064607 ,//Sn1-1+H,//Sn2-3-H
                670.5142857 ,//Sn2-3
                671.5221107 ,//Sn2-3+H,//Sn3-3-H
                672.5299357 ,//Sn3-3
                673.5377608 ,//Sn3-3+H
                681.5064607 ,//Sn1-2-H
                682.5142857 ,//Sn1-2
                683.5221107 ,//Sn1-2+H,//Sn2-4-H
                684.5299357 ,//Sn2-4
                685.5377608 ,//Sn2-4+H,//Sn3-4-H
                686.5455858 ,//Sn3-4
                687.5534108 ,//Sn3-4+H
                695.5221107 ,//Sn1-3-H
                696.5299357 ,//Sn1-3
                697.5377608 ,//Sn1-3+H,//Sn2-5-H
                698.5455858 ,//Sn2-5
                699.5534108 ,//Sn2-5+H,//Sn3-5-H
                700.5612359 ,//Sn3-5
                701.5690609 ,//Sn3-5+H
                709.5377608 ,//Sn1-4-H
                710.5455858 ,//Sn1-4
                711.5534108 ,//Sn1-4+H,//Sn2-6-H
                712.5612359 ,//Sn2-6
                713.5690609 ,//Sn2-6+H,//Sn3-6-H
                714.5768859 ,//Sn3-6
                715.584711  ,//Sn3-6+H
                723.5534108 ,//Sn1-5-H
                724.5612359 ,//Sn1-5
                725.5690609 ,//Sn1-5+H,//Sn2-7-H
                726.5768859 ,//Sn2-7
                727.584711  ,//Sn2-7+H,//Sn3-7-H
                728.592536  ,//Sn3-7
                729.600361  ,//Sn3-7+H
                737.5690609 ,//Sn1-6-H
                738.5768859 ,//Sn1-6
                739.584711  ,//Sn1-6+H,//Sn2-8-H
                740.592536  ,//Sn2-8
                741.600361  ,//Sn2-8+H,//Sn3-8-H
                742.6081861 ,//Sn3-8
                743.6160111 ,//Sn3-8+H
                751.584711  ,//Sn1-7-H
                752.592536  ,//Sn1-7,//Sn2-Δ9-H
                753.600361  ,//Sn1-7+H,//Sn2-Δ9
                754.6081861 ,//Sn2-Δ9+H,//Sn3-Δ9-H
                755.6160111 ,//Sn3-Δ9
                756.6238361 ,//Sn3-Δ9+H
                765.600361  ,//Sn1-8-H,//Sn2-10-H
                766.6081861 ,//Sn1-8,//Sn2-10
                767.6160111 ,//Sn1-8+H,//Sn2-10+H,//Sn3-10-H
                768.6238361 ,//Sn3-10
                769.6316612 ,//Sn3-10+H
                779.6160111 ,//Sn1-9-H,//Sn2-11-H
                780.6238361 ,//Sn1-9,//Sn2-11
                781.6316612 ,//Sn1-9+H,//Sn2-11+H,//Sn3-11-H
                782.6394862 ,//Sn3-11
                783.6473112 ,//Sn3-11+H
                793.6316612 ,//Sn1-10-H,//Sn2-Δ12-H
                794.6394862 ,//Sn1-10,//Sn2-12,//Sn3-Δ12-H
                795.6473112 ,//Sn1-10+H,//Sn3-Δ12,//Sn2-12+H
                796.6551363 ,//Sn3-Δ12+H
                807.6473112 ,//Sn1-11-H,//Sn2-13-H,//Sn3-13-H
                808.6551363 ,//Sn1-11,//Sn2-13,//Sn3-13
                809.6629613 ,//Sn1-11+H,//Sn2-13+H,//Sn3-13+H
                821.6629613 ,//Sn1-12-H,//Sn2-14-H,//Sn3-14-H
                822.6707863 ,//Sn1-12,//Sn2-14,//Sn3-14
                823.6786114 ,//Sn1-12+H,//Sn2-14+H,//Sn3-14+H
                835.6786114 ,//Sn1-13-H,//Sn2-15-H,//Sn3-15-H
                836.6864364 ,//Sn1-13,//Sn2-15,//Sn3-15
                837.6942614 ,//Sn1-13+H,//Sn2-15+H,//Sn3-15+H
                849.6942614 ,//Sn1-14-H,//Sn2-16-H,//Sn3-16-H
                850.7020865 ,//Sn1-14,//Sn2-16,//Sn3-16
                851.7099115 ,//Sn1-14+H,//Sn2-16+H,//Sn3-16+H
                863.7099115 ,//Sn1-15-H,//Sn2-17-H,//Sn3-17-H
                864.7177365 ,//Sn1-15,//Sn2-17,//Sn3-17
                865.7255615 ,//Sn1-15+H,//Sn2-17+H,//Sn3-17+H
                879.7412116 ,//precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class BMPSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //BMP 18:1(9)_18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.BMP, 774.5410857, new PositionLevelChains(acyl1, acyl2));

            var generator = new BMPSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                155.01093,// C3H9O6P - H2O	
                173.02150, // C3H9O6P	
                321.27936, // -C3H9O6P -18:1(9)-H2O	
                339.28992, // -C3H9O6P -18:1(9)	
                419.25625063016,// 18:1(9) + C3H6O5P, 18:1(9) + C3H6O5P
                458.27972606724, // -18:1(9) - OH, -18:1(9) - OH
                461.26681,// -H2O -CH2(Sn1)	
                475.28246571887, // -18:1(9), -18:1(9)
                479.27738,//-CH2(Sn1)	
                //493.29303,// -18:1(9)-H2O	
                501.2611818 ,//sn1-1-H
                502.2690068 ,//sn1-1
                503.2768318 ,//sn1-1+H
                //511.30356   ,// -18:1(9), -18:1(9)
                515.2768318 ,//sn1-2-H
                516.2846568 ,//sn1-2
                517.2924819 ,//sn1-2+H
                529.2924819 ,//sn1-3-H
                530.3003069 ,//sn1-3
                531.3081319 ,//sn1-3+H
                543.3081319 ,//sn1-4-H
                544.315957  ,//sn1-4
                545.323782  ,//sn1-4+H
                557.323782  ,//sn1-5-H
                558.331607  ,//sn1-5
                559.3394321 ,//sn1-5+H
                571.3394321 ,//sn1-6-H
                572.3472571 ,//sn1-6
                573.3550821 ,//sn1-6+H
                585.3550821 ,//sn1-7-H
                586.3629072 ,//sn1-7
                587.3707322 ,//sn1-7+H
                599.3707322 ,//sn1-8-H
                600.3785572 ,//sn1-8
                601.3863823 ,//sn1-8+H
                603.535236, //Precursor -C3H9O6P
                612.3785572 ,//sn1-Δ9-H
                613.3863823 ,//sn1-Δ9
                614.3942073 ,//sn1-Δ9+H
                625.3863823 ,//sn1-10-H
                626.3942073 ,//sn1-10
                627.4020323 ,//sn1-10+H
                639.4020323 ,//sn1-11-H
                640.4098574 ,//sn1-11
                641.4176824 ,//sn1-11+H
                653.4176824 ,//sn1-12-H
                654.4255074 ,//sn1-12
                655.4333325 ,//sn1-12+H
                667.4333325 ,//sn1-13-H
                668.4411575 ,//sn1-13
                669.4489825 ,//sn1-13+H
                681.4489825 ,//sn1-14-H
                682.4568075 ,//sn1-14
                683.4646326 ,//sn1-14+H
                695.4646326 ,//sn1-15-H
                696.4724576 ,//sn1-15
                697.4802826 ,//sn1-15+H
                709.4802826 ,//sn1-16-H
                710.4881077 ,//sn1-16
                711.4959327 ,//sn1-16+H
                723.4959327 ,//sn1-17-H
                724.5037577 ,//sn1-17
                725.5115828 ,//sn1-17+H
                739.5272328 ,//[M+H]+ - 2H2O
                757.5377975 ,//[M+H]+ - H2O
                775.5483622 ,//precursor [M+H]+
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_NH4()
        {
            //BMP 18:1(9)_18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.BMP, 774.5410857, new PositionLevelChains(acyl1, acyl2));

            var generator = new BMPSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+NH4]+"));

            var expects = new[]
            {
                155.01093   ,// C3H9O6P - H2O
                173.0215    , // C3H9O6P
                321.27936   , // -C3H9O6P -18:1(9)-H2O
                339.28992   , // -C3H9O6P -18:1(9)
                419.25570206497, // 18:1(9) + C3H6O5P, 18:1(9) + C3H6O5P
                458.27917750205, // - 18:1(9) - OH, -18:1(9) - OH
                461.26681   ,// -H2O -CH2(Sn1)
                475.28191715368, // -18:1(9), -18:1(9)
                479.27738   ,//-CH2(Sn1)
                501.2611818 ,//sn1-1-H
                502.2690068 ,//sn1-1
                503.2768318 ,//sn1-1+H
                515.2768318 ,//sn1-2-H
                516.2846568 ,//sn1-2
                517.2924819 ,//sn1-2+H
                529.2924819 ,//sn1-3-H
                530.3003069 ,//sn1-3
                531.3081319 ,//sn1-3+H
                543.3081319 ,//sn1-4-H
                544.315957  ,//sn1-4
                545.323782  ,//sn1-4+H
                557.323782  ,//sn1-5-H
                558.331607  ,//sn1-5
                559.3394321 ,//sn1-5+H
                571.3394321 ,//sn1-6-H
                572.3472571 ,//sn1-6
                573.3550821 ,//sn1-6+H
                585.3550821 ,//sn1-7-H
                586.3629072 ,//sn1-7
                587.3707322 ,//sn1-7+H
                599.3707322 ,//sn1-8-H
                600.3785572 ,//sn1-8
                601.3863823 ,//sn1-8+H
                603.535236, //Precursor -C3H9O6P
                612.3785572 ,//sn1-Δ9-H
                613.3863823 ,//sn1-Δ9
                614.3942073 ,//sn1-Δ9+H
                625.3863823 ,//sn1-10-H
                626.3942073 ,//sn1-10
                627.4020323 ,//sn1-10+H
                639.4020323 ,//sn1-11-H
                640.4098574 ,//sn1-11
                641.4176824 ,//sn1-11+H
                653.4176824 ,//sn1-12-H
                654.4255074 ,//sn1-12
                655.4333325 ,//sn1-12+H
                667.4333325 ,//sn1-13-H
                668.4411575 ,//sn1-13
                669.4489825 ,//sn1-13+H
                681.4489825 ,//sn1-14-H
                682.4568075 ,//sn1-14
                683.4646326 ,//sn1-14+H
                695.4646326 ,//sn1-15-H
                696.4724576 ,//sn1-15
                697.4802826 ,//sn1-15+H
                709.4802826 ,//sn1-16-H
                710.4881077 ,//sn1-16
                711.4959327 ,//sn1-16+H
                723.4959327 ,//sn1-17-H
                724.5037577 ,//sn1-17
                725.5115828 ,//sn1-17+H
                739.5272328 ,//[M+H]+ - 2H2O
                757.5377975 ,//[M+H]+ - H2O
                775.5483622 ,//precursor [M+H]+
                792.5749113 ,//precursor [M+NH4]+
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
        [TestMethod()]
        public void GenerateTest_Na()
        {
            //BMP 18:1(9)_18:1(9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.BMP, 774.5410857, new PositionLevelChains(acyl1, acyl2));

            var generator = new BMPSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+Na]+"));

            var expects = new[]
            {
                176.9928792 ,// C3H9O6P - H2O
                195.0034439 ,// C3H9O6P
                441.2381952 ,// -C3H6O2 -18:1(9)-H2O 
                459.2487599 ,// -C3H6O2 -18:1(9) 
                501.2593246 ,// -CH2(Sn1)
                516.2827997 ,// -18:1(9)-OH
                533.2855393 ,// -18:1(9)
                559.2642554 ,//sn1-1-H
                560.2720804 ,//sn1-1
                561.2799054 ,//sn1-1+H
                573.2799054 ,//sn1-2-H
                574.2877305 ,//sn1-2
                575.2955555 ,//sn1-2+H
                587.2955555 ,//sn1-3-H
                588.3033805 ,//sn1-3
                589.3112055 ,//sn1-3+H
                601.3112055 ,//sn1-4-H
                602.3190306 ,//sn1-4
                603.3268556 ,//sn1-4+H
                615.3268556 ,//sn1-5-H
                616.3346806 ,//sn1-5
                617.3425057 ,//sn1-5+H
                625.5171803, // Precursor -C3H9O6P
                629.3425057 ,//sn1-6-H
                630.3503307 ,//sn1-6
                631.3581557 ,//sn1-6+H
                643.3581557 ,//sn1-7-H
                644.3659808 ,//sn1-7
                645.3738058 ,//sn1-7+H
                657.3738058 ,//sn1-8-H
                658.3816308 ,//sn1-8
                659.3894559 ,//sn1-8+H
                670.3816308 ,//sn1-Δ9-H
                671.3894559 ,//sn1-Δ9
                672.3972809 ,//sn1-Δ9+H
                683.3894559 ,//sn1-10-H
                684.3972809 ,//sn1-10
                685.4051059 ,//sn1-10+H
                697.4051059 ,//sn1-11-H
                698.412931  ,//sn1-11
                699.420756  ,//sn1-11+H
                711.420756  ,//sn1-12-H
                712.428581  ,//sn1-12
                713.4364061 ,//sn1-12+H
                725.4364061 ,//sn1-13-H
                726.4442311 ,//sn1-13
                727.4520561 ,//sn1-13+H
                739.4520561 ,//sn1-14-H
                740.4598812 ,//sn1-14
                741.4677062 ,//sn1-14+H
                753.4677062 ,//sn1-15-H
                754.4755312 ,//sn1-15
                755.4833563 ,//sn1-15+H
                767.4833563 ,//sn1-16-H
                768.4911813 ,//sn1-16
                769.4990063 ,//sn1-16+H
                781.4990063 ,//sn1-17-H
                782.5068313 ,//sn1-17
                783.5146564 ,//sn1-17+H
                797.5303064 ,//precursor [M+Na]+
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class DGTSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //DGTS 16:0_18:1
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.DGTS, 737.61695, new PositionLevelChains(acyl1, acyl2));

            var generator = new DGTSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                130.08625504885998,//Header - CH2 
                175.12029482877,//C8H16NO3   
                202.14376992497998,//Gly-C   
                204.1230344804,//Gly-O   
                397.29484682923993,// -18:1(9)-O -C3H9N  
                414.29758648086994,// -18:1(9) -C3H9N  
                423.31049689337993,//-16:0-O -C3H9N  
                440.31323654500994,//-16:0 -C3H9N  
                457.37617115473995,// -18:1(9)-O   
                469.37617115473995,// -CH2(Sn1)   
                474.37891080636996,// -18:1(9)   
                483.39182121887995,//-16:0-O   
                500.35817536178996,// -18:1(9) C1-H  
                500.39456087050996,//-16:0   
                501.36600039385996,//18:1(9) C1  
                502.37382542592997,//18:1(9) C1+H  
                514.3738254259299,//18:1(9) C2-H  
                515.3816504579999,//18:1(9) C2  
                516.3894754900698,//18:1(9) C2+H  
                526.3738254259299,// 16:0 C1-H  
                527.3816504579999,// 16:0 C1  
                528.3894754900698,// 16:0 C1+H, 18:1(9) C3-H
                529.3973005221399,//18:1(9) C3  
                530.4051255542098,//18:1(9) C3+H  
                540.3894754900699,// 16:0 C2-H  
                541.3973005221399,// 16:0 C2  
                542.4051255542098,// 16:0 C2+H, 18:1(9) C4-H
                543.4129505862799,//18:1(9) C4  
                544.4207756183498,//18:1(9) C4+H  
                554.4051255542099,// 16:0 C3-H  
                555.4129505862799,// 16:0 C3  
                556.4207756183498,// 16:0 C3+H, 18:1(9) C5-H
                557.4286006504199,//18:1(9) C5  
                558.4364256824898,//18:1(9) C5+H  
                568.4207756183499,// 16:0 C4-H  
                569.4286006504199,// 16:0 C4  
                570.4364256824898,// 16:0 C4+H, 18:1(9) C6-H
                571.4442507145599,//18:1(9) C6  
                572.4520757466298,//18:1(9) C6+H  
                582.4364256824899,// 16:0 C5-H  
                583.4442507145599,// 16:0 C5  
                584.4520757466298,// 16:0 C5+H, 18:1(9) C7-H
                585.4599007786999,//18:1(9) C7  
                586.4677258107698,//18:1(9) C7+H  
                596.4520757466299,// 16:0 C6-H  
                597.4599007786999,// 16:0 C6  
                598.4677258107698,// 16:0 C6+H, 18:1(9) C8-H
                599.4755508428399,//18:1(9) C8  
                600.4833758749098,//18:1(9) C8+H  
                610.4677258107699,// 16:0 C7-H  
                611.4755508428399,// 16:0 C7, 18:1(9) C9-H
                612.4833758749098,// 16:0 C7+H, 18:1(9) C9
                613.4912009069798,//18:1(9) C9+H  
                624.4833758749099,// 16:0 C8-H, 18:1(9) C10-H
                625.4912009069799,// 16:0 C8, 18:1(9) C10
                626.4990259390498,// 16:0 C8+H, 18:1(9) C10+H
                638.49902593905,// 16:0 C9-H, 18:1(9) C11-H
                639.5068509711199,// 16:0 C9, 18:1(9) C11
                640.5146760031898,// 16:0 C9+H, 18:1(9) C11+H
                652.51467600319,// 16:0 C10-H, 18:1(9) C12-H
                653.5225010352599,// 16:0 C10, 18:1(9) C12
                654.5303260673298,// 16:0 C10+H, 18:1(9) C12+H
                666.53032606733,// 16:0 C11-H, 18:1(9) C13-H
                667.5381510993999,// 16:0 C11, 18:1(9) C13
                668.5459761314698,// 16:0 C11+H, 18:1(9) C13+H
                680.54597613147,// 16:0 C12-H, 18:1(9) C14-H
                681.5538011635399,// 16:0 C12, 18:1(9) C14
                682.5616261956098,// 16:0 C12+H, 18:1(9) C14+H
                693.6265721809799,//Precursor - CO2 
                694.56162619561,// 16:0 C13-H, 18:1(9) C15-H
                695.5694512276799,// 16:0 C13, 18:1(9) C15
                696.5772762597499,// 16:0 C13+H, 18:1(9) C15+H
                708.57727625975,// 16:0 C14-H, 18:1(9) C16-H
                709.5851012918199,// 16:0 C14, 18:1(9) C16
                710.5929263238899,// 16:0 C14+H, 18:1(9) C16+H
                722.59292632389,// 16:0 C15-H, 18:1(9) C17-H
                723.6007513559599,// 16:0 C15, 18:1(9) C17
                724.6085763880299,// 16:0 C15+H, 18:1(9) C17+H
                738.6242264521699,//Precursor   
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class DGTASpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //DGTA 16:0_18:1
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.DGTA, 737.61695, new PositionLevelChains(acyl1, acyl2));

            var generator = new DGTASpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                144.101905113, //  Header
                160.09681973256, //  Header +O
                162.1124697967, //  Header +H2O
                202.14376992497998,//Gly-C   
                204.1230344804,//Gly-O   
                457.37617115473995,// -18:1(9)-O   
                469.37617115473995,// -CH2(Sn1)   
                474.37891080636996,// -18:1(9)   
                483.39182121887995,//-16:0-O   
                500.35817536178996,// -18:1(9) C1-H  
                500.39456087050996,//-16:0   
                501.36600039385996,//18:1(9) C1  
                502.37382542592997,//18:1(9) C1+H  
                514.3738254259299,//18:1(9) C2-H  
                515.3816504579999,//18:1(9) C2  
                516.3894754900698,//18:1(9) C2+H  
                526.3738254259299,// 16:0 C1-H  
                527.3816504579999,// 16:0 C1  
                528.3894754900698,// 16:0 C1+H, 18:1(9) C3-H
                529.3973005221399,//18:1(9) C3  
                530.4051255542098,//18:1(9) C3+H  
                540.3894754900699,// 16:0 C2-H  
                541.3973005221399,// 16:0 C2  
                542.4051255542098,// 16:0 C2+H, 18:1(9) C4-H
                543.4129505862799,//18:1(9) C4  
                544.4207756183498,//18:1(9) C4+H  
                554.4051255542099,// 16:0 C3-H  
                555.4129505862799,// 16:0 C3  
                556.4207756183498,// 16:0 C3+H, 18:1(9) C5-H
                557.4286006504199,//18:1(9) C5  
                558.4364256824898,//18:1(9) C5+H  
                568.4207756183499,// 16:0 C4-H  
                569.4286006504199,// 16:0 C4  
                570.4364256824898,// 16:0 C4+H, 18:1(9) C6-H
                571.4442507145599,//18:1(9) C6  
                572.4520757466298,//18:1(9) C6+H  
                582.4364256824899,// 16:0 C5-H  
                583.4442507145599,// 16:0 C5  
                584.4520757466298,// 16:0 C5+H, 18:1(9) C7-H
                585.4599007786999,//18:1(9) C7  
                586.4677258107698,//18:1(9) C7+H  
                596.4520757466299,// 16:0 C6-H  
                597.4599007786999,// 16:0 C6  
                598.4677258107698,// 16:0 C6+H, 18:1(9) C8-H
                599.4755508428399,//18:1(9) C8  
                600.4833758749098,//18:1(9) C8+H  
                610.4677258107699,// 16:0 C7-H  
                611.4755508428399,// 16:0 C7, 18:1(9) C9-H
                612.4833758749098,// 16:0 C7+H, 18:1(9) C9
                613.4912009069798,//18:1(9) C9+H  
                624.4833758749099,// 16:0 C8-H, 18:1(9) C10-H
                625.4912009069799,// 16:0 C8, 18:1(9) C10
                626.4990259390498,// 16:0 C8+H, 18:1(9) C10+H
                638.49902593905,// 16:0 C9-H, 18:1(9) C11-H
                639.5068509711199,// 16:0 C9, 18:1(9) C11
                640.5146760031898,// 16:0 C9+H, 18:1(9) C11+H
                652.51467600319,// 16:0 C10-H, 18:1(9) C12-H
                653.5225010352599,// 16:0 C10, 18:1(9) C12
                654.5303260673298,// 16:0 C10+H, 18:1(9) C12+H
                666.53032606733,// 16:0 C11-H, 18:1(9) C13-H
                667.5381510993999,// 16:0 C11, 18:1(9) C13
                668.5459761314698,// 16:0 C11+H, 18:1(9) C13+H
                680.54597613147,// 16:0 C12-H, 18:1(9) C14-H
                681.5538011635399,// 16:0 C12, 18:1(9) C14
                682.5616261956098,// 16:0 C12+H, 18:1(9) C14+H
                693.6265721809799,//Precursor - CO2 
                694.56162619561,// 16:0 C13-H, 18:1(9) C15-H
                695.5694512276799,// 16:0 C13, 18:1(9) C15
                696.5772762597499,// 16:0 C13+H, 18:1(9) C15+H
                708.57727625975,// 16:0 C14-H, 18:1(9) C16-H
                709.5851012918199,// 16:0 C14, 18:1(9) C16
                710.5929263238899,// 16:0 C14+H, 18:1(9) C16+H
                722.59292632389,// 16:0 C15-H, 18:1(9) C17-H
                723.6007513559599,// 16:0 C15, 18:1(9) C17
                724.6085763880299,// 16:0 C15+H, 18:1(9) C17+H
                738.6242264521699,//Precursor   
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class LDGTSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LDGTS 20:5 O=C([O-])C(CCOCC(O)COC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)[N+](C)(C)C
            var acyl1 = new AcylChain(20, DoubleBond.CreateFromPosition(4, 7, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LDGTS, 519.35599, new PositionLevelChains(acyl1));

            var generator = new LDGTSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                117.07843001678998,//Header - C2H3
                130.08625504885998,//Header - CH2
                144.101905113,//Header  
                162.1124697967,//Header + H2O
                173.10464476463,//C8H14NO3
                175.12029482877,//C8H16NO3  
                204.1230344804,//Gly-O  
                204.12303618681,// -CH2(SN1)  
                217.130861,// -20:5-O  
                236.14925093465,// -20:5  
                262.12851549007,// 20:5 C1-H 
                263.13634052214,// 20:5 C1 
                264.14416555421,// 20:5 C1+H 
                276.14416555421,// 20:5 C2-H 
                277.15199058628,// 20:5 C2 
                278.15981561835,// 20:5 C2+H 
                290.15981561835,// 20:5 C3-H 
                291.16764065042,// 20:5 C3 
                292.17546568249,// 20:5 C3+H 
                303.16764065042,// 20:5 C4-H 
                304.17546568249,// 20:5 C4 
                305.18329071456003,// 20:5 C4+H 
                316.17546568249,// 20:5 C5-H 
                317.18329071456003,// 20:5 C5 
                318.19111574663003,// 20:5 C5+H 
                330.19111574663003,// 20:5 C6-H 
                331.19894077870003,// 20:5 C6 
                332.20676581077004,// 20:5 C6+H 
                343.19894077870003,// 20:5 C7-H 
                344.20676581077004,// 20:5 C7 
                345.21459084284004,// 20:5 C7+H 
                356.20676581077004,// 20:5 C8-H 
                357.21459084284004,// 20:5 C8 
                358.22241587491004,// 20:5 C8+H 
                370.22241587491004,// 20:5 C9-H 
                371.23024090698004,// 20:5 C9 
                372.23806593905005,// 20:5 C9+H 
                384.23806593905005,// 20:5 C10-H 
                385.24589097112005,// 20:5 C10 
                386.25371600319005,// 20:5 C10+H 
                397.24589097112,// 20:5 C11-H 
                398.25371600319,// 20:5 C11 
                399.26154103526,// 20:5 C11+H 
                410.25371600319005,// 20:5 C12-H 
                411.26154103526005,// 20:5 C12 
                412.26936606733005,// 20:5 C12+H 
                424.26936606733005,// 20:5 C13-H 
                425.27719109940006,// 20:5 C13 
                426.28501613147006,// 20:5 C13+H 
                437.2771910994,// 20:5 C14-H 
                438.28501613147,// 20:5 C14 
                439.29284116354,// 20:5 C14+H 
                450.28501613147006,// 20:5 C15-H 
                451.29284116354006,// 20:5 C15 
                452.30066619561006,// 20:5 C15+H 
                464.30066619561006,// 20:5 C16-H 
                465.30849122768007,// 20:5 C16 
                466.31631625975007,// 20:5 C16+H 
                475.36561218098,//Precursor - CO2
                477.30849122768,// 20:5 C17-H 
                478.31631625975,// 20:5 C17 
                479.32414129182,// 20:5 C17+H 
                490.31631625975007,// 20:5 C18-H 
                491.32414129182007,// 20:5 C18 
                492.3319663238901,// 20:5 C18+H 
                502.35270176847,//Precursor - H2O
                504.33196632389,// 20:5 C19-H 
                505.33979135596,// 20:5 C19 
                506.34761638803,// 20:5 C19+H 
                520.3632664521699,//Precursor  
            };
            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]
    public class LDGTASpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //LDGTA 20:5 
            var acyl1 = new AcylChain(20, DoubleBond.CreateFromPosition(4, 7, 11, 14, 17), new Oxidized(0));
            var lipid = new Lipid(LbmClass.LDGTA, 519.35599, new PositionLevelChains(acyl1));

            var generator = new LDGTASpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                144.101905113,//Header  
                160.0968197,//Header + O
                162.1124698,//Header + H2O
                175.12029482877,//C8H16NO3  
                204.1230344804,//Gly-O  
                204.12303618681,// -CH2(SN1)  
                218.13868625,// -20:5-O  
                236.14925093465,// -20:5  
                262.12851549007,// 20:5 C1-H 
                263.13634052214,// 20:5 C1 
                264.14416555421,// 20:5 C1+H 
                276.14416555421,// 20:5 C2-H 
                277.15199058628,// 20:5 C2 
                278.15981561835,// 20:5 C2+H 
                290.15981561835,// 20:5 C3-H 
                291.16764065042,// 20:5 C3 
                292.17546568249,// 20:5 C3+H 
                303.16764065042,// 20:5 C4-H 
                304.17546568249,// 20:5 C4 
                305.18329071456003,// 20:5 C4+H 
                316.17546568249,// 20:5 C5-H 
                317.18329071456003,// 20:5 C5 
                318.19111574663003,// 20:5 C5+H 
                330.19111574663003,// 20:5 C6-H 
                331.19894077870003,// 20:5 C6 
                332.20676581077004,// 20:5 C6+H 
                343.19894077870003,// 20:5 C7-H 
                344.20676581077004,// 20:5 C7 
                345.21459084284004,// 20:5 C7+H 
                356.20676581077004,// 20:5 C8-H 
                357.21459084284004,// 20:5 C8 
                358.22241587491004,// 20:5 C8+H 
                370.22241587491004,// 20:5 C9-H 
                371.23024090698004,// 20:5 C9 
                372.23806593905005,// 20:5 C9+H 
                384.23806593905005,// 20:5 C10-H 
                385.24589097112005,// 20:5 C10 
                386.25371600319005,// 20:5 C10+H 
                397.24589097112,// 20:5 C11-H 
                398.25371600319,// 20:5 C11 
                399.26154103526,// 20:5 C11+H 
                410.25371600319005,// 20:5 C12-H 
                411.26154103526005,// 20:5 C12 
                412.26936606733005,// 20:5 C12+H 
                424.26936606733005,// 20:5 C13-H 
                425.27719109940006,// 20:5 C13 
                426.28501613147006,// 20:5 C13+H 
                437.2771910994,// 20:5 C14-H 
                438.28501613147,// 20:5 C14 
                439.29284116354,// 20:5 C14+H 
                450.28501613147006,// 20:5 C15-H 
                451.29284116354006,// 20:5 C15 
                452.30066619561006,// 20:5 C15+H 
                464.30066619561006,// 20:5 C16-H 
                465.30849122768007,// 20:5 C16 
                466.31631625975007,// 20:5 C16+H 
                475.36561218098,//Precursor - CO2
                477.30849122768,// 20:5 C17-H 
                478.31631625975,// 20:5 C17 
                479.32414129182,// 20:5 C17+H 
                490.31631625975007,// 20:5 C18-H 
                491.32414129182007,// 20:5 C18 
                492.3319663238901,// 20:5 C18+H 
                502.35270176847,//Precursor - H2O
                504.33196632389,// 20:5 C19-H 
                505.33979135596,// 20:5 C19 
                506.34761638803,// 20:5 C19+H 
                520.3632664521699,//Precursor  
            };
            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class MGSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            //
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(11), new Oxidized(0));
            var lipid = new Lipid(LbmClass.MG, 328.2614, new PositionLevelChains(acyl1));

            var generator = new MGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
            //    119.03337, // 16:1(11) C1-H
            //    120.04120, // 16:1(11) C1
            //    121.04902, // 16:1(11) C1+H
            //    133.04902, // 16:1(11) C2-H
            //    134.05685, // 16:1(11) C2
            //    135.06467, // 16:1(11) C2+H
            //    147.06467, // 16:1(11) C3-H
            //    148.07250, // 16:1(11) C3
            //    149.08032, // 16:1(11) C3+H
            //    161.08032, // 16:1(11) C4-H
            //    162.08815, // 16:1(11) C4
            //    163.09597, // 16:1(11) C4+H
            //    175.09597, // 16:1(11) C5-H
            //    176.10380, // 16:1(11) C5
            //    177.11162, // 16:1(11) C5+H
            //    189.11162, // 16:1(11) C6-H
            //    190.11945, // 16:1(11) C6
            //    191.12727, // 16:1(11) C6+H
            //    203.12727, // 16:1(11) C7-H
            //    204.13510, // 16:1(11) C7
            //    205.14292, // 16:1(11) C7+H
            //    217.14292, // 16:1(11) C8-H
            //    218.15075, // 16:1(11) C8
            //    219.15857, // 16:1(11) C8+H
            //    231.15857, // 16:1(11) C9-H
            //    232.16640, // 16:1(11) C9
            //    233.17422, // 16:1(11) C9+H
                237.22129, // 16:1(11) acyl+
                //245.17422, // 16:1(11) C10-H
                //246.18205, // 16:1(11) C10
                //247.18987, // 16:1(11) C10+H
                //258.18205, // 16:1(11) C11-H
                //259.18987, // 16:1(11) C11
                //260.19770, // 16:1(11) C11+H
                //271.18987, // 16:1(11) C12-H
                //272.19770, // 16:1(11) C12
                //273.20552, // 16:1(11) C12+H
                //285.20552, // 16:1(11) C13-H
                //286.21335, // 16:1(11) C13
                //287.22117, // 16:1(11) C13+H
                //299.22117, // 16:1(11) C14-H
                //300.22900, // 16:1(11) C14
                //301.23682, // 16:1(11) C14+H
                311.25811, // [M+H]+ -H2O
                //313.23682, // 16:1(11) C15-H
                //314.24465, // 16:1(11) C15
                //315.25247, // 16:1(11) C15+H
                329.26867, // [M+H]+
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }

        [TestMethod()]
        public void GenerateTest_NH4()
        {
            var acyl1 = new AcylChain(16, DoubleBond.CreateFromPosition(11), new Oxidized(0));
            var lipid = new Lipid(LbmClass.MG, 328.2614, new PositionLevelChains(acyl1));

            var generator = new MGSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+NH4]+"));

            var expects = new[]
            {
                //119.03337, // 16:1(11) C1-H
                //120.04120, // 16:1(11) C1
                //121.04902, // 16:1(11) C1+H
                //133.04902, // 16:1(11) C2-H
                //134.05685, // 16:1(11) C2
                //135.06467, // 16:1(11) C2+H
                //147.06467, // 16:1(11) C3-H
                //148.07250, // 16:1(11) C3
                //149.08032, // 16:1(11) C3+H
                //161.08032, // 16:1(11) C4-H
                //162.08815, // 16:1(11) C4
                //163.09597, // 16:1(11) C4+H
                //175.09597, // 16:1(11) C5-H
                //176.10380, // 16:1(11) C5
                //177.11162, // 16:1(11) C5+H
                //189.11162, // 16:1(11) C6-H
                //190.11945, // 16:1(11) C6
                //191.12727, // 16:1(11) C6+H
                //203.12727, // 16:1(11) C7-H
                //204.13510, // 16:1(11) C7
                //205.14292, // 16:1(11) C7+H
                //217.14292, // 16:1(11) C8-H
                //218.15075, // 16:1(11) C8
                //219.15857, // 16:1(11) C8+H
                //231.15857, // 16:1(11) C9-H
                //232.16640, // 16:1(11) C9
                //233.17422, // 16:1(11) C9+H
                237.22129, // 16:1(11) acyl+
                //245.17422, // 16:1(11) C10-H
                //246.18205, // 16:1(11) C10
                //247.18987, // 16:1(11) C10+H
                //258.18205, // 16:1(11) C11-H
                //259.18987, // 16:1(11) C11
                //260.19770, // 16:1(11) C11+H
                //271.18987, // 16:1(11) C12-H
                //272.19770, // 16:1(11) C12
                //273.20552, // 16:1(11) C12+H
                //285.20552, // 16:1(11) C13-H
                //286.21335, // 16:1(11) C13
                //287.22117, // 16:1(11) C13+H
                //299.22117, // 16:1(11) C14-H
                //300.22900, // 16:1(11) C14
                //301.23682, // 16:1(11) C14+H
                311.25811, // [M+H]+ -H2O
                //313.23682, // 16:1(11) C15-H
                //314.24465, // 16:1(11) C15
                //315.25247, // 16:1(11) C15+H
                329.26867, // [M+H]+
                346.29522, // Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
    [TestClass()]
    public class CARSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest_H()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.CAR, 425.350509, new PositionLevelChains(acyl1));

            var generator = new CARSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIon.GetAdductIon("[M+H]+"));

            var expects = new[]
            {
                144.10191, //  Header-H2O 
                162.11247, //  Header 
                188.09173, // Acyl 18:1(9) C1-H
                189.09956, // Acyl 18:1(9) C1
                190.10738, // Acyl 18:1(9) C1+H
                202.10738, // Acyl 18:1(9) C2-H
                203.11521, // Acyl 18:1(9) C2
                204.12303, // Acyl 18:1(9) C2+H
                216.12303, // Acyl 18:1(9) C3-H
                217.13086, // Acyl 18:1(9) C3
                218.13868, // Acyl 18:1(9) C3+H
                230.13868, // Acyl 18:1(9) C4-H
                231.14651, // Acyl 18:1(9) C4
                232.15433, // Acyl 18:1(9) C4+H
                244.15433, // Acyl 18:1(9) C5-H
                245.16216, // Acyl 18:1(9) C5
                246.16998, // Acyl 18:1(9) C5+H
                258.16998, // Acyl 18:1(9) C6-H
                259.17781, // Acyl 18:1(9) C6
                260.18563, // Acyl 18:1(9) C6+H
                265.25259, //  [Acyl]+ 
                272.18563, // Acyl 18:1(9) C7-H
                273.19346, // Acyl 18:1(9) C7
                274.20128, // Acyl 18:1(9) C7+H
                286.20128, // Acyl 18:1(9) C8-H
                287.20911, // Acyl 18:1(9) C8
                288.21693, // Acyl 18:1(9) C8+H
                299.20911, // Acyl 18:1(9) C9-H
                300.21693, // Acyl 18:1(9) C9
                301.22476, // Acyl 18:1(9) C9+H
                312.21693, // Acyl 18:1(9) C10-H
                313.22476, // Acyl 18:1(9) C10
                314.23258, // Acyl 18:1(9) C10+H
                326.23258, // Acyl 18:1(9) C11-H
                327.24041, // Acyl 18:1(9) C11
                328.24824, // Acyl 18:1(9) C11+H
                340.24824, // Acyl 18:1(9) C12-H
                341.25606, // Acyl 18:1(9) C12
                342.26389, // Acyl 18:1(9) C12+H
                354.26389, // Acyl 18:1(9) C13-H
                355.27171, // Acyl 18:1(9) C13
                356.27954, // Acyl 18:1(9) C13+H
                368.27954, // Acyl 18:1(9) C14-H
                369.28736, // Acyl 18:1(9) C14
                370.29519, // Acyl 18:1(9) C14+H
                381.36013, // Precursor-CHO2 
                382.29519, // Acyl 18:1(9) C15-H
                383.30301, // Acyl 18:1(9) C15
                384.31084, // Acyl 18:1(9) C15+H
                396.31084, // Acyl 18:1(9) C16-H
                397.31866, // Acyl 18:1(9) C16
                398.32649, // Acyl 18:1(9) C16+H
                410.32649, // Acyl 18:1(9) C17-H
                411.33431, // Acyl 18:1(9) C17
                412.34214, // Acyl 18:1(9) C17+H
                426.35779, // Precursor 

            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.ZipInternal(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }
}