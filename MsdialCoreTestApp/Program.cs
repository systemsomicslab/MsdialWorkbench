using CompMs.MsdialCore.Utility;
using System;

namespace MsdialCoreTestApp {
    class Program {
        static void Main(string[] args) {
            //var filepath = @"C:\Users\hiroshi.tsugawa\Desktop\MsdialTestData\SWATH_NEG\Nega_Swath_Chlamydomonas_1.mzML";
            var filepath = @"D:\PROJECT_SCIEX_MSMSALL\ABF\704_Egg2 Egg Yolk.abf";
            RawDataDump.Dump(filepath);
        }
    }
}
