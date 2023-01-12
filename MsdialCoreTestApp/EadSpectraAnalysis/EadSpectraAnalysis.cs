using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.EadSpectraAnalysis {
    public sealed class EadSpectraAnalysis {
        public static void GenerateSpectralEntropyList(string inputdir, string outputdir) {
            var folders = Directory.GetDirectories(inputdir, "*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }
            var outputfilename = "entropy.txt";
            var outputfile = Path.Combine(outputdir, outputfilename);
            using (var sw = new StreamWriter(outputfile)) {
                sw.WriteLine("Name\tEntropy\tEnergy");
                foreach (var folder in folders) { 
                var files = Directory.GetFiles(folder, "*.msp", SearchOption.TopDirectoryOnly);
                foreach (var file in files) {
                    var records = MspFileParser.MspFileReader(file);
                        for (int i = 0; i < records.Count; i++) {
                            var record = records[i];
                            var normalizedSpectrum = SpectrumHandler.GetNormalizedPeak4SpectralEntropyCalc(record.Spectrum, record.PrecursorMz);
                            var entropy = MsScanMatching.GetSpectralEntropy(normalizedSpectrum);
                            sw.WriteLine(record.Name + "\t" + entropy + "\t" + record.FragmentationCondition);
                        }
                    }
                }
            }
        }

        public static void GenerateNodeEdgeFiles(string inputdir, string outputdir) {
            var folders = Directory.GetDirectories(inputdir, "*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }

            var minimumPeakMatch = 6;
            var maxEdgeNumPerNode = 10;
            var maxPrecursorDiff = 400.0;
            var maxPrecursorDiff_Percent = 45;

            foreach (var folder in folders) {
                var files = Directory.GetFiles(folder, "*.msp", SearchOption.TopDirectoryOnly);
                foreach (var file in files) {
                    var nodefilename = Path.GetFileNameWithoutExtension(file) + "_node.txt";
                    var edgefilename = Path.GetFileNameWithoutExtension(file) + "_edge.txt";
                    var nodefile = Path.Combine(outputdir, nodefilename);
                    var edgefile = Path.Combine(outputdir, edgefilename);
                    var records = MspFileParser.MspFileReader(file);
                    foreach (var record in records) {
                        record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
                    }
                    for (int i = 0; i < records.Count; i++) {
                        for (int j = i + 1; j < records.Count; j++) {
                            var prop1 = records[i];
                            var prop2 = records[j];
                            var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                            if (massDiff > maxPrecursorDiff) continue;
                            if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;
                        }
                    }


                    
                }
            }
        }
    }
}
