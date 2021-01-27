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
            //     , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri"
            //     , "-o"
            //     , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri"
            //     , "-m"
            //     , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri\gcmsparam_kovats.txt"
            //     , "-p" };

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
            args = new string[]
            {
                "dims"
                , "-i"
                , @"D:\msdial_test\Msdial\out\MSMSALL_Positive"
                , "-o"
                , @"D:\msdial_test\Msdial\out\MSMSALL_Positive"
                , "-m"
                , @"D:\msdial_test\Msdial\out\MSMSALL_Positive\dims_param.txt"
                , "-p"
            };
            MainProcess.Run(args);

            // RawDataDump.Dump(@"D:\infusion_project\data\abf\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.abf");
            // var allspectra = DataAccess.GetAllSpectra(@"D:\test_data\wine\0717_kinetex_wine_50_4min_pos_IDA_A1.abf");
            // Console.WriteLine("Scan number 2510");
            // foreach (var spec in allspectra[2510].Spectrum)
            //     Console.WriteLine($"Mass = {spec.Mz}, Intensity = {spec.Intensity}");
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
    }
}
