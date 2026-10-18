using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Normalize;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcMsApi.Export;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process;

/// <summary>
/// Normalizes an aligned result against internal standards, from the command line.
/// </summary>
/// <remarks>
/// The algorithm already lives in MsdialCore and is what the graphical application
/// calls; only the way in was missing. This supplies it: read a saved project and its
/// alignment, build the standard table, normalize, export.
///
/// One thing the graphical application can take for granted and a command line cannot:
/// which aligned peak *is* each standard. StandardCompound identifies it by PeakID,
/// which is an alignment ID and therefore belongs to one alignment and no other. A
/// table written for one run would silently point at unrelated peaks in the next, so a
/// standard may instead name itself and be resolved against the annotations of the
/// alignment actually being normalized. An unresolved standard stops the run rather
/// than quietly normalizing its whole lipid class against nothing.
///
/// A caller that has already decided which peak each standard is -- by m/z, retention
/// time and adduct, none of which the name lookup looks at -- needs the opposite
/// guarantee: that a standard it could not find stays unfound. With --peak-ids-only a
/// standard is taken from its PeakID or not at all. Such a caller may also have found a
/// standard that MS-DIAL left unnamed, or named as something else; AssignName exports
/// that peak under the standard's name, so MS-DIAL's own matrix says what it is.
/// </remarks>
public sealed class NormalizationProcess {
    /// <summary>Label rows plus the column-name row that precede the data.</summary>
    private const int HeaderRowCount = 5;

    /// <summary>The annotation method a peak named by AssignName is exported with.</summary>
    internal const string AssignmentMethod = "internal standard assignment";

