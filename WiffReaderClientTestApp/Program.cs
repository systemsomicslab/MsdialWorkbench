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

            using (var rawDataAccess = new RawDataAccess(@"D:\PROJECT_Ohno_MetabolomeWorkingGroup\feces data\Additional Data_20210203\04_Pos\210202_TT1_Iclass_WK_Feces_002_A001_Pos.wiff", 0, false)) {
                rawDataAccess.DataDump(@"D:\PROJECT_Ohno_MetabolomeWorkingGroup\feces data\Additional Data_20210203\04_Pos\210202_TT1_Iclass_WK_Feces_002_A001_Pos.wiff");
                // rawDataAccess.GetMeasurement();
            }

            //using (var rawDataAccess = new RawDataAccess(@"D:\0_Code\MsdialWorkbenchDemo\wiff2\03  1.wiff2", 0, false)) {
            //    rawDataAccess.DataDump(@"D:\0_Code\MsdialWorkbenchDemo\wiff2\03  1.wiff2");
            //    // rawDataAccess.GetMeasurement();
            //}

            sw.Stop();
            Console.WriteLine($"Finish: {sw.ElapsedMilliseconds / 1000d} second");
            Console.ReadKey();
        }
    }
}
