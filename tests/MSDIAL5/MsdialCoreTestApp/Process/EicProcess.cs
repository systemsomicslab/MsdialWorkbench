using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
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
            if (subcommand == "rtcorrection") {
                return RunRtCorrection(args);
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

        public int RunRtCorrection(
            IReadOnlyList<FileSystemInfo> inputPaths,
            FileInfo libraryFile,
            FileInfo outputFile,
            FileInfo? methodFile,
            FileInfo? selectionFile,
            IonMode ionMode,
            AcquisitionType acquisitionType) {
            if (inputPaths.Count == 0) {
                return ArgsError();
            }
            if (!libraryFile.Exists) {
                Console.Error.WriteLine($"RT correction standard library was not found: {libraryFile.FullName}");
                return -1;
            }

            var standards = TextLibraryParser.StandardTextLibraryReader(libraryFile.FullName, out var error)
                ?.Where(standard => standard.IsTargetMolecule)
                .OrderBy(standard => standard.ChromXs.RT.Value)
                .ToList() ?? [];
            if (!error.IsEmptyOrNull()) {
                Console.Error.WriteLine(error);
            }
            if (standards.Count == 0) {
                Console.Error.WriteLine("No enabled RT correction standards were found.");
                return -1;
            }

            var rawFiles = ResolveRawFiles(inputPaths.Select(path => path.FullName));
            if (rawFiles.Count == 0) {
                Console.Error.WriteLine("No supported raw data files were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile.FullName) ?? ".");
            var parameter = methodFile is null
                ? new MsdialLcmsParameter()
                : ConfigParser.ReadForLcmsParameter(methodFile.FullName);
            parameter.IonMode = ionMode;
            parameter.CompoundListForRtCorrectionPath = libraryFile.FullName;
            parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = true;
            parameter.ReferenceFileParam.RtCorrectionPeakSelectionFilePath = selectionFile?.FullName ?? string.Empty;
            var analysisFiles = CreateRtCorrectionAnalysisFiles(rawFiles, acquisitionType, outputFile.FullName);
            RetentionTimeCorrectionProcess.Prepare(
                analysisFiles,
                parameter,
                Path.GetDirectoryName(outputFile.FullName) ?? ".");
            var analysisFilesByPath = analysisFiles.ToDictionary(
                file => Path.GetFullPath(file.AnalysisFilePath),
                StringComparer.OrdinalIgnoreCase);
            using var writer = new StreamWriter(outputFile.FullName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine("FileName,FilePath,StandardId,StandardName,ReferenceRT,RTTolerance,TargetMz,MzTolerance,MinimumHeight,ScanId,RT,CorrectedRT,Intensity,SmoothedIntensity");
            var successfulFiles = 0;
            foreach (var rawFile in rawFiles) {
                Console.WriteLine($"Extracting RT correction EICs: {rawFile}");
                var spectra = LoadMeasurement(rawFile);
                if (spectra.Count == 0) {
                    Console.Error.WriteLine($"No raw spectra were found: {rawFile}");
                    continue;
                }
                WriteRtCorrectionEics(
                    writer,
                    rawFile,
                    standards,
                    spectra,
                    ionMode,
                    acquisitionType,
                    analysisFilesByPath[Path.GetFullPath(rawFile)]);
                successfulFiles++;
            }

            if (successfulFiles == 0) {
                Console.Error.WriteLine("RT correction EIC extraction failed for every input file.");
                return -1;
            }
            Console.WriteLine($"RT correction EIC audit CSV: {outputFile.FullName}");
            return 0;
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

        private int RunRtCorrection(string[] args) {
            var inputPaths = new List<FileSystemInfo>();
            FileInfo? libraryFile = null;
            FileInfo? methodFile = null;
            FileInfo? selectionFile = null;
            FileInfo? outputFile = null;
            var acquisitionType = AcquisitionType.DDA;
            var ionMode = IonMode.Positive;

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputPaths.Add(ToFileSystemInfo(args[++i]));
                }
                else if (args[i] == "-library" && i + 1 < args.Length) {
                    libraryFile = new FileInfo(args[++i]);
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = new FileInfo(args[++i]);
                }
                else if (args[i] == "-m" && i + 1 < args.Length) {
                    methodFile = new FileInfo(args[++i]);
                }
                else if (args[i] == "-selection" && i + 1 < args.Length) {
                    selectionFile = new FileInfo(args[++i]);
                }
                else if (args[i] == "-acquisitiontype" && i + 1 < args.Length) {
                    if (!Enum.TryParse(args[++i], true, out acquisitionType)) {
                        return ArgsError();
                    }
                }
                else if (args[i] == "-ionmode" && i + 1 < args.Length) {
                    if (!Enum.TryParse(args[++i], true, out ionMode)) {
                        return ArgsError();
                    }
                }
            }

            if (inputPaths.Count == 0 || libraryFile is null || outputFile is null) {
                return ArgsError();
            }
            return RunRtCorrection(inputPaths, libraryFile, outputFile, methodFile, selectionFile, ionMode, acquisitionType);
        }

        private static List<AnalysisFileBean> CreateRtCorrectionAnalysisFiles(
            IReadOnlyList<string> rawFiles,
            AcquisitionType acquisitionType,
            string outputFile) {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".";
            var runId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            return rawFiles.Select((rawFile, index) => {
                var name = Path.GetFileNameWithoutExtension(rawFile);
                return new AnalysisFileBean {
                    AnalysisFileId = index,
                    AnalysisFileIncluded = true,
                    AnalysisFileName = name,
                    AnalysisFilePath = Path.GetFullPath(rawFile),
                    AnalysisFileAnalyticalOrder = index + 1,
                    AnalysisFileClass = "Sample",
                    AnalysisFileType = AnalysisFileType.Sample,
                    AcquisitionType = acquisitionType,
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(
                        Path.Combine(outputDirectory, $"{name}_{runId}.rtc")),
                };
            }).ToList();
        }

        private static void WriteRtCorrectionEics(
            StreamWriter writer,
            string rawFile,
            IReadOnlyList<MoleculeMsReference> standards,
            IReadOnlyList<RawSpectrum> spectra,
            IonMode ionMode,
            AcquisitionType acquisitionType,
            AnalysisFileBean analysisFile) {
            var rawSpectra = new RawSpectra(spectra, ionMode, acquisitionType);
            var originalRts = analysisFile.RetentionTimeCorrectionBean.OriginalRt;
            var correctedRts = analysisFile.RetentionTimeCorrectionBean.PredictedRt;
            foreach (var standard in standards) {
                var referenceRt = standard.ChromXs.RT.Value;
                var begin = Math.Max(0d, referenceRt - standard.RetentionTimeTolerance);
                var end = referenceRt + standard.RetentionTimeTolerance;
                var range = new ChromatogramRange(begin, end, ChromXType.RT, ChromXUnit.Min);
                using var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(
                    new MzRange(standard.PrecursorMz, standard.MassTolerance), range);
                using var smoothed = chromatogram.ChromatogramSmoothing(SmoothingMethod.LinearWeightedMovingAverage, 3);
                for (var i = 0; i < chromatogram.Length; i++) {
                    writer.WriteLine(string.Join(",",
                        CsvEscape(Path.GetFileNameWithoutExtension(rawFile)),
                        CsvEscape(Path.GetFullPath(rawFile)),
                        standard.ScanID,
                        CsvEscape(standard.Name),
                        referenceRt.ToString(CultureInfo.InvariantCulture),
                        standard.RetentionTimeTolerance.ToString(CultureInfo.InvariantCulture),
                        standard.PrecursorMz.ToString(CultureInfo.InvariantCulture),
                        standard.MassTolerance.ToString(CultureInfo.InvariantCulture),
                        standard.MinimumPeakHeight.ToString(CultureInfo.InvariantCulture),
                        chromatogram.Id(i),
                        chromatogram.Time(i).ToString(CultureInfo.InvariantCulture),
                        MapCorrectedRetentionTime(chromatogram.Time(i), originalRts, correctedRts)
                            .ToString(CultureInfo.InvariantCulture),
                        chromatogram.Intensity(i).ToString(CultureInfo.InvariantCulture),
                        smoothed.Intensity(i).ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private static double MapCorrectedRetentionTime(
            double retentionTime,
            IReadOnlyList<double>? originalRts,
            IReadOnlyList<double>? correctedRts) {
            if (originalRts is null || correctedRts is null || originalRts.Count == 0
                || originalRts.Count != correctedRts.Count) {
                return retentionTime;
            }
            if (retentionTime <= originalRts[0]) {
                return correctedRts[0];
            }
            var last = originalRts.Count - 1;
            if (retentionTime >= originalRts[last]) {
                return correctedRts[last];
            }
            var low = 0;
            var high = last;
            while (low + 1 < high) {
                var middle = low + (high - low) / 2;
                if (originalRts[middle] < retentionTime) {
                    low = middle;
                }
                else {
                    high = middle;
                }
            }
            var lower = low;
            var upper = high;
            var width = originalRts[upper] - originalRts[lower];
            if (width <= 0d) {
                return correctedRts[lower];
            }
            var ratio = (retentionTime - originalRts[lower]) / width;
            return correctedRts[lower] + ratio * (correctedRts[upper] - correctedRts[lower]);
        }

        private static List<string> ResolveRawFiles(IEnumerable<string> inputPaths) {
            var files = new List<string>();
            foreach (var inputPath in inputPaths) {
                if ((File.Exists(inputPath) && IsSupportedRawPath(inputPath)) || IsVendorDataDirectory(inputPath)) {
                    files.Add(Path.GetFullPath(inputPath));
                    continue;
                }
                if (File.Exists(inputPath)) {
                    Console.Error.WriteLine($"Unsupported raw data path: {inputPath}");
                    continue;
                }
                if (!Directory.Exists(inputPath)) {
                    Console.Error.WriteLine($"Input path was not found: {inputPath}");
                    continue;
                }

                files.AddRange(Directory.EnumerateFileSystemEntries(inputPath)
                    .Where(path => File.Exists(path) || IsVendorDataDirectory(path))
                    .Where(IsSupportedRawPath)
                    .Select(Path.GetFullPath));
            }
            return files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToList();
        }

        private static bool IsVendorDataDirectory(string path) {
            return Directory.Exists(path) && IsSupportedRawPath(path);
        }

        private static bool IsSupportedRawPath(string path) {
            var extension = Path.GetExtension(path);
            return extension.Equals(".abf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".cdf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".d", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ibf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".mzml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".raw", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".wiff", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".wiff2", StringComparison.OrdinalIgnoreCase);
        }

        private static FileSystemInfo ToFileSystemInfo(string path) {
            return Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path);
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
            Console.Error.WriteLine("MsdialConsoleApp.exe eic rtcorrection -i <raw file or folder> [-i <raw file> ...] -library <standard txt> -o <output csv> [-m <method file>] [-selection <edited selections.tsv>] [-ionmode Positive|Negative] [-acquisitiontype DDA|SWATH|AIF]");
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
