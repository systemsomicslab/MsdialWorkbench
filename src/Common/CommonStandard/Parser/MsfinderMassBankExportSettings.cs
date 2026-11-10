using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RfxRawData = Rfx.Riken.OsakaUniv.RawData;

namespace CompMs.Common.Parser
{
    /// <summary>
    /// What every record of an MS-FINDER MassBank export states, as the person exporting gives it.
    /// </summary>
    /// <remarks>
    /// MS-FINDER reads these per query from the files it imports, but there they are often missing or not in MassBank's
    /// vocabulary (an MS-DIAL record before PR #872 says "QTOF" and has no authors), and a query never carries the
    /// contributor ID. The export therefore asks for them once and writes them into every record;
    /// <see cref="Suggest"/> starts the dialog with what the queries agree on.
    /// </remarks>
    public sealed class MsfinderMassBankExportSettings
    {
        /// <summary>The MassBank contributor ID, the second block of ACCESSION.</summary>
        public string ContributorId { get; set; } = string.Empty;

        public string Authors { get; set; } = string.Empty;

        /// <summary>LICENSE, one of <see cref="MassBankVocabulary.Licenses"/>.</summary>
        public string License { get; set; } = string.Empty;

        public string Instrument { get; set; } = string.Empty;

        /// <summary>AC$INSTRUMENT_TYPE, e.g. "LC-ESI-QTOF".</summary>
        public string InstrumentType { get; set; } = string.Empty;

        /// <summary>
        /// The handler of one export: what these settings give every record, MS-FINDER's accession software code and
        /// MS$DATA_PROCESSING, and the accessions it issues.
        /// </summary>
        /// <param name="msfinderVersion">The MS-FINDER version MS$DATA_PROCESSING: WHOLE names, e.g. "3.73".</param>
        /// <param name="splashCalculator">PK$SPLASH of the printed peaks; MassBankSplash.Compute.</param>
        public MassBankRecordHandler CreateHandler(string msfinderVersion, Func<string, string> splashCalculator) {
            return new MassBankRecordHandler(IonMode.Positive, InstrumentType ?? string.Empty, splashCalculator)
            {
                ContributorIdentifier = ContributorId ?? string.Empty,
                Authors = Authors ?? string.Empty,
                License = License ?? string.Empty,
                Instrument = Instrument ?? string.Empty,
            }.ForMsfinder(msfinderVersion);
        }

        /// <summary>What keeps these settings from making valid records; empty when nothing does.</summary>
        public IReadOnlyList<string> Validate(string msfinderVersion) {
            return CreateHandler(msfinderVersion, _ => string.Empty).ValidateMetadata();
        }

        /// <summary>
        /// The settings the queries agree on, to start the export dialog with: each value that every query states the
        /// same, and empty where they differ or none states one. A value is suggested whether or not MassBank accepts it,
        /// so that the dialog can show what the files say; the contributor ID is never in a query and is left empty.
        /// </summary>
        public static MsfinderMassBankExportSettings Suggest(IEnumerable<RfxRawData> queries) {
            var list = queries?.Where(q => q != null).ToList() ?? new List<RfxRawData>();
            return new MsfinderMassBankExportSettings
            {
                Authors = Agreed(list.Select(q => q.Authors)),
                License = Agreed(list.Select(q => q.License)),
                Instrument = Agreed(list.Select(q => q.Instrument)),
                InstrumentType = Agreed(list.Select(q => q.InstrumentType)),
            };
        }

