using CompMs.RawDataHandler.Wiff1Net4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciexWiff1ReaderTestApp {
    class Program {
        static void Main(string[] args) {
            new Wiff1ReaderDotNet4().ReadSciexWiff1Data(@"D:\msdial_demofiles\Wiff1Swath\Posi_Swath_Chlamydomonas_1.wiff",
                0, 0.0, out string errorString);
        }
    }
}
