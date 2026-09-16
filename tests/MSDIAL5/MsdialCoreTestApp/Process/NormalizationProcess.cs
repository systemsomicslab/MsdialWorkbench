using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Normalize;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcMsApi.Export;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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
/// </remarks>
public sealed class NormalizationProcess {
    /// <summary>Label rows plus the column-name row that precede the data.</summary>
    private const int HeaderRowCount = 5;

    public int Run(
        FileInfo projectFile,
        FileInfo standardsFile,
        DirectoryInfo outputDirectory,
        IonAbundanceUnit unit,
        int alignmentIndex,
        bool applyDilutionFactor,
        bool allowUnresolvedStandards,
        bool allowMismatchedPeakIds) {
        if (!projectFile.Exists) {
            Console.Error.WriteLine($"Project file was not found: {projectFile.FullName}");
            return -1;
        }
        if (!standardsFile.Exists) {
            Console.Error.WriteLine($"Internal standard table was not found: {standardsFile.FullName}");
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

        List<StandardRecord> records;
        try {
            records = ReadStandards(standardsFile.FullName);
        }
        catch (FormatException error) {
            Console.Error.WriteLine(error.Message);
            return -1;
        }
        if (records.Count == 0) {
            Console.Error.WriteLine("The internal standard table contains no rows.");
            return -1;
        }

        var resolution = ResolveStandards(records, spots);
        foreach (var line in resolution.Report) {
            Console.WriteLine(line);
        }
        if (resolution.Mismatched.Count > 0 && !allowMismatchedPeakIds) {
            // A standard that cannot be found already stops the run. One found and
            // demonstrably pointing at a different compound is the worse case of the two,
            // and it used to warn and carry on -- quantifying a lipid class against
            // whatever happened to occupy that alignment ID.
            Console.Error.WriteLine(
                $"{resolution.Mismatched.Count} standard(s) name an alignment ID that holds a different "
                + $"compound: {string.Join("; ", resolution.Mismatched)}. An alignment ID belongs to the run it "
                + "was written for. Remove the PeakID column so the standards resolve by name, or pass "
                + "--allow-mismatched-peak-ids if the annotations are wrong rather than the table.");
            return 2;
        }
        if (resolution.Mismatched.Count > 0) {
            Console.WriteLine(
                $"WARNING: {resolution.Mismatched.Count} standard(s) were taken from an alignment ID that holds "
                + $"a different compound: {string.Join("; ", resolution.Mismatched)}.");
        }
        if (resolution.Unresolved.Count > 0) {
            var summary = string.Join(", ", resolution.Unresolved);
            if (!allowUnresolvedStandards) {
                Console.Error.WriteLine(
                    $"{resolution.Unresolved.Count} internal standard(s) were not found in the alignment: {summary}. "
                    + "Every lipid class they cover would be left without a concentration. "
                    + "Confirm the annotation, or pass --allow-unresolved-standards to continue, which "
                    + "empties those rows rather than quantifying them against another class.");
                return 2;
            }
            Console.WriteLine(
                $"WARNING: {resolution.Unresolved.Count} internal standard(s) did not resolve: {summary}. "
                + "Rows in the lipid classes they cover carry no concentration and say so.");
        }
        if (resolution.Compounds.Count == 0) {
            Console.Error.WriteLine("No internal standard could be resolved, so nothing can be normalized.");
            return 2;
        }

        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        Normalization.SplashNormalize(
            files,
            spots,
            storage.DataBaseMapper,
            resolution.Compounds,
            unit,
            evaluator,
            applyDilutionFactor);
        container.IsNormalized = true;

        var decResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);
        var accessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
        var stats = new[] { StatsValue.Average, StatsValue.Stdev };
        Directory.CreateDirectory(outputDirectory.FullName);

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
            var path = Path.Combine(outputDirectory.FullName, alignmentFile.FileName + suffix);
            using (var stream = File.Open(path, FileMode.Create, FileAccess.Write)) {
                new AlignmentCSVExporter().Export(
                    stream, spots, decResults, files, new MulticlassFileMetaAccessor(0), accessor,
                    new LegacyQuantValueAccessor(exportType, storage.Parameter), stats);
            }
            written.Add(path);
            if (exportType == "Normalized height") {
                var redacted = RedactSubstitutedClasses(path, resolution);
                if (redacted > 0) {
                    Console.WriteLine(
                        $"{redacted} row(s) had no standard of their own class and carry no concentration.");
                }
            }
        }

