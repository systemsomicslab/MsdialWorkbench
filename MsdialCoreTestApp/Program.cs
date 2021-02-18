using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Linq;

namespace CompMs.App.MsdialConsole {
    class Program {
        static void Main(string[] args) {
            // gcms
            // args = new string[] {
            //     "gcms"
            //     , "-i"
            //     , @"D:\msdial_test\Msdial\out\GCMS"
            //     , "-o"
            //     , @"D:\msdial_test\Msdial\out\GCMS"
            //     , "-m"
            //     , @"D:\msdial_test\Msdial\out\GCMS\Msdial-GCMS-Param.txt"
            //     , "-p"
            // };

            // lcms
            // args = new string[]
            // {
            //     "lcms"
            //     , "-i"
            //     , @"D:\test_data\wine\"
            //     , "-o"
            //     , @"D:\test_data\wine\"
            //     , "-m"
            //     , @"D:\test_data\wine\lcms_param.txt"
            //     , "-p"
            // };

            // dims
            // args = new string[]
            // {
            //     "dims"
            //     , "-i"
            //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive"
            //     , "-o"
            //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive"
            //     , "-m"
            //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive\dims_param.txt"
            //     , "-p"
            // };

            // imms
            // args = new string[]
            // {
            //     "imms"
            //     , "-i"
            //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf"
            //     , "-o"
            //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf"
            //     , "-m"
            //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf\Msdial-imms-Param.txt"
            //     , "-p"
            // };

            // lcimms
            args = new string[] {
                "lcimms"
                , "-i"
                , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF"
                , "-o"
                , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF"
                , "-m"
                , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF\lcimms_param.txt"
                , "-p"
            };

            MainProcess.Run(args);

            var lcmsfile = @"D:\msdial_test\Msdial\out\wine\0717_kinetex_wine_50_4min_pos_IDA_A1.abf";
            var dimsfile = @"D:\msdial_test\Msdial\out\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.abf";
            var immsfile = @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef\kidney1_3times_timsON_pasef_neg000001.d";
            var lcimmsfile = @"D:\BugReport\20201216_MS2missing\PS78_Plasma1_4_1_4029.d";
            // Console.WriteLine("Lcms");
            // DumpN(lcmsfile, 50);
            // Console.WriteLine("Dims");
            // DumpN(dimsfile, 50);
            // Console.WriteLine("Imms");
            // DumpN(immsfile, 1000);
            // Console.WriteLine("LcImms");
            // DumpN(lcimmsfile, 1000);
            // RawDataDump.Dump(immsfile);
            // Console.WriteLine("Scan number 1359");
            // foreach (var spec in allspectra[1359].Spectrum)
            //     Console.WriteLine($"Mass = {spec.Mz}, Intensity = {spec.Intensity}");

            // var serializer = MsdialCore.Parser.ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            // var deserializer = MsdialCore.Parser.ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            // var spots = Enumerable.Range(0, 1000).Select(_ => new Common.Components.ChromXs()).ToArray();
            // var pss = new System.Collections.Generic.List<System.Collections.Generic.IEnumerable<MsdialCore.DataObj.ChromatogramPeakInfo>> {
            //     deserializer.DeserializeAllFromFile(@"D:\test_data\wine2\tmpC573.tmp"),
            //     deserializer.DeserializeAllFromFile(@"D:\test_data\wine2\tmp6C46.tmp"),
            // };
            // var qss = CompMs.Common.Extension.IEnumerableExtension.Sequence(pss);
            // serializer.SerializeNToFile(@"D:\test_data\wine2\ABCD.tmp", spots.Zip(qss, (spot, qs) => new MsdialCore.DataObj.ChromatogramSpotInfo(qs, spot)), spots.Length);
            // Console.ReadKey();
        }

        private static void DumpN(string file, int n) {
            var allspectra = DataAccess.GetAllSpectra(file);
            Console.WriteLine($"Number of spectrum: {allspectra.Count}");
            Console.WriteLine($"Number of Ms1 spectrum {allspectra.Count(spec => spec.MsLevel == 1)}"); 
            Console.WriteLine($"Number of scan {allspectra.Where(spec => spec.MsLevel == 1).Select(spec => spec.ScanNumber).Distinct().Count()}"); 
            for(int i = 0; i < n; i++) {
                var spec = allspectra[i];
                Console.WriteLine("Original index={0} ID={1}, Time={2}, Drift ID={3}, Drift time={4}, Polarity={5}, MS level={6}, Precursor mz={7}, CollisionEnergy={8}, SpecCount={9}",
                    spec.OriginalIndex, spec.ScanNumber, spec.ScanStartTime, spec.DriftScanNumber, spec.DriftTime, spec.ScanPolarity, spec.MsLevel, spec.Precursor?.SelectedIonMz ?? -1, spec.CollisionEnergy, spec.Spectrum.Length);
            }
        }
    }
}