    public int Run(
        FileInfo projectFile,
        FileInfo standardsFile,
        DirectoryInfo outputDirectory,
        IonAbundanceUnit unit,
        int alignmentIndex,
        bool applyDilutionFactor,
        bool allowUnresolvedStandards,
        bool allowMismatchedPeakIds,
        bool peakIdsOnly = false) {
        var options = new NormalizeOptions {
            Unit = unit,
            ApplyDilutionFactor = applyDilutionFactor,
            AllowUnresolvedStandards = allowUnresolvedStandards,
            AllowMismatchedPeakIds = allowMismatchedPeakIds,
            PeakIdsOnly = peakIdsOnly,
        };
        if (!projectFile.Exists) {
            Console.Error.WriteLine($"Project file was not found: {projectFile.FullName}");
            return -1;
        }
        if (!standardsFile.Exists) {
            Console.Error.WriteLine($"Internal standard table was not found: {standardsFile.FullName}");
            return -1;
        }

        // The table first: one the options refuse is refused before a project is loaded for
        // nothing.
        List<StandardRecord> records;
        try {
            records = ReadStandards(standardsFile.FullName, options.PeakIdsOnly);
        }
        catch (FormatException error) {
            Console.Error.WriteLine(error.Message);
            return -1;
        }
        if (records.Count == 0) {
            Console.Error.WriteLine("The internal standard table contains no rows.");
            return -1;
        }

        // The data storage is only half a project: the annotation databases live beside it
        // and the raw MessagePack load leaves them null, which surfaces much later as a
        // null reference inside the evaluator. Load it the way the application does.
        IMsdialDataStorage<ParameterBase> storage;
        try {
            storage = LoadProject(projectFile.FullName);
        }
        catch (Exception error) {
            // The run writes both a .mdproject and a .mddata, and only the second holds
            // the data storage. Passing the one that looks more like a project produced
            // fifteen frames of MessagePack internals and no statement of what to do.
            var extension = Path.GetExtension(projectFile.FullName);
            var sibling = Path.ChangeExtension(projectFile.FullName, ".mddata");
            var advice = File.Exists(sibling) && !extension.Equals(".mddata", StringComparison.OrdinalIgnoreCase)
                ? $" Pass the data file beside it instead: {sibling}"
                : " Pass the .mddata file written by the analysis run.";
            Console.Error.WriteLine(
                $"{projectFile.FullName} could not be read as an MS-DIAL data file "
                + $"({error.GetType().Name})." + advice);
            return -1;
        }
        var files = storage.AnalysisFiles.Where(file => file.AnalysisFileIncluded).ToList();
        if (files.Count == 0) {
            Console.Error.WriteLine("The project contains no included analysis files.");
            return -1;
        }
        if (storage.AlignmentFiles is null || storage.AlignmentFiles.Count == 0) {
            Console.Error.WriteLine("The project contains no alignment result to normalize.");
            return -1;
        }
        if (alignmentIndex < 0 || alignmentIndex >= storage.AlignmentFiles.Count) {
            Console.Error.WriteLine(
                $"Alignment index {alignmentIndex} is outside the project's {storage.AlignmentFiles.Count} alignment result(s).");
            return -1;
        }

        var alignmentFile = storage.AlignmentFiles[alignmentIndex];
        var container = AlignmentResultContainer.Load(alignmentFile);
        var spots = container.AlignmentSpotProperties;
        if (spots is null || spots.Count == 0) {
            Console.Error.WriteLine("The alignment result contains no spots.");
            return -1;
        }

        var result = NormalizeAndExport(
            files,
            spots,
            records,
            storage.DataBaseMapper,
            FacadeMatchResultEvaluator.FromDataBases(storage.DataBases),
            storage.Parameter,
            () => MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _),
            outputDirectory.FullName,
            alignmentFile.FileName,
            options);
        if (result == 0) {
            container.IsNormalized = true;
        }
        return result;
    }

    internal sealed class NormalizeOptions {
        public IonAbundanceUnit Unit = IonAbundanceUnit.NormalizedByInternalStandardPeakHeight;
        public bool ApplyDilutionFactor;
        public bool AllowUnresolvedStandards;
        public bool AllowMismatchedPeakIds;
        public bool PeakIdsOnly;
    }

    /// <summary>
    /// Resolves the standards, normalizes the spots in place and writes both matrices.
    /// </summary>
    /// <remarks>
    /// Everything after the project has been loaded, so that it can be run on an alignment
    /// built in memory.
    /// </remarks>
    internal static int NormalizeAndExport(
        IReadOnlyList<AnalysisFileBean> files,
        IReadOnlyList<AlignmentSpotProperty> spots,
        IReadOnlyList<StandardRecord> records,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        ParameterBase parameter,
        Func<IReadOnlyList<MSDecResult>> readSpectra,
        string outputDirectory,
        string alignmentName,
        NormalizeOptions options) {
        if (options.ApplyDilutionFactor) {
            // Multiplying by zero, a negative or a missing factor gives a number that looks
            // like a concentration and is not one.
            var unusable = files
                .Where(file => !(file.DilutionFactor > 0d) || double.IsInfinity(file.DilutionFactor))
                .ToList();
            if (unusable.Count > 0) {
                Console.Error.WriteLine(
                    $"--apply-dilution-factor multiplies each file's values by its dilution factor, and {unusable.Count} "
                    + "file(s) have none that can be used: "
                    + string.Join(", ", unusable.Select(file => $"{file.AnalysisFileName} ({Format(file.DilutionFactor)})"))
                    + ". Set a positive dilution factor for every included file, or leave the option out.");
                return -1;
            }
        }

        var resolution = ResolveStandards(records, spots, options.PeakIdsOnly);
        foreach (var line in resolution.Report) {
            Console.WriteLine(line);
        }
        if (resolution.NeverLookedUp.Count > 0) {
            // Where those peaks go is where any peak without a standard of its own class goes,
            // and that depends on the "Any others" row: there may be none, or it may not resolve.
            var destination = resolution.FallbackName.Length > 0
                ? $"Their peaks go to the {StandardCompound.AnyOthers} standard {resolution.FallbackName}, not to the one named for them."
                : resolution.UnresolvedFallback.Length > 0
                    ? $"Their peaks would go to the {StandardCompound.AnyOthers} standard {resolution.UnresolvedFallback}, "
                        + "which did not resolve, so they carry no concentration."
                    : $"With no {StandardCompound.AnyOthers} standard in the table, their peaks are exported as raw intensities.";
            Console.WriteLine(
                $"WARNING: {resolution.NeverLookedUp.Count} class(es) in the internal standard table have no MS-DIAL "
                + $"lipid class name, so the normalizer never looks a peak up under them: {string.Join("; ", resolution.NeverLookedUp)}. "
                + destination);
        }
        if (resolution.AssignmentConflicts.Count > 0) {
            Console.Error.WriteLine(
                $"{resolution.AssignmentConflicts.Count} alignment ID(s) are assigned to one standard and taken as "
                + $"another: {string.Join("; ", resolution.AssignmentConflicts)}. A peak is one standard or the other; "
                + "correct the PeakID or the AssignName column.");
            return 2;
        }
        if (resolution.Mismatched.Count > 0 && !options.AllowMismatchedPeakIds) {
            // A standard that cannot be found already stops the run. One found and
            // demonstrably pointing at a different compound is the worse case of the two,
            // and it used to warn and carry on -- quantifying a lipid class against
            // whatever happened to occupy that alignment ID.
            if (resolution.MismatchedTaken.Count > 0) {
                var remedy = options.PeakIdsOnly
                    ? "Correct the PeakID"
                    : "Remove the PeakID column so the standards resolve by name";
                Console.Error.WriteLine(
                    $"{resolution.MismatchedTaken.Count} standard(s) name an alignment ID that holds a different "
                    + $"compound or none: {string.Join("; ", resolution.MismatchedTaken)}. An alignment ID belongs to the "
                    + $"run it was written for. {remedy}, or pass --allow-mismatched-peak-ids if the annotations are wrong "
                    + "rather than the table.");
            }
            // Renaming a peak is what AssignName is for, and the peak it renames carries
            // another name, or none, by definition. The guard is the same; the advice is not.
            if (resolution.MismatchedRenames.Count > 0) {
                Console.Error.WriteLine(
                    $"{resolution.MismatchedRenames.Count} standard(s) are assigned by AssignName to a peak that carries "
                    + $"another name or none: {string.Join("; ", resolution.MismatchedRenames)}. AssignName renames such a "
                    + "peak only with --allow-mismatched-peak-ids: pass it if these alignment IDs were found in this "
                    + "alignment, so that a table written for another run cannot rename an unrelated peak.");
            }
            return 2;
        }
        if (resolution.Mismatched.Count > 0) {
            Console.WriteLine(
                $"WARNING: {resolution.Mismatched.Count} standard(s) were taken from an alignment ID that holds "
                + $"a different compound: {string.Join("; ", resolution.Mismatched)}.");
        }
        if (resolution.UnresolvedNeeded.Count > 0) {
            var summary = string.Join(", ", resolution.UnresolvedNeeded);
            var lost = string.Join(", ", resolution.ClassesLost);
            if (!options.AllowUnresolvedStandards) {
                var remedy = options.PeakIdsOnly
                    ? "Give each one the alignment ID of its peak"
                    : "Confirm the annotation";
                Console.Error.WriteLine(
                    $"{resolution.UnresolvedNeeded.Count} internal standard(s) were not found in the alignment: {summary}. "
                    + $"Rows in the classes only they would quantify would be left without a concentration: {lost}. "
                    + $"{remedy}, or pass --allow-unresolved-standards to continue, which "
                    + "empties those rows rather than quantifying them against another class.");
                return 2;
            }
            Console.WriteLine(
                $"WARNING: {resolution.UnresolvedNeeded.Count} internal standard(s) did not resolve: {summary}. "
                + $"Rows in the classes only they would quantify carry no concentration and say so: {lost}.");
        }
        if (resolution.UnresolvedNotNeeded.Count > 0) {
            // Said, but not refused: nothing in the matrix depends on them.
            Console.WriteLine(
                $"WARNING: {resolution.UnresolvedNotNeeded.Count} internal standard(s) did not resolve and are not "
                + $"needed, so no row is emptied for them: {string.Join("; ", resolution.UnresolvedNotNeeded)}.");
        }
        if (resolution.Compounds.Count == 0) {
            Console.Error.WriteLine("No internal standard could be resolved, so nothing can be normalized.");
            return 2;
        }

        // The dilution factor is applied here rather than by the normalizer, which pairs each
        // spot's values with the included files by position. The values cover every file in
        // the project, so with a file excluded anywhere but last, each later file took its
        // neighbour's factor.
        Normalization.SplashNormalize(
            files,
            spots,
            refer,
            resolution.Compounds,
            options.Unit,
            evaluator,
            applyDilutionFactor: false);
        if (options.ApplyDilutionFactor) {
            ApplyDilutionFactors(files, spots);
        }

        var adjustments = PlanExportAdjustments(files, spots, refer, evaluator, resolution);
        foreach (var line in adjustments.Warnings) {
            Console.WriteLine(line);
        }

        var decResults = readSpectra();
        var accessor = new StandardAssigningMetadataAccessor(refer, parameter, adjustments.Assigned);
        var stats = new[] { StatsValue.Average, StatsValue.Stdev };
        var fileNames = files.OrderBy(file => file.AnalysisFileId).Select(file => file.AnalysisFileName).ToList();
        Directory.CreateDirectory(outputDirectory);

        // Both matrices, always. The normalized one is derived from the raw one by a
        // division nobody can check without seeing both, and a concentration published
        // without the measurement behind it asks to be taken on trust.
        // "Height" is the raw peak height and normalizing does not touch it: it writes to
        // a separate field, so exporting the wrong one produces a file identical to the
        // input while every log line reports success.
        var written = new List<string>();
        foreach (var (exportType, suffix) in new[] {
            ("Height", "_Height.txt"),
            ("Normalized height", "_NormalizedHeight.txt"),
        }) {
            var path = Path.Combine(outputDirectory, alignmentName + suffix);
            using (var stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                new AlignmentCSVExporter().Export(
                    stream, spots, decResults, files, new MulticlassFileMetaAccessor(0), accessor,
                    new IncludedFilesQuantValueAccessor(exportType, parameter, files), stats);
            }
            int redacted;
            try {
                redacted = AdjustExport(path, adjustments, fileNames, normalized: exportType == "Normalized height");
            }
            catch (InvalidDataException error) {
                // A matrix the redaction could not read would leave substituted numbers in
                // place; better no file than that one.
                File.Delete(path);
                Console.Error.WriteLine($"{path} was not written: {error.Message}");
                return -1;
            }
            written.Add(path);
            if (redacted > 0) {
                Console.WriteLine(
                    $"{redacted} row(s) had no standard of their own class and carry no concentration.");
            }
        }

        Console.WriteLine($"Normalized unit: {options.Unit}");
        Console.WriteLine($"Dilution factor applied: {options.ApplyDilutionFactor}");
        if (options.ApplyDilutionFactor) {
            Console.WriteLine(
                "Dilution factors (multiplied): "
                + string.Join(", ", files.Select(file => $"{file.AnalysisFileName} {Format(file.DilutionFactor)}")));
        }
        foreach (var path in written) {
            Console.WriteLine(path);
        }
        return 0;
    }

    /// <summary>
    /// Multiplies each file's normalized values by that file's dilution factor.
    /// </summary>
    /// <remarks>
    /// Matched by file ID, so an excluded file between two included ones changes nothing.
    /// Like the normalizer, it covers every spot, the internal standards included. An
    /// excluded file's values are left unmultiplied; neither matrix shows them, in a column
    /// or in a class statistic (<see cref="IncludedFilesQuantValueAccessor"/>).
    /// </remarks>
    private static void ApplyDilutionFactors(
        IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<AlignmentSpotProperty> spots) {
        var factors = files.ToDictionary(file => file.AnalysisFileId, file => file.DilutionFactor);
        foreach (var spot in spots) {
            foreach (INormalizableValue value in spot.AlignedPeakProperties) {
                if (!factors.TryGetValue(value.FileID, out var factor)) continue;
                value.NormalizedPeakHeight *= factor;
                value.NormalizedPeakAreaAboveBaseline *= factor;
                value.NormalizedPeakAreaAboveZero *= factor;
            }
        }
    }

    /// <summary>What the exported matrices say beyond what the normalizer wrote.</summary>
    internal sealed class ExportAdjustments {
        /// <summary>Alignment ID -> why the whole row carries no concentration.</summary>
        public Dictionary<int, string> Redacted { get; } = new Dictionary<int, string>();

        /// <summary>Alignment ID -> the files left empty in that row, and why.</summary>
        public Dictionary<int, PartialRedaction> PartlyRedacted { get; } = new Dictionary<int, PartialRedaction>();

        /// <summary>Alignment ID -> the standard the peak is exported as.</summary>
        public Dictionary<int, string> Assigned { get; } = new Dictionary<int, string>();

        public List<string> Warnings { get; } = new List<string>();
    }

    internal sealed class PartialRedaction {
        public PartialRedaction(IReadOnlyCollection<string> files, string reason) {
            Files = files;
            Reason = reason;
        }

        public IReadOnlyCollection<string> Files { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Decides which numbers are removed from the normalized matrix, and which peaks are
    /// exported under a standard's name.
    /// </summary>
    /// <remarks>
    /// When a class's own standard does not resolve, the normalizer does not leave that
    /// class alone: it falls through to the "Any others" standard and quantifies the class
    /// against a compound of an entirely different one. A cardiolipin divided by a
    /// lysophosphatidylcholine is not a concentration, and it was written into the matrix
    /// in the same unit, with the same comment, as a properly quantified row -- nothing in
    /// the file told them apart. With "Any others" itself unresolved there is nothing to
    /// fall through to, and the raw peak height was exported in its place.
    ///
    /// A standard with no height in a file is the same problem inside one file: the
    /// normalizer divides by 1 instead, which turns a peak height into a number in the
    /// concentration's unit several orders of magnitude off. Those cells are emptied, not
    /// the row, because the other files had the standard to divide by.
    ///
    /// A row's class is the one the normalizer looked it up under (<see cref="NormalizerClass"/>),
    /// compared with the table's class as it reached the normalizer. Comparing the exported
    /// Ontology column with the table as written missed every class spelled one way in the
    /// one and the other way in the other: every ceramide, hexosylceramide and the like. Nor
    /// is the Ontology column what the normalizer reads: a peak that is not reference
    /// matched is "Unknown" to it, and divided by "Any others", whatever the column says;
    /// emptying it by its label threw away a value the normalizer had given it on the same
    /// terms as any unknown peak. Whether a row was quantified at all is read from what the
    /// normalizer did, not from its unit, which a caller may name "Intensity" itself.
    ///
    /// A standard's own row is left as the normalizer wrote it when the standard is what it was
    /// divided by: its amount on a concentration unit, its average height on the default
    /// unit. The default unit looks every peak up by its class, the standards' own peaks
    /// included, so a standard whose class has another standard -- or none, such as one that
    /// AssignName found on a peak MS-DIAL left unnamed -- is divided by that other one, and
    /// its row is checked like any other.
    /// </remarks>
    internal static ExportAdjustments PlanExportAdjustments(
        IReadOnlyList<AnalysisFileBean> files,
        IReadOnlyList<AlignmentSpotProperty> spots,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        StandardResolution resolution) {
        var plan = new ExportAdjustments();
        foreach (var pair in resolution.Assignments) {
            plan.Assigned[pair.Key] = pair.Value;
        }

        var byId = spots.ToDictionary(spot => spot.MasterAlignmentID, spot => spot);
        var included = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
        var standards = resolution.Compounds
            .GroupBy(compound => compound.PeakID)
            .ToDictionary(
                group => group.Key,
                group => string.Join(" / ", group.Select(compound => compound.StandardName).Distinct(StringComparer.OrdinalIgnoreCase)));

        var heightless = new Dictionary<int, List<string>>();
        foreach (var id in standards.Keys) {
            if (!byId.TryGetValue(id, out var standard)) continue;
            var missing = standard.AlignedPeakProperties
                .Where(peak => included.ContainsKey(peak.FileID) && !(((INormalizableValue)peak).PeakHeight > 0d))
                .Select(peak => included[peak.FileID])
                .ToList();
            if (missing.Count > 0) {
                heightless[id] = missing;
            }
        }

        var emptied = heightless.Keys.ToDictionary(id => id, _ => 0);
        foreach (var spot in spots) {
            var id = spot.MasterAlignmentID;
            if (spot.InternalStandardAlignmentID == id) continue;
            if (resolution.UnresolvedClasses.TryGetValue(NormalizerClass(spot, refer, evaluator), out var designated)) {
                plan.Redacted[id] =
                    $"NOT QUANTIFIED: the {designated.TargetClass} standard {designated.StandardName} did not resolve in this alignment";
                continue;
            }
            // Both ways through the normalizer leave the standard's ID at -1 on a row they
            // filled with its raw values, and only there.
            if (resolution.UnresolvedFallback.Length > 0 && spot.InternalStandardAlignmentID < 0) {
                plan.Redacted[id] =
                    $"NOT QUANTIFIED: no standard of its own class, and the {StandardCompound.AnyOthers} standard "
                    + $"{resolution.UnresolvedFallback} did not resolve in this alignment";
                continue;
            }
            if (heightless.TryGetValue(spot.InternalStandardAlignmentID, out var missingIn)) {
                plan.PartlyRedacted[id] = new PartialRedaction(
                    missingIn,
                    $"NOT QUANTIFIED in {missingIn.Count} file(s) ({string.Join(", ", missingIn)}): the internal standard "
                    + $"{standards[spot.InternalStandardAlignmentID]} has no peak height there");
                emptied[spot.InternalStandardAlignmentID]++;
            }
        }
        foreach (var pair in heightless.OrderBy(item => item.Key)) {
            plan.Warnings.Add(
                $"WARNING: internal standard {standards[pair.Key]} (alignment ID {pair.Key}) has no peak height in "
                + $"{pair.Value.Count} file(s): {string.Join(", ", pair.Value)}. The {emptied[pair.Key]} row(s) it "
                + "normalizes are left empty in those files instead of being divided by 1.");
        }
        return plan;
    }

    /// <summary>
    /// Rewrites one exported matrix: removes the numbers <paramref name="plan"/> says carry
    /// no concentration.
    /// </summary>
    /// <remarks>
    /// Refusing outright is the default. Where the run is allowed to continue anyway, the
    /// affected rows keep their identity and lose their numbers, and say why in place of
    /// them. An annotated wrong number is still read by the next script; an empty cell is
    /// not. A class average or deviation that took in an emptied cell is emptied with it. An
    /// assigned peak is named by the exporter (<see cref="StandardAssigningMetadataAccessor"/>);
    /// emptied here, its Comment still says where its name came from.
    ///
    /// The exporter writes ASCII, so a file named in kanji heads its column with question
    /// marks, one for each character it cannot write. The header is compared with the names
    /// as the exporter wrote them, files are found by their position, which the exporter
    /// fixes by file ID, and the matrix is written back in the same encoding.
    /// </remarks>
    /// <param name="fileNames">The included files' names, ordered by file ID.</param>
    /// <returns>The number of rows emptied entirely.</returns>
    internal static int AdjustExport(
        string matrixPath, ExportAdjustments plan, IReadOnlyList<string> fileNames, bool normalized) {
        if (!normalized || (plan.Redacted.Count == 0 && plan.PartlyRedacted.Count == 0)) return 0;
        var lines = File.ReadAllLines(matrixPath, Encoding.ASCII);
        if (lines.Length <= HeaderRowCount) return 0;
        var header = lines[HeaderRowCount - 1].Split('\t');
        var classes = lines[0].Split('\t');
        var idColumn = Array.IndexOf(header, "Alignment ID");
        var nameColumn = Array.IndexOf(header, "Metabolite name");
        var commentColumn = Array.IndexOf(header, "Comment");
        var firstSample = Array.IndexOf(classes, "Class") + 1;
        var firstStat = firstSample + fileNames.Count;
        var asWritten = fileNames.Select(name => Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(name)));
        if (idColumn < 0 || nameColumn < 0 || commentColumn < 0 || firstSample <= 0 || firstStat > header.Length
            || !header.Skip(firstSample).Take(fileNames.Count).SequenceEqual(asWritten)) {
            throw new InvalidDataException(
                "its layout is not the one this command exports (Alignment ID, Metabolite name, Comment, "
                + "a Class label row and one column per included file), so the rows that carry no "
                + "concentration could not be emptied.");
        }
        var fileColumns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var position = 0; position < fileNames.Count; position++) {
            fileColumns[fileNames[position]] = firstSample + position;
        }

        var redacted = 0;
        var changed = false;
        for (var index = HeaderRowCount; index < lines.Length; index++) {
            var cells = lines[index].Split('\t');
            if (cells.Length <= Math.Max(idColumn, Math.Max(nameColumn, commentColumn))) continue;
            if (!int.TryParse(cells[idColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) continue;
            var before = string.Join("\t", cells);
            if (plan.Redacted.TryGetValue(id, out var reason)) {
                for (var column = firstSample; column < cells.Length; column++) {
                    cells[column] = string.Empty;
                }
                cells[commentColumn] = plan.Assigned.ContainsKey(id) ? WithAssignmentMethod(reason) : reason;
                redacted++;
            }
            else if (plan.PartlyRedacted.TryGetValue(id, out var partial)) {
                var classesEmptied = new HashSet<string>(StringComparer.Ordinal);
                foreach (var file in partial.Files) {
                    if (!fileColumns.TryGetValue(file, out var column) || column >= cells.Length) continue;
                    cells[column] = string.Empty;
                    if (column < classes.Length) {
                        classesEmptied.Add(classes[column]);
                    }
                }
                for (var column = firstStat; column < cells.Length && column < header.Length; column++) {
                    if (classesEmptied.Contains(header[column])) {
                        cells[column] = string.Empty;
                    }
                }
                cells[commentColumn] = cells[commentColumn].Length == 0
                    ? partial.Reason
                    : cells[commentColumn] + "; " + partial.Reason;
            }
            var after = string.Join("\t", cells);
            if (after != before) {
                lines[index] = after;
                changed = true;
            }
        }
        if (changed) {
            File.WriteAllLines(matrixPath, lines, Encoding.ASCII);
        }
        return redacted;
    }

    /// <summary>
    /// The Comment of a peak named by AssignName: its annotation method becomes the
    /// assignment, so the name is not attributed to an annotator that did not give it.
    /// </summary>
    private static string WithAssignmentMethod(string comment) {
        const string methodLabel = "Annotation method: ";
        var assigned = methodLabel + AssignmentMethod;
        var parts = comment.Length == 0
            ? new List<string>()
            : comment.Split(new[] { "; " }, StringSplitOptions.None).ToList();
        var method = parts.FindIndex(part => part.StartsWith(methodLabel, StringComparison.Ordinal));
        if (method >= 0) {
            parts[method] = assigned;
        }
        else {
            var unit = parts.FindIndex(part => part.StartsWith("Normalized unit: ", StringComparison.Ordinal));
            parts.Insert(unit >= 0 ? unit + 1 : parts.Count, assigned);
        }
        return string.Join("; ", parts);
    }

    /// <summary>
    /// The exporter's metadata, except that a peak AssignName names is exported as that
    /// standard and as nothing else.
    /// </summary>
    /// <remarks>
    /// Renaming alone was not enough. The row kept the Formula, Ontology, InChIKey, SMILES,
    /// scores and annotation tag of whatever MS-DIAL had matched to that peak, so a standard
    /// found on a peak annotated as a sphingomyelin still read as a sphingomyelin to anything
    /// keyed on those columns, under a name that said otherwise. The table says which standard
    /// a peak is, not its formula, structure or class -- its TargetClass is the class the
    /// standard quantifies, and "Any others" is none -- so those columns are written as for a
    /// peak with no annotation. What was measured stays: m/z, retention time, adduct, spectra.
    /// </remarks>
    internal sealed class StandardAssigningMetadataAccessor : LcmsMetadataAccessor {
        private readonly IReadOnlyDictionary<int, string> _assigned;

        public StandardAssigningMetadataAccessor(
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            ParameterBase parameter,
            IReadOnlyDictionary<int, string> assigned)
            : base(refer, parameter, false) {
            _assigned = assigned;
        }

        protected override Dictionary<string, string> GetContentCore(
            AlignmentSpotProperty spot, IMSScanProperty msdec, MoleculeMsReference reference, MsScanMatchResult matchResult) {
            if (!_assigned.TryGetValue(spot.MasterAlignmentID, out var standard)) {
                return base.GetContentCore(spot, msdec, reference, matchResult);
            }
            var content = base.GetContentCore(spot, msdec, null!, MsScanMatchResultContainer.UnknownResult);
            content["Metabolite name"] = standard;
            content["Comment"] = WithAssignmentMethod(content["Comment"]);
            return content;
        }
    }

    /// <summary>
    /// The exporter's values, with each class's average and deviation taken over the files the
    /// matrix shows.
    /// </summary>
    /// <remarks>
    /// The exporter's own statistics group every value the alignment holds, an excluded file's
    /// included: a value no column shows, which the dilution factor never reached, and which
    /// the normalizer divided by 1 where the standard had no height in that file.
    /// </remarks>
    internal sealed class IncludedFilesQuantValueAccessor : IQuantValueAccessor {
        private readonly LegacyQuantValueAccessor _values;
        private readonly string _exportType;
        private readonly ParameterBase _parameter;
        private readonly HashSet<int> _included;

        public IncludedFilesQuantValueAccessor(string exportType, ParameterBase parameter, IEnumerable<AnalysisFileBean> includedFiles) {
            _values = new LegacyQuantValueAccessor(exportType, parameter);
            _exportType = exportType;
            _parameter = parameter;
            _included = new HashSet<int>(includedFiles.Select(file => file.AnalysisFileId));
        }

        public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) => _values.GetQuantHeaders(files);

        public List<string> GetStatHeaders() => _values.GetStatHeaders();

        public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot) => _values.GetQuantValues(spot);

        public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) {
            var groups = spot.AlignedPeakProperties
                .Where(peak => _included.Contains(peak.FileID))
                .GroupBy(peak => _parameter.FileID_ClassName[peak.FileID], peak => DataAccess.GetSpotValue(peak, _exportType))
                .ToDictionary(group => group.Key, group => group.ToArray());
            // A class whose files are all excluded keeps its column, empty: the exporter skips a
            // class it is given no value for, and every statistic after it would move one column.
            return GetStatHeaders().ToDictionary(
                fileClass => fileClass,
                fileClass => !groups.TryGetValue(fileClass, out var values) ? string.Empty
                    : stat == StatsValue.Average ? BasicMathematics.Mean(values).ToString()
                    : ReplaceNaN(BasicMathematics.Stdev(values)).ToString());
        }

        private static double ReplaceNaN(double value) => double.IsNaN(value) ? 0d : value;
    }

    private const string UnknownClass = "Unknown", UndefinedClass = "Undefined";

    /// <summary>The MS-DIAL lipid class names, which the normalizer accepts from an Ontology.</summary>
    private static readonly HashSet<string> LipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());

    /// <summary>
    /// The class the normalizer looks a peak up under: NormalizationTargetSpot.GetAnnotatedLipidClass,
    /// which MsdialCore keeps internal.
    /// </summary>
    /// <remarks>
    /// "Unknown" for a peak that is not reference matched, whatever its label says. Otherwise
    /// the reference's CompoundClass, or its Ontology when that is empty and the Ontology is
    /// an MS-DIAL lipid class name, through the lipid class enumeration; a class the
    /// enumeration has no name for comes out "Undefined".
    /// </remarks>
    internal static string NormalizerClass(
        AlignmentSpotProperty spot,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        if (!spot.IsReferenceMatched(evaluator)) {
            return UnknownClass;
        }
        var reference = refer.Refer(spot.MatchResults.Representative);
        var lipidClass = reference?.CompoundClass;
        if (string.IsNullOrEmpty(lipidClass) && reference?.Ontology is string ontology && LipidClasses.Contains(ontology)) {
            lipidClass = ontology;
        }
        if (!string.IsNullOrEmpty(lipidClass)) {
            lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(
                LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass!));
        }
        return string.IsNullOrEmpty(lipidClass) ? UnknownClass : lipidClass!;
    }

    /// <summary>
    /// The spelling the normalizer looks a lipid class up under.
    /// </summary>
    /// <remarks>
    /// The normalizer takes a peak's class from its reference and passes it through the
    /// lipid class enumeration, so a ceramide whose reference says "Cer_NS" is looked up as
    /// "Cer-NS". A table naming the class the way the exported Ontology column spells it
    /// never matched: those peaks were quantified against "Any others" with nothing to say
    /// so. A table's class now goes through the same conversion. "Any others" is matched in
    /// any case, as the report already did. A name the enumeration has no name for is kept
    /// as written, not made "Undefined": the normalizer looks every such peak up as
    /// "Undefined", and one standard would then quantify all of them. No peak is looked up
    /// under it (<see cref="IsLookedUp"/>).
    /// </remarks>
    internal static string CanonicalClass(string? lipidClass) {
        var name = (lipidClass ?? string.Empty).Trim();
        if (name.Equals(StandardCompound.AnyOthers, StringComparison.OrdinalIgnoreCase)) {
            return StandardCompound.AnyOthers;
        }
        var converted = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(
            LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(name));
        return converted == UndefinedClass ? name : converted;
    }

    /// <summary>
    /// Whether the normalizer ever looks a peak up under a table's class: whether it is one
    /// <see cref="NormalizerClass"/> can return, or "Any others".
    /// </summary>
    internal static bool IsLookedUp(string canonicalClass) {
        return canonicalClass == StandardCompound.AnyOthers
            || canonicalClass == UnknownClass
            || canonicalClass == UndefinedClass
            || LipidClasses.Contains(canonicalClass);
    }

    private static IMsdialDataStorage<ParameterBase> LoadProject(string projectFilePath) {
        var projectFolder = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)) ?? ".";
        var projectFileName = Path.GetFileName(projectFilePath);
        var serializer = new MsdialIntegrateSerializer();
        using (IStreamManager streamManager = new DirectoryTreeStreamManager(projectFolder)) {
            var storage = serializer
                .LoadAsync(streamManager, projectFileName, projectFolder, string.Empty)
                .GetAwaiter().GetResult();
            streamManager.Complete();
            storage.FixDatasetFolder(projectFolder);
            return storage;
        }
    }

    internal sealed class StandardRecord {
        public string StandardName = string.Empty;
        public string TargetClass = string.Empty;

        /// <summary><see cref="TargetClass"/> as the normalizer looks it up.</summary>
        public string CanonicalClass = string.Empty;
        public double Concentration;
        public double DilutionRate = 1d;
        public double MolecularWeight;
        public int PeakID = -1;

        /// <summary>Export the peak at <see cref="PeakID"/> under <see cref="StandardName"/>.</summary>
        public bool AssignName;
        public int LineNumber;
    }

    internal sealed class StandardResolution {
        public List<StandardCompound> Compounds = new List<StandardCompound>();
        public List<string> Unresolved = new List<string>();
        public List<string> Report = new List<string>();

        /// <summary>Standards whose given alignment ID names a different compound.</summary>
        public List<string> Mismatched = new List<string>();

        /// <summary>Of <see cref="Mismatched"/>, the rows that do not assign: the peak is only taken.</summary>
        public List<string> MismatchedTaken = new List<string>();

        /// <summary>Of <see cref="Mismatched"/>, the rows whose AssignName would rename the peak.</summary>
        public List<string> MismatchedRenames = new List<string>();

        /// <summary>Of <see cref="Unresolved"/>, the rows whose absence empties rows of the matrix.</summary>
        public List<string> UnresolvedNeeded = new List<string>();

        /// <summary>
        /// Of <see cref="Unresolved"/>, the rows nothing depends on: their class is quantified
        /// by another row, or the normalizer never looks a peak up under it.
        /// </summary>
        public List<string> UnresolvedNotNeeded = new List<string>();

        /// <summary>The classes <see cref="UnresolvedNeeded"/> leaves without a concentration, as the table wrote them.</summary>
        public List<string> ClassesLost = new List<string>();

        /// <summary>Standards matching more than one aligned peak.</summary>
        public List<string> Ambiguous = new List<string>();

        /// <summary>
        /// Lipid class, as the normalizer spells it -> the class as the table wrote it and
        /// the standard named for it that could not be found. A class another row resolved
        /// is not here. Compared exactly, as the normalizer's own lookup is.
        /// </summary>
        public Dictionary<string, (string TargetClass, string StandardName)> UnresolvedClasses =
            new Dictionary<string, (string TargetClass, string StandardName)>(StringComparer.Ordinal);

        /// <summary>
        /// The table's classes the normalizer never looks a peak up under, each with the
        /// standard named for it: their peaks go to "Any others" whatever the table says.
        /// </summary>
        public List<string> NeverLookedUp = new List<string>();

        /// <summary>The "Any others" standard those classes now fall through to.</summary>
        public string FallbackName = string.Empty;

        /// <summary>The "Any others" standard, when it is named and did not resolve.</summary>
        public string UnresolvedFallback = string.Empty;

        /// <summary>Alignment ID -> the standard name AssignName exports it under.</summary>
        public Dictionary<int, string> Assignments = new Dictionary<int, string>();

        /// <summary>
        /// Alignment IDs that one row assigns to a standard and another row, assigning or
        /// not, takes as a different one.
        /// </summary>
        public List<string> AssignmentConflicts = new List<string>();
    }

    /// <summary>
    /// Reads the class-to-standard table: which standard normalizes which lipid class,
    /// at what amount. "Any others" covers every class the table does not name.
    /// </summary>
    internal static List<StandardRecord> ReadStandards(string path, bool peakIdsOnly = false) {
        var lines = File.ReadAllLines(path);
        var header = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (header is null) {
            throw new FormatException("The internal standard table is empty.");
        }
        var separator = header.Contains('\t') ? '\t' : ',';
        var columns = header.Split(separator)
            .Select((name, index) => (name: Normalize(name), index))
            .ToDictionary(item => item.name, item => item.index);

        int Column(string name) => columns.TryGetValue(Normalize(name), out var index) ? index : -1;
        var nameColumn = Column("StandardName");
        var classColumn = Column("TargetClass");
        var concentrationColumn = Column("Concentration");
        if (nameColumn < 0 || classColumn < 0 || concentrationColumn < 0) {
            throw new FormatException(
                "The internal standard table needs StandardName, TargetClass and Concentration columns; "
                + $"it has: {string.Join(", ", header.Split(separator))}");
        }
        var peakColumn = Column("PeakID");
        var dilutionColumn = Column("DilutionRate");
        var weightColumn = Column("MolecularWeight");
        var assignColumn = Column("AssignName");
        if (peakIdsOnly && peakColumn < 0) {
            throw new FormatException(
                "--peak-ids-only takes each standard from its PeakID, and the internal standard table has no "
                + $"PeakID column; it has: {string.Join(", ", header.Split(separator))}");
        }

        var records = new List<StandardRecord>();
        var started = false;
        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!started) { started = true; continue; }
            var cells = line.Split(separator);
            string Cell(int column) => column >= 0 && column < cells.Length ? cells[column].Trim() : string.Empty;
            var standardName = Cell(nameColumn);
            var targetClass = Cell(classColumn);
            if (standardName.Length == 0 || targetClass.Length == 0) continue;
            var record = new StandardRecord {
                StandardName = standardName,
                TargetClass = targetClass,
                CanonicalClass = CanonicalClass(targetClass),
                LineNumber = index + 1,
                PeakID = ParseInt(Cell(peakColumn), -1),
                MolecularWeight = ParseDouble(Cell(weightColumn), 0d),
            };
            // Without the option such an ID was looked up by name, and still is. With it, an
            // ID that is not one would be reported as no ID at all, and a caller could not
            // tell its own error from a standard it had deliberately left unfound.
            var peakId = Cell(peakColumn);
            if (peakIdsOnly && peakId.Length > 0
                && !int.TryParse(peakId, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) {
                throw new FormatException(
                    $"Line {record.LineNumber}: PeakID '{peakId}' for {standardName} is not an alignment ID. "
                    + "Write a whole number, or leave it empty for a standard that was not found.");
            }
            var concentration = Cell(concentrationColumn);
            if (!double.TryParse(concentration, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                throw new FormatException(
                    $"Line {record.LineNumber}: '{concentration}' is not a concentration for {standardName}.");
            }
            record.Concentration = value;
            record.DilutionRate = ParseDouble(Cell(dilutionColumn), 1d);
            if (record.DilutionRate <= 0d) record.DilutionRate = 1d;
            var assign = Cell(assignColumn);
            if (!TryParseFlag(assign, out record.AssignName)) {
                throw new FormatException(
                    $"Line {record.LineNumber}: AssignName '{assign}' for {standardName} is neither true nor false.");
            }
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// Ties each standard to the aligned peak that carries it, by the alignment ID the
    /// table gives or, failing that and unless <paramref name="peakIdsOnly"/>, by the name
    /// the alignment annotated.
    /// </summary>
    internal static StandardResolution ResolveStandards(
        IReadOnlyList<StandardRecord> records, IReadOnlyList<AlignmentSpotProperty> spots, bool peakIdsOnly = false) {
        var result = new StandardResolution();
        var byId = spots.ToDictionary(spot => spot.MasterAlignmentID, spot => spot);
        var byName = new Dictionary<string, List<AlignmentSpotProperty>>(StringComparer.OrdinalIgnoreCase);
        foreach (var spot in spots) {
            foreach (var alias in NameAliases(spot.Name)) {
                if (!byName.TryGetValue(alias, out var bucket)) {
                    byName[alias] = bucket = new List<AlignmentSpotProperty>();
                }
                bucket.Add(spot);
            }
        }

        // One line per standard, not per class it covers: a standard covering forty
        // classes repeated its own resolution forty times, and the handful of lines that
        // needed a decision were interleaved somewhere in the middle of the rest.
        var resolvedOnce = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var classesOf = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var unresolvedOnce = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var unresolvedWhy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var assignedOver = new Dictionary<int, string>();
        var unresolvedRecords = new List<StandardRecord>();

        foreach (var record in records) {
            // A class with no MS-DIAL lipid class name is passed on as written and never
            // matches: its peaks are looked up as "Undefined" and go to "Any others".
            var lookedUp = IsLookedUp(record.CanonicalClass);
            var unmatched = $"{record.TargetClass} ({record.StandardName})";
            if (!lookedUp && !result.NeverLookedUp.Contains(unmatched)) {
                result.NeverLookedUp.Add(unmatched);
            }
            AlignmentSpotProperty? spot = null;
            var how = string.Empty;
            var why = string.Empty;
            if (record.PeakID >= 0 && byId.TryGetValue(record.PeakID, out var byIdSpot)) {
                spot = byIdSpot;
                how = $"alignment ID {record.PeakID}";
                // An ID naming a different compound is a table written for another run.
                // Quantifying a class against whatever landed on that ID is a worse
                // outcome than not quantifying it, so it stops rather than warns.
                if (!NameAliases(byIdSpot.Name).Contains(record.StandardName, StringComparer.OrdinalIgnoreCase)) {
                    result.Mismatched.Add(
                        $"{record.StandardName} (alignment ID {record.PeakID} is annotated '{byIdSpot.Name}')");
                    var carries = string.IsNullOrWhiteSpace(byIdSpot.Name) ? "unannotated" : $"annotated '{byIdSpot.Name}'";
                    (record.AssignName ? result.MismatchedRenames : result.MismatchedTaken).Add(
                        $"{record.StandardName} (alignment ID {record.PeakID}, {carries})");
                }
                if (record.AssignName && !result.Assignments.ContainsKey(record.PeakID)) {
                    result.Assignments[record.PeakID] = record.StandardName;
                    assignedOver[record.PeakID] = byIdSpot.Name ?? string.Empty;
                }
            }
            else if (peakIdsOnly) {
                // The caller found its standards itself, and one it left without an ID is
                // one it did not find. A name lookup would overrule that with whichever
                // same-named peak is most abundant -- another adduct of it included.
                why = record.PeakID < 0
                    ? "no alignment ID in the table, and --peak-ids-only does not look a standard up by name"
                    : $"alignment ID {record.PeakID} is not in this alignment";
            }
            else if (byName.TryGetValue(record.StandardName, out var candidates)) {
                spot = candidates.OrderByDescending(item => item.HeightAverage).First();
                how = $"annotation, alignment ID {spot.MasterAlignmentID}";
                if (candidates.Count > 1 && !resolvedOnce.ContainsKey(record.StandardName)) {
                    result.Ambiguous.Add(
                        $"{record.StandardName}: {candidates.Count} aligned peaks carry this annotation; "
                        + $"the most abundant (ID {spot.MasterAlignmentID}) was used");
                }
            }

            if (spot is null) {
                result.Unresolved.Add($"{record.StandardName} (for {record.TargetClass})");
                unresolvedRecords.Add(record);
                if (!unresolvedOnce.TryGetValue(record.StandardName, out var missingFor)) {
                    unresolvedOnce[record.StandardName] = missingFor = new List<string>();
                    unresolvedWhy[record.StandardName] = new List<string>();
                }
                missingFor.Add(record.TargetClass);
                if (why.Length > 0 && !unresolvedWhy[record.StandardName].Contains(why)) {
                    unresolvedWhy[record.StandardName].Add(why);
                }
                if (record.CanonicalClass == StandardCompound.AnyOthers) {
                    if (result.UnresolvedFallback.Length == 0) {
                        result.UnresolvedFallback = record.StandardName;
                    }
                }
                else if (!result.UnresolvedClasses.ContainsKey(record.CanonicalClass)) {
                    result.UnresolvedClasses[record.CanonicalClass] = (record.TargetClass, record.StandardName);
                }
                continue;
            }
            if (record.CanonicalClass == StandardCompound.AnyOthers) {
                result.FallbackName = record.StandardName;
            }
            resolvedOnce[record.StandardName] = $"{how}, concentration {record.Concentration}";
            if (!classesOf.TryGetValue(record.StandardName, out var covered)) {
                classesOf[record.StandardName] = covered = new List<string>();
            }
            if (lookedUp) {
                covered.Add(record.TargetClass);
            }
            result.Compounds.Add(new StandardCompound {
                StandardName = record.StandardName,
                TargetClass = record.CanonicalClass,
                Concentration = record.Concentration,
                DilutionRate = record.DilutionRate,
                MolecularWeight = record.MolecularWeight,
                PeakID = spot.MasterAlignmentID,
            });
        }
        // A class some other row did resolve is quantified against that row's standard.
        foreach (var compound in result.Compounds) {
            result.UnresolvedClasses.Remove(compound.TargetClass);
        }
        if (result.FallbackName.Length > 0) {
            result.UnresolvedFallback = string.Empty;
        }
        // Which unresolved standards cost a concentration. Saying that every class an
        // unresolved row names is left without one was wrong for two kinds of row, and a run
        // was refused over them: a class another row resolved is quantified against that
        // row's standard, and a class the normalizer never looks a peak up under goes to
        // "Any others" whatever the table names for it.
        foreach (var record in unresolvedRecords) {
            string? quantifiedBy = null;
            if (record.CanonicalClass == StandardCompound.AnyOthers) {
                if (result.FallbackName.Length > 0) {
                    quantifiedBy = result.FallbackName;
                }
            }
            else if (!IsLookedUp(record.CanonicalClass)) {
                result.UnresolvedNotNeeded.Add(
                    $"{record.StandardName} (for {record.TargetClass}, under which the normalizer looks no peak up)");
                continue;
            }
            else if (!result.UnresolvedClasses.ContainsKey(record.CanonicalClass)) {
                quantifiedBy = result.Compounds.First(compound => compound.TargetClass == record.CanonicalClass).StandardName;
            }
            if (quantifiedBy is not null) {
                result.UnresolvedNotNeeded.Add($"{record.StandardName} (for {record.TargetClass}, which {quantifiedBy} quantifies)");
                continue;
            }
            result.UnresolvedNeeded.Add($"{record.StandardName} (for {record.TargetClass})");
            var lost = record.CanonicalClass == StandardCompound.AnyOthers
                ? "any class without a standard of its own"
                : record.TargetClass;
            if (!result.ClassesLost.Contains(lost)) {
                result.ClassesLost.Add(lost);
            }
        }
        // A peak AssignName exports as one standard cannot quantify a class as another,
        // whether or not that row assigns too: the matrix would name it one thing and
        // the standard's own row would carry whichever amount the normalizer applied last.
        foreach (var pair in result.Assignments.OrderBy(item => item.Key)) {
            var others = result.Compounds
                .Where(compound => compound.PeakID == pair.Key
                    && !compound.StandardName.Equals(pair.Value, StringComparison.OrdinalIgnoreCase))
                .Select(compound => compound.StandardName)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var other in others) {
                result.AssignmentConflicts.Add($"alignment ID {pair.Key} ({pair.Value} and {other})");
            }
        }

        foreach (var pair in resolvedOnce.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)) {
            var covered = classesOf.TryGetValue(pair.Key, out var list) ? list : new List<string>();
            result.Report.Add($"  {pair.Key} via {pair.Value}");
            result.Report.Add($"      covers {covered.Count} class(es): {string.Join(", ", covered)}");
        }
        foreach (var pair in result.Assignments.OrderBy(item => item.Key)) {
            var over = assignedOver[pair.Key];
            result.Report.Add(
                $"  ASSIGNED {pair.Value} to alignment ID {pair.Key} "
                + (string.IsNullOrWhiteSpace(over) ? "(unannotated)" : $"(annotated '{over}')"));
        }
        foreach (var line in result.Ambiguous) {
            result.Report.Add($"  AMBIGUOUS {line}");
        }
        // Last, so the lines that need a decision are the ones still on screen.
        foreach (var pair in unresolvedOnce.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)) {
            result.Report.Add(
                $"  UNRESOLVED {pair.Key} -- named for {pair.Value.Count} class(es): "
                + string.Join(", ", pair.Value));
            foreach (var reason in unresolvedWhy[pair.Key]) {
                result.Report.Add($"      {reason}");
            }
        }
        return result;
    }

    /// <summary>
    /// The names an aligned peak answers to. MS-DIAL reports a lipid at two resolutions
    /// separated by a bar, such as "PC 33:1(d7)|PC 15:0_18:1(d7)", and a standard table
    /// may reasonably name either one.
    /// </summary>
    private static IEnumerable<string> NameAliases(string? name) {
        if (string.IsNullOrWhiteSpace(name)) yield break;
        yield return name!.Trim();
        foreach (var part in name!.Split('|')) {
            var trimmed = part.Trim();
            if (trimmed.Length > 0) yield return trimmed;
        }
    }

    private static string Normalize(string value) =>
        new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static int ParseInt(string value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static double ParseDouble(string value, double fallback) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static bool TryParseFlag(string value, out bool flag) {
        switch (value.Trim().ToLowerInvariant()) {
            case "":
            case "false":
            case "0":
            case "no":
                flag = false;
                return true;
            case "true":
            case "1":
            case "yes":
                flag = true;
                return true;
            default:
                flag = false;
                return false;
        }
    }

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
