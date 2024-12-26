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
            Console.WriteLine($"[M+H]+");
            Console.WriteLine($"LipidName:{result.LipidName}");
            Console.WriteLine($"AnnotationLevel:{result.AnnotationLevel}");
            Console.WriteLine($"[M+H-H2O]+");
            Console.WriteLine($"LipidName:{result2.LipidName}");
            Console.WriteLine($"AnnotationLevel:{result2.AnnotationLevel}");
        }
        [TestMethod()]
        public void HexCeramideNDSTest()
        {
            //HexCer 18:0;O2/24:1
            var target = new MSScanProperty
            {
                PrecursorMz = 812.6974,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 812.697395, Intensity =50, },
                    new SpectrumPeak { Mass = 794.686831, Intensity =400, },
                    new SpectrumPeak { Mass = 650.644572, Intensity =50, },
                    new SpectrumPeak { Mass = 632.634007, Intensity =100, },
                    new SpectrumPeak { Mass = 614.623443, Intensity =100, },
                    new SpectrumPeak { Mass = 366.373042, Intensity =100, },
                    new SpectrumPeak { Mass = 284.294791, Intensity =200, },
                    new SpectrumPeak { Mass = 266.284227, Intensity =999, },
                    new SpectrumPeak { Mass = 254.284227, Intensity =200, },
                }
            };

            var target2 = new MSScanProperty
            {
                PrecursorMz = 794.6868,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 794.686831, Intensity =999, },
                    new SpectrumPeak { Mass = 650.644572, Intensity =50, },
                    new SpectrumPeak { Mass = 632.634007, Intensity =100, },
                    new SpectrumPeak { Mass = 614.623443, Intensity =100, },
                    new SpectrumPeak { Mass = 366.373042, Intensity =100, },
                    new SpectrumPeak { Mass = 284.294791, Intensity =200, },
                    new SpectrumPeak { Mass = 266.284227, Intensity =999, },
                    new SpectrumPeak { Mass = 254.284227, Intensity =200, },

                }
            };
            var totalCarbon = 42;
            var totalDbBond = 1;
            var totalOxidized = 2;
            var sn1Carbon = 18;
            var sn1DbBond = 0;
            var sn2Carbon = 24;
            var sn2DbBond = 1;

            //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
            //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfHexceramidends(target, 0.025,
                812.6974f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H]+"));

            var result2 = LipidMsmsCharacterization.JudgeIfHexceramidends(target2, 0.025,
                794.6868f, totalCarbon, totalDbBond,
                         sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                         adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
            Console.WriteLine($"HexCer_NDS test (HexCer 42:1;O2|HexCer 18:0;O2/24:1)");
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

        [TestMethod()]
        public void AHexCerTest()
        {
            //AHexCer 60:2;3O|AHexCer (O-18:1)18:1;2O/24:0;O
            var target = new MSScanProperty
            {
                PrecursorMz = 1092.9376,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 1092.937626, Intensity =150, },
                    new SpectrumPeak { Mass = 1074.927061, Intensity =999, },
                    new SpectrumPeak { Mass = 666.639487, Intensity =175, },
                    new SpectrumPeak { Mass = 648.628922, Intensity =150, },
                    new SpectrumPeak { Mass = 630.618357, Intensity =150, },
                    new SpectrumPeak { Mass = 427.305415, Intensity =200, },
                    new SpectrumPeak { Mass = 282.279141, Intensity =100, },
                    new SpectrumPeak { Mass = 264.268576, Intensity =200, },
                    new SpectrumPeak { Mass = 252.268576, Intensity =50, },
                    new SpectrumPeak { Mass = 239.236942, Intensity =150, },
                }
            };

            var target2 = new MSScanProperty
            {
                PrecursorMz = 1074.9271,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 1074.927061, Intensity =999, },
                    new SpectrumPeak { Mass = 666.639487, Intensity =175, },
                    new SpectrumPeak { Mass = 648.628922, Intensity =150, },
                    new SpectrumPeak { Mass = 630.618357, Intensity =150, },
                    new SpectrumPeak { Mass = 427.305415, Intensity =200, },
                    new SpectrumPeak { Mass = 282.279141, Intensity =100, },
                    new SpectrumPeak { Mass = 264.268576, Intensity =200, },
                    new SpectrumPeak { Mass = 252.268576, Intensity =50, },
                    new SpectrumPeak { Mass = 239.236942, Intensity =150, },
                }
            };
            var totalCarbon = 60;
            var totalDbBond = 2;
            var totalOxidized = 3;
            var SphCarbon = 18;
            var SphDoubleBond = 1;
            var ExtAcylCarbon = 18;
            var ExtAcylDoubleBond = 1;

            //public static LipidMolecule JudgeIfAcylhexcer(IMSScanProperty msScanProp, double ms2Tolerance,
            //    double theoreticalMz, int totalCarbon, int totalDoubleBond, int totalOxidized,// If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            //    int minExtAcylCarbon, int maxExtAcylCarbon, int minExtAcylDoubleBond, int maxExtAcylDoubleBond,
            //    int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
            //    AdductIon adduct)

            var result = LipidMsmsCharacterization.JudgeIfAcylhexcer(target, 0.025,
                1092.9376f, totalCarbon, totalDbBond, totalOxidized,
                ExtAcylCarbon, ExtAcylCarbon, ExtAcylDoubleBond, ExtAcylDoubleBond,
                SphCarbon, SphCarbon, SphDoubleBond, SphDoubleBond,
                         adduct = AdductIon.GetAdductIon("[M+H]+"));

            var result2 = LipidMsmsCharacterization.JudgeIfAcylhexcer(target2, 0.025,
                1074.9271f, totalCarbon, totalDbBond, totalOxidized,
                ExtAcylCarbon, ExtAcylCarbon, ExtAcylDoubleBond, ExtAcylDoubleBond,
                SphCarbon, SphCarbon, SphDoubleBond, SphDoubleBond,
                adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
            Console.WriteLine($"AHexCer test (AHexCer (O-18:0)18:1;2O/24:0;O)");
            Console.WriteLine($"[M+H]+:{result.LipidName}");
            Console.WriteLine($"[M+H]+:{result.AnnotationLevel}");
            Console.WriteLine($"[M+H-H2O]+:{result2.LipidName}");
            Console.WriteLine($"[M+H-H2O]+:{result2.AnnotationLevel}");
            //
        }
        #region
        //[TestMethod()]
        //public void HexCeramideNSTest2()
        //{
        //    //HexCer 34:1;O2|HexCer 18:1;O2/16:0
        //    var target = new MSScanProperty
        //    {
        //        PrecursorMz = 682.56325,
        //        Spectrum = new List<SpectrumPeak>
        //        {
        //            #region
        //            new SpectrumPeak { Mass = 59.0471, Intensity =344, },
        //            new SpectrumPeak { Mass = 60.0638, Intensity =84, },
        //            new SpectrumPeak { Mass = 73.04221, Intensity =138, },
        //            new SpectrumPeak { Mass = 79.05439, Intensity =57, },
        //            new SpectrumPeak { Mass = 83.08579, Intensity =56, },
        //            new SpectrumPeak { Mass = 95.08436, Intensity =54, },
        //            new SpectrumPeak { Mass = 97.10049, Intensity =52, },
        //            new SpectrumPeak { Mass = 100.08176, Intensity =59, },
        //            new SpectrumPeak { Mass = 103.07151, Intensity =64, },
        //            new SpectrumPeak { Mass = 109.09947, Intensity =61, },
        //            new SpectrumPeak { Mass = 115.07238, Intensity =158, },
        //            new SpectrumPeak { Mass = 121.10228, Intensity =52, },
        //            new SpectrumPeak { Mass = 123.11612, Intensity =62, },
        //            new SpectrumPeak { Mass = 127.07444, Intensity =64, },
        //            new SpectrumPeak { Mass = 131.06807, Intensity =53, },
        //            new SpectrumPeak { Mass = 131.08401, Intensity =71, },
        //            new SpectrumPeak { Mass = 133.0843, Intensity =103, },
        //            new SpectrumPeak { Mass = 134.09096, Intensity =59, },
        //            new SpectrumPeak { Mass = 137.08078, Intensity =99, },
        //            new SpectrumPeak { Mass = 149.04161, Intensity =65, },
        //            new SpectrumPeak { Mass = 157.09628, Intensity =264, },
        //            new SpectrumPeak { Mass = 157.12294, Intensity =110, },
        //            new SpectrumPeak { Mass = 159.14014, Intensity =50, },
        //            new SpectrumPeak { Mass = 177.14085, Intensity =54, },
        //            new SpectrumPeak { Mass = 184.07127, Intensity =704, },
        //            new SpectrumPeak { Mass = 185.07938, Intensity =77, },
        //            new SpectrumPeak { Mass = 193.15724, Intensity =55, },
        //            new SpectrumPeak { Mass = 204.15562, Intensity =62, },
        //            new SpectrumPeak { Mass = 209.04326, Intensity =166, },
        //            new SpectrumPeak { Mass = 209.14954, Intensity =52, },
        //            new SpectrumPeak { Mass = 213.08218, Intensity =58, },
        //            new SpectrumPeak { Mass = 219.13367, Intensity =59, },
        //            new SpectrumPeak { Mass = 223.06116, Intensity =757, },
        //            new SpectrumPeak { Mass = 223.07907, Intensity =57, },
        //            new SpectrumPeak { Mass = 224.06538, Intensity =172, },
        //            new SpectrumPeak { Mass = 227.13039, Intensity =58, },
        //            new SpectrumPeak { Mass = 232.15945, Intensity =78, },
        //            new SpectrumPeak { Mass = 233.17073, Intensity =243, },
        //            new SpectrumPeak { Mass = 242.18578, Intensity =69, },
        //            new SpectrumPeak { Mass = 250.19313, Intensity =84, },
        //            new SpectrumPeak { Mass = 252.26115, Intensity =300, },
        //            new SpectrumPeak { Mass = 253.12534, Intensity =78, },
        //            new SpectrumPeak { Mass = 257.26367, Intensity =83, },
        //            new SpectrumPeak { Mass = 264.26651, Intensity =5373, },
        //            new SpectrumPeak { Mass = 265.20322, Intensity =56, },
        //            new SpectrumPeak { Mass = 265.26837, Intensity =943, },
        //            new SpectrumPeak { Mass = 266.26559, Intensity =189, },
        //            new SpectrumPeak { Mass = 277.18405, Intensity =76, },
        //            new SpectrumPeak { Mass = 281.04932, Intensity =1521, },
        //            new SpectrumPeak { Mass = 281.09427, Intensity =61, },
        //            new SpectrumPeak { Mass = 282.04648, Intensity =353, },
        //            new SpectrumPeak { Mass = 282.27921, Intensity =216, },
        //            new SpectrumPeak { Mass = 283.27661, Intensity =103, },
        //            new SpectrumPeak { Mass = 284.0397, Intensity =68, },
        //            new SpectrumPeak { Mass = 291.21353, Intensity =108, },
        //            new SpectrumPeak { Mass = 297.07846, Intensity =548, },
        //            new SpectrumPeak { Mass = 298.08136, Intensity =184, },
        //            new SpectrumPeak { Mass = 299.03922, Intensity =98, },
        //            new SpectrumPeak { Mass = 309.21219, Intensity =64, },
        //            new SpectrumPeak { Mass = 313.29813, Intensity =74, },
        //            new SpectrumPeak { Mass = 329.20944, Intensity =125, },
        //            new SpectrumPeak { Mass = 341.01431, Intensity =583, },
        //            new SpectrumPeak { Mass = 341.20285, Intensity =96, },
        //            new SpectrumPeak { Mass = 342.0014, Intensity =169, },
        //            new SpectrumPeak { Mass = 355.06693, Intensity =3580, },
        //            new SpectrumPeak { Mass = 356.06873, Intensity =917, },
        //            new SpectrumPeak { Mass = 357.0455, Intensity =826, },
        //            new SpectrumPeak { Mass = 357.06769, Intensity =113, },
        //            new SpectrumPeak { Mass = 357.24564, Intensity =84, },
        //            new SpectrumPeak { Mass = 358.04056, Intensity =186, },
        //            new SpectrumPeak { Mass = 367.26562, Intensity =58, },
        //            new SpectrumPeak { Mass = 371.09915, Intensity =3824, },
        //            new SpectrumPeak { Mass = 372.09927, Intensity =1037, },
        //            new SpectrumPeak { Mass = 373.0752, Intensity =396, },
        //            new SpectrumPeak { Mass = 374.07327, Intensity =91, },
        //            new SpectrumPeak { Mass = 387.18643, Intensity =77, },
        //            new SpectrumPeak { Mass = 405.27954, Intensity =60, },
        //            new SpectrumPeak { Mass = 407.3024, Intensity =72, },
        //            new SpectrumPeak { Mass = 408.3049, Intensity =77, },
        //            new SpectrumPeak { Mass = 413.29422, Intensity =97, },
        //            new SpectrumPeak { Mass = 413.30811, Intensity =93, },
        //            new SpectrumPeak { Mass = 427.30502, Intensity =101, },
        //            new SpectrumPeak { Mass = 429.08539, Intensity =1316, },
        //            new SpectrumPeak { Mass = 430.0892, Intensity =401, },
        //            new SpectrumPeak { Mass = 431.08722, Intensity =166, },
        //            new SpectrumPeak { Mass = 431.30099, Intensity =65, },
        //            new SpectrumPeak { Mass = 433.07458, Intensity =76, },
        //            new SpectrumPeak { Mass = 444.33142, Intensity =128, },
        //            new SpectrumPeak { Mass = 445.0874, Intensity =170, },
        //            new SpectrumPeak { Mass = 445.11188, Intensity =370, },
        //            new SpectrumPeak { Mass = 445.32645, Intensity =66, },
        //            new SpectrumPeak { Mass = 446.10028, Intensity =150, },
        //            new SpectrumPeak { Mass = 447.1088, Intensity =153, },
        //            new SpectrumPeak { Mass = 447.32397, Intensity =53, },
        //            new SpectrumPeak { Mass = 448.11926, Intensity =85, },
        //            new SpectrumPeak { Mass = 475.98669, Intensity =108, },
        //            new SpectrumPeak { Mass = 476.99875, Intensity =74, },
        //            new SpectrumPeak { Mass = 477.98532, Intensity =74, },
        //            new SpectrumPeak { Mass = 490.47049, Intensity =285, },
        //            new SpectrumPeak { Mass = 498.35608, Intensity =54, },
        //            new SpectrumPeak { Mass = 502.49478, Intensity =1985, },
        //            new SpectrumPeak { Mass = 503.09668, Intensity =175, },
        //            new SpectrumPeak { Mass = 503.49265, Intensity =814, },
        //            new SpectrumPeak { Mass = 503.53568, Intensity =62, },
        //            new SpectrumPeak { Mass = 504.45862, Intensity =70, },
        //            new SpectrumPeak { Mass = 504.5105, Intensity =363, },
        //            new SpectrumPeak { Mass = 505.07571, Intensity =156, },
        //            new SpectrumPeak { Mass = 505.51593, Intensity =144, },
        //            new SpectrumPeak { Mass = 506.07639, Intensity =98, },
        //            new SpectrumPeak { Mass = 520.50671, Intensity =783, },
        //            new SpectrumPeak { Mass = 520.53448, Intensity =69, },
        //            new SpectrumPeak { Mass = 521.50134, Intensity =280, },
        //            new SpectrumPeak { Mass = 522.51141, Intensity =100, },
        //            new SpectrumPeak { Mass = 523.35864, Intensity =67, },
        //            new SpectrumPeak { Mass = 523.46832, Intensity =440, },
        //            new SpectrumPeak { Mass = 524.47278, Intensity =157, },
        //            new SpectrumPeak { Mass = 541.39337, Intensity =174, },
        //            new SpectrumPeak { Mass = 545.40485, Intensity =73, },
        //            new SpectrumPeak { Mass = 546.48346, Intensity =82, },
        //            new SpectrumPeak { Mass = 558.46655, Intensity =66, },
        //            new SpectrumPeak { Mass = 560.47571, Intensity =67, },
        //            new SpectrumPeak { Mass = 564.04938, Intensity =205, },
        //            new SpectrumPeak { Mass = 565.04297, Intensity =130, },
        //            new SpectrumPeak { Mass = 566.06964, Intensity =71, },
        //            new SpectrumPeak { Mass = 569.93433, Intensity =52, },
        //            new SpectrumPeak { Mass = 580.09833, Intensity =160, },
        //            new SpectrumPeak { Mass = 601.47046, Intensity =77, },
        //            new SpectrumPeak { Mass = 623.56305, Intensity =62, },
        //            new SpectrumPeak { Mass = 625.55389, Intensity =162, },
        //            new SpectrumPeak { Mass = 626.44696, Intensity =62, },
        //            new SpectrumPeak { Mass = 626.48969, Intensity =76, },
        //            new SpectrumPeak { Mass = 626.57477, Intensity =74, },
        //            new SpectrumPeak { Mass = 627.57288, Intensity =50, },
        //            new SpectrumPeak { Mass = 643.57013, Intensity =85, },
        //            new SpectrumPeak { Mass = 643.58746, Intensity =64, },
        //            new SpectrumPeak { Mass = 644.61407, Intensity =86, },
        //            new SpectrumPeak { Mass = 645.31958, Intensity =71, },
        //            new SpectrumPeak { Mass = 660.28424, Intensity =126, },
        //            new SpectrumPeak { Mass = 660.33789, Intensity =85, },
        //            new SpectrumPeak { Mass = 661.3111, Intensity =63, },
        //            new SpectrumPeak { Mass = 662.47107, Intensity =76, },
        //            new SpectrumPeak { Mass = 666.49506, Intensity =50, },
        //            new SpectrumPeak { Mass = 667.59412, Intensity =84, },
        //            new SpectrumPeak { Mass = 668.51453, Intensity =66, },
        //            new SpectrumPeak { Mass = 672.4549, Intensity =78, },
        //            new SpectrumPeak { Mass = 674.50024, Intensity =78, },
        //            new SpectrumPeak { Mass = 677.49969, Intensity =51, },
        //            new SpectrumPeak { Mass = 678.41235, Intensity =77, },
        //            new SpectrumPeak { Mass = 678.52911, Intensity =148, },
        //            new SpectrumPeak { Mass = 679.38721, Intensity =55, },
        //            new SpectrumPeak { Mass = 679.42432, Intensity =166, },
        //            new SpectrumPeak { Mass = 679.44177, Intensity =111, },
        //            new SpectrumPeak { Mass = 679.48657, Intensity =95, },
        //            new SpectrumPeak { Mass = 679.51953, Intensity =68, },
        //            new SpectrumPeak { Mass = 679.54486, Intensity =93, },
        //            new SpectrumPeak { Mass = 680.4267, Intensity =50, },
        //            new SpectrumPeak { Mass = 680.46368, Intensity =74, },
        //            new SpectrumPeak { Mass = 680.54956, Intensity =83, },
        //            new SpectrumPeak { Mass = 681.43707, Intensity =393, },
        //            new SpectrumPeak { Mass = 682.37476, Intensity =80, },
        //            new SpectrumPeak { Mass = 682.44946, Intensity =113, },
        //            new SpectrumPeak { Mass = 682.47949, Intensity =679, },
        //            new SpectrumPeak { Mass = 682.50848, Intensity =246, },
        //            new SpectrumPeak { Mass = 682.55768, Intensity =9777, },
        //            new SpectrumPeak { Mass = 682.59692, Intensity =238, },
        //            new SpectrumPeak { Mass = 683.20935, Intensity =130, },
        //            new SpectrumPeak { Mass = 683.43915, Intensity =862, },
        //            new SpectrumPeak { Mass = 683.45789, Intensity =137, },
        //            new SpectrumPeak { Mass = 683.50079, Intensity =125, },
        //            new SpectrumPeak { Mass = 683.56085, Intensity =5175, },
        //            new SpectrumPeak { Mass = 683.62268, Intensity =104, },
        //            new SpectrumPeak { Mass = 683.64868, Intensity =87, },
        //            new SpectrumPeak { Mass = 684.48938, Intensity =277, },
        //            new SpectrumPeak { Mass = 684.51318, Intensity =116, },
        //            new SpectrumPeak { Mass = 684.56128, Intensity =964, },
        //            new SpectrumPeak { Mass = 684.65179, Intensity =53, },
        //            new SpectrumPeak { Mass = 685.23187, Intensity =89, },
        //            new SpectrumPeak { Mass = 685.36298, Intensity =112, },
        //            new SpectrumPeak { Mass = 685.41064, Intensity =119, },
        //            new SpectrumPeak { Mass = 685.4693, Intensity =70, },
        //            new SpectrumPeak { Mass = 685.60461, Intensity =78, },
        //            new SpectrumPeak { Mass = 685.63239, Intensity =51, },
        //            new SpectrumPeak { Mass = 686.15277, Intensity =71, },
        //            new SpectrumPeak { Mass = 686.3764, Intensity =59, },
        //            new SpectrumPeak { Mass = 686.43573, Intensity =156, },
        //            new SpectrumPeak { Mass = 686.46069, Intensity =78, },
        //            new SpectrumPeak { Mass = 686.53149, Intensity =336, },
        //            new SpectrumPeak { Mass = 686.56213, Intensity =90, },
        //            new SpectrumPeak { Mass = 686.62781, Intensity =92, },
        //            new SpectrumPeak { Mass = 687.4975, Intensity =88, },
        //            #endregion
        //        }
        //    };
        //    var totalCarbon = 34;
        //    var totalDbBond = 1;
        //    var totalOxidized = 2;
        //    var sn1Carbon = 18;
        //    var sn1DbBond = 1;
        //    var sn2Carbon = 16;
        //    var sn2DbBond = 0;

        //    //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
        //    //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        //    //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
        //    //AdductIon adduct)

        //    var result = LipidMsmsCharacterization.JudgeIfHexceramidens(target, 0.025,
        //        682.5616f, totalCarbon, totalDbBond,
        //                 sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
        //                 adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
        //    Console.WriteLine($"HexCer_NS test (HexCer 34:1;O2|HexCer 18:1;O2/16:0)");
        //    Console.WriteLine($"[M+H-H2O]+");
        //    Console.WriteLine($"LipidName:{result.LipidName}");
        //    Console.WriteLine($"AnnotationLevel:{result.AnnotationLevel}");
        //    //
        //}

        //[TestMethod()]
        //public void HexCeramideNSTest3()
        //{
        //    //HexCer 34:1;O2|HexCer 18:1;O2/16:0
        //    var target = new MSScanProperty
        //    {
        //        PrecursorMz = 700.57254,
        //        Spectrum = new List<SpectrumPeak>
        //        {
        //            #region
        //                new SpectrumPeak { Mass = 62.96165, Intensity =111, },
        //                new SpectrumPeak { Mass = 71.06101, Intensity =807, },
        //                new SpectrumPeak { Mass = 82.06452, Intensity =97, },
        //                new SpectrumPeak { Mass = 86.09559, Intensity =11670, },
        //                new SpectrumPeak { Mass = 86.45371, Intensity =60, },
        //                new SpectrumPeak { Mass = 87.04351, Intensity =140, },
        //                new SpectrumPeak { Mass = 87.09856, Intensity =472, },
        //                new SpectrumPeak { Mass = 89.05726, Intensity =152, },
        //                new SpectrumPeak { Mass = 97.06015, Intensity =70, },
        //                new SpectrumPeak { Mass = 98.98281, Intensity =1925, },
        //                new SpectrumPeak { Mass = 104.10639, Intensity =2032, },
        //                new SpectrumPeak { Mass = 105.10858, Intensity =174, },
        //                new SpectrumPeak { Mass = 109.09913, Intensity =79, },
        //                new SpectrumPeak { Mass = 123.11552, Intensity =73, },
        //                new SpectrumPeak { Mass = 124.47537, Intensity =75, },
        //                new SpectrumPeak { Mass = 124.97963, Intensity =3776, },
        //                new SpectrumPeak { Mass = 124.99888, Intensity =61, },
        //                new SpectrumPeak { Mass = 125.98276, Intensity =68, },
        //                new SpectrumPeak { Mass = 127.00128, Intensity =117, },
        //                new SpectrumPeak { Mass = 131.09241, Intensity =77, },
        //                new SpectrumPeak { Mass = 139.11028, Intensity =60, },
        //                new SpectrumPeak { Mass = 145.10068, Intensity =112, },
        //                new SpectrumPeak { Mass = 166.06062, Intensity =561, },
        //                new SpectrumPeak { Mass = 170.05238, Intensity =59, },
        //                new SpectrumPeak { Mass = 173.11971, Intensity =86, },
        //                new SpectrumPeak { Mass = 183.13808, Intensity =79, },
        //                new SpectrumPeak { Mass = 183.28362, Intensity =60, },
        //                new SpectrumPeak { Mass = 183.32001, Intensity =1205, },
        //                new SpectrumPeak { Mass = 183.70569, Intensity =59, },
        //                new SpectrumPeak { Mass = 183.78021, Intensity =59, },
        //                new SpectrumPeak { Mass = 183.81998, Intensity =53, },
        //                new SpectrumPeak { Mass = 183.91644, Intensity =62, },
        //                new SpectrumPeak { Mass = 184.62785, Intensity =54, },
        //                new SpectrumPeak { Mass = 184.85851, Intensity =83, },
        //                new SpectrumPeak { Mass = 184.87994, Intensity =110, },
        //                new SpectrumPeak { Mass = 185.07556, Intensity =34189, },
        //                new SpectrumPeak { Mass = 185.34427, Intensity =130, },
        //                new SpectrumPeak { Mass = 185.61214, Intensity =79, },
        //                new SpectrumPeak { Mass = 185.74559, Intensity =87, },
        //                new SpectrumPeak { Mass = 185.7709, Intensity =58, },
        //                new SpectrumPeak { Mass = 185.87164, Intensity =154, },
        //                new SpectrumPeak { Mass = 186.02827, Intensity =141, },
        //                new SpectrumPeak { Mass = 186.06097, Intensity =4097, },
        //                new SpectrumPeak { Mass = 186.07722, Intensity =114, },
        //                new SpectrumPeak { Mass = 186.19496, Intensity =79, },
        //                new SpectrumPeak { Mass = 186.31268, Intensity =156, },
        //                new SpectrumPeak { Mass = 186.46716, Intensity =59, },
        //                new SpectrumPeak { Mass = 186.58076, Intensity =58, },
        //                new SpectrumPeak { Mass = 186.7979, Intensity =107, },
        //                new SpectrumPeak { Mass = 186.82175, Intensity =88, },
        //                new SpectrumPeak { Mass = 186.86317, Intensity =65, },
        //                new SpectrumPeak { Mass = 187.05762, Intensity =168, },
        //                new SpectrumPeak { Mass = 187.07922, Intensity =51, },
        //                new SpectrumPeak { Mass = 187.19067, Intensity =76, },
        //                new SpectrumPeak { Mass = 187.70259, Intensity =150, },
        //                new SpectrumPeak { Mass = 187.83028, Intensity =85, },
        //                new SpectrumPeak { Mass = 188.02945, Intensity =97, },
        //                new SpectrumPeak { Mass = 188.33878, Intensity =98, },
        //                new SpectrumPeak { Mass = 188.91824, Intensity =55, },
        //                new SpectrumPeak { Mass = 189.10336, Intensity =53, },
        //                new SpectrumPeak { Mass = 189.47054, Intensity =65, },
        //                new SpectrumPeak { Mass = 209.14618, Intensity =60, },
        //                new SpectrumPeak { Mass = 223.06349, Intensity =62, },
        //                new SpectrumPeak { Mass = 233.17107, Intensity =196, },
        //                new SpectrumPeak { Mass = 236.23317, Intensity =271, },
        //                new SpectrumPeak { Mass = 247.24342, Intensity =113, },
        //                new SpectrumPeak { Mass = 252.26726, Intensity =246, },
        //                new SpectrumPeak { Mass = 252.53546, Intensity =68, },
        //                new SpectrumPeak { Mass = 255.26375, Intensity =60, },
        //                new SpectrumPeak { Mass = 256.26138, Intensity =218, },
        //                new SpectrumPeak { Mass = 257.2662, Intensity =72, },
        //                new SpectrumPeak { Mass = 264.26672, Intensity =6663, },
        //                new SpectrumPeak { Mass = 264.28992, Intensity =114, },
        //                new SpectrumPeak { Mass = 265.0918, Intensity =64, },
        //                new SpectrumPeak { Mass = 265.27029, Intensity =1536, },
        //                new SpectrumPeak { Mass = 266.25729, Intensity =138, },
        //                new SpectrumPeak { Mass = 279.0502, Intensity =73, },
        //                new SpectrumPeak { Mass = 282.04797, Intensity =68, },
        //                new SpectrumPeak { Mass = 282.27692, Intensity =404, },
        //                new SpectrumPeak { Mass = 294.08798, Intensity =53, },
        //                new SpectrumPeak { Mass = 295.06024, Intensity =93, },
        //                new SpectrumPeak { Mass = 299.72601, Intensity =79, },
        //                new SpectrumPeak { Mass = 337.27237, Intensity =76, },
        //                new SpectrumPeak { Mass = 338.28308, Intensity =83, },
        //                new SpectrumPeak { Mass = 353.04459, Intensity =78, },
        //                new SpectrumPeak { Mass = 355.05069, Intensity =207, },
        //                new SpectrumPeak { Mass = 357.06284, Intensity =63, },
        //                new SpectrumPeak { Mass = 363.28119, Intensity =75, },
        //                new SpectrumPeak { Mass = 368.10141, Intensity =168, },
        //                new SpectrumPeak { Mass = 369.08423, Intensity =131, },
        //                new SpectrumPeak { Mass = 371.0784, Intensity =236, },
        //                new SpectrumPeak { Mass = 371.35715, Intensity =85, },
        //                new SpectrumPeak { Mass = 429.07736, Intensity =101, },
        //                new SpectrumPeak { Mass = 441.11279, Intensity =105, },
        //                new SpectrumPeak { Mass = 445.11212, Intensity =66, },
        //                new SpectrumPeak { Mass = 450.30347, Intensity =114, },
        //                new SpectrumPeak { Mass = 490.4848, Intensity =381, },
        //                new SpectrumPeak { Mass = 491.49228, Intensity =226, },
        //                new SpectrumPeak { Mass = 495.4476, Intensity =134, },
        //                new SpectrumPeak { Mass = 496.33249, Intensity =83, },
        //                new SpectrumPeak { Mass = 496.4523, Intensity =72, },
        //                new SpectrumPeak { Mass = 502.49548, Intensity =3747, },
        //                new SpectrumPeak { Mass = 502.54071, Intensity =130, },
        //                new SpectrumPeak { Mass = 503.47202, Intensity =271, },
        //                new SpectrumPeak { Mass = 503.49951, Intensity =695, },
        //                new SpectrumPeak { Mass = 503.53912, Intensity =90, },
        //                new SpectrumPeak { Mass = 504.50272, Intensity =810, },
        //                new SpectrumPeak { Mass = 504.52246, Intensity =82, },
        //                new SpectrumPeak { Mass = 505.50699, Intensity =225, },
        //                new SpectrumPeak { Mass = 520.46222, Intensity =51, },
        //                new SpectrumPeak { Mass = 520.50702, Intensity =11110, },
        //                new SpectrumPeak { Mass = 520.54333, Intensity =164, },
        //                new SpectrumPeak { Mass = 520.57416, Intensity =51, },
        //                new SpectrumPeak { Mass = 521.43719, Intensity =69, },
        //                new SpectrumPeak { Mass = 521.51215, Intensity =3614, },
        //                new SpectrumPeak { Mass = 522.48279, Intensity =65, },
        //                new SpectrumPeak { Mass = 522.51117, Intensity =517, },
        //                new SpectrumPeak { Mass = 538.50836, Intensity =267, },
        //                new SpectrumPeak { Mass = 539.52417, Intensity =76, },
        //                new SpectrumPeak { Mass = 558.41003, Intensity =56, },
        //                new SpectrumPeak { Mass = 561.41064, Intensity =56, },
        //                new SpectrumPeak { Mass = 562.4903, Intensity =63, },
        //                new SpectrumPeak { Mass = 574.3432, Intensity =54, },
        //                new SpectrumPeak { Mass = 575.10187, Intensity =174, },
        //                new SpectrumPeak { Mass = 576.08368, Intensity =70, },
        //                new SpectrumPeak { Mass = 591.14215, Intensity =91, },
        //                new SpectrumPeak { Mass = 626.48865, Intensity =1221, },
        //                new SpectrumPeak { Mass = 627.47076, Intensity =744, },
        //                new SpectrumPeak { Mass = 627.52515, Intensity =92, },
        //                new SpectrumPeak { Mass = 628.50745, Intensity =97, },
        //                new SpectrumPeak { Mass = 633.30054, Intensity =53, },
        //                new SpectrumPeak { Mass = 643.59735, Intensity =69, },
        //                new SpectrumPeak { Mass = 644.48987, Intensity =56, },
        //                new SpectrumPeak { Mass = 648.33148, Intensity =62, },
        //                new SpectrumPeak { Mass = 663.41949, Intensity =53, },
        //                new SpectrumPeak { Mass = 665.63196, Intensity =71, },
        //                new SpectrumPeak { Mass = 682.49664, Intensity =89, },
        //                new SpectrumPeak { Mass = 682.51154, Intensity =81, },
        //                new SpectrumPeak { Mass = 682.55908, Intensity =12189, },
        //                new SpectrumPeak { Mass = 683.40033, Intensity =57, },
        //                new SpectrumPeak { Mass = 683.46252, Intensity =99, },
        //                new SpectrumPeak { Mass = 683.56158, Intensity =3982, },
        //                new SpectrumPeak { Mass = 684.50732, Intensity =84, },
        //                new SpectrumPeak { Mass = 684.5639, Intensity =849, },
        //                new SpectrumPeak { Mass = 684.62091, Intensity =74, },
        //                new SpectrumPeak { Mass = 685.50507, Intensity =53, },
        //                new SpectrumPeak { Mass = 685.56415, Intensity =13604, },
        //                new SpectrumPeak { Mass = 686.56714, Intensity =3279, },
        //                new SpectrumPeak { Mass = 686.60693, Intensity =168, },
        //                new SpectrumPeak { Mass = 688.54932, Intensity =198, },
        //                new SpectrumPeak { Mass = 688.58112, Intensity =268, },
        //                new SpectrumPeak { Mass = 695.32245, Intensity =87, },
        //                new SpectrumPeak { Mass = 695.4198, Intensity =182, },
        //                new SpectrumPeak { Mass = 696.48743, Intensity =52, },
        //                new SpectrumPeak { Mass = 697.32526, Intensity =141, },
        //                new SpectrumPeak { Mass = 697.53998, Intensity =58, },
        //                new SpectrumPeak { Mass = 698.66711, Intensity =51, },
        //                new SpectrumPeak { Mass = 699.2193, Intensity =51, },
        //                new SpectrumPeak { Mass = 699.53656, Intensity =131, },
        //                new SpectrumPeak { Mass = 700.55939, Intensity =179, },
        //                new SpectrumPeak { Mass = 700.67694, Intensity =268, },
        //                new SpectrumPeak { Mass = 700.716, Intensity =99, },
        //                new SpectrumPeak { Mass = 701.05267, Intensity =53, },
        //                new SpectrumPeak { Mass = 701.27179, Intensity =70, },
        //                new SpectrumPeak { Mass = 701.57794, Intensity =280, },
        //                new SpectrumPeak { Mass = 701.68048, Intensity =85, },
        //                new SpectrumPeak { Mass = 701.72449, Intensity =73, },
        //                new SpectrumPeak { Mass = 702.12915, Intensity =88, },
        //                new SpectrumPeak { Mass = 702.30792, Intensity =136, },
        //                new SpectrumPeak { Mass = 702.41736, Intensity =102, },
        //                new SpectrumPeak { Mass = 702.47369, Intensity =52, },
        //                new SpectrumPeak { Mass = 702.55603, Intensity =143, },
        //                new SpectrumPeak { Mass = 702.65845, Intensity =156, },
        //                new SpectrumPeak { Mass = 703.57269, Intensity =95, },
        //                new SpectrumPeak { Mass = 704.57709, Intensity =103532, },
        //            #endregion
        //        }
        //    };
        //    var totalCarbon = 34;
        //    var totalDbBond = 1;
        //    var totalOxidized = 2;
        //    var sn1Carbon = 18;
        //    var sn1DbBond = 1;
        //    var sn2Carbon = 16;
        //    var sn2DbBond = 0;

        //    //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
        //    //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        //    //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
        //    //AdductIon adduct)

        //    var result = LipidMsmsCharacterization.JudgeIfHexceramidens(target, 0.025,
        //        700.5722f, totalCarbon, totalDbBond,
        //                 sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
        //                 adduct = AdductIon.GetAdductIon("[M+H]+"));
        //    Console.WriteLine($"HexCer_NS test (HexCer 34:1;O2|HexCer 18:1;O2/16:0)");
        //    Console.WriteLine($"[M+H]+");
        //    Console.WriteLine($"LipidName:{result.LipidName}");
        //    Console.WriteLine($"AnnotationLevel:{result.AnnotationLevel}");
        //    //
        //}

        //[TestMethod()]
        //public void HexCeramideNSTest4()
        //{
        //    //HexCer 34:1;O2|HexCer 18:1;O2/16:0
        //    var target = new MSScanProperty
        //    {
        //        PrecursorMz = 766.65181,
        //        Spectrum = new List<SpectrumPeak>
        //        {
        //            #region
        //            new SpectrumPeak { Mass = 57.07333, Intensity =53, },
        //            new SpectrumPeak { Mass = 59.05005, Intensity =1369, },
        //            new SpectrumPeak { Mass = 60.04211, Intensity =270, },
        //            new SpectrumPeak { Mass = 60.06152, Intensity =71, },
        //            new SpectrumPeak { Mass = 69.03328, Intensity =120, },
        //            new SpectrumPeak { Mass = 69.04424, Intensity =214, },
        //            new SpectrumPeak { Mass = 71.06672, Intensity =1284, },
        //            new SpectrumPeak { Mass = 72.0885, Intensity =124, },
        //            new SpectrumPeak { Mass = 76.07389, Intensity =72, },
        //            new SpectrumPeak { Mass = 80.94673, Intensity =123, },
        //            new SpectrumPeak { Mass = 81.03157, Intensity =74, },
        //            new SpectrumPeak { Mass = 83.08431, Intensity =398, },
        //            new SpectrumPeak { Mass = 85.02451, Intensity =60, },
        //            new SpectrumPeak { Mass = 85.06364, Intensity =69, },
        //            new SpectrumPeak { Mass = 85.08536, Intensity =1525, },
        //            new SpectrumPeak { Mass = 86.07987, Intensity =390, },
        //            new SpectrumPeak { Mass = 87.10183, Intensity =75, },
        //            new SpectrumPeak { Mass = 90.07088, Intensity =65, },
        //            new SpectrumPeak { Mass = 91.05515, Intensity =60, },
        //            new SpectrumPeak { Mass = 95.04617, Intensity =71, },
        //            new SpectrumPeak { Mass = 95.06142, Intensity =113, },
        //            new SpectrumPeak { Mass = 97.04428, Intensity =135, },
        //            new SpectrumPeak { Mass = 97.09855, Intensity =455, },
        //            new SpectrumPeak { Mass = 98.98242, Intensity =65, },
        //            new SpectrumPeak { Mass = 99.04447, Intensity =106, },
        //            new SpectrumPeak { Mass = 99.11619, Intensity =737, },
        //            new SpectrumPeak { Mass = 100.08042, Intensity =66, },
        //            new SpectrumPeak { Mass = 101.09458, Intensity =555, },
        //            new SpectrumPeak { Mass = 102.09701, Intensity =190, },
        //            new SpectrumPeak { Mass = 104.10781, Intensity =77, },
        //            new SpectrumPeak { Mass = 105.06616, Intensity =85, },
        //            new SpectrumPeak { Mass = 107.96578, Intensity =82, },
        //            new SpectrumPeak { Mass = 109.07451, Intensity =160, },
        //            new SpectrumPeak { Mass = 111.08003, Intensity =53, },
        //            new SpectrumPeak { Mass = 111.09188, Intensity =448, },
        //            new SpectrumPeak { Mass = 112.04644, Intensity =57, },
        //            new SpectrumPeak { Mass = 112.11855, Intensity =57, },
        //            new SpectrumPeak { Mass = 113.05732, Intensity =52, },
        //            new SpectrumPeak { Mass = 113.12981, Intensity =567, },
        //            new SpectrumPeak { Mass = 115.06199, Intensity =221, },
        //            new SpectrumPeak { Mass = 117.08947, Intensity =1384, },
        //            new SpectrumPeak { Mass = 118.0804, Intensity =138, },
        //            new SpectrumPeak { Mass = 119.09034, Intensity =156, },
        //            new SpectrumPeak { Mass = 121.09847, Intensity =73, },
        //            new SpectrumPeak { Mass = 123.07882, Intensity =117, },
        //            new SpectrumPeak { Mass = 123.11974, Intensity =105, },
        //            new SpectrumPeak { Mass = 123.93012, Intensity =110, },
        //            new SpectrumPeak { Mass = 125.00018, Intensity =224, },
        //            new SpectrumPeak { Mass = 125.06008, Intensity =77, },
        //            new SpectrumPeak { Mass = 125.12976, Intensity =190, },
        //            new SpectrumPeak { Mass = 127.14657, Intensity =329, },
        //            new SpectrumPeak { Mass = 128.15182, Intensity =105, },
        //            new SpectrumPeak { Mass = 129.05403, Intensity =90, },
        //            new SpectrumPeak { Mass = 129.0864, Intensity =110, },
        //            new SpectrumPeak { Mass = 131.07857, Intensity =54, },
        //            new SpectrumPeak { Mass = 133.08733, Intensity =51, },
        //            new SpectrumPeak { Mass = 137.10997, Intensity =148, },
        //            new SpectrumPeak { Mass = 139.07364, Intensity =165, },
        //            new SpectrumPeak { Mass = 139.14554, Intensity =141, },
        //            new SpectrumPeak { Mass = 141.08925, Intensity =82, },
        //            new SpectrumPeak { Mass = 141.16182, Intensity =449, },
        //            new SpectrumPeak { Mass = 142.16893, Intensity =147, },
        //            new SpectrumPeak { Mass = 143.0683, Intensity =118, },
        //            new SpectrumPeak { Mass = 145.11597, Intensity =54, },
        //            new SpectrumPeak { Mass = 147.11598, Intensity =76, },
        //            new SpectrumPeak { Mass = 149.13055, Intensity =88, },
        //            new SpectrumPeak { Mass = 151.14578, Intensity =85, },
        //            new SpectrumPeak { Mass = 153.16302, Intensity =236, },
        //            new SpectrumPeak { Mass = 154.16185, Intensity =51, },
        //            new SpectrumPeak { Mass = 155.10468, Intensity =78, },
        //            new SpectrumPeak { Mass = 155.17723, Intensity =394, },
        //            new SpectrumPeak { Mass = 156.11264, Intensity =52, },
        //            new SpectrumPeak { Mass = 157.08456, Intensity =140, },
        //            new SpectrumPeak { Mass = 157.10413, Intensity =162, },
        //            new SpectrumPeak { Mass = 158.12918, Intensity =135, },
        //            new SpectrumPeak { Mass = 160.14006, Intensity =115, },
        //            new SpectrumPeak { Mass = 165.12698, Intensity =75, },
        //            new SpectrumPeak { Mass = 165.16061, Intensity =63, },
        //            new SpectrumPeak { Mass = 169.19408, Intensity =354, },
        //            new SpectrumPeak { Mass = 170.19629, Intensity =97, },
        //            new SpectrumPeak { Mass = 171.10565, Intensity =89, },
        //            new SpectrumPeak { Mass = 174.00764, Intensity =50, },
        //            new SpectrumPeak { Mass = 174.99646, Intensity =90, },
        //            new SpectrumPeak { Mass = 177.11078, Intensity =63, },
        //            new SpectrumPeak { Mass = 177.14442, Intensity =97, },
        //            new SpectrumPeak { Mass = 179.10266, Intensity =90, },
        //            new SpectrumPeak { Mass = 181.19142, Intensity =134, },
        //            new SpectrumPeak { Mass = 182.96779, Intensity =73, },
        //            new SpectrumPeak { Mass = 183.2058, Intensity =464, },
        //            new SpectrumPeak { Mass = 184.07152, Intensity =33381, },
        //            new SpectrumPeak { Mass = 184.21281, Intensity =80, },
        //            new SpectrumPeak { Mass = 185.07651, Intensity =1033, },
        //            new SpectrumPeak { Mass = 186.07272, Intensity =121, },
        //            new SpectrumPeak { Mass = 187.10844, Intensity =68, },
        //            new SpectrumPeak { Mass = 193.12714, Intensity =106, },
        //            new SpectrumPeak { Mass = 193.19344, Intensity =60, },
        //            new SpectrumPeak { Mass = 194.99193, Intensity =163, },
        //            new SpectrumPeak { Mass = 195.09464, Intensity =55, },
        //            new SpectrumPeak { Mass = 195.20546, Intensity =106, },
        //            new SpectrumPeak { Mass = 197.22305, Intensity =129, },
        //            new SpectrumPeak { Mass = 201.12389, Intensity =52, },
        //            new SpectrumPeak { Mass = 211.23436, Intensity =206, },
        //            new SpectrumPeak { Mass = 213.10388, Intensity =140, },
        //            new SpectrumPeak { Mass = 222.08162, Intensity =63, },
        //            new SpectrumPeak { Mass = 223.0835, Intensity =246, },
        //            new SpectrumPeak { Mass = 224.06256, Intensity =69, },
        //            new SpectrumPeak { Mass = 225.07928, Intensity =163, },
        //            new SpectrumPeak { Mass = 227.12943, Intensity =57, },
        //            new SpectrumPeak { Mass = 233.17159, Intensity =827, },
        //            new SpectrumPeak { Mass = 233.22502, Intensity =51, },
        //            new SpectrumPeak { Mass = 235.13101, Intensity =71, },
        //            new SpectrumPeak { Mass = 236.23482, Intensity =1263, },
        //            new SpectrumPeak { Mass = 237.22241, Intensity =316, },
        //            new SpectrumPeak { Mass = 241.20782, Intensity =82, },
        //            new SpectrumPeak { Mass = 243.20589, Intensity =57, },
        //            new SpectrumPeak { Mass = 247.14378, Intensity =85, },
        //            new SpectrumPeak { Mass = 247.23502, Intensity =199, },
        //            new SpectrumPeak { Mass = 249.16258, Intensity =52, },
        //            new SpectrumPeak { Mass = 250.19528, Intensity =92, },
        //            new SpectrumPeak { Mass = 250.24802, Intensity =222, },
        //            new SpectrumPeak { Mass = 251.22375, Intensity =91, },
        //            new SpectrumPeak { Mass = 251.24701, Intensity =56, },
        //            new SpectrumPeak { Mass = 252.26358, Intensity =465, },
        //            new SpectrumPeak { Mass = 253.26207, Intensity =1390, },
        //            new SpectrumPeak { Mass = 254.28745, Intensity =335, },
        //            new SpectrumPeak { Mass = 255.17497, Intensity =84, },
        //            new SpectrumPeak { Mass = 255.26555, Intensity =52, },
        //            new SpectrumPeak { Mass = 264.26651, Intensity =15019, },
        //            new SpectrumPeak { Mass = 265.27008, Intensity =2589, },
        //            new SpectrumPeak { Mass = 266.27365, Intensity =330, },
        //            new SpectrumPeak { Mass = 267.26273, Intensity =57, },
        //            new SpectrumPeak { Mass = 268.26685, Intensity =79, },
        //            new SpectrumPeak { Mass = 269.24521, Intensity =85, },
        //            new SpectrumPeak { Mass = 269.27609, Intensity =229, },
        //            new SpectrumPeak { Mass = 274.2045, Intensity =63, },
        //            new SpectrumPeak { Mass = 275.19562, Intensity =55, },
        //            new SpectrumPeak { Mass = 279.09921, Intensity =57, },
        //            new SpectrumPeak { Mass = 280.26562, Intensity =51, },
        //            new SpectrumPeak { Mass = 281.09818, Intensity =87, },
        //            new SpectrumPeak { Mass = 281.24835, Intensity =73, },
        //            new SpectrumPeak { Mass = 282.27826, Intensity =295, },
        //            new SpectrumPeak { Mass = 284.26913, Intensity =108, },
        //            new SpectrumPeak { Mass = 285.23972, Intensity =118, },
        //            new SpectrumPeak { Mass = 285.25763, Intensity =111, },
        //            new SpectrumPeak { Mass = 291.21359, Intensity =335, },
        //            new SpectrumPeak { Mass = 292.21097, Intensity =136, },
        //            new SpectrumPeak { Mass = 296.07275, Intensity =53, },
        //            new SpectrumPeak { Mass = 297.10284, Intensity =125, },
        //            new SpectrumPeak { Mass = 298.27271, Intensity =114, },
        //            new SpectrumPeak { Mass = 301.26749, Intensity =56, },
        //            new SpectrumPeak { Mass = 306.27267, Intensity =1958, },
        //            new SpectrumPeak { Mass = 307.27777, Intensity =733, },
        //            new SpectrumPeak { Mass = 308.24072, Intensity =175, },
        //            new SpectrumPeak { Mass = 309.18039, Intensity =52, },
        //            new SpectrumPeak { Mass = 309.2023, Intensity =203, },
        //            new SpectrumPeak { Mass = 311.24945, Intensity =76, },
        //            new SpectrumPeak { Mass = 311.32901, Intensity =2483, },
        //            new SpectrumPeak { Mass = 312.25653, Intensity =65, },
        //            new SpectrumPeak { Mass = 312.33008, Intensity =600, },
        //            new SpectrumPeak { Mass = 313.27353, Intensity =74, },
        //            new SpectrumPeak { Mass = 313.33585, Intensity =122, },
        //            new SpectrumPeak { Mass = 321.29196, Intensity =54, },
        //            new SpectrumPeak { Mass = 328.27689, Intensity =64, },
        //            new SpectrumPeak { Mass = 330.27643, Intensity =551, },
        //            new SpectrumPeak { Mass = 331.28091, Intensity =151, },
        //            new SpectrumPeak { Mass = 337.29492, Intensity =51, },
        //            new SpectrumPeak { Mass = 338.81277, Intensity =167, },
        //            new SpectrumPeak { Mass = 339.0386, Intensity =54, },
        //            new SpectrumPeak { Mass = 340.35651, Intensity =370, },
        //            new SpectrumPeak { Mass = 341.35635, Intensity =62, },
        //            new SpectrumPeak { Mass = 347.23514, Intensity =53, },
        //            new SpectrumPeak { Mass = 351.28412, Intensity =63, },
        //            new SpectrumPeak { Mass = 354.27121, Intensity =69, },
        //            new SpectrumPeak { Mass = 363.31006, Intensity =57, },
        //            new SpectrumPeak { Mass = 367.3645, Intensity =67, },
        //            new SpectrumPeak { Mass = 367.82083, Intensity =242, },
        //            new SpectrumPeak { Mass = 368.3222, Intensity =193, },
        //            new SpectrumPeak { Mass = 368.38443, Intensity =77, },
        //            new SpectrumPeak { Mass = 368.82501, Intensity =69, },
        //            new SpectrumPeak { Mass = 369.3432, Intensity =206, },
        //            new SpectrumPeak { Mass = 369.36823, Intensity =4105, },
        //            new SpectrumPeak { Mass = 369.42599, Intensity =66, },
        //            new SpectrumPeak { Mass = 370.31003, Intensity =97, },
        //            new SpectrumPeak { Mass = 370.37335, Intensity =1003, },
        //            new SpectrumPeak { Mass = 371.22717, Intensity =88, },
        //            new SpectrumPeak { Mass = 371.37958, Intensity =208, },
        //            new SpectrumPeak { Mass = 372.11575, Intensity =88, },
        //            new SpectrumPeak { Mass = 373.17953, Intensity =59, },
        //            new SpectrumPeak { Mass = 385.20892, Intensity =87, },
        //            new SpectrumPeak { Mass = 385.26312, Intensity =118, },
        //            new SpectrumPeak { Mass = 386.28598, Intensity =80, },
        //            new SpectrumPeak { Mass = 386.37051, Intensity =60, },
        //            new SpectrumPeak { Mass = 388.34299, Intensity =60, },
        //            new SpectrumPeak { Mass = 391.28378, Intensity =73, },
        //            new SpectrumPeak { Mass = 393.18445, Intensity =66, },
        //            new SpectrumPeak { Mass = 396.84818, Intensity =369, },
        //            new SpectrumPeak { Mass = 397.34943, Intensity =219, },
        //            new SpectrumPeak { Mass = 397.85406, Intensity =62, },
        //            new SpectrumPeak { Mass = 401.97556, Intensity =66, },
        //            new SpectrumPeak { Mass = 405.25937, Intensity =67, },
        //            new SpectrumPeak { Mass = 408.14001, Intensity =64, },
        //            new SpectrumPeak { Mass = 409.13889, Intensity =101, },
        //            new SpectrumPeak { Mass = 409.29263, Intensity =79, },
        //            new SpectrumPeak { Mass = 413.25244, Intensity =55, },
        //            new SpectrumPeak { Mass = 415.24002, Intensity =78, },
        //            new SpectrumPeak { Mass = 415.31274, Intensity =50, },
        //            new SpectrumPeak { Mass = 417.2648, Intensity =78, },
        //            new SpectrumPeak { Mass = 421.22879, Intensity =61, },
        //            new SpectrumPeak { Mass = 424.31305, Intensity =146, },
        //            new SpectrumPeak { Mass = 425.37079, Intensity =59, },
        //            new SpectrumPeak { Mass = 425.86084, Intensity =248, },
        //            new SpectrumPeak { Mass = 426.3287, Intensity =105, },
        //            new SpectrumPeak { Mass = 426.35526, Intensity =183, },
        //            new SpectrumPeak { Mass = 426.37894, Intensity =60, },
        //            new SpectrumPeak { Mass = 427.28824, Intensity =131, },
        //            new SpectrumPeak { Mass = 427.41257, Intensity =1084, },
        //            new SpectrumPeak { Mass = 428.41342, Intensity =535, },
        //            new SpectrumPeak { Mass = 429.42517, Intensity =66, },
        //            new SpectrumPeak { Mass = 431.08221, Intensity =184, },
        //            new SpectrumPeak { Mass = 433.09949, Intensity =77, },
        //            new SpectrumPeak { Mass = 433.29007, Intensity =175, },
        //            new SpectrumPeak { Mass = 437.26968, Intensity =118, },
        //            new SpectrumPeak { Mass = 439.30322, Intensity =51, },
        //            new SpectrumPeak { Mass = 439.38251, Intensity =58, },
        //            new SpectrumPeak { Mass = 441.29401, Intensity =119, },
        //            new SpectrumPeak { Mass = 441.31964, Intensity =63, },
        //            new SpectrumPeak { Mass = 442.32031, Intensity =86, },
        //            new SpectrumPeak { Mass = 443.40646, Intensity =97, },
        //            new SpectrumPeak { Mass = 444.28326, Intensity =80, },
        //            new SpectrumPeak { Mass = 444.33911, Intensity =387, },
        //            new SpectrumPeak { Mass = 445.33047, Intensity =109, },
        //            new SpectrumPeak { Mass = 449.24564, Intensity =53, },
        //            new SpectrumPeak { Mass = 453.25113, Intensity =84, },
        //            new SpectrumPeak { Mass = 454.89288, Intensity =209, },
        //            new SpectrumPeak { Mass = 455.38763, Intensity =92, },
        //            new SpectrumPeak { Mass = 462.94141, Intensity =81, },
        //            new SpectrumPeak { Mass = 465.29663, Intensity =70, },
        //            new SpectrumPeak { Mass = 465.33392, Intensity =560, },
        //            new SpectrumPeak { Mass = 465.38678, Intensity =56, },
        //            new SpectrumPeak { Mass = 466.34833, Intensity =152, },
        //            new SpectrumPeak { Mass = 467.25491, Intensity =52, },
        //            new SpectrumPeak { Mass = 467.32782, Intensity =84, },
        //            new SpectrumPeak { Mass = 467.40494, Intensity =53, },
        //            new SpectrumPeak { Mass = 468.33157, Intensity =65, },
        //            new SpectrumPeak { Mass = 469.08234, Intensity =97, },
        //            new SpectrumPeak { Mass = 469.3306, Intensity =492, },
        //            new SpectrumPeak { Mass = 470.35626, Intensity =98, },
        //            new SpectrumPeak { Mass = 475.97495, Intensity =92, },
        //            new SpectrumPeak { Mass = 476.98706, Intensity =132, },
        //            new SpectrumPeak { Mass = 477.98978, Intensity =155, },
        //            new SpectrumPeak { Mass = 478.98267, Intensity =70, },
        //            new SpectrumPeak { Mass = 479.99652, Intensity =52, },
        //            new SpectrumPeak { Mass = 481.25555, Intensity =62, },
        //            new SpectrumPeak { Mass = 482.37775, Intensity =66, },
        //            new SpectrumPeak { Mass = 483.14398, Intensity =73, },
        //            new SpectrumPeak { Mass = 483.33325, Intensity =163, },
        //            new SpectrumPeak { Mass = 483.37549, Intensity =254, },
        //            new SpectrumPeak { Mass = 483.90591, Intensity =126, },
        //            new SpectrumPeak { Mass = 484.37054, Intensity =141, },
        //            new SpectrumPeak { Mass = 484.40228, Intensity =92, },
        //            new SpectrumPeak { Mass = 485.24063, Intensity =50, },
        //            new SpectrumPeak { Mass = 485.45432, Intensity =604, },
        //            new SpectrumPeak { Mass = 486.44427, Intensity =154, },
        //            new SpectrumPeak { Mass = 486.46445, Intensity =90, },
        //            new SpectrumPeak { Mass = 487.10611, Intensity =68, },
        //            new SpectrumPeak { Mass = 489.05768, Intensity =50, },
        //            new SpectrumPeak { Mass = 489.34631, Intensity =51, },
        //            new SpectrumPeak { Mass = 491.27127, Intensity =115, },
        //            new SpectrumPeak { Mass = 491.3299, Intensity =91, },
        //            new SpectrumPeak { Mass = 491.36734, Intensity =1248, },
        //            new SpectrumPeak { Mass = 491.38647, Intensity =72, },
        //            new SpectrumPeak { Mass = 491.47333, Intensity =56, },
        //            new SpectrumPeak { Mass = 492.27005, Intensity =53, },
        //            new SpectrumPeak { Mass = 492.37024, Intensity =565, },
        //            new SpectrumPeak { Mass = 494.2587, Intensity =53, },
        //            new SpectrumPeak { Mass = 495.2128, Intensity =59, },
        //            new SpectrumPeak { Mass = 497.19952, Intensity =51, },
        //            new SpectrumPeak { Mass = 497.38904, Intensity =246, },
        //            new SpectrumPeak { Mass = 500.38376, Intensity =54, },
        //            new SpectrumPeak { Mass = 503.29623, Intensity =52, },
        //            new SpectrumPeak { Mass = 504.33401, Intensity =448, },
        //            new SpectrumPeak { Mass = 507.26346, Intensity =79, },
        //            new SpectrumPeak { Mass = 508.37625, Intensity =98, },
        //            new SpectrumPeak { Mass = 509.20496, Intensity =229, },
        //            new SpectrumPeak { Mass = 509.26892, Intensity =62, },
        //            new SpectrumPeak { Mass = 509.37869, Intensity =86, },
        //            new SpectrumPeak { Mass = 510.21548, Intensity =173, },
        //            new SpectrumPeak { Mass = 510.25629, Intensity =72, },
        //            new SpectrumPeak { Mass = 510.38602, Intensity =129, },
        //            new SpectrumPeak { Mass = 511.21014, Intensity =89, },
        //            new SpectrumPeak { Mass = 511.38498, Intensity =114, },
        //            new SpectrumPeak { Mass = 512.22369, Intensity =107, },
        //            new SpectrumPeak { Mass = 512.43774, Intensity =65, },
        //            new SpectrumPeak { Mass = 515.35449, Intensity =89, },
        //            new SpectrumPeak { Mass = 517.37109, Intensity =61, },
        //            new SpectrumPeak { Mass = 524.3689, Intensity =57, },
        //            new SpectrumPeak { Mass = 524.39722, Intensity =65, },
        //            new SpectrumPeak { Mass = 538.49677, Intensity =60, },
        //            new SpectrumPeak { Mass = 540.40454, Intensity =64, },
        //            new SpectrumPeak { Mass = 542.41449, Intensity =170, },
        //            new SpectrumPeak { Mass = 543.49243, Intensity =328, },
        //            new SpectrumPeak { Mass = 544.49805, Intensity =194, },
        //            new SpectrumPeak { Mass = 545.50775, Intensity =87, },
        //            new SpectrumPeak { Mass = 547.4129, Intensity =67, },
        //            new SpectrumPeak { Mass = 549.47766, Intensity =77, },
        //            new SpectrumPeak { Mass = 550.02167, Intensity =60, },
        //            new SpectrumPeak { Mass = 550.47369, Intensity =128, },
        //            new SpectrumPeak { Mass = 551.01697, Intensity =151, },
        //            new SpectrumPeak { Mass = 551.98389, Intensity =80, },
        //            new SpectrumPeak { Mass = 552.02234, Intensity =121, },
        //            new SpectrumPeak { Mass = 553.00208, Intensity =84, },
        //            new SpectrumPeak { Mass = 554.00043, Intensity =156, },
        //            new SpectrumPeak { Mass = 559.48523, Intensity =89, },
        //            new SpectrumPeak { Mass = 561.45288, Intensity =51, },
        //            new SpectrumPeak { Mass = 561.51501, Intensity =63, },
        //            new SpectrumPeak { Mass = 563.05621, Intensity =127, },
        //            new SpectrumPeak { Mass = 563.36194, Intensity =78, },
        //            new SpectrumPeak { Mass = 564.05914, Intensity =74, },
        //            new SpectrumPeak { Mass = 564.08551, Intensity =107, },
        //            new SpectrumPeak { Mass = 565.06702, Intensity =157, },
        //            new SpectrumPeak { Mass = 566.05414, Intensity =246, },
        //            new SpectrumPeak { Mass = 566.46515, Intensity =63, },
        //            new SpectrumPeak { Mass = 567.05865, Intensity =230, },
        //            new SpectrumPeak { Mass = 568.07446, Intensity =80, },
        //            new SpectrumPeak { Mass = 568.5379, Intensity =58, },
        //            new SpectrumPeak { Mass = 570.96802, Intensity =102, },
        //            new SpectrumPeak { Mass = 574.58807, Intensity =700, },
        //            new SpectrumPeak { Mass = 574.61591, Intensity =77, },
        //            new SpectrumPeak { Mass = 575.34747, Intensity =89, },
        //            new SpectrumPeak { Mass = 575.48962, Intensity =720, },
        //            new SpectrumPeak { Mass = 575.56708, Intensity =235, },
        //            new SpectrumPeak { Mass = 575.59021, Intensity =136, },
        //            new SpectrumPeak { Mass = 576.49182, Intensity =434, },
        //            new SpectrumPeak { Mass = 576.59351, Intensity =119, },
        //            new SpectrumPeak { Mass = 578.49829, Intensity =98, },
        //            new SpectrumPeak { Mass = 579.47662, Intensity =78, },
        //            new SpectrumPeak { Mass = 580.46295, Intensity =53, },
        //            new SpectrumPeak { Mass = 580.50952, Intensity =54, },
        //            new SpectrumPeak { Mass = 580.52557, Intensity =115, },
        //            new SpectrumPeak { Mass = 581.41718, Intensity =62, },
        //            new SpectrumPeak { Mass = 583.45959, Intensity =60, },
        //            new SpectrumPeak { Mass = 586.5379, Intensity =233, },
        //            new SpectrumPeak { Mass = 586.58661, Intensity =12186, },
        //            new SpectrumPeak { Mass = 586.63989, Intensity =72, },
        //            new SpectrumPeak { Mass = 587.54108, Intensity =65, },
        //            new SpectrumPeak { Mass = 587.5874, Intensity =4821, },
        //            new SpectrumPeak { Mass = 587.62317, Intensity =298, },
        //            new SpectrumPeak { Mass = 587.98291, Intensity =53, },
        //            new SpectrumPeak { Mass = 588.55078, Intensity =189, },
        //            new SpectrumPeak { Mass = 588.6001, Intensity =2417, },
        //            new SpectrumPeak { Mass = 588.72809, Intensity =52, },
        //            new SpectrumPeak { Mass = 589.59888, Intensity =686, },
        //            new SpectrumPeak { Mass = 589.61792, Intensity =78, },
        //            new SpectrumPeak { Mass = 591.49225, Intensity =489, },
        //            new SpectrumPeak { Mass = 592.48157, Intensity =173, },
        //            new SpectrumPeak { Mass = 593.47858, Intensity =100, },
        //            new SpectrumPeak { Mass = 598.44666, Intensity =97, },
        //            new SpectrumPeak { Mass = 599.45807, Intensity =122, },
        //            new SpectrumPeak { Mass = 600.48822, Intensity =65, },
        //            new SpectrumPeak { Mass = 601.47937, Intensity =779, },
        //            new SpectrumPeak { Mass = 601.52008, Intensity =230, },
        //            new SpectrumPeak { Mass = 602.48682, Intensity =324, },
        //            new SpectrumPeak { Mass = 602.52747, Intensity =207, },
        //            new SpectrumPeak { Mass = 603.44952, Intensity =99, },
        //            new SpectrumPeak { Mass = 603.47107, Intensity =176, },
        //            new SpectrumPeak { Mass = 603.49249, Intensity =62, },
        //            new SpectrumPeak { Mass = 604.48352, Intensity =85, },
        //            new SpectrumPeak { Mass = 604.52856, Intensity =65, },
        //            new SpectrumPeak { Mass = 604.5946, Intensity =3355, },
        //            new SpectrumPeak { Mass = 604.65289, Intensity =179, },
        //            new SpectrumPeak { Mass = 605.48071, Intensity =75, },
        //            new SpectrumPeak { Mass = 605.53931, Intensity =55, },
        //            new SpectrumPeak { Mass = 605.60327, Intensity =1273, },
        //            new SpectrumPeak { Mass = 605.64337, Intensity =84, },
        //            new SpectrumPeak { Mass = 606.53076, Intensity =140, },
        //            new SpectrumPeak { Mass = 606.58978, Intensity =232, },
        //            new SpectrumPeak { Mass = 606.6087, Intensity =59, },
        //            new SpectrumPeak { Mass = 607.51459, Intensity =103, },
        //            new SpectrumPeak { Mass = 607.53088, Intensity =89, },
        //            new SpectrumPeak { Mass = 607.55261, Intensity =663, },
        //            new SpectrumPeak { Mass = 608.50769, Intensity =68, },
        //            new SpectrumPeak { Mass = 608.56189, Intensity =139, },
        //            new SpectrumPeak { Mass = 609.50775, Intensity =55, },
        //            new SpectrumPeak { Mass = 612.58752, Intensity =190, },
        //            new SpectrumPeak { Mass = 617.45532, Intensity =86, },
        //            new SpectrumPeak { Mass = 617.50269, Intensity =552, },
        //            new SpectrumPeak { Mass = 617.58807, Intensity =94, },
        //            new SpectrumPeak { Mass = 619.47705, Intensity =55, },
        //            new SpectrumPeak { Mass = 620.53705, Intensity =136, },
        //            new SpectrumPeak { Mass = 621.5658, Intensity =155, },
        //            new SpectrumPeak { Mass = 622.56042, Intensity =60, },
        //            new SpectrumPeak { Mass = 623.50037, Intensity =66, },
        //            new SpectrumPeak { Mass = 625.51147, Intensity =129, },
        //            new SpectrumPeak { Mass = 627.53204, Intensity =178, },
        //            new SpectrumPeak { Mass = 630.55206, Intensity =143, },
        //            new SpectrumPeak { Mass = 630.57825, Intensity =228, },
        //            new SpectrumPeak { Mass = 633.57153, Intensity =677, },
        //            new SpectrumPeak { Mass = 634.55231, Intensity =161, },
        //            new SpectrumPeak { Mass = 635.36426, Intensity =114, },
        //            new SpectrumPeak { Mass = 637.08521, Intensity =135, },
        //            new SpectrumPeak { Mass = 637.29028, Intensity =97, },
        //            new SpectrumPeak { Mass = 637.36829, Intensity =105, },
        //            new SpectrumPeak { Mass = 638.02728, Intensity =86, },
        //            new SpectrumPeak { Mass = 638.08606, Intensity =155, },
        //            new SpectrumPeak { Mass = 638.34265, Intensity =96, },
        //            new SpectrumPeak { Mass = 638.53662, Intensity =79, },
        //            new SpectrumPeak { Mass = 638.57794, Intensity =4385, },
        //            new SpectrumPeak { Mass = 639.08673, Intensity =415, },
        //            new SpectrumPeak { Mass = 639.56561, Intensity =1202, },
        //            new SpectrumPeak { Mass = 639.58142, Intensity =1023, },
        //            new SpectrumPeak { Mass = 639.60846, Intensity =107, },
        //            new SpectrumPeak { Mass = 639.64496, Intensity =79, },
        //            new SpectrumPeak { Mass = 640.06927, Intensity =298, },
        //            new SpectrumPeak { Mass = 640.57629, Intensity =831, },
        //            new SpectrumPeak { Mass = 641.08545, Intensity =241, },
        //            new SpectrumPeak { Mass = 641.54199, Intensity =107, },
        //            new SpectrumPeak { Mass = 642.0874, Intensity =98, },
        //            new SpectrumPeak { Mass = 642.58478, Intensity =84, },
        //            new SpectrumPeak { Mass = 646.54315, Intensity =54, },
        //            new SpectrumPeak { Mass = 651.57373, Intensity =66, },
        //            new SpectrumPeak { Mass = 653.13605, Intensity =123, },
        //            new SpectrumPeak { Mass = 653.31531, Intensity =103, },
        //            new SpectrumPeak { Mass = 654.1507, Intensity =69, },
        //            new SpectrumPeak { Mass = 656.51422, Intensity =58, },
        //            new SpectrumPeak { Mass = 657.48413, Intensity =110, },
        //            new SpectrumPeak { Mass = 659.44641, Intensity =58, },
        //            new SpectrumPeak { Mass = 659.52991, Intensity =79, },
        //            new SpectrumPeak { Mass = 659.56842, Intensity =283, },
        //            new SpectrumPeak { Mass = 660.46497, Intensity =136, },
        //            new SpectrumPeak { Mass = 660.5592, Intensity =288, },
        //            new SpectrumPeak { Mass = 661.43585, Intensity =570, },
        //            new SpectrumPeak { Mass = 662.43158, Intensity =138, },
        //            new SpectrumPeak { Mass = 666.53955, Intensity =75, },
        //            new SpectrumPeak { Mass = 667.55127, Intensity =82, },
        //            new SpectrumPeak { Mass = 668.37305, Intensity =88, },
        //            new SpectrumPeak { Mass = 674.5434, Intensity =73, },
        //            new SpectrumPeak { Mass = 676.54956, Intensity =318, },
        //            new SpectrumPeak { Mass = 676.60199, Intensity =142, },
        //            new SpectrumPeak { Mass = 677.52087, Intensity =74, },
        //            new SpectrumPeak { Mass = 677.60486, Intensity =121, },
        //            new SpectrumPeak { Mass = 678.59692, Intensity =80, },
        //            new SpectrumPeak { Mass = 679.54816, Intensity =93, },
        //            new SpectrumPeak { Mass = 680.48645, Intensity =76, },
        //            new SpectrumPeak { Mass = 681.45856, Intensity =76, },
        //            new SpectrumPeak { Mass = 685.47565, Intensity =91, },
        //            new SpectrumPeak { Mass = 686.54791, Intensity =54, },
        //            new SpectrumPeak { Mass = 688.50476, Intensity =82, },
        //            new SpectrumPeak { Mass = 692.5929, Intensity =136, },
        //            new SpectrumPeak { Mass = 695.59161, Intensity =62, },
        //            new SpectrumPeak { Mass = 703.49115, Intensity =71, },
        //            new SpectrumPeak { Mass = 703.53235, Intensity =108, },
        //            new SpectrumPeak { Mass = 703.55402, Intensity =65, },
        //            new SpectrumPeak { Mass = 704.48633, Intensity =171, },
        //            new SpectrumPeak { Mass = 704.54962, Intensity =197, },
        //            new SpectrumPeak { Mass = 704.60144, Intensity =188, },
        //            new SpectrumPeak { Mass = 705.42517, Intensity =115, },
        //            new SpectrumPeak { Mass = 705.46057, Intensity =233, },
        //            new SpectrumPeak { Mass = 705.47974, Intensity =52, },
        //            new SpectrumPeak { Mass = 705.6167, Intensity =133, },
        //            new SpectrumPeak { Mass = 706.45447, Intensity =184, },
        //            new SpectrumPeak { Mass = 707.3692, Intensity =102, },
        //            new SpectrumPeak { Mass = 707.53693, Intensity =74, },
        //            new SpectrumPeak { Mass = 709.68561, Intensity =50, },
        //            new SpectrumPeak { Mass = 711.69336, Intensity =75, },
        //            new SpectrumPeak { Mass = 711.71161, Intensity =59, },
        //            new SpectrumPeak { Mass = 713.67444, Intensity =80, },
        //            new SpectrumPeak { Mass = 713.71167, Intensity =62, },
        //            new SpectrumPeak { Mass = 714.52899, Intensity =145, },
        //            new SpectrumPeak { Mass = 714.56494, Intensity =94, },
        //            new SpectrumPeak { Mass = 714.70093, Intensity =73, },
        //            new SpectrumPeak { Mass = 715.49023, Intensity =99, },
        //            new SpectrumPeak { Mass = 717.56354, Intensity =373, },
        //            new SpectrumPeak { Mass = 717.60602, Intensity =164, },
        //            new SpectrumPeak { Mass = 717.63855, Intensity =76, },
        //            new SpectrumPeak { Mass = 718.59302, Intensity =65, },
        //            new SpectrumPeak { Mass = 718.61859, Intensity =73, },
        //            new SpectrumPeak { Mass = 719.41119, Intensity =72, },
        //            new SpectrumPeak { Mass = 719.56549, Intensity =147, },
        //            new SpectrumPeak { Mass = 720.55151, Intensity =55, },
        //            new SpectrumPeak { Mass = 720.63483, Intensity =84, },
        //            new SpectrumPeak { Mass = 721.40784, Intensity =259, },
        //            new SpectrumPeak { Mass = 721.50214, Intensity =177, },
        //            new SpectrumPeak { Mass = 723.3891, Intensity =120, },
        //            new SpectrumPeak { Mass = 723.50958, Intensity =102, },
        //            new SpectrumPeak { Mass = 724.03442, Intensity =56, },
        //            new SpectrumPeak { Mass = 724.52264, Intensity =67, },
        //            new SpectrumPeak { Mass = 725.08167, Intensity =81, },
        //            new SpectrumPeak { Mass = 725.15765, Intensity =240, },
        //            new SpectrumPeak { Mass = 725.362, Intensity =73, },
        //            new SpectrumPeak { Mass = 726.17029, Intensity =454, },
        //            new SpectrumPeak { Mass = 726.74615, Intensity =54, },
        //            new SpectrumPeak { Mass = 727.15277, Intensity =654, },
        //            new SpectrumPeak { Mass = 727.52051, Intensity =117, },
        //            new SpectrumPeak { Mass = 727.64185, Intensity =85, },
        //            new SpectrumPeak { Mass = 728.15796, Intensity =713, },
        //            new SpectrumPeak { Mass = 728.2312, Intensity =54, },
        //            new SpectrumPeak { Mass = 728.58911, Intensity =55, },
        //            new SpectrumPeak { Mass = 729.15686, Intensity =415, },
        //            new SpectrumPeak { Mass = 729.198, Intensity =62, },
        //            new SpectrumPeak { Mass = 729.52704, Intensity =54, },
        //            new SpectrumPeak { Mass = 729.63666, Intensity =145, },
        //            new SpectrumPeak { Mass = 729.66791, Intensity =145, },
        //            new SpectrumPeak { Mass = 730.12024, Intensity =135, },
        //            new SpectrumPeak { Mass = 730.16803, Intensity =122, },
        //            new SpectrumPeak { Mass = 730.49915, Intensity =73, },
        //            new SpectrumPeak { Mass = 730.73157, Intensity =58, },
        //            new SpectrumPeak { Mass = 731.13116, Intensity =128, },
        //            new SpectrumPeak { Mass = 731.573, Intensity =328, },
        //            new SpectrumPeak { Mass = 731.66321, Intensity =60, },
        //            new SpectrumPeak { Mass = 732.54321, Intensity =119, },
        //            new SpectrumPeak { Mass = 732.58765, Intensity =309, },
        //            new SpectrumPeak { Mass = 733.54102, Intensity =52, },
        //            new SpectrumPeak { Mass = 733.57928, Intensity =72, },
        //            new SpectrumPeak { Mass = 733.62317, Intensity =72, },
        //            new SpectrumPeak { Mass = 734.49585, Intensity =57, },
        //            new SpectrumPeak { Mass = 734.56934, Intensity =103, },
        //            new SpectrumPeak { Mass = 734.63605, Intensity =175, },
        //            new SpectrumPeak { Mass = 734.66431, Intensity =87, },
        //            new SpectrumPeak { Mass = 735.61237, Intensity =110, },
        //            new SpectrumPeak { Mass = 735.65863, Intensity =104, },
        //            new SpectrumPeak { Mass = 736.64624, Intensity =96, },
        //            new SpectrumPeak { Mass = 739.38116, Intensity =127, },
        //            new SpectrumPeak { Mass = 739.41455, Intensity =178, },
        //            new SpectrumPeak { Mass = 741.16718, Intensity =90, },
        //            new SpectrumPeak { Mass = 742.17084, Intensity =257, },
        //            new SpectrumPeak { Mass = 743.18939, Intensity =432, },
        //            new SpectrumPeak { Mass = 743.2157, Intensity =54, },
        //            new SpectrumPeak { Mass = 744.18677, Intensity =324, },
        //            new SpectrumPeak { Mass = 744.63843, Intensity =61, },
        //            new SpectrumPeak { Mass = 745.18207, Intensity =310, },
        //            new SpectrumPeak { Mass = 745.23236, Intensity =59, },
        //            new SpectrumPeak { Mass = 745.48676, Intensity =92, },
        //            new SpectrumPeak { Mass = 745.5033, Intensity =83, },
        //            new SpectrumPeak { Mass = 745.56506, Intensity =211, },
        //            new SpectrumPeak { Mass = 745.58942, Intensity =76, },
        //            new SpectrumPeak { Mass = 745.70776, Intensity =68, },
        //            new SpectrumPeak { Mass = 745.73987, Intensity =161, },
        //            new SpectrumPeak { Mass = 746.1795, Intensity =72, },
        //            new SpectrumPeak { Mass = 746.52051, Intensity =178, },
        //            new SpectrumPeak { Mass = 746.53851, Intensity =54, },
        //            new SpectrumPeak { Mass = 746.55542, Intensity =118, },
        //            new SpectrumPeak { Mass = 746.57477, Intensity =56, },
        //            new SpectrumPeak { Mass = 746.633, Intensity =77, },
        //            new SpectrumPeak { Mass = 747.48102, Intensity =192, },
        //            new SpectrumPeak { Mass = 747.49805, Intensity =151, },
        //            new SpectrumPeak { Mass = 747.52289, Intensity =86, },
        //            new SpectrumPeak { Mass = 747.56073, Intensity =73, },
        //            new SpectrumPeak { Mass = 747.59607, Intensity =141, },
        //            new SpectrumPeak { Mass = 748.44812, Intensity =64, },
        //            new SpectrumPeak { Mass = 748.47247, Intensity =127, },
        //            new SpectrumPeak { Mass = 748.54309, Intensity =188, },
        //            new SpectrumPeak { Mass = 749.46277, Intensity =80, },
        //            new SpectrumPeak { Mass = 749.50281, Intensity =174, },
        //            new SpectrumPeak { Mass = 750.65002, Intensity =151, },
        //            new SpectrumPeak { Mass = 751.55585, Intensity =86, },
        //            new SpectrumPeak { Mass = 751.64752, Intensity =102, },
        //            new SpectrumPeak { Mass = 752.68518, Intensity =58, },
        //            new SpectrumPeak { Mass = 755.09967, Intensity =60, },
        //            new SpectrumPeak { Mass = 758.51562, Intensity =51, },
        //            new SpectrumPeak { Mass = 759.25012, Intensity =54, },
        //            new SpectrumPeak { Mass = 759.31744, Intensity =58, },
        //            new SpectrumPeak { Mass = 759.42584, Intensity =60, },
        //            new SpectrumPeak { Mass = 759.54126, Intensity =291, },
        //            new SpectrumPeak { Mass = 760.16888, Intensity =50, },
        //            new SpectrumPeak { Mass = 760.24652, Intensity =292, },
        //            new SpectrumPeak { Mass = 760.26556, Intensity =52, },
        //            new SpectrumPeak { Mass = 760.57806, Intensity =874, },
        //            new SpectrumPeak { Mass = 760.63989, Intensity =127, },
        //            new SpectrumPeak { Mass = 760.9079, Intensity =186, },
        //            new SpectrumPeak { Mass = 760.92841, Intensity =182, },
        //            new SpectrumPeak { Mass = 761.25537, Intensity =51, },
        //            new SpectrumPeak { Mass = 761.48492, Intensity =248, },
        //            new SpectrumPeak { Mass = 761.51849, Intensity =235, },
        //            new SpectrumPeak { Mass = 761.57697, Intensity =478, },
        //            new SpectrumPeak { Mass = 761.63452, Intensity =389, },
        //            new SpectrumPeak { Mass = 761.92511, Intensity =55, },
        //            new SpectrumPeak { Mass = 761.99457, Intensity =51, },
        //            new SpectrumPeak { Mass = 762.13562, Intensity =82, },
        //            new SpectrumPeak { Mass = 762.57172, Intensity =36485, },
        //            new SpectrumPeak { Mass = 763.18036, Intensity =273, },
        //            new SpectrumPeak { Mass = 763.50037, Intensity =93, },
        //            new SpectrumPeak { Mass = 763.57404, Intensity =18076, },
        //            new SpectrumPeak { Mass = 764.15875, Intensity =91, },
        //            new SpectrumPeak { Mass = 764.18842, Intensity =74, },
        //            new SpectrumPeak { Mass = 764.36578, Intensity =57, },
        //            new SpectrumPeak { Mass = 764.50702, Intensity =379, },
        //            new SpectrumPeak { Mass = 764.57251, Intensity =4907, },
        //            new SpectrumPeak { Mass = 764.60425, Intensity =254, },
        //            new SpectrumPeak { Mass = 764.64258, Intensity =53, },
        //            new SpectrumPeak { Mass = 765.10492, Intensity =229, },
        //            new SpectrumPeak { Mass = 765.12823, Intensity =322, },
        //            new SpectrumPeak { Mass = 765.17792, Intensity =83, },
        //            new SpectrumPeak { Mass = 765.32391, Intensity =83, },
        //            new SpectrumPeak { Mass = 765.39099, Intensity =228, },
        //            new SpectrumPeak { Mass = 765.54388, Intensity =806, },
        //            new SpectrumPeak { Mass = 765.5882, Intensity =972, },
        //            new SpectrumPeak { Mass = 765.63287, Intensity =265, },
        //            new SpectrumPeak { Mass = 765.69501, Intensity =131, },
        //            new SpectrumPeak { Mass = 765.87573, Intensity =56, },
        //            new SpectrumPeak { Mass = 765.91418, Intensity =602, },
        //            new SpectrumPeak { Mass = 765.93652, Intensity =54, },
        //            new SpectrumPeak { Mass = 766.08405, Intensity =79, },
        //            new SpectrumPeak { Mass = 766.12421, Intensity =263, },
        //            new SpectrumPeak { Mass = 766.16241, Intensity =90, },
        //            new SpectrumPeak { Mass = 766.2641, Intensity =608, },
        //            new SpectrumPeak { Mass = 766.32013, Intensity =52, },
        //            new SpectrumPeak { Mass = 766.65057, Intensity =104516, },
        //            new SpectrumPeak { Mass = 766.93176, Intensity =79, },
        //            new SpectrumPeak { Mass = 767.14044, Intensity =249, },
        //            new SpectrumPeak { Mass = 767.56604, Intensity =186, },
        //            new SpectrumPeak { Mass = 767.65356, Intensity =52672, },
        //            new SpectrumPeak { Mass = 768.12476, Intensity =64, },
        //            new SpectrumPeak { Mass = 768.16119, Intensity =72, },
        //            new SpectrumPeak { Mass = 768.24939, Intensity =102, },
        //            new SpectrumPeak { Mass = 768.33466, Intensity =103, },
        //            new SpectrumPeak { Mass = 768.49664, Intensity =111, },
        //            new SpectrumPeak { Mass = 768.65796, Intensity =8305, },
        //            new SpectrumPeak { Mass = 769.11053, Intensity =87, },
        //            new SpectrumPeak { Mass = 769.15863, Intensity =113, },
        //            new SpectrumPeak { Mass = 769.41895, Intensity =101, },
        //            new SpectrumPeak { Mass = 769.66925, Intensity =22067, },
        //            new SpectrumPeak { Mass = 770.31873, Intensity =80, },
        //            new SpectrumPeak { Mass = 770.3811, Intensity =82, },
        //            new SpectrumPeak { Mass = 770.67126, Intensity =7036, },
        //            new SpectrumPeak { Mass = 770.71545, Intensity =304, },
        //            new SpectrumPeak { Mass = 771.30322, Intensity =60, },
        //            new SpectrumPeak { Mass = 771.33002, Intensity =58, },
        //            new SpectrumPeak { Mass = 771.43127, Intensity =51, },
        //            new SpectrumPeak { Mass = 771.47168, Intensity =52, },
        //            #endregion
        //        }
        //    };
        //    var totalCarbon = 40;
        //    var totalDbBond = 1;
        //    var totalOxidized = 2;
        //    var sn1Carbon = 18;
        //    var sn1DbBond = 1;
        //    var sn2Carbon = 22;
        //    var sn2DbBond = 0;

        //    //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
        //    //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        //    //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
        //    //AdductIon adduct)

        //    var result = LipidMsmsCharacterization.JudgeIfHexceramidens(target, 0.025,
        //        766.65181f, totalCarbon, totalDbBond,
        //                 sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
        //                 adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
        //    Console.WriteLine($"HexCer_NS test (HexCer 40:1;O2|HexCer 18:1;O2/22:0)");
        //    Console.WriteLine($"[M+H-H2O]+");
        //    Console.WriteLine($"LipidName:{result.LipidName}");
        //    Console.WriteLine($"AnnotationLevel:{result.AnnotationLevel}");
        //    //
        //}
        //[TestMethod()]
        //public void HexCeramideHSTest2()
        //{
        //    //HexCer 18:1;2O/16:0;O
        //    var target = new MSScanProperty
        //    {
        //        PrecursorMz = 698.55462,
        //        Spectrum = new List<SpectrumPeak>
        //        {
        //            #region
        //                new SpectrumPeak { Mass = 101.05642, Intensity =62, },
        //                new SpectrumPeak { Mass = 107.08813, Intensity =78, },
        //                new SpectrumPeak { Mass = 121.10245, Intensity =65, },
        //                new SpectrumPeak { Mass = 123.08045, Intensity =72, },
        //                new SpectrumPeak { Mass = 139.06358, Intensity =73, },
        //                new SpectrumPeak { Mass = 140.11794, Intensity =103, },
        //                new SpectrumPeak { Mass = 169.08241, Intensity =83, },
        //                new SpectrumPeak { Mass = 175.09798, Intensity =55, },
        //                new SpectrumPeak { Mass = 199.08498, Intensity =64, },
        //                new SpectrumPeak { Mass = 215.12363, Intensity =61, },
        //                new SpectrumPeak { Mass = 223.03363, Intensity =109, },
        //                new SpectrumPeak { Mass = 234.17598, Intensity =63, },
        //                new SpectrumPeak { Mass = 249.17657, Intensity =67, },
        //                new SpectrumPeak { Mass = 264.24933, Intensity =196, },
        //                new SpectrumPeak { Mass = 276.27084, Intensity =99, },
        //                new SpectrumPeak { Mass = 293.17593, Intensity =54, },
        //                new SpectrumPeak { Mass = 319.28583, Intensity =89, },
        //                new SpectrumPeak { Mass = 341.22067, Intensity =67, },
        //                new SpectrumPeak { Mass = 386.28262, Intensity =74, },
        //                new SpectrumPeak { Mass = 403.32629, Intensity =56, },
        //                new SpectrumPeak { Mass = 465.30136, Intensity =64, },
        //                new SpectrumPeak { Mass = 503.28088, Intensity =111, },
        //                new SpectrumPeak { Mass = 506.49402, Intensity =141, },
        //                new SpectrumPeak { Mass = 518.49133, Intensity =756, },
        //                new SpectrumPeak { Mass = 519.49133, Intensity =274, },
        //                new SpectrumPeak { Mass = 536.50012, Intensity =402, },
        //                new SpectrumPeak { Mass = 537.47791, Intensity =97, },
        //                new SpectrumPeak { Mass = 538.45569, Intensity =53, },
        //                new SpectrumPeak { Mass = 594.08771, Intensity =56, },
        //                new SpectrumPeak { Mass = 619.43262, Intensity =62, },
        //                new SpectrumPeak { Mass = 637.27747, Intensity =80, },
        //                new SpectrumPeak { Mass = 639.57446, Intensity =63, },
        //                new SpectrumPeak { Mass = 655.98645, Intensity =51, },
        //                new SpectrumPeak { Mass = 670.25201, Intensity =62, },
        //                new SpectrumPeak { Mass = 677.43817, Intensity =61, },
        //                new SpectrumPeak { Mass = 679.4151, Intensity =109, },
        //                new SpectrumPeak { Mass = 693.35797, Intensity =70, },
        //                new SpectrumPeak { Mass = 695.30438, Intensity =72, },
        //                new SpectrumPeak { Mass = 695.42383, Intensity =60, },
        //                new SpectrumPeak { Mass = 695.44525, Intensity =64, },
        //                new SpectrumPeak { Mass = 696.34753, Intensity =78, },
        //                new SpectrumPeak { Mass = 696.49036, Intensity =54, },
        //                new SpectrumPeak { Mass = 696.56622, Intensity =65, },
        //                new SpectrumPeak { Mass = 697.36548, Intensity =197, },
        //                new SpectrumPeak { Mass = 698.133, Intensity =53, },
        //                new SpectrumPeak { Mass = 698.30493, Intensity =201, },
        //                new SpectrumPeak { Mass = 698.47559, Intensity =88, },
        //                new SpectrumPeak { Mass = 698.5564, Intensity =846, },
        //                new SpectrumPeak { Mass = 699.39148, Intensity =188, },
        //                new SpectrumPeak { Mass = 699.45093, Intensity =127, },
        //                new SpectrumPeak { Mass = 699.50494, Intensity =120, },
        //                new SpectrumPeak { Mass = 699.54968, Intensity =380, },
        //                new SpectrumPeak { Mass = 700.39594, Intensity =65, },
        //                new SpectrumPeak { Mass = 700.47443, Intensity =95, },
        //                new SpectrumPeak { Mass = 701.43549, Intensity =123, },
        //                new SpectrumPeak { Mass = 701.48291, Intensity =64, },
        //                new SpectrumPeak { Mass = 701.53882, Intensity =94, },
        //                new SpectrumPeak { Mass = 701.59198, Intensity =130, },
        //                new SpectrumPeak { Mass = 702.40356, Intensity =160, },
        //                new SpectrumPeak { Mass = 702.42505, Intensity =52, },
        //            #endregion
        //        }
        //    };
        //    var totalCarbon = 34;
        //    var totalDbBond = 1;
        //    var totalOxidized = 3;
        //    var sn1Carbon = 18;
        //    var sn1DbBond = 1;
        //    var sn2Carbon = 16;
        //    var sn2DbBond = 0;

        //    //public static LipidMolecule JudgeIfHexceramidens(IMSScanProperty msScanProp, double ms2Tolerance,
        //    //double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        //    //int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond,
        //    //AdductIon adduct)

        //    var result = LipidMsmsCharacterization.JudgeIfHexceramideo(target, 0.025,
        //        698.5565f, totalCarbon, totalDbBond,
        //                 sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
        //                 adduct = AdductIon.GetAdductIon("[M+H-H2O]+"));
        //    Console.WriteLine($"HexCer_HS test (HexCer 18:1;2O/16:0;O)");
        //    Console.WriteLine($"[M+H-H2O]+");
        //    Console.WriteLine($"LipidName:{result.LipidName}");
        //    Console.WriteLine($"AnnotationLevel:{result.AnnotationLevel}");
        //    //
        //}
        #endregion

    }
 
}
