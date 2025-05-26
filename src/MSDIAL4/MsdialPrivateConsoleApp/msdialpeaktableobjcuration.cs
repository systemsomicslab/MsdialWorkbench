using CompMs.Common.Lipidomics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {

    public class PeakRecord {
        public int PeakId { get; set; }
        public string Name { get; set; }
        public int ScanNumber { get; set; }
        public double RTLeft { get; set; }
        public double RTRetentionTime { get; set; }
        public double RTRight { get; set; }
        public double PrecursorMz { get; set; }
        public double Height { get; set; }
        public double Area { get; set; }
        public double EstimatedNoise { get; set; }
        public double SignalToNoise { get; set; }
        public double Sharpness { get; set; }
        public double GaussianSimilarity { get; set; }
        public double IdealSlope { get; set; }
        public double Symmetry { get; set; }
        public string ModelMasses { get; set; }
        public string Adduct { get; set; }
        public int Isotope { get; set; }
        public string Comment { get; set; }
        public string ReferenceRT { get; set; }
        public string ReferenceMz { get; set; }
        public string Formula { get; set; }
        public string Ontology { get; set; }
        public string InChIKey { get; set; }
        public string Smiles { get; set; }
        public string AnnotationTag { get; set; }
        public bool RtMatched { get; set; }
        public bool MzMatched { get; set; }
        public bool MsMsMatched { get; set; }
        public string RtSimilarity { get; set; }
        public string MzSimilarity { get; set; }
        public string SimpleDotProduct { get; set; }
        public string WeightedDotProduct { get; set; }
        public string ReverseDotProduct { get; set; }
        public string MatchedPeaksCount { get; set; }
        public string MatchedPeaksPercentage { get; set; }
        public string TotalScore { get; set; }
        public double SignalToNoiseMsMs { get; set; }
        public string Ms1Isotopes { get; set; }
        public string MsMsSpectrum { get; set; }
    }

    public sealed class msdialpeaktableobjcuration {
        private msdialpeaktableobjcuration() { }

        public static void ExportScoreDist(string[] files, string output) {
            var list = GetPeakRecordsFromMultipleFiles(files);
            var header = new List<string>() { "SimpleDotProduct", "WeightedDotProduct", "ReverseDotProduct", "MatchedPeaksCount", "MatchedPeaksPercentage" };
            using (StreamWriter sw = new StreamWriter(output)) {
                sw.WriteLine(String.Join("\t", header));
                foreach (var query in list.Where(n => n.SimpleDotProduct != "null" && n.SimpleDotProduct != "-1.00")) {
                //foreach (var query in list.Where(n => n.SimpleDotProduct != "null" && n.SimpleDotProduct != "-1.00" && Math.Sqrt(double.Parse(n.ReverseDotProduct)) > 0.5 && Math.Sqrt(double.Parse(n.WeightedDotProduct)) > 0.0)) {
                        var values = new List<string>() { Math.Sqrt(double.Parse(query.SimpleDotProduct)).ToString(), Math.Sqrt(double.Parse(query.WeightedDotProduct)).ToString(),
                        Math.Sqrt(double.Parse(query.ReverseDotProduct)).ToString(), query.MatchedPeaksCount, query.MatchedPeaksPercentage };
                    sw.WriteLine(String.Join("\t", values));
                }
            }
        }

        public static List<PeakRecord> GetPeakRecordsFromMultipleFiles(string[] files) {
            var globallist = new List<PeakRecord>();
            foreach (string file in files) {
                if (File.Exists(file)) {
                    var list = GetPeakRecords(file);
                    globallist.AddRange(list);
                }
            }
            return globallist;
        }

        public static PeakRecord Parse(string[] fields) {
            return new PeakRecord {
                PeakId = int.Parse(fields[0]),
                Name = fields[1],
                ScanNumber = int.Parse(fields[2]),
                RTLeft = double.Parse(fields[3], CultureInfo.InvariantCulture),
                RTRetentionTime = double.Parse(fields[4], CultureInfo.InvariantCulture),
                RTRight = double.Parse(fields[5], CultureInfo.InvariantCulture),
                PrecursorMz = double.Parse(fields[6], CultureInfo.InvariantCulture),
                Height = double.Parse(fields[7], CultureInfo.InvariantCulture),
                Area = double.Parse(fields[8], CultureInfo.InvariantCulture),
                EstimatedNoise = double.Parse(fields[9], CultureInfo.InvariantCulture),
                SignalToNoise = double.Parse(fields[10], CultureInfo.InvariantCulture),
                Sharpness = double.Parse(fields[11], CultureInfo.InvariantCulture),
                GaussianSimilarity = double.Parse(fields[12], CultureInfo.InvariantCulture),
                IdealSlope = double.Parse(fields[13], CultureInfo.InvariantCulture),
                Symmetry = double.Parse(fields[14], CultureInfo.InvariantCulture),
                ModelMasses = fields[15],
                Adduct = fields[16],
                Isotope = int.Parse(fields[17]),
                Comment = fields[18],
                ReferenceRT = fields[19],
                ReferenceMz = fields[20],
                Formula = fields[21],
                Ontology = fields[22],
                InChIKey = fields[23],
                Smiles = fields[24],
                AnnotationTag = fields[25],
                RtMatched = bool.Parse(fields[26]),
                MzMatched = bool.Parse(fields[27]),
                MsMsMatched = bool.Parse(fields[28]),
                RtSimilarity = fields[29],
                MzSimilarity = fields[30],
                SimpleDotProduct = fields[31],
                WeightedDotProduct = fields[32],
                ReverseDotProduct = fields[33],
                MatchedPeaksCount = fields[34],
                MatchedPeaksPercentage = fields[35],
                TotalScore = fields[36],
                SignalToNoiseMsMs = double.Parse(fields[37], CultureInfo.InvariantCulture),
                Ms1Isotopes = fields[38],
                MsMsSpectrum = fields[39],
            };
        }

        public static List<PeakRecord> GetPeakRecords(string input) {
            var list = new List<PeakRecord>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    list.Add(Parse(linearray));
                }
            }
            return list;
        }
    }
}
