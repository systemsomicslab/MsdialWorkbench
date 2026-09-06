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
    public int Run(
        FileInfo projectFile,
        FileInfo standardsFile,
        FileInfo outputFile,
        IonAbundanceUnit unit,
        int alignmentIndex,
        bool applyDilutionFactor,
        bool allowUnresolvedStandards) {
        if (!projectFile.Exists) {
            Console.Error.WriteLine($"Project file was not found: {projectFile.FullName}");
            return -1;
        }
        if (!standardsFile.Exists) {
            Console.Error.WriteLine($"Internal standard table was not found: {standardsFile.FullName}");
            return -1;
        }

        IMsdialDataStorage<ParameterBase> storage =
            Common.MessagePack.MessagePackDefaultHandler.LoadFromFile<MsdialDataStorage>(projectFile.FullName);
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
        if (resolution.Unresolved.Count > 0) {
            var summary = string.Join(", ", resolution.Unresolved);
            if (!allowUnresolvedStandards) {
                Console.Error.WriteLine(
                    $"{resolution.Unresolved.Count} internal standard(s) were not found in the alignment: {summary}. "
                    + "Every lipid class they cover would be left unnormalized. "
                    + "Confirm the annotation, or pass --allow-unresolved-standards to continue without them.");
                return 2;
            }
            Console.WriteLine(
                $"WARNING: continuing without {resolution.Unresolved.Count} internal standard(s): {summary}.");
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
        var quantAccessor = new LegacyQuantValueAccessor("Height", storage.Parameter);
        var stats = new[] { StatsValue.Average, StatsValue.Stdev };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile.FullName)) ?? ".");
        using (var stream = File.Open(outputFile.FullName, FileMode.Create, FileAccess.Write)) {
            new AlignmentCSVExporter().Export(
                stream, spots, decResults, files, new MulticlassFileMetaAccessor(0), accessor, quantAccessor, stats);
        }

        Console.WriteLine($"Normalized unit: {unit}");
        Console.WriteLine($"Dilution factor applied: {applyDilutionFactor}");
        Console.WriteLine(outputFile.FullName);
        return 0;
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

        foreach (var record in records) {
            AlignmentSpotProperty? spot = null;
            var how = string.Empty;
            if (record.PeakID >= 0 && byId.TryGetValue(record.PeakID, out var byIdSpot)) {
                spot = byIdSpot;
                how = $"alignment ID {record.PeakID}";
                // An ID that names a different compound is a table written for another
                // run. Saying so is the whole point of carrying the name as well.
                if (!NameAliases(byIdSpot.Name).Contains(record.StandardName, StringComparer.OrdinalIgnoreCase)) {
                    result.Report.Add(
                        $"  WARNING {record.StandardName}: alignment ID {record.PeakID} is annotated "
                        + $"'{byIdSpot.Name}'. The table may belong to a different alignment.");
                }
            }
            else if (byName.TryGetValue(record.StandardName, out var candidates)) {
                spot = candidates.OrderByDescending(item => item.HeightAverage).First();
                how = $"annotation, alignment ID {spot.MasterAlignmentID}";
                if (candidates.Count > 1) {
                    result.Report.Add(
                        $"  NOTE {record.StandardName}: {candidates.Count} aligned peaks carry this annotation; "
                        + $"the most abundant (ID {spot.MasterAlignmentID}) was used.");
                }
            }

            if (spot is null) {
                result.Unresolved.Add($"{record.StandardName} (for {record.TargetClass})");
                result.Report.Add($"  UNRESOLVED {record.StandardName} -> {record.TargetClass}");
                continue;
            }
            result.Report.Add(
                $"  {record.StandardName} -> {record.TargetClass} via {how}, concentration {record.Concentration}");
            result.Compounds.Add(new StandardCompound {
                StandardName = record.StandardName,
                TargetClass = record.TargetClass,
                Concentration = record.Concentration,
                DilutionRate = record.DilutionRate,
                MolecularWeight = record.MolecularWeight,
                PeakID = spot.MasterAlignmentID,
            });
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
