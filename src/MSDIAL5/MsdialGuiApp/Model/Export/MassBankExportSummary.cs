using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Parser;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// What a MassBank export wrote and what it did not, with the reasons, for the message the person sees at the end.
    /// </summary>
    internal sealed class MassBankExportSummary
    {
        private const string StructureNotAvailable = "the structure was written as N/A";

        private readonly Dictionary<string, int> _skipped = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _adjusted = new Dictionary<string, int>();

        public int Written { get; private set; }

        public int Skipped => _skipped.Values.Sum();

        public void AddWritten(MassBankRecordResult result) {
            Written++;
            foreach (var note in result.Notes.Select(Category).Distinct()) {
                Increment(_adjusted, note);
            }
        }

        public void AddSkipped(string reason) {
            Increment(_skipped, reason);
        }

        /// <summary>The message to show when the export ends.</summary>
        public MessageRequest ToMessage(string destination) {
            var content = new StringBuilder();
            content.Append(Written == 1 ? "1 MassBank record was written" : $"{Number(Written)} MassBank records were written");
            content.Append(string.IsNullOrEmpty(destination) ? "." : $" to {destination}.");
            if (_skipped.Count > 0) {
                content.AppendLine().AppendLine().Append(Skipped == 1 ? "1 spot was not exported:" : $"{Number(Skipped)} spots were not exported:");
                foreach (var entry in _skipped.OrderByDescending(e => e.Value).ThenBy(e => e.Key)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            if (_adjusted.Count > 0) {
                content.AppendLine().AppendLine().Append("Written with changes so that the records pass the MassBank Validator:");
                foreach (var entry in _adjusted.OrderByDescending(e => e.Value).ThenBy(e => e.Key)) {
                    content.AppendLine().Append($"  {Number(entry.Value)}  {entry.Key}");
                }
            }
            return new MessageRequest
            {
                Caption = "MassBank export",
                Content = content.ToString(),
                Severity = Written == 0 && Skipped > 0 ? MessageSeverity.Warning : MessageSeverity.Information,
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

        private static string Category(string note) {
            return note.StartsWith(StructureNotAvailable, System.StringComparison.Ordinal)
                ? "the structure was written as N/A because the SMILES gave no usable InChI"
                : note;
        }

        private static void Increment(Dictionary<string, int> counts, string key) {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        private static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
