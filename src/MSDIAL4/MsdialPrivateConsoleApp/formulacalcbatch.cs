using CompMs.Common.FormulaGenerator.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {
    public sealed class formulacalcbatch {
        private formulacalcbatch() { }  
        public static void calc(string input, string output) {
            using (var sw = new StreamWriter(output)) {
                using (var sr = new StreamReader(input)) {
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        var formula = FormulaStringParcer.Convert2FormulaObjV2(line);
                        var mass = formula.Mass;
                        sw.WriteLine(mass); 
                    }
                }
            }
        }
    }
}
