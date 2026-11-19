using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.DataObjTest {

    public class ResultTemp {
        public string FileName { get; set; } = string.Empty;
        public string PeakID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Intensity { get; set; } = string.Empty;
        public string Ontology { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public override string ToString() { 
            return FileName + "\t" + PeakID + "\t" + Name + "\t" + Ontology + "\t" + Comment + "\t" + Intensity;
        
        }
    }

    public sealed class CreateStatisticsInEieioProject {
        private CreateStatisticsInEieioProject() { }

        public static void WriteSummary(string inputdir, string output) {
            var files = Directory.GetFiles(inputdir);
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("File name\tPeak ID\tName\tOntology\tComment\tIntensity");
                foreach (var file in files) {
                    var results = GetResults(file);
                    foreach (var result in results) {
                        sw.WriteLine(result.ToString());
                    }
                }
            }
        }

        public static List<ResultTemp> GetResults(string input) {
            var filename = Path.GetFileName(input);
            var results = new List<ResultTemp>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                var lineNumber = 1;
                while (sr.ReadLine() is string line) {
                    lineNumber++;
                    var linearray = line.Split('\t');
                    // Ontology is read from column 16.
                    if (linearray.Length < 17) {
                        throw new InvalidDataException($"Expected at least 17 columns in '{input}', line {lineNumber}.");
                    }
                    var metname = linearray[1];
                    if (metname == string.Empty || metname.StartsWith("w/o") || metname.StartsWith("RIKEN")) {
                        continue;
                    }

                    var resultTemp = new ResultTemp() {
                        FileName = filename,
                        Name = metname,
                        PeakID = linearray[0],
                        Intensity = linearray[7],
                        Ontology = linearray[16],
                        Comment = linearray[12]
                    };
                    results.Add(resultTemp);
                }
            }
            return results;
        }
    }
}
