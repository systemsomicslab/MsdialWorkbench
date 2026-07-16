using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
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
                return ArgsError();
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

        private int RunRaw(string[] args)
        {
            var inputFile = string.Empty;
            var outputFile = string.Empty;
            var acquisitionType = AcquisitionType.DDA;
            var targets = new List<TargetQuery>();

            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] == "-i" && i + 1 < args.Length)
                {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-acquisitiontype" && i + 1 < args.Length)
                {
                    if (!Enum.TryParse(args[i + 1], true, out acquisitionType))
                    {
                        return ArgsError();
                    }
                }
                else if (args[i] == "-target" && i + 2 < args.Length)
                {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var mz)
                        || !double.TryParse(args[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var tolerance))
                    {
                        return ArgsError();
                    }
                    targets.Add(new TargetQuery(mz, tolerance));
                    i += 2;
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull() || targets.Count == 0)
            {
                return ArgsError();
            }

            var spectra = LoadMeasurement(inputFile);
            if (spectra.Count == 0)
            {
                Console.Error.WriteLine("No raw spectra were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            WriteRawCsv(outputFile, targets, spectra, acquisitionType);

            return 0;
        }

        private static void WriteRawCsv(string outputFile, List<TargetQuery> targets, IReadOnlyList<RawSpectrum> spectra, AcquisitionType acquisitionType)
        {
            var rawSpectra = new RawSpectra(spectra, IonMode.Positive, acquisitionType);
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine( "ScanId,RT,TargetMz,Tolerance,Intensity");
            var chromatogramRange = new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            foreach (var target in targets)
            {
                using var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(target.Mz, target.Tolerance), chromatogramRange);
                for (var i = 0; i < chromatogram.Length; i++)
                {
                    writer.WriteLine(string.Join(",",
                        chromatogram.Id(i),
                        chromatogram.Time(i).ToString(CultureInfo.InvariantCulture),
                        target.Mz.ToString(CultureInfo.InvariantCulture),
                        target.Tolerance.ToString(CultureInfo.InvariantCulture),
                        chromatogram.Intensity(i).ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private int RunProject(string[] args) {
            var inputFile = string.Empty;
            var rawFile = string.Empty;
            var parameterFile = string.Empty;
            var outputFile = string.Empty;
            var outputFormat = "json";

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-raw" && i + 1 < args.Length) {
                    rawFile = args[i + 1];
                }
                else if (args[i] == "-param" && i + 1 < args.Length) {
                    parameterFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-format" && i + 1 < args.Length) {
                    outputFormat = args[i + 1];
                }
            }

            if (inputFile.IsEmptyOrNull() || rawFile.IsEmptyOrNull() || parameterFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull()) {
                return ArgsError();
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

            if (!File.Exists(parameterFile)) {
                Console.Error.WriteLine($"Project parameter file was not found: {parameterFile}");
                return -1;
            }

            var parameter = ConfigParser.ReadForLcmsParameter(parameterFile);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            if (string.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase))
            {
                WriteProjectCsv(outputFile, peaks, measurement, parameter);
                return 0;
            }
            else
            {
                var json = BuildProjectJson(Path.GetFullPath(inputFile), peaks, measurement, parameter);
                File.WriteAllText(outputFile, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return 0;
            }
        }

        private static void WriteProjectCsv(string outputFile, List<ChromatogramPeakFeature> peaks, IReadOnlyList<RawSpectrum> measurement, ParameterBase parameter)
        {
            var rawSpectra = new RawSpectra(measurement, parameter.IonMode, parameter.ProjectParam.AcquisitionType);
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("MasterPeakId,Name,ScanId,RT,TargetMz,Tolerance,Intensity");
            var range = new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            var chromatograms = rawSpectra.GetMS1ExtractedChromatograms(peaks.Select(p => p.PrecursorMz), parameter.CentroidMs1Tolerance, range);
            foreach (var (chromatogram, peak) in chromatograms.Zip(peaks, (c, p) => (c, p)))
            {
                foreach (var dataPoint in chromatogram.AsPeakArray()) {
                    writer.WriteLine(string.Join(",",
                        peak.MasterPeakID,
                        CsvEscape(peak.Name),
                        dataPoint.Id,
                        dataPoint.Time.ToString(CultureInfo.InvariantCulture),
                        chromatogram.ExtractedMz.ToString(CultureInfo.InvariantCulture),
                        parameter.CentroidMs1Tolerance.ToString(CultureInfo.InvariantCulture),
                        dataPoint.Intensity.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private static string BuildProjectJson(string source, IReadOnlyList<ChromatogramPeakFeature> peaks, IReadOnlyList<RawSpectrum> measurement, ParameterBase parameter) {
            var rawSpectra = new RawSpectra(measurement, parameter.IonMode, parameter.ProjectParam.AcquisitionType);
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"source\": \"{EscapeJson(source)}\",");
            sb.AppendLine("  \"peaks\": [");
            for (var i = 0; i < peaks.Count; i++) {
                sb.Append(BuildProjectPeakJson(peaks[i], rawSpectra, parameter));
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

        private static string BuildProjectPeakJson(ChromatogramPeakFeature peak, RawSpectra rawSpectra, ParameterBase parameter) {
            var chromatogram = ExtractPeakChromatogram(peak, rawSpectra, parameter);
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

        private static ProjectChromatogram ExtractPeakChromatogram(ChromatogramPeakFeature peak, RawSpectra rawSpectra, ParameterBase parameter) {
            var rts = new List<double>();
            var intensities = new List<double>();
            var targetMz = peak.PeakFeature.Mass;
            var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(targetMz, parameter.CentroidMs1Tolerance), new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min));
            foreach (var dataPoint in chromatogram.AsPeakArray()) {
                rts.Add(dataPoint.Time);
                intensities.Add(dataPoint.Intensity);
            }
            return new ProjectChromatogram(rts, intensities);
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

        private static double GetChromValue(Common.Components.ChromXs chromXs) {
            return chromXs?.Value ?? 0d;
        }

        private static IReadOnlyList<RawSpectrum> LoadMeasurement(string inputFile) {
            using var access = new RawDataAccess(inputFile, 0, false, false, false);
            var measurement = access.GetMeasurement();
            return measurement?.SpectrumList ?? [];
        }

        private static int ArgsError() {
            Console.Error.WriteLine("MsdialConsoleApp.exe eic raw -i <input file> -o <output csv> -target <mz> <tolerance> [-target <mz> <tolerance> ...]");
            Console.Error.WriteLine("MsdialConsoleApp.exe eic project -i <peak file> -raw <raw file> -o <output file> [-format csv|json]");
            return -1;
        }

        private readonly struct TargetQuery(double mz, double tolerance)
        {
            public double Mz { get; } = mz;
            public double Tolerance { get; } = tolerance;
        }

        private readonly struct ProjectChromatogram(List<double> rts, List<double> intensities)
        {
            public List<double> Rts { get; } = rts;
            public List<double> Intensities { get; } = intensities;
            public int PointCount => Math.Min(Rts.Count, Intensities.Count);
        }
    }
}
