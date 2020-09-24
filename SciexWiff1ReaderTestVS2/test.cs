using CompMs.RawDataHandler.Wiff1Net4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SciexWiff1ReaderTestVS2
{
    public class Test
    {
        public void run() {
            var file = @"D:\msdial_demofiles\Wiff1Swath\Posi_Swath_Chlamydomonas_1.wiff";
            var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(file, 0, 0, out _);
        }
    }
}
