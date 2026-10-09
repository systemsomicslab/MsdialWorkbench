using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

internal static class RetentionTimeCorrectionProcess {
    private const string SelectionHeader = "File path\tFile name\tStandard ID\tStandard name\tReference RT (min)\tDetected RT (min)\tSelected RT (min)\tUse\tPeak height";

    /// <summary>
    /// Detect the anchors in every file and build each file's RT correction.
    /// </summary>
    /// <remarks>
    /// The anchors are passed in, already loaded by LoadStandards, rather than read here. The
    /// caller loads them before it reads anything heavy, so a malformed library -- a format that
    /// is easy to get wrong -- stops the run before the raw data and the annotation libraries are
    /// read, and this reads the file no second time.
    /// </remarks>
    public static void Prepare(
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        ParameterBase parameter,
        IReadOnlyList<MoleculeMsReference> standardLibrary,
        string outputFolder) {
        var rtParameter = parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam;
        if (!rtParameter.ExcuteRtCorrection) {
            return;
        }
        if (standardLibrary.Count == 0) {
            throw new InvalidOperationException("RT correction needs the anchor library loaded by LoadStandards.");
        }

        var standards = standardLibrary.ToList();
        parameter.RetentionTimeCorrectionCommon.StandardLibrary = standards;
        Console.WriteLine($"RT correction started with {standards.Count} anchor peak(s).");
        var selectionDescription = rtParameter.PeakSelectionMode == RetentionTimeCorrectionPeakSelectionMode.Weighted
            ? $"{rtParameter.PeakSelectionMode} (RT weight {rtParameter.PeakSelectionRtWeight.ToString(CultureInfo.InvariantCulture)})"
            : rtParameter.PeakSelectionMode.ToString();
        Console.WriteLine($"RT correction automatic peak selection: {selectionDescription}.");

        var originalMinimumAmplitude = parameter.MinimumAmplitude;
        parameter.MinimumAmplitude = standards.Min(standard => standard.MinimumPeakHeight);
        try {
            var completed = 0;
            var parallelOptions = new ParallelOptions {
                MaxDegreeOfParallelism = Math.Max(1, parameter.NumThreads),
            };
            Parallel.ForEach(analysisFiles, parallelOptions, file => {
                var providerFactory = new StandardDataProviderFactory { IgnoreRtCorrection = true };
                var provider = providerFactory.Create(file);
                RetentionTimeCorrection.Execute(file, parameter, provider);
                var current = Interlocked.Increment(ref completed);
                Console.WriteLine($"RT correction anchor detection: {current}/{analysisFiles.Count} ({file.AnalysisFileName})");
            });
        }
        finally {
            parameter.MinimumAmplitude = originalMinimumAmplitude;
        }

        Directory.CreateDirectory(outputFolder);
        var selectionInput = parameter.ReferenceFileParam.RtCorrectionPeakSelectionFilePath;
        var detectedRts = SnapshotDetectedRetentionTimes(analysisFiles);
        if (!string.IsNullOrEmpty(selectionInput)) {
            ApplySelectionFile(selectionInput, analysisFiles, parameter.RetentionTimeCorrectionCommon);
        }
        else {
            InitializeCellInfo(analysisFiles, parameter.RetentionTimeCorrectionCommon);
        }

        var commonStandards = RetentionTimeCorrectionMethod.MakeCommonStdList(analysisFiles.ToList(), standards);
        RetentionTimeCorrectionMethod.UpdateRtCorrectionBean(
            analysisFiles.ToList(),
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parameter.NumThreads) },
            rtParameter,
            commonStandards);

        var auditName = selectionInput.IsEmptyOrNull()
            ? "rt_correction_peak_selections.tsv"
            : "rt_correction_peak_selections_applied.tsv";
        var auditPath = Path.Combine(outputFolder, auditName);
        WriteSelectionFile(auditPath, analysisFiles, detectedRts);
        Console.WriteLine($"RT correction peak selections: {auditPath}");
        Console.WriteLine("RT correction finished.");
    }

    public static void WriteSelectionFile(
        string outputPath,
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        IReadOnlyDictionary<(string FilePath, int StandardId), double>? detectedRts = null) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.WriteLine(SelectionHeader);
        foreach (var file in analysisFiles) {
            foreach (var standard in file.RetentionTimeCorrectionBean.StandardList.OrderBy(item => item.Reference.ScanID)) {
                var selectedRt = standard.SamplePeakAreaBean.PeakFeature.ChromXsTop.RT.Value;
                var key = (Path.GetFullPath(file.AnalysisFilePath), standard.Reference.ScanID);
                var detectedRt = detectedRts is not null && detectedRts.TryGetValue(key, out var originalRt)
                    ? originalRt
                    : selectedRt;
                writer.WriteLine(string.Join("\t",
                    file.AnalysisFilePath,
                    file.AnalysisFileName,
                    standard.Reference.ScanID.ToString(CultureInfo.InvariantCulture),
                    standard.Reference.Name,
                    standard.Reference.ChromXs.RT.Value.ToString(CultureInfo.InvariantCulture),
                    detectedRt.ToString(CultureInfo.InvariantCulture),
                    selectedRt.ToString(CultureInfo.InvariantCulture),
                    (selectedRt > 0d).ToString(),
                    standard.SamplePeakAreaBean.PeakFeature.PeakHeightTop.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    private static Dictionary<(string FilePath, int StandardId), double> SnapshotDetectedRetentionTimes(
        IReadOnlyList<AnalysisFileBean> analysisFiles) {
        return analysisFiles
            .SelectMany(file => file.RetentionTimeCorrectionBean.StandardList.Select(standard => new {
                Key = (Path.GetFullPath(file.AnalysisFilePath), standard.Reference.ScanID),
                Rt = standard.SamplePeakAreaBean.PeakFeature.ChromXsTop.RT.Value,
            }))
            .ToDictionary(item => item.Key, item => item.Rt);
    }

    /// <summary>
    /// Load and check the RT correction anchors from the library the parameters name, as they
    /// stand now.
    /// </summary>
    /// <remarks>
    /// CALL THIS BEFORE ANYTHING HEAVY IS READ. The library is small and its format is easy to
    /// get wrong, so it is checked before the analysis files, the annotation libraries and the
    /// raw data are touched: a mistake in it costs seconds, not a run. The anchors it returns are
    /// handed to Prepare, so the file is read once.
    ///
    /// CALL IT AFTER EVERY PATH REWRITE. ConfigParser used to load the library the moment it read
    /// "Compounds library file path for RT correction", from the value exactly as written. A
    /// relative value was therefore opened from the working directory, and anything that
    /// rewrote the path afterwards -- GC-MS resolves it against the method file's folder, the
    /// rt-correction command replaces it with its own library argument -- left the stored library
    /// and the stored path naming different files. Loaded from CompoundListForRtCorrectionPath
    /// once the method file has been read, the anchors are the file the parameters name.
    ///
    /// Nothing else reads StandardLibrary in the console or in the core code it calls:
    /// RetentionTimeCorrection.Execute is reached only from Prepare, and ParametersAsText is
    /// never called by the console.
    ///
    /// A library with errors is refused with the parser's own message, which the caller prints.
    /// </remarks>
    internal static List<MoleculeMsReference> LoadStandards(ParameterBase parameter) {
        if (parameter.CompoundListForRtCorrectionPath.IsEmptyOrNull()) {
            throw new InvalidOperationException("Compounds library file path for RT correction is required when Execute RT correction is True.");
        }
        return LoadStandards(parameter.CompoundListForRtCorrectionPath);
    }

    /// <summary>
    /// Load and check the RT correction anchors from one library file.
    /// </summary>
    internal static List<MoleculeMsReference> LoadStandards(string libraryPath) {
        if (!File.Exists(libraryPath)) {
            throw new FileNotFoundException("RT correction anchor library was not found.", libraryPath);
        }

        var standards = TextLibraryParser.StandardTextLibraryReader(libraryPath, out var error)
            ?.Where(standard => standard.IsTargetMolecule)
            .OrderBy(standard => standard.ChromXs.RT.Value)
            .ToList() ?? [];
        if (!error.IsEmptyOrNull()) {
            throw new InvalidDataException(error);
        }
        if (standards.Count == 0) {
            throw new InvalidDataException("The RT correction anchor library contains no enabled standards.");
        }
        return standards;
    }

    private static void ApplySelectionFile(
        string selectionPath,
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        RetentionTimeCorrectionCommon rtCorrectionCommon) {
        if (!File.Exists(selectionPath)) {
            throw new FileNotFoundException("RT correction peak selection file was not found.", selectionPath);
        }

        var filesByPath = analysisFiles.ToDictionary(
            file => Path.GetFullPath(file.AnalysisFilePath),
            StringComparer.OrdinalIgnoreCase);
        var filesByName = analysisFiles
            .GroupBy(file => file.AnalysisFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(selectionPath);
        if (lines.Length == 0) {
            throw new InvalidDataException("RT correction peak selection file is empty.");
        }

        var headers = lines[0].Split('\t');
        var indexes = headers
            .Select((header, index) => (Header: header.Trim(), Index: index))
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);
        var required = new[] { "Standard ID", "Selected RT (min)", "Use" };
        foreach (var header in required) {
            if (!indexes.ContainsKey(header)) {
                throw new InvalidDataException($"RT correction peak selection file is missing the '{header}' column.");
            }
        }
        if (!indexes.ContainsKey("File path") && !indexes.ContainsKey("File name")) {
            throw new InvalidDataException("RT correction peak selection file requires either 'File path' or 'File name'.");
        }

        var applied = 0;
        for (var lineNumber = 1; lineNumber < lines.Length; lineNumber++) {
            if (string.IsNullOrWhiteSpace(lines[lineNumber])) {
                continue;
            }
            var cells = lines[lineNumber].Split('\t');
            var file = ResolveFile(cells, indexes, filesByPath, filesByName, lineNumber + 1);
            if (!TryGetCell(cells, indexes["Standard ID"], out var standardIdText)
                || !int.TryParse(standardIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var standardId)) {
                throw new InvalidDataException($"Invalid Standard ID at line {lineNumber + 1}.");
            }
            if (!TryGetCell(cells, indexes["Use"], out var useText) || !bool.TryParse(useText, out var use)) {
                throw new InvalidDataException($"Invalid Use value at line {lineNumber + 1}; use True or False.");
            }
            if (!TryGetCell(cells, indexes["Selected RT (min)"], out var selectedRtText)
                || !double.TryParse(selectedRtText, NumberStyles.Float, CultureInfo.InvariantCulture, out var selectedRt)) {
                throw new InvalidDataException($"Invalid Selected RT (min) at line {lineNumber + 1}.");
            }

            var pair = file.RetentionTimeCorrectionBean.StandardList.SingleOrDefault(item => item.Reference.ScanID == standardId)
                ?? throw new InvalidDataException($"Standard ID {standardId} was not found for '{file.AnalysisFileName}' at line {lineNumber + 1}.");
            pair.SamplePeakAreaBean.ChromXs.RT = new RetentionTime(use ? selectedRt : 0d, ChromXUnit.Min);
            applied++;
        }

        InitializeCellInfo(analysisFiles, rtCorrectionCommon, manuallyModified: true);
        Console.WriteLine($"Applied {applied} RT correction peak selection row(s) from {selectionPath}.");
    }

    private static AnalysisFileBean ResolveFile(
        string[] cells,
        IReadOnlyDictionary<string, int> indexes,
        IReadOnlyDictionary<string, AnalysisFileBean> filesByPath,
        IReadOnlyDictionary<string, List<AnalysisFileBean>> filesByName,
        int lineNumber) {
        if (indexes.TryGetValue("File path", out var pathIndex)
            && TryGetCell(cells, pathIndex, out var path)
            && !path.IsEmptyOrNull()) {
            var fullPath = Path.GetFullPath(path);
            if (filesByPath.TryGetValue(fullPath, out var file)) {
                return file;
            }
        }
        if (indexes.TryGetValue("File name", out var nameIndex)
            && TryGetCell(cells, nameIndex, out var name)
            && filesByName.TryGetValue(name, out var matches)
            && matches.Count == 1) {
            return matches[0];
        }
        throw new InvalidDataException($"Analysis file could not be resolved at line {lineNumber}.");
    }

    private static bool TryGetCell(string[] cells, int index, out string value) {
        if (index >= 0 && index < cells.Length) {
            value = cells[index].Trim();
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static void InitializeCellInfo(
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        RetentionTimeCorrectionCommon rtCorrectionCommon,
        bool manuallyModified = false) {
        rtCorrectionCommon.SampleCellInfoListList = analysisFiles
            .Select(file => file.RetentionTimeCorrectionBean.StandardList
                .Select(pair => pair.SamplePeakAreaBean.PeakFeature.ChromXsTop.RT.Value <= 0d
                    ? SampleListCellInfo.Zero
                    : manuallyModified ? SampleListCellInfo.ManualModified : SampleListCellInfo.Normal)
                .ToList())
            .ToList();
    }
}
