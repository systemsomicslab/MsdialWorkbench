using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.DataObjTest {

    public class ResultTemp {
        public string FileName { get; set; }
        public string PeakID { get; set; }
        public string Name { get; set; }
        public string Intensity { get; set; }
        public string Ontology { get; set; }
        public string Comment { get; set; }
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
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
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
