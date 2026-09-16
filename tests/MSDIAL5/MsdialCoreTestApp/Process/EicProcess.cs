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

        public int RunRaw(FileInfo inputFile, FileInfo outputFile, IReadOnlyList<double> targetValues, AcquisitionType acquisitionType) {
            if (targetValues.Count == 0 || targetValues.Count % 2 != 0) {
                return ArgsError();
            }

            var targets = new List<TargetQuery>(targetValues.Count / 2);
            for (var i = 0; i < targetValues.Count; i += 2) {
                targets.Add(new TargetQuery(targetValues[i], targetValues[i + 1]));
            }
            return RunRaw(inputFile, outputFile, targets, acquisitionType);
        }

        private int RunRaw(FileInfo inputFile, FileInfo outputFile, IReadOnlyList<TargetQuery> targets, AcquisitionType acquisitionType) {
            var spectra = LoadMeasurement(inputFile.FullName);
            if (spectra.Count == 0) {
                Console.Error.WriteLine("No raw spectra were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile.FullName) ?? ".");
            WriteRawCsv(outputFile.FullName, targets, spectra, acquisitionType);
            return 0;
        }

        public int RunProject(FileInfo inputFile, FileInfo outputFile, string outputFormat) {
            if (!File.Exists(inputFile.FullName)) {
                Console.Error.WriteLine($"Project file was not found: {inputFile.FullName}");
                return -1;
            }

            var project = LoadProject(inputFile.FullName);
            var files = project.AnalysisFiles.Where(file => file.AnalysisFileIncluded).ToList();
            if (files.Count == 0) {
                Console.Error.WriteLine("No included analysis files were found in the project.");
                return -1;
            }

            if (string.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase)) {
                WriteProjectCsv(outputFile.FullName, project);
                return 0;
            }
            if (string.Equals(outputFormat, "json", StringComparison.OrdinalIgnoreCase)) {
                WriteProjectJson(outputFile.FullName, project);
                return 0;
            }

            Console.Error.WriteLine($"Invalid output format: {outputFormat}. Valid options are: csv, json.");
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

            return RunRaw(new FileInfo(inputFile), new FileInfo(outputFile), targets, acquisitionType);
        }

        private static void WriteRawCsv(string outputPath, IReadOnlyList<TargetQuery> targets, IReadOnlyList<RawSpectrum> spectra, AcquisitionType acquisitionType)
        {
            var ionMode = spectra.FirstOrDefault(s => s.MsLevel == 1)?.ScanPolarity == ScanPolarity.Negative ? IonMode.Negative : IonMode.Positive;
            var rawSpectra = new RawSpectra(spectra, ionMode, acquisitionType);
            var chromatogramRange = new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
            writer.WriteLine("ScanId,RT,TargetMz,Tolerance,Intensity");
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
            var outputFile = string.Empty;
            var outputFormat = "json";

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-format" && i + 1 < args.Length) {
                    outputFormat = args[i + 1];
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull()) {
                return ArgsError();
            }

            return RunProject(new FileInfo(inputFile), new FileInfo(outputFile), outputFormat);
        }

        private static void WriteProjectCsv(string outputPath, IMsdialDataStorage<ParameterBase> project)
        {
            var rootOutput = Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".";
            Directory.CreateDirectory(rootOutput);
            var range = new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            foreach (var file in project.AnalysisFiles.Where(file => file.AnalysisFileIncluded))
            {
                if (file.PeakAreaBeanInformationFilePath.IsEmptyOrNull() || !File.Exists(file.PeakAreaBeanInformationFilePath)) {
                     Console.Error.WriteLine($"Peak feature file was not found: {file.PeakAreaBeanInformationFilePath}");
                     continue;
                 }
                 if (file.AnalysisFilePath.IsEmptyOrNull() || !File.Exists(file.AnalysisFilePath)) {
                     Console.Error.WriteLine($"Raw file was not found: {file.AnalysisFilePath}");
                    continue;
                }

                var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var measurement = LoadMeasurement(file.AnalysisFilePath);
                if (measurement.Count == 0) {
                    Console.Error.WriteLine($"No raw spectra were found in: {file.AnalysisFilePath}");
                    continue;
                }

                var parameter = project.Parameter;
                var rawSpectra = new RawSpectra(measurement, parameter.IonMode, file.AcquisitionType);
                var chromatograms = rawSpectra.GetMS1ExtractedChromatograms(peaks.Select(p => p.PrecursorMz), parameter.CentroidMs1Tolerance, range);
                var fileOutput = CreateSplitOutputPath(outputPath, SanitizeFileName(file.AnalysisFileName), ".csv");
                using var writer = new StreamWriter(fileOutput, false, Encoding.ASCII);
                writer.WriteLine("FileId,FileName,PeakId,Name,ScanId,RT,TargetMz,Tolerance,Intensity");
                foreach (var (chromatogram, peak) in chromatograms.Zip(peaks, (c, p) => (c, p)))
                {
                    for (var i = 0; i < chromatogram.Length; i++)
                    {
                        writer.WriteLine(string.Join(",",
                            file.AnalysisFileId,
                            CsvEscape(file.AnalysisFileName),
                            peak.MasterPeakID,
                            CsvEscape(peak.Name),
                            chromatogram.Id(i),
                            chromatogram.Time(i).ToString(CultureInfo.InvariantCulture),
                            chromatogram.ExtractedMz.ToString(CultureInfo.InvariantCulture),
                            parameter.CentroidMs1Tolerance.ToString(CultureInfo.InvariantCulture),
                            chromatogram.Intensity(i).ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }
        }

        private static void WriteProjectJson(string outputPath, IMsdialDataStorage<ParameterBase> project) {
            var rootOutput = Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".";
            Directory.CreateDirectory(rootOutput);
            foreach (var file in project.AnalysisFiles.Where(file => file.AnalysisFileIncluded)) {
                if (file.AnalysisFilePath.IsEmptyOrNull() || !File.Exists(file.AnalysisFilePath)) {
                    Console.Error.WriteLine($"Raw file was not found: {file.AnalysisFilePath}");
                    continue;
                }
                if (!file.PeakAreaBeanInformationFilePath.IsEmptyOrNull() && !File.Exists(file.PeakAreaBeanInformationFilePath)) {
                    Console.Error.WriteLine($"Peak feature file was not found: {file.PeakAreaBeanInformationFilePath}");
                    continue;
                }

                var fileOutput = CreateSplitOutputPath(outputPath, SanitizeFileName(file.AnalysisFileName), ".json");
                var json = BuildProjectJson(project, file);
                File.WriteAllText(fileOutput, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        private static string BuildProjectJson(IMsdialDataStorage<ParameterBase> project, AnalysisFileBean file) {
            var peaks = file.PeakAreaBeanInformationFilePath.IsEmptyOrNull()
                ? []
                : MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath) ?? [];
            IReadOnlyList<RawSpectrum> measurement = LoadMeasurement(file.AnalysisFilePath);
            RawSpectra rawSpectra = new(measurement, project.Parameter.IonMode, file.AcquisitionType);
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"raw file\": \"{EscapeJson(file.AnalysisFilePath)}\",");
            sb.AppendLine($"  \"peak file\": \"{EscapeJson(file.PeakAreaBeanInformationFilePath)}\",");
            sb.AppendLine("  \"peaks\": [");

            for (int i = 0; i < peaks.Count; i++) {
                var peak = peaks[i];
                var chromatogramRange = ChromatogramRange.FromPeakFeature(peak).ExtendRelative(1d);
                var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(peaks[i].PrecursorMz, project.Parameter.CentroidMs1Tolerance), chromatogramRange);
                BuildProjectPeakJson(sb, peaks[i], chromatogram);
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

        private static void BuildProjectPeakJson(StringBuilder sb, ChromatogramPeakFeature peak, ExtractedIonChromatogram chromatogram1) {
            var chromatogram = ConvertPeakChromatogram(chromatogram1);
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": {peak.MasterPeakID},");
            sb.AppendLine($"      \"name\": \"{EscapeJson(peak.Name)}\",");
            sb.AppendLine($"      \"mz\": {peak.PrecursorMz.ToString(CultureInfo.InvariantCulture)},");
            switch (peak.ChromXs.GetRepresentativeXAxis().Type)
            {
                case ChromXType.RT:
                    sb.AppendLine("      \"retention time\": {");
                    sb.AppendLine($"        \"left\": {peak.PeakFeature.ChromXsLeft.RT.Value.ToString(CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"        \"top\": {peak.PeakFeature.ChromXsTop.RT.Value.ToString(CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"        \"right\": {peak.PeakFeature.ChromXsRight.RT.Value.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine("      },");
                    break;
                case ChromXType.Drift:
                    sb.AppendLine("      \"drift time\": {");
                    sb.AppendLine($"        \"left\": {peak.PeakFeature.ChromXsLeft.Drift.Value.ToString(CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"        \"top\": {peak.PeakFeature.ChromXsTop.Drift.Value.ToString(CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"        \"right\": {peak.PeakFeature.ChromXsRight.Drift.Value.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine("      },");
                    break;
            }
            sb.AppendLine("      \"chromatogram\": {");
            sb.AppendLine($"        \"times\": [{string.Join(",", chromatogram.Times.Select(v => v.ToString(CultureInfo.InvariantCulture)))}],");
            sb.AppendLine($"        \"intensities\": [{string.Join(",", chromatogram.Intensities.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]");
            sb.AppendLine("      }");
            sb.Append("    }");
        }

        private static ProjectChromatogram ConvertPeakChromatogram(ExtractedIonChromatogram chromatogram) {
            var times = new List<double>(chromatogram.Length);
            var intensities = new List<double>(chromatogram.Length);
            for (var i = 0; i < chromatogram.Length; i++) {
                times.Add(chromatogram.Time(i));
                intensities.Add(chromatogram.Intensity(i));
            }
            return new ProjectChromatogram(times, intensities);
        }

        private static IMsdialDataStorage<ParameterBase> LoadProject(string projectFile) {
            return Common.MessagePack.MessagePackDefaultHandler.LoadFromFile<MsdialDataStorage>(projectFile);
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
            return value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n")
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private static string CreateSplitOutputPath(string outputPath, string suffix, string extension) {
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath) ?? ".";
            var fileName = Path.GetFileNameWithoutExtension(fullPath);
            return Path.Combine(directory, $"{fileName}_{suffix}{extension}");
        }

        private static string SanitizeFileName(string value) {
            var sanitized = value ?? string.Empty;
            foreach (var invalid in Path.GetInvalidFileNameChars()) {
                sanitized = sanitized.Replace(invalid, '_');
            }
            return sanitized.IsEmptyOrNull() ? "output" : sanitized;
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
            Console.Error.WriteLine("MsdialConsoleApp.exe eic raw -i <input file> -o <output csv> -target <mz> <tolerance> [-target <mz> <tolerance> ...] [-acquisitiontype DDA|DIA]");
            Console.Error.WriteLine("MsdialConsoleApp.exe eic project -i <project file> -o <output file> [-format csv|json]");
            return -1;
        }

        private readonly struct TargetQuery(double mz, double tolerance)
        {
            public double Mz { get; } = mz;
            public double Tolerance { get; } = tolerance;
        }

        private readonly struct ProjectChromatogram(List<double> times, List<double> intensities)
        {
            public List<double> Times { get; } = times;
            public List<double> Intensities { get; } = intensities;
            public int PointCount => Math.Min(Times.Count, Intensities.Count);
        }
    }
}
