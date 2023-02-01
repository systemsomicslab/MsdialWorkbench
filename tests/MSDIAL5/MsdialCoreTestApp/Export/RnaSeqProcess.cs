using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.Export {
    public sealed class RnaSeqProcess {
        private RnaSeqProcess() { }

        public static void Convert2Csv4ViolinPlot(string input, string output) {
            string[] header = null;
            var arraylist = new List<string[]>();
            using (var sr = new StreamReader(input)) {
                header = sr.ReadLine().Split(',');
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == null) continue;
                    var linearray = line.Split(',');
                    arraylist.Add(linearray);
                }
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Sample,Ion,Lipid");
                for (int i = 1; i < header.Length; i++) {
                    for (int j = 0; j < arraylist.Count; j++) {
                        if (arraylist[j][i] == "0") continue;
                        sw.WriteLine(header[i] + "," + arraylist[j][i] + "," + arraylist[j][0]);
                    }
                }
            }
        }

        public static void MergeData(string inputDir, string output) {
            var files = Directory.GetFiles(inputDir);
            foreach (var file in files) {
            }
        }
    }
}
