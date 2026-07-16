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

            if (!File.Exists(inputFile)) {
                Console.Error.WriteLine($"Project file was not found: {inputFile}");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            var project = LoadProject(inputFile);
            var files = project.AnalysisFiles.Where(file => file.AnalysisFileIncluded).ToList();
            if (files.Count == 0) {
                Console.Error.WriteLine("No included analysis files were found in the project.");
                return -1;
            }

            if (string.Equals(outputFormat, "csv", StringComparison.OrdinalIgnoreCase))
            {
                WriteProjectCsv(outputFile, project);
                return 0;
            }
            else
            {
                var json = BuildProjectJson(Path.GetFullPath(inputFile), project);
                File.WriteAllText(outputFile, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return 0;
            }
        }

        private static void WriteProjectCsv(string outputFile, IMsdialDataStorage<ParameterBase> project)
        {
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("FileId,FileName,PeakId,Name,ScanId,RT,TargetMz,Tolerance,Intensity");
            var range = new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min);
            foreach (var file in project.AnalysisFiles.Where(file => file.AnalysisFileIncluded))
            {
                if (file.PeakAreaBeanInformationFilePath.IsEmptyOrNull()) {
                    continue;
                }

                var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var measurement = LoadMeasurement(file.AnalysisFilePath);
                if (measurement.Count == 0) {
                    continue;
                }

                var parameter = project.Parameter;
                var rawSpectra = new RawSpectra(measurement, parameter.IonMode, file.AcquisitionType);
                var chromatograms = rawSpectra.GetMS1ExtractedChromatograms(peaks.Select(p => p.PrecursorMz), parameter.CentroidMs1Tolerance, range);
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

        private static string BuildProjectJson(string source, IMsdialDataStorage<ParameterBase> project) {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"source\": \"{EscapeJson(source)}\",");
            sb.AppendLine("  \"peaks\": [");
            var files = project.AnalysisFiles.Where(file => file.AnalysisFileIncluded).ToList();
            for (var i = 0; i < files.Count; i++) {
                sb.Append(BuildProjectPeakJson(project, files[i]));
                if (i < files.Count - 1) {
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

        private static string BuildProjectPeakJson(IMsdialDataStorage<ParameterBase> project, AnalysisFileBean file) {
            var measurement = LoadMeasurement(file.AnalysisFilePath);
            var peaks = file.PeakAreaBeanInformationFilePath.IsEmptyOrNull()
                ? []
                : MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath) ?? [];
            var rawSpectra = new RawSpectra(measurement, project.Parameter.IonMode, (CompMs.Common.Enum.AcquisitionType)file.AcquisitionType);
            var chromatogram = peaks.Count > 0
                ? ExtractPeakChromatogram(rawSpectra, project.Parameter, peaks[0].PrecursorMz)
                : new ProjectChromatogram([], []);
            var sb = new StringBuilder();
            sb.AppendLine("    {");
            sb.AppendLine($"      \"id\": {file.AnalysisFileId},");
            sb.AppendLine($"      \"masterPeakId\": {file.AnalysisFileId},");
            sb.AppendLine($"      \"name\": \"{EscapeJson(file.AnalysisFileName)}\",");
            sb.AppendLine($"      \"mz\": {(peaks.Count > 0 ? peaks[0].PrecursorMz : 0d).ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine("      \"rt\": {");
            sb.AppendLine($"        \"left\": {0.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"        \"top\": {0.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"        \"right\": {0.ToString(CultureInfo.InvariantCulture)}");
            sb.AppendLine("      },");
            sb.AppendLine("      \"chromatogram\": {");
            sb.AppendLine($"        \"rts\": [{string.Join(", ", chromatogram.Rts.Select(v => v.ToString(CultureInfo.InvariantCulture)))}],");
            sb.AppendLine($"        \"intensities\": [{string.Join(", ", chromatogram.Intensities.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]");
            sb.AppendLine("      }");
            sb.Append("    }");
            return sb.ToString();
        }

        private static ProjectChromatogram ExtractPeakChromatogram(RawSpectra rawSpectra, ParameterBase parameter, double targetMz) {
            var rts = new List<double>();
            var intensities = new List<double>();
            var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(targetMz, parameter.CentroidMs1Tolerance), new ChromatogramRange(0d, double.MaxValue, ChromXType.RT, ChromXUnit.Min));
            foreach (var dataPoint in chromatogram.AsPeakArray()) {
                rts.Add(dataPoint.Time);
                intensities.Add(dataPoint.Intensity);
            }
            return new ProjectChromatogram(rts, intensities);
        }

        private static IMsdialDataStorage<ParameterBase> LoadProject(string projectFile) {
            using var stream = new FileStream(projectFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var manager = ZipStreamManager.OpenGet(stream);
            return MsdialDataStorage.Serializer.LoadAsync(manager, Path.GetFileName(projectFile), Path.GetDirectoryName(Path.GetFullPath(projectFile)) ?? string.Empty, string.Empty).GetAwaiter().GetResult();
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
