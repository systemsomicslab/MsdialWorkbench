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
            foreach (var folder in folders) { 
                var files = Directory.GetFiles(folder, "*.msp", SearchOption.TopDirectoryOnly);
                foreach (var file in files) {
                    var records = MspFileParser.MspFileReader(file);
                    var outputfilename = Path.GetFileName(file) + "_entropy.txt";
                    var outputfile = Path.Combine(outputdir, outputfilename);
                    using (var sw = new StreamWriter(outputfile)) {
                        for (int i = 0; i < records.Count; i++) {
                            var record = records[i];
                            var normalizedSpectrum = SpectrumHandler.GetNormalizedPeak4SpectralEntropyCalc(record.Spectrum, record.PrecursorMz);
                            var entropy = MsScanMatching.GetSpectralEntropy(normalizedSpectrum);
                            sw.WriteLine(i + "\t" + entropy);
                        }
                    }
                }
            }
        }
    }
}