        Console.WriteLine($"Normalized unit: {unit}");
        Console.WriteLine($"Dilution factor applied: {applyDilutionFactor}");
        foreach (var path in written) {
            Console.WriteLine(path);
        }
        return 0;
    }

    /// <summary>
    /// Removes the numbers that were produced by dividing by the wrong standard.
    /// </summary>
    /// <remarks>
    /// When a class's own standard does not resolve, the normalizer does not leave that
    /// class alone: it falls through to the "Any others" standard and quantifies the class
    /// against a compound of an entirely different one. A cardiolipin divided by a
    /// lysophosphatidylcholine is not a concentration, and it was written into the matrix
    /// in the same unit, with the same comment, as a properly quantified row -- nothing in
    /// the file told them apart.
    ///
    /// Refusing outright is the default. Where the run is allowed to continue anyway, the
    /// affected rows keep their identity and lose their numbers, and say why in place of
    /// them. An annotated wrong number is still read by the next script; an empty cell is
    /// not.
    /// </remarks>
    private static int RedactSubstitutedClasses(string matrixPath, StandardResolution resolution) {
        if (resolution.UnresolvedClasses.Count == 0) return 0;
        var lines = File.ReadAllLines(matrixPath);
        if (lines.Length <= HeaderRowCount) return 0;
        var header = lines[HeaderRowCount - 1].Split('	');
        var ontologyColumn = Array.IndexOf(header, "Ontology");
        var commentColumn = Array.IndexOf(header, "Comment");
        var firstSample = Array.IndexOf(lines[0].Split('	'), "Class") + 1;
        if (ontologyColumn < 0 || commentColumn < 0 || firstSample <= 0) return 0;

        var redacted = 0;
        for (var index = HeaderRowCount; index < lines.Length; index++) {
            var cells = lines[index].Split('	');
            if (cells.Length <= ontologyColumn) continue;
            var ontology = cells[ontologyColumn].Trim();
            if (!resolution.UnresolvedClasses.TryGetValue(ontology, out var designated)) continue;
            for (var column = firstSample; column < cells.Length; column++) {
                cells[column] = string.Empty;
            }
            cells[commentColumn] =
                $"NOT QUANTIFIED: the {ontology} standard {designated} did not resolve in this alignment";
            lines[index] = string.Join("	", cells);
            redacted++;
        }
        if (redacted > 0) {
            File.WriteAllLines(matrixPath, lines);
        }
        return redacted;
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

    private sealed class StandardRecord {
        public string StandardName = string.Empty;
        public string TargetClass = string.Empty;
        public double Concentration;
        public double DilutionRate = 1d;
        public double MolecularWeight;
        public int PeakID = -1;
        public int LineNumber;
    }

    private sealed class StandardResolution {
        public List<StandardCompound> Compounds = new List<StandardCompound>();
        public List<string> Unresolved = new List<string>();
        public List<string> Report = new List<string>();

        /// <summary>Lipid class -> the standard named for it that could not be found.</summary>
        /// <summary>Standards whose given alignment ID names a different compound.</summary>
        public List<string> Mismatched = new List<string>();

        /// <summary>Standards matching more than one aligned peak.</summary>
        public List<string> Ambiguous = new List<string>();

        public Dictionary<string, string> UnresolvedClasses =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The "Any others" standard those classes now fall through to.</summary>
        public string FallbackName = string.Empty;
    }

    /// <summary>
    /// Reads the class-to-standard table: which standard normalizes which lipid class,
    /// at what amount. "Any others" covers every class the table does not name.
    /// </summary>
    private static List<StandardRecord> ReadStandards(string path) {
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
                LineNumber = index + 1,
                PeakID = ParseInt(Cell(peakColumn), -1),
                MolecularWeight = ParseDouble(Cell(weightColumn), 0d),
            };
            var concentration = Cell(concentrationColumn);
            if (!double.TryParse(concentration, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
                throw new FormatException(
                    $"Line {record.LineNumber}: '{concentration}' is not a concentration for {standardName}.");
            }
            record.Concentration = value;
            record.DilutionRate = ParseDouble(Cell(dilutionColumn), 1d);
            if (record.DilutionRate <= 0d) record.DilutionRate = 1d;
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// Ties each standard to the aligned peak that carries it, by the alignment ID the
    /// table gives or, failing that, by the name the alignment annotated.
    /// </summary>
    private static StandardResolution ResolveStandards(
        IReadOnlyList<StandardRecord> records, IReadOnlyList<AlignmentSpotProperty> spots) {
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

        foreach (var record in records) {
            AlignmentSpotProperty? spot = null;
            var how = string.Empty;
            if (record.PeakID >= 0 && byId.TryGetValue(record.PeakID, out var byIdSpot)) {
                spot = byIdSpot;
                how = $"alignment ID {record.PeakID}";
                // An ID naming a different compound is a table written for another run.
                // Quantifying a class against whatever landed on that ID is a worse
                // outcome than not quantifying it, so it stops rather than warns.
                if (!NameAliases(byIdSpot.Name).Contains(record.StandardName, StringComparer.OrdinalIgnoreCase)) {
                    result.Mismatched.Add(
                        $"{record.StandardName} (alignment ID {record.PeakID} is annotated '{byIdSpot.Name}')");
                }
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
                if (!unresolvedOnce.TryGetValue(record.StandardName, out var missingFor)) {
                    unresolvedOnce[record.StandardName] = missingFor = new List<string>();
                }
                missingFor.Add(record.TargetClass);
                if (!record.TargetClass.Equals(StandardCompound.AnyOthers, StringComparison.OrdinalIgnoreCase)) {
                    result.UnresolvedClasses[record.TargetClass] = record.StandardName;
                }
                continue;
            }
            if (record.TargetClass.Equals(StandardCompound.AnyOthers, StringComparison.OrdinalIgnoreCase)) {
                result.FallbackName = record.StandardName;
            }
            resolvedOnce[record.StandardName] = $"{how}, concentration {record.Concentration}";
            if (!classesOf.TryGetValue(record.StandardName, out var covered)) {
                classesOf[record.StandardName] = covered = new List<string>();
            }
            covered.Add(record.TargetClass);
            result.Compounds.Add(new StandardCompound {
                StandardName = record.StandardName,
                TargetClass = record.TargetClass,
                Concentration = record.Concentration,
                DilutionRate = record.DilutionRate,
                MolecularWeight = record.MolecularWeight,
                PeakID = spot.MasterAlignmentID,
            });
        }
        foreach (var pair in resolvedOnce.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)) {
            var covered = classesOf.TryGetValue(pair.Key, out var list) ? list : new List<string>();
            result.Report.Add($"  {pair.Key} via {pair.Value}");
            result.Report.Add($"      covers {covered.Count} class(es): {string.Join(", ", covered)}");
        }
        foreach (var line in result.Ambiguous) {
            result.Report.Add($"  AMBIGUOUS {line}");
        }
        // Last, so the lines that need a decision are the ones still on screen.
        foreach (var pair in unresolvedOnce.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)) {
            result.Report.Add(
                $"  UNRESOLVED {pair.Key} -- named for {pair.Value.Count} class(es): "
                + string.Join(", ", pair.Value));
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
}