        /// <summary>
        /// Why the records cannot go to <paramref name="destination"/>; null when they can. MS-FINDER reads every MassBank
        /// record in its project folder as a query, so the records are never written there.
        /// </summary>
        public static string ValidateDestination(string destination, string projectFolder) {
            if (string.IsNullOrWhiteSpace(destination)) {
                return "Choose the folder to export the MassBank records to.";
            }
            try {
                if (!Path.IsPathRooted(destination)) {
                    return "Give the export folder as a full path.";
                }
                if (!string.IsNullOrWhiteSpace(projectFolder) && string.Equals(NormalizeFolder(destination), NormalizeFolder(projectFolder), StringComparison.OrdinalIgnoreCase)) {
                    return "Choose a folder other than the MS-FINDER project folder: MS-FINDER reads every MassBank record there as a query.";
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) {
                return $"The export folder \"{destination}\" is not a valid path.";
            }
            return null;
        }

        private static string NormalizeFolder(string folder) {
            return Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string Agreed(IEnumerable<string> values) {
            var distinct = values.Select(v => v?.Trim() ?? string.Empty).Distinct(StringComparer.Ordinal).ToList();
            return distinct.Count == 1 ? distinct[0] : string.Empty;
        }
    }

    /// <summary>
    /// What an MS-FINDER MassBank export wrote and what it did not, with the reasons, for the message the person sees.
    /// </summary>
    public sealed class MsfinderMassBankExportSummary
    {
        private readonly Dictionary<string, int> _skipped = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _adjusted = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _writtenFiles = new List<string>();

        public int Written => _writtenFiles.Count;

        public int Skipped => _skipped.Values.Sum();

        /// <summary>The files written, in the order of the queries.</summary>
        public IReadOnlyList<string> WrittenFiles => _writtenFiles;

        /// <summary>Why queries were not exported, with how many each reason kept out.</summary>
        public IReadOnlyDictionary<string, int> SkipReasons => _skipped;

        /// <summary>The adjustments the writer made for MassBank, with how many records each concerns.</summary>
        public IReadOnlyDictionary<string, int> Adjustments => _adjusted;

        /// <summary>The MassBank records in the destination this export did not write, such as those of an earlier export.</summary>
        public int OtherRecords { get; private set; }

        /// <summary>Counts the record of <paramref name="result"/>, which was written to <paramref name="path"/>.</summary>
        public void AddWritten(MassBankRecordResult result, string path) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }
            _writtenFiles.Add(path);
            foreach (var note in result.Notes.Distinct()) {
                Increment(_adjusted, note);
            }
        }

        public void AddSkipped(string reason) {
            Increment(_skipped, reason ?? string.Empty);
        }

        /// <summary>
        /// Counts the MassBank records in <paramref name="folder"/> this export did not write, such as those of an earlier
        /// export into the same folder; a folder that cannot be listed counts none, as the records are already written.
        /// </summary>
        /// <param name="accessionPrefix">The first block of every accession, <see cref="MassBankRecordHandler.Identifier"/>.</param>
        public void CountOtherRecords(string folder, string accessionPrefix) {
            try {
                var written = new HashSet<string>(_writtenFiles.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
                OtherRecords = Directory.EnumerateFiles(folder, accessionPrefix + "-*.txt", SearchOption.TopDirectoryOnly)
                    .Count(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && !written.Contains(Path.GetFullPath(f)));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) {
                OtherRecords = 0;
            }
        }

        /// <summary>The message to show when the export ends.</summary>
        public string ToMessage(string destination) {
            var content = new StringBuilder();
            content.Append(Written == 1 ? "1 MassBank record was written" : $"{Number(Written)} MassBank records were written");
            content.Append(string.IsNullOrEmpty(destination) ? "." : $" to {destination}.");
            if (OtherRecords > 0) {
                content.AppendLine().AppendLine().Append(OtherRecords == 1
                    ? "1 MassBank record in the destination was not written by this export (it may be from an earlier export). It has an accession of its own, so the MassBank Validator accepts the folder with both: submit one set only, and move the other out of the folder first."
                    : $"{Number(OtherRecords)} MassBank records in the destination were not written by this export (they may be from an earlier export). They have accessions of their own, so the MassBank Validator accepts the folder with both sets: submit one set only, and move the other out of the folder first.");
            }
            if (_skipped.Count > 0) {
                content.AppendLine().AppendLine().Append(Skipped == 1 ? "1 query was not exported:" : $"{Number(Skipped)} queries were not exported:");
                foreach (var entry in _skipped.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.Ordinal)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            if (_adjusted.Count > 0) {
                content.AppendLine().AppendLine().Append("Written with these adjustments for MassBank:");
                foreach (var entry in _adjusted.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.Ordinal)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            return content.ToString();
        }

        private static void Increment(Dictionary<string, int> counts, string key) {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        private static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
