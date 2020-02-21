using Msdial.Lcms.Dataprocess;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {
    public sealed class CcsCalculator {
        private CcsCalculator() { }

        //input [0] 1/k0, [1] formula [2] adduct
        public static void Run(string input, string output) {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Precursor m/z\tCCS");
                using (var sr = new StreamReader(input, Encoding.ASCII)) {
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;

                        var lineArray = line.Split('\t');
                        var dtString = lineArray[0];
                        var formulaString = lineArray[1];
                        var adductString = lineArray[2];

                        var dt = double.Parse(dtString);
                        var formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                        var adduct = AdductIonParcer.GetAdductIonBean(adductString);
                        var preMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adduct, formula.Mass);

                        var ccs = IonMobilityUtility.MobilityToCrossSection(IonMobilityType.Tims, dt, Math.Abs(adduct.ChargeNumber), preMz, null, true);

                        sw.WriteLine(preMz + "\t" + ccs);
                    }
                }
            }
        }
    }
}
