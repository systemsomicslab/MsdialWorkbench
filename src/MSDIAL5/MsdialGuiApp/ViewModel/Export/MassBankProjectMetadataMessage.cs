using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    /// <summary>
    /// What the MassBank export dialogs say when the project metadata would not make valid records.
    /// </summary>
    internal static class MassBankProjectMetadataMessage
    {
        internal const string Header = "MassBank records cannot be exported until these are set in Option > Project property setting:";

        internal const string ConstrainSuggestion = "Turn on \"Constrain to MassBank format\" there to choose the instrument type and the licence from MassBank's lists and to have every value checked before it is saved.";

        /// <summary>
        /// The problems as a list under <see cref="Header"/>, followed by <see cref="ConstrainSuggestion"/> when the
        /// project is not constrained to MassBank format; empty when there are no problems.
        /// </summary>
        public static string Describe(IReadOnlyList<string> problems, bool isProjectConstrainedToMassBankFormat) {
            if (problems is null || problems.Count == 0) {
                return string.Empty;
            }
            var lines = new List<string> { Header, };
            lines.AddRange(problems.Select(p => "- " + p));
            if (!isProjectConstrainedToMassBankFormat) {
                lines.Add(ConstrainSuggestion);
            }
            return string.Join("\n", lines);
        }
    }
}
