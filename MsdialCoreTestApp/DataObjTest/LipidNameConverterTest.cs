using CompMs.Common.Lipidomics;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.App.MsdialConsole.DataObjTest {
    public sealed class LipidNameConverterTest {
        private LipidNameConverterTest() { }
        public static string Convert2SummedLipidNameTest(string lipidname, string ontology) { //TG 18:1_18:1_18:3
            var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(lipidname, ontology);
            return molecule.SublevelLipidName;
        }
    }
}
