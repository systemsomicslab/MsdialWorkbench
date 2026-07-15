using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process
{
    public sealed class EicProcess
    {
        public int Run(string[] args) {
            if (args.Length < 2) {
                return argsError();
            }

            var subcommand = args[1];
            if (subcommand == "raw") {
                return RunRaw(args);
            }
            if (subcommand == "project") {
                return RunProject(args);
            }

            Console.Error.WriteLine($"Invalid eic subcommand: {subcommand}");
            return -1;
        }

        private int RunRaw(string[] args) {
            var inputFile = string.Empty;
            var outputFile = string.Empty;
            var targets = new List<TargetQuery>();

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-target" && i + 2 < args.Length) {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var mz)
                        || !double.TryParse(args[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var tolerance)) {
                        return argsError();
                    }
                    targets.Add(new TargetQuery(mz, tolerance));
                    i += 2;
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull() || targets.Count == 0) {
                return argsError();
            }

            var spectra = LoadMeasurement(inputFile);
            if (spectra.Count == 0) {
                Console.Error.WriteLine("No raw spectra were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("ScanId,RT,TargetMz,Tolerance,Intensity");

            foreach (var spectrum in spectra.Where(n => n.MsLevel == 1)) {
                foreach (var target in targets) {
                    var intensity = spectrum.Spectrum
                        .Where(peak => Math.Abs(peak.Mz - target.Mz) <= target.Tolerance)
                        .Sum(peak => peak.Intensity);
                    writer.WriteLine(string.Join(",",
                        spectrum.ScanNumber,
                        spectrum.ScanStartTime.ToString(CultureInfo.InvariantCulture),
                        target.Mz.ToString(CultureInfo.InvariantCulture),
                        target.Tolerance.ToString(CultureInfo.InvariantCulture),
                        intensity.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return 0;
        }

        private int RunProject(string[] args) {
            var inputFile = string.Empty;
            var rawFile = string.Empty;
            var outputFile = string.Empty;
            var outputFormat = "json";

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-raw" && i + 1 < args.Length) {
                    rawFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-format" && i + 1 < args.Length) {
                    outputFormat = args[i + 1];
                }
            }

            if (inputFile.IsEmptyOrNull() || rawFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull()) {
                return argsError();
            }

            if (!File.Exists(inputFile)) {
                Console.Error.WriteLine($"Project peak file was not found: {inputFile}");
                return -1;
            }

            if (!File.Exists(rawFile)) {
                Console.Error.WriteLine($"Raw data file was not found: {rawFile}");
                return -1;
            }

            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(inputFile);
            if (peaks == null || peaks.Count == 0) {
                Console.Error.WriteLine("No chromatogram peaks were found.");
                return -1;
            }

            var measurement = LoadMeasurement(rawFile);
            if (measurement == null || measurement.Count == 0) {
                Console.Error.WriteLine("No raw spectra were found for project export.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            if (string.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase)) {
                WriteProjectCsv(outputFile, peaks, measurement);
                return 0;
            }

            var json = BuildProjectJson(Path.GetFullPath(inputFile), peaks, measurement);
            File.WriteAllText(outputFile, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return 0;
        }

        private static void WriteProjectCsv(string outputFile, IReadOnlyList<ChromatogramPeakFeature> peaks, IReadOnlyList<RawSpectrum> measurement) {
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("PeakId,MasterPeakId,Name,ScanId,RT,TargetMz,Tolerance,Intensity");
            foreach (var peak in peaks) {
                WriteProjectRawCsvRows(writer, peak, measurement);
            }
        }

        private static void WriteProjectRawCsvRows(StreamWriter writer, ChromatogramPeakFeature peak, IReadOnlyList<RawSpectrum> measurement) {
            var targetMz = peak.PeakFeature.Mass;
            var tolerance = EstimateTolerance(peak);
            foreach (var spectrum in measurement.Where(n => n.MsLevel == 1)) {
                var intensity = spectrum.Spectrum
                    .Where(point => Math.Abs(point.Mz - targetMz) <= tolerance)
                    .Sum(point => point.Intensity);
                writer.WriteLine(string.Join(",",
                    peak.PeakID,
                    peak.MasterPeakID,
                    CsvEscape(peak.Name),
                    spectrum.ScanNumber,
                    spectrum.ScanStartTime.ToString(CultureInfo.InvariantCulture),
                    targetMz.ToString(CultureInfo.InvariantCulture),
                    tolerance.ToString(CultureInfo.InvariantCulture),
                    intensity.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static string BuildProjectJson(string source, IReadOnlyList<ChromatogramPeakFeature> peaks, IReadOnlyList<RawSpectrum> measurement) {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"source\": \"{EscapeJson(source)}\",");
            sb.AppendLine("  \"peaks\": [");
            for (var i = 0; i < peaks.Count; i++) {
                sb.Append(BuildProjectPeakJson(peaks[i], measurement));
                if (i < peaks.Count - 1) {
                    sb.AppendLine(",");
                }
                else {
                    sb.AppendLine();
                }
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildProjectPeakJson(ChromatogramPeakFeature peak, IReadOnlyList<RawSpectrum> measurement) {
            var chromatogram = ExtractPeakChromatogram(peak, measurement);
            var sb = new StringBuilder();
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": {peak.PeakID},");
            sb.AppendLine($"      \"masterPeakId\": {peak.MasterPeakID},");
            sb.AppendLine($"      \"name\": \"{EscapeJson(peak.Name)}\",");
            sb.AppendLine($"      \"mz\": {peak.PeakFeature.Mass.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine("      \"rt\": {");
            sb.AppendLine($"        \"left\": {GetChromValue(peak.PeakFeature.ChromXsLeft).ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"        \"top\": {GetChromValue(peak.PeakFeature.ChromXsTop).ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"        \"right\": {GetChromValue(peak.PeakFeature.ChromXsRight).ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine("      },");
            sb.AppendLine("      \"chromatogram\": {");
            sb.AppendLine($"        \"rts\": [{string.Join(", ", chromatogram.Rts.Select(v => v.ToString(CultureInfo.InvariantCulture)))}],");
            sb.AppendLine($"        \"intensities\": [{string.Join(", ", chromatogram.Intensities.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]");
            sb.AppendLine("      }");
            sb.Append("    }");
            return sb.ToString();
        }

        private static ProjectChromatogram ExtractPeakChromatogram(ChromatogramPeakFeature peak, IReadOnlyList<RawSpectrum> measurement) {
            var rts = new List<double>();
            var intensities = new List<double>();
            var targetMz = peak.PeakFeature.Mass;
            var tolerance = EstimateTolerance(peak);
            foreach (var spectrum in measurement.Where(n => n.MsLevel == 1)) {
                var intensity = spectrum.Spectrum
                    .Where(point => Math.Abs(point.Mz - targetMz) <= tolerance)
                    .Sum(point => point.Intensity);
                rts.Add(spectrum.ScanStartTime);
                intensities.Add(intensity);
            }
            if (rts.Count == 0) {
                AddChromPoint(rts, intensities, peak.PeakFeature.ChromXsLeft, peak.PeakFeature.PeakHeightLeft);
                AddChromPoint(rts, intensities, peak.PeakFeature.ChromXsTop, peak.PeakFeature.PeakHeightTop);
                AddChromPoint(rts, intensities, peak.PeakFeature.ChromXsRight, peak.PeakFeature.PeakHeightRight);
            }
            return new ProjectChromatogram(rts, intensities);
        }

        private static void AddChromPoint(List<double> rts, List<double> intensities, CompMs.Common.Components.ChromXs chromXs, double intensity) {
            if (chromXs == null) {
                return;
            }
            rts.Add(chromXs.Value);
            intensities.Add(intensity);
        }

        private static string EscapeJson(string value) {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string CsvEscape(string value) {
            if (value == null) {
                return string.Empty;
            }
            return value.Contains(",") || value.Contains("\"")
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private static double GetChromValue(CompMs.Common.Components.ChromXs chromXs) {
            return chromXs?.Value ?? 0d;
        }

        private static double EstimateTolerance(ChromatogramPeakFeature peak) {
            var left = GetChromValue(peak.PeakFeature.ChromXsLeft);
            var top = GetChromValue(peak.PeakFeature.ChromXsTop);
            var right = GetChromValue(peak.PeakFeature.ChromXsRight);
            var width = Math.Max(0d, right - left);
            if (width > 0d) {
                return width / 2d;
            }
            return Math.Max(0.01d, Math.Abs(top - left));
        }

        private static IReadOnlyList<RawSpectrum> LoadMeasurement(string inputFile) {
            using var access = new RawDataAccess(inputFile, 0, false, false, false);
            var measurement = access.GetMeasurement();
            return measurement?.SpectrumList ?? new List<RawSpectrum>();
        }

        private static int argsError() {
            Console.Error.WriteLine("MsdialConsoleApp.exe eic raw -i <input file> -o <output csv> -target <mz> <tolerance> [-target <mz> <tolerance> ...]");
            Console.Error.WriteLine("MsdialConsoleApp.exe eic project -i <peak file> -raw <raw file> -o <output file> [-format csv|json]");
            return -1;
        }

        private readonly struct TargetQuery {
            public TargetQuery(double mz, double tolerance) {
                Mz = mz;
                Tolerance = tolerance;
            }

            public double Mz { get; }
            public double Tolerance { get; }
        }

        private readonly struct ProjectChromatogram {
            public ProjectChromatogram(List<double> rts, List<double> intensities) {
                Rts = rts;
                Intensities = intensities;
            }

            public List<double> Rts { get; }
            public List<double> Intensities { get; }
            public int PointCount => Math.Min(Rts.Count, Intensities.Count);
        }
    }
}
