using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// What a MassBank export wrote and what it did not, with the reasons, for the message the person sees at the end.
    /// </summary>
    internal sealed class MassBankExportSummary
    {
        private readonly Dictionary<string, int> _skipped = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _adjusted = new Dictionary<string, int>();
        private readonly HashSet<string> _writtenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _otherRecords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly string _item;
        private readonly string _items;

        /// <param name="item">What one record is made from, for the message: "spot" or "peak".</param>
        /// <param name="items">The plural of <paramref name="item"/>.</param>
        public MassBankExportSummary(string item = "spot", string items = "spots") {
            _item = item;
            _items = items;
        }

        public int Written { get; private set; }

        public int Skipped => _skipped.Values.Sum();

        /// <summary>
        /// The MassBank records found in the folders this export wrote into that it did not write itself, as
        /// <see cref="CountOtherRecords"/> last counted them.
        /// </summary>
        public int OtherRecords => _otherRecords.Values.Sum();

        /// <summary>Counts the record of <paramref name="result"/>, which was written to <paramref name="path"/>.</summary>
        public void AddWritten(MassBankRecordResult result, string path) {
            Written++;
            // The handler's notes are fixed sentences (an exception is named by its type only), so they group as they are.
            foreach (var note in result.Notes.Distinct()) {
                Increment(_adjusted, note);
            }
            if (!string.IsNullOrEmpty(path)) {
                _writtenFiles.Add(Path.GetFullPath(path));
            }
        }

        public void AddSkipped(string reason) {
            Increment(_skipped, reason);
        }

        /// <summary>
        /// Counts the MassBank records in <paramref name="folder"/> that this export did not write, such as those of an
        /// earlier export into the same folder. Call it once the export has written into the folder; counting a folder
        /// again replaces its count.
        /// </summary>
        /// <remarks>
        /// Every export takes accessions of its own (they carry the export time to the second), so a re-export, after
        /// curation for example, does not replace the records of the one before: it adds a second set beside them, which
        /// the MassBank Validator accepts in the same folder because no accession repeats. Only the folder itself is
        /// looked at, as that is what the export writes into. Counting is advice for the message; a folder that cannot be
        /// listed is left out rather than failing an export that has already written its records.
        /// </remarks>
        /// <param name="accessionPrefix">
        /// The first block of every accession, <see cref="MassBankRecordHandler.Identifier"/>: a record's file is named
        /// after its accession, "{prefix}-{contributor}-{record ID}.txt", so any contributor's records are counted.
        /// </param>
        public void CountOtherRecords(string folder, string accessionPrefix) {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(accessionPrefix)) {
                return;
            }
            try {
                if (!Directory.Exists(folder)) {
                    return;
                }
                var count = 0;
                foreach (var file in Directory.EnumerateFiles(folder, accessionPrefix + "-*.txt", SearchOption.TopDirectoryOnly)) {
                    // The search pattern also matches extensions that only begin with ".txt".
                    if (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && !_writtenFiles.Contains(Path.GetFullPath(file))) {
                        count++;
                    }
                }
                _otherRecords[Path.GetFullPath(folder)] = count;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
            }
        }

        /// <summary>The message to show when the export ends.</summary>
        /// <param name="destination">
        /// Where the records were written, as the message completes "records were written to ...": a folder, or for the
        /// peak list export of several samples "per-sample folders under" the folder; empty to leave it out.
        /// </param>
        public MessageRequest ToMessage(string destination) {
            var content = new StringBuilder();
            content.Append(Written == 1 ? "1 MassBank record was written" : $"{Number(Written)} MassBank records were written");
            content.Append(string.IsNullOrEmpty(destination) ? "." : $" to {destination}.");
            var others = OtherRecords;
            if (others > 0) {
                content.AppendLine().AppendLine().Append(others == 1
                    ? "1 MassBank record in the destination was not written by this export (it may be from an earlier export). It has an accession of its own, so the MassBank Validator accepts the folder with both: submit one set only, and move the other out of the folder first."
                    : $"{Number(others)} MassBank records in the destination were not written by this export (they may be from an earlier export). They have accessions of their own, so the MassBank Validator accepts the folder with both sets: submit one set only, and move the other out of the folder first.");
            }
            if (_skipped.Count > 0) {
                content.AppendLine().AppendLine().Append(Skipped == 1 ? $"1 {_item} was not exported:" : $"{Number(Skipped)} {_items} were not exported:");
                foreach (var entry in _skipped.OrderByDescending(e => e.Value).ThenBy(e => e.Key)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            if (_adjusted.Count > 0) {
                content.AppendLine().AppendLine().Append("Written with these adjustments for MassBank:");
                foreach (var entry in _adjusted.OrderByDescending(e => e.Value).ThenBy(e => e.Key)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            return new MessageRequest
            {
                Caption = "MassBank export",
                Content = content.ToString(),
                Severity = (Written == 0 && Skipped > 0) || others > 0 ? MessageSeverity.Warning : MessageSeverity.Information,
            };
        }

        /// <summary>The message to show when the project metadata keeps the export from starting.</summary>
        public static MessageRequest MetadataProblems(IReadOnlyList<string> problems) {
            return new MessageRequest
            {
                Caption = "MassBank records were not exported",
                Content = "MassBank records were not exported, because they would not pass the MassBank Validator:\n\n"
                    + string.Join("\n", problems.Select(p => "- " + p))
                    + "\n\nSet the instrument type, instrument, authors, license and collision energy in Option > Project property setting, and the contributor ID in the export dialog.",
                Severity = MessageSeverity.Error,
            };
        }

        private static void Increment(Dictionary<string, int> counts, string key) {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        private static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
