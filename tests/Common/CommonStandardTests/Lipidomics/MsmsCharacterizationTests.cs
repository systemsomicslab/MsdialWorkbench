using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class MsmsCharacterizationTests
    {
        private AdductIon adduct;

        [TestMethod()]
        public void ASHexCerCharacterizationTest()
        {
            //
            var target = new MSScanProperty
            {
                PrecursorMz = 1126.85547,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 70.6539, Intensity =4245, },
                    new SpectrumPeak { Mass = 73.10381, Intensity =4293, },
                    new SpectrumPeak { Mass = 77.86407, Intensity =4671, },
                    new SpectrumPeak { Mass = 96.95885, Intensity =107380, },
                    new SpectrumPeak { Mass = 105.70238, Intensity =4631, },
                    new SpectrumPeak { Mass = 109.63298, Intensity =4590, },
                    new SpectrumPeak { Mass = 130.076, Intensity =4834, },
                    new SpectrumPeak { Mass = 187.03737, Intensity =5211, },
                    new SpectrumPeak { Mass = 200.36009, Intensity =19559, },
                    new SpectrumPeak { Mass = 255.23323, Intensity =91509, },
                    new SpectrumPeak { Mass = 480.86212, Intensity =5097, },
                    new SpectrumPeak { Mass = 870.62146, Intensity =7941, },
                    new SpectrumPeak { Mass = 1126.85461, Intensity =656581, },
                }
            };
            var totalCarbon = 58;
            var totalDbBond = 2;
            var totalOxidized = 2;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 24;
            var sn2DbBond = 1;
            var sn3Carbon = 16;
            var sn3DbBond = 0;
            //public static LipidMolecule JudgeIfAshexcer(IMSScanProperty msScanProp, double ms2Tolerance,
            //    double theoreticalMz, int totalCarbon, int totalDoubleBond, int totalOxidized,// If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //    int minExtAcylCarbon, int maxExtAcylCarbon, int minExtAcylDoubleBond, int maxExtAcylDoubleBond,
            //    int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //    AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfAshexcer(target, 0.025,
                1126.8536f, totalCarbon, totalDbBond, totalOxidized,
                         sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M-H]-"));
            Console.WriteLine($"{result.LipidName}");
            Console.WriteLine($"{result.AnnotationLevel}");
        }
        [TestMethod()]
        public void ASHexCerCharacterizationTest2()
        {
            //
            var target = new MSScanProperty
            {
                PrecursorMz = 1144.8656,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 79.95629, Intensity =8127, },
                    new SpectrumPeak { Mass = 96.95882, Intensity =101070, },
                    new SpectrumPeak { Mass = 105.31581, Intensity =5056, },
                    new SpectrumPeak { Mass = 113.02306, Intensity =6497, },
                    new SpectrumPeak { Mass = 177.77856, Intensity =4869, },
                    new SpectrumPeak { Mass = 184.79161, Intensity =4649, },
                    new SpectrumPeak { Mass = 255.23222, Intensity =37471, },
                    new SpectrumPeak { Mass = 656.82147, Intensity =7909, },
                    new SpectrumPeak { Mass = 1144.86572, Intensity =550451, },
                }
            };
            var totalCarbon = 58;
            var totalDbBond = 2;
            var totalOxidized = 3;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 24;
            var sn2DbBond = 0;
            var sn3Carbon = 16;
            var sn3DbBond = 0;
            //public static LipidMolecule JudgeIfAshexcer(IMSScanProperty msScanProp, double ms2Tolerance,
            //    double theoreticalMz, int totalCarbon, int totalDoubleBond, int totalOxidized,// If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //    int minExtAcylCarbon, int maxExtAcylCarbon, int minExtAcylDoubleBond, int maxExtAcylDoubleBond,
            //    int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //    AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfAshexcer(target, 0.025,
                1144.8643f, totalCarbon, totalDbBond, totalOxidized,
                         sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M-H]-"));
            Console.WriteLine($"{result.LipidName}");
            Console.WriteLine($"{result.AnnotationLevel}");
        }
        [TestMethod()]
        public void ASHexCerCharacterizationTest3()
        {
            //
            var target = new MSScanProperty
            {
                PrecursorMz = 1128.8693,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 92.75924, Intensity =4142, },
                    new SpectrumPeak { Mass = 96.9588, Intensity =65543, },
                    new SpectrumPeak { Mass = 184.65691, Intensity =5017, },
                    new SpectrumPeak { Mass = 203.88467, Intensity =6886, },
                    new SpectrumPeak { Mass = 203.90297, Intensity =5302, },
                    new SpectrumPeak { Mass = 255.233, Intensity =32080, },
                    new SpectrumPeak { Mass = 266.48315, Intensity =5206, },
                    new SpectrumPeak { Mass = 422.24164, Intensity =5518, },
                    new SpectrumPeak { Mass = 872.61127, Intensity =7763, },
                    new SpectrumPeak { Mass = 1128.86804, Intensity =356477, },
                }
            };
            var totalCarbon = 58;
            var totalDbBond = 1;
            var totalOxidized = 2;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 24;
            var sn2DbBond = 0;
            var sn3Carbon = 16;
            var sn3DbBond = 0;
            //public static LipidMolecule JudgeIfAshexcer(IMSScanProperty msScanProp, double ms2Tolerance,
            //    double theoreticalMz, int totalCarbon, int totalDoubleBond, int totalOxidized,// If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //    int minExtAcylCarbon, int maxExtAcylCarbon, int minExtAcylDoubleBond, int maxExtAcylDoubleBond,
            //    int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //    AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfAshexcer(target, 0.025,
                1128.8693f, totalCarbon, totalDbBond, totalOxidized,
                         sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M-H]-"));
            //var result2 = LipidAnnotation.Characterize(1128.8693f, 0.025,
            //             target, 
            //             new List<LipidMolecule>() {
            //                 new LipidMolecule() { 
            //                     LipidName = "(O-16:0)18:1;O2/24:0" ,
            //                     Adduct = new AdductIon() { AdductIonName = "[M-H]-", IonMode = IonMode.Negative } ,
            //                     Mz =1128.8693f
            //                 } 
            //             },
            //             IonMode.Negative,
            //             0.01,0.025);
            Console.WriteLine($"{result.LipidName}");
            Console.WriteLine($"{result.AnnotationLevel}");
        //    Console.WriteLine($"{result2.LipidName}");
        //    Console.WriteLine($"{result.AnnotationLevel}");
        //
        }
        [TestMethod()]
        public void HexCeramideNSTest()
        {
            //HexCer 42:1;O2|HexCer 18:1;O2/24:0
            var target = new MSScanProperty
            {
                PrecursorMz = 812.69766,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 252.2683, Intensity =558, },
                    new SpectrumPeak { Mass = 264.2684, Intensity =6815, },
                    new SpectrumPeak { Mass = 282.27912, Intensity =669, },
                    new SpectrumPeak { Mass = 368.3884, Intensity =158, },
                    new SpectrumPeak { Mass = 602.62408, Intensity =260, },
                    new SpectrumPeak { Mass = 614.62462, Intensity =1083, },
                    new SpectrumPeak { Mass = 616.6392, Intensity =226, },
                    new SpectrumPeak { Mass = 632.63468, Intensity =2325, },
                    new SpectrumPeak { Mass = 794.68899, Intensity =2681, },
                }
            };

            var target2 = new MSScanProperty
            {
                PrecursorMz = 794.68589,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 252.2654, Intensity =118, },
                    new SpectrumPeak { Mass = 264.26886, Intensity =1280, },
                    new SpectrumPeak { Mass = 282.27973, Intensity =118, },
                    new SpectrumPeak { Mass = 495.40356, Intensity =131, },
                    new SpectrumPeak { Mass = 614.62836, Intensity =223, },
                    new SpectrumPeak { Mass = 616.63641, Intensity =122, },
                    new SpectrumPeak { Mass = 632.6329, Intensity =102, },
                    new SpectrumPeak { Mass = 794.68511, Intensity =645, },
                }
            };
            var totalCarbon = 42;
            var totalDbBond = 1;
            var totalOxidized = 2;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 24;
            var sn2DbBond = 0;

        //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
            //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfHexceramidens(target, 0.025,
                812.69739f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H]+"));

            var result2 = LipidMsmsCharacterization.JudgeIfHexceramidens(target2, 0.025,
                794.68683f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
            Console.WriteLine($"HexCer_NS test (HexCer 42:1;O2|HexCer 18:1;O2/24:0)");
            Console.WriteLine($"[M+H]+:{result.LipidName}");
            Console.WriteLine($"[M+H]+:{result.AnnotationLevel}");
            Console.WriteLine($"[M+H-H2O]+:{result2.LipidName}");
            Console.WriteLine($"[M+H-H2O]+:{result2.AnnotationLevel}");
            //
        }
        [TestMethod()]
        public void HexCeramideHSTest()
        {
            //HexCer 34:1;O3|HexCer 18:1;O2/16:0;O
            var target = new MSScanProperty
            {
                PrecursorMz = 716.56508,
                Spectrum = new List<SpectrumPeak>
                {

                    new SpectrumPeak { Mass = 252.26888, Intensity =148, },
                    new SpectrumPeak { Mass = 264.26828, Intensity =1056, },
                    new SpectrumPeak { Mass = 282.28342, Intensity =120, },
                    new SpectrumPeak { Mass = 518.49257, Intensity =269, },
                    new SpectrumPeak { Mass = 536.50176, Intensity =186, },
                    new SpectrumPeak { Mass = 608.2612, Intensity =12, },
                    new SpectrumPeak { Mass = 698.55307, Intensity =81, },
                    new SpectrumPeak { Mass = 716.37043, Intensity =12, },
                    new SpectrumPeak { Mass = 716.3921, Intensity =36, },
                    new SpectrumPeak { Mass = 716.5499, Intensity =24, },

                }
            };

            var target2 = new MSScanProperty
            {
                PrecursorMz = 698.5565,
                Spectrum = new List<SpectrumPeak>
                {

                    new SpectrumPeak { Mass = 698.556545, Intensity = 999, },
                    new SpectrumPeak { Mass = 536.503721, Intensity = 700, },
                    new SpectrumPeak { Mass = 518.493157, Intensity = 800, },
                    new SpectrumPeak { Mass = 506.493157, Intensity = 400, },
                    new SpectrumPeak { Mass = 282.279141, Intensity = 300, },
                    new SpectrumPeak { Mass = 264.268576, Intensity = 700, },
                    new SpectrumPeak { Mass = 252.268576, Intensity = 300, },
                }
            };
            var totalCarbon = 34;
            var totalDbBond = 1;
            var totalOxidized = 3;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 16;
            var sn2DbBond = 0;



            var result = LipidMsmsCharacterization.JudgeIfHexceramideo(target, 0.025,
                716.56708f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H]+"));

            var result2 = LipidMsmsCharacterization.JudgeIfHexceramideo(target2, 0.025,
                698.5565f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
            Console.WriteLine($"HexCer_HS test (HexCer 34:1;O3|HexCer 18:1;O2/16:0;O)");
            Console.WriteLine($"[M+H]+:{result.LipidName}");
            Console.WriteLine($"[M+H]+:{result.AnnotationLevel}");
            Console.WriteLine($"[M+H-H2O]+:{result2.LipidName}");
            Console.WriteLine($"[M+H-H2O]+:{result2.AnnotationLevel}");
            //
        }
        [TestMethod()]
        public void HexCeramideAPTest()
        {
            //HexCer 18:1;O3/24:0;(2OH)
            var target = new MSScanProperty
            {
                PrecursorMz = 844.6872,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 844.687225, Intensity =50, },
                    new SpectrumPeak { Mass = 682.634401, Intensity =500, },
                    new SpectrumPeak { Mass = 664.623836, Intensity =999, },
                    new SpectrumPeak { Mass = 646.613272, Intensity =300, },
                    new SpectrumPeak { Mass = 298.274056, Intensity =200, },
                    new SpectrumPeak { Mass = 280.263491, Intensity =400, },
                    new SpectrumPeak { Mass = 262.252926, Intensity =200, },
                }
            };

            var target2 = new MSScanProperty
            {
                PrecursorMz = 826.6767,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 682.634401, Intensity = 500, },
                    new SpectrumPeak { Mass = 664.623836, Intensity = 999, },
                    new SpectrumPeak { Mass = 646.613272, Intensity = 300, },
                    new SpectrumPeak { Mass = 298.274056, Intensity = 200, },
                    new SpectrumPeak { Mass = 280.263491, Intensity = 400, },
                    new SpectrumPeak { Mass = 262.252926, Intensity = 200, },
                }
            };
            var totalCarbon = 42;
            var totalDbBond = 1;
            var totalOxidized = 4;
            var sn1Carbon = 18;
            var sn1DbBond = 1;
            var sn2Carbon = 24;
            var sn2DbBond = 0;



            var result = LipidMsmsCharacterization.JudgeIfHexceramideap(target, 0.025,
                844.6872f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H]+"));

            var result2 = LipidMsmsCharacterization.JudgeIfHexceramideap(target2, 0.025,
                826.6767f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
            Console.WriteLine($"HexCer_AP test (HexCer 18:1;O3/24:0;(2OH))");
            Console.WriteLine($"[M+H]+:{result.LipidName}");
            Console.WriteLine($"[M+H]+:{result.AnnotationLevel}");
            Console.WriteLine($"[M+H-H2O]+:{result2.LipidName}");
            Console.WriteLine($"[M+H-H2O]+:{result2.AnnotationLevel}");
            //
        }

    }
}
