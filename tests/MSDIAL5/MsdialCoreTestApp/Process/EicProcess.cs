using CompMs.Common.DataObj;
using CompMs.Common.Extension;
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
            var targetMz = double.NaN;
            var mzTolerance = 0.01d;

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-mz" && i + 1 < args.Length) {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out targetMz)) {
                        return argsError();
                    }
                }
                else if (args[i] == "-tolerance" && i + 1 < args.Length) {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out mzTolerance)) {
                        return argsError();
                    }
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull() || double.IsNaN(targetMz)) {
                return argsError();
            }

            var spectra = LoadMeasurement(inputFile);
            if (spectra.Count == 0) {
                Console.Error.WriteLine("No raw spectra were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("ScanId,RT,Intensity");

            foreach (var spectrum in spectra.Where(n => n.MsLevel == 1)) {
                var intensity = spectrum.Spectrum
                    .Where(peak => Math.Abs(peak.Mz - targetMz) <= mzTolerance)
                    .Sum(peak => peak.Intensity);
                writer.WriteLine(string.Join(",", spectrum.ScanNumber, spectrum.ScanStartTime.ToString(CultureInfo.InvariantCulture), intensity.ToString(CultureInfo.InvariantCulture)));
            }

            return 0;
        }

        private int RunProject(string[] args) {
            var inputFile = string.Empty;
            var outputFile = string.Empty;
            var maxPoints = 20;

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-maxPoints" && i + 1 < args.Length) {
                    if (!int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out maxPoints) || maxPoints <= 0) {
                        return argsError();
                    }
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull()) {
                return argsError();
            }

            if (!File.Exists(inputFile)) {
                Console.Error.WriteLine($"Project peak file was not found: {inputFile}");
                return -1;
            }

            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(inputFile);
            if (peaks == null || peaks.Count == 0) {
                Console.Error.WriteLine("No chromatogram peaks were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            var json = BuildProjectJson(Path.GetFullPath(inputFile), peaks, maxPoints);
            File.WriteAllText(outputFile, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return 0;
        }

        private static string BuildProjectJson(string source, IReadOnlyList<CompMs.MsdialCore.DataObj.ChromatogramPeakFeature> peaks, int maxPoints) {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"source\": \"{EscapeJson(source)}\",");
            sb.AppendLine("  \"peaks\": [");
            for (var i = 0; i < peaks.Count; i++) {
                sb.Append(BuildProjectPeakJson(peaks[i], maxPoints));
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

        private static string BuildProjectPeakJson(CompMs.MsdialCore.DataObj.ChromatogramPeakFeature peak, int maxPoints) {
            var points = BuildPeakPoints(peak).Take(maxPoints).ToList();
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
            sb.AppendLine("      \"points\": [");
            for (var i = 0; i < points.Count; i++) {
                sb.Append($"        {{ \"rt\": {points[i].Rt.ToString(CultureInfo.InvariantCulture)}, \"intensity\": {points[i].Intensity.ToString(CultureInfo.InvariantCulture)} }}");
                if (i < points.Count - 1) {
                    sb.AppendLine(",");
                }
                else {
                    sb.AppendLine();
                }
            }
            sb.AppendLine("      ]");
            sb.Append("    }");
            return sb.ToString();
        }

        private static IEnumerable<ProjectPoint> BuildPeakPoints(CompMs.MsdialCore.DataObj.ChromatogramPeakFeature peak) {
            yield return new ProjectPoint(GetChromValue(peak.PeakFeature.ChromXsLeft), peak.PeakFeature.PeakHeightLeft);
            yield return new ProjectPoint(GetChromValue(peak.PeakFeature.ChromXsTop), peak.PeakFeature.PeakHeightTop);
            yield return new ProjectPoint(GetChromValue(peak.PeakFeature.ChromXsRight), peak.PeakFeature.PeakHeightRight);
        }

        private static string EscapeJson(string value) {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static double GetChromValue(CompMs.Common.Components.ChromXs chromXs) {
            return chromXs?.Value ?? 0d;
        }

        private readonly struct ProjectPoint {
            public ProjectPoint(double rt, double intensity) {
                Rt = rt;
                Intensity = intensity;
            }

            public double Rt { get; }
            public double Intensity { get; }
        }

        private static IReadOnlyList<RawSpectrum> LoadMeasurement(string inputFile) {
            using var access = new RawDataAccess(inputFile, 0, false, false, false);
            var measurement = access.GetMeasurement();
            return measurement?.SpectrumList ?? new List<RawSpectrum>();
        }

        private static int argsError() {
            Console.Error.WriteLine("MsdialConsoleApp.exe eic raw -i <input file> -o <output csv> -mz <target m/z> [-tolerance <mz tolerance>]");
            Console.Error.WriteLine("MsdialConsoleApp.exe eic project -i <peak file> -o <output json> [-maxPoints <count>]");
            return -1;
        }
    }
}
