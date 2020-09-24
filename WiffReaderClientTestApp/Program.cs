using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WiffReaderClientTestApp
{
    class Program
    {
        static void Main(string[] args) {

            var sw = new Stopwatch();
            sw.Start();
            
            /*
            using (var rawDataAccess = new RawDataAccess(@"D:\infusion_project\data\Riken_infusion_Data\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.wiff", 0, false)) {
                rawDataAccess.DataDump(@"D:\infusion_project\data\Riken_infusion_Data\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.wiff");
            }
            */
            /*
            using (var rawDataAccess = new RawDataAccess(@"\\mtbdt\Mtb_info\data\infusion_ms_project\sciex_msmsall\703_Egg2 Egg White.wiff", 0, false)) {
                rawDataAccess.DataDump(@"\\mtbdt\Mtb_info\data\infusion_ms_project\sciex_msmsall\703_Egg2 Egg White.wiff");
            }
            */
            using (var rawDataAccess = new RawDataAccess(@"D:\0_Programs\SDK\20141003-RDAM_NET4\20141003-RDAM_NET4\ABFSimpleConverters\ABFCvtSvrABWf_viaMzWiff\bin\Debug\testIn.wiff", 0, false)) {
                rawDataAccess.DataDump(@"D:\0_Programs\SDK\20141003-RDAM_NET4\20141003-RDAM_NET4\ABFSimpleConverters\ABFCvtSvrABWf_viaMzWiff\bin\Debug\testIn.wiff");
                // rawDataAccess.GetMeasurement();
            }

            sw.Stop();
            Console.WriteLine($"Finish: {sw.ElapsedMilliseconds / 1000d} second");
            Console.ReadKey();
        }
    }
}
