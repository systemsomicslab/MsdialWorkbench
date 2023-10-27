using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {
    public sealed class PubChemRestApp {
        private PubChemRestApp() { }    

        public static void Run(string input) {
            var formulalist = new List<string>();
            using (var sr = new StreamReader(input)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    formulalist.Add(line);
                }
            }

            var pubrest = new PugRestProtocol();
            var downloadDirectory = @"C:\Users\hiros\Desktop\test\download";
            foreach (var formula in formulalist) {
                Console.WriteLine("{0} started.", formula);
                var newDir = Path.Combine(downloadDirectory, formula);
                if (!Directory.Exists(newDir)) {
                    Directory.CreateDirectory(newDir);
                }

                pubrest.SearchSdfByFormula(formula, newDir, 100, new List<int>());
            }
        }
    }
}
