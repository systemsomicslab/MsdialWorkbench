using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process;

public sealed class RtCorrectionProcess
{
    public int Run(
        IReadOnlyList<FileSystemInfo> inputPaths,
        FileInfo libraryFile,
        FileInfo outputFile,
        FileInfo? methodFile,
        FileInfo? selectionFile,
        IonMode ionMode,
        AcquisitionType acquisitionType) {
        if (inputPaths.Count == 0) {
            Console.Error.WriteLine("At least one raw data input is required.");
            return -1;
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
        parameter.RtCorrectionPeakSelectionFilePath = selectionFile?.FullName ?? string.Empty;
        var analysisFiles = CreateAnalysisFiles(rawFiles, acquisitionType, outputFile.FullName);
        RetentionTimeCorrectionProcess.Prepare(
            analysisFiles,
            parameter,
            Path.GetDirectoryName(outputFile.FullName) ?? ".");
        var analysisFilesByPath = analysisFiles.ToDictionary(
            file => Path.GetFullPath(file.AnalysisFilePath),
            StringComparer.OrdinalIgnoreCase);
        using var writer = new StreamWriter(
            outputFile.FullName,
            false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.WriteLine("FileName,FilePath,StandardId,StandardName,ReferenceRT,RTTolerance,TargetMz,MzTolerance,MinimumHeight,ScanId,RT,CorrectedRT,Intensity,SmoothedIntensity");
        var successfulFiles = 0;
        foreach (var rawFile in rawFiles) {
            Console.WriteLine($"Extracting RT correction EICs: {rawFile}");
            var spectra = LoadMeasurement(rawFile);
            if (spectra.Count == 0) {
                Console.Error.WriteLine($"No raw spectra were found: {rawFile}");
                continue;
            }
            WriteAnchorEics(
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

    private static List<AnalysisFileBean> CreateAnalysisFiles(
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

    private static void WriteAnchorEics(
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
            using var smoothed = chromatogram.ChromatogramSmoothing(
                SmoothingMethod.LinearWeightedMovingAverage, 3);
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
        var width = originalRts[high] - originalRts[low];
        if (width <= 0d) {
            return correctedRts[low];
        }
        var ratio = (retentionTime - originalRts[low]) / width;
        return correctedRts[low] + ratio * (correctedRts[high] - correctedRts[low]);
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
        return files
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path)
            .ToList();
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

    private static IReadOnlyList<RawSpectrum> LoadMeasurement(string inputFile) {
        using var access = new RawDataAccess(inputFile, 0, false, false, false);
        var measurement = access.GetMeasurement();
        return measurement?.SpectrumList ?? [];
    }

    private static string CsvEscape(string value) {
        if (value == null) {
            return string.Empty;
        }
        return value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n")
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
