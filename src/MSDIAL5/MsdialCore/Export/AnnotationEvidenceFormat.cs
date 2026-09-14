using CompMs.Common.DataObj.Result;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    /// <summary>
    /// One representation of the annotation evidence columns shared by the analysis (.mdpeak) and
    /// the alignment (.mdalign) text exports.
    /// </summary>
    /// <remarks>
    /// These columns answer a different question from the score columns beside them. A score says
    /// how well two things agreed; the evidence record says what was compared at all, what kind of
    /// evidence stands behind the name, and how many candidates the run had to choose from. They are
    /// written together so that a reader of the file can tell a weak match from an unexamined one.
    ///
    /// WHY EVERY ABSENT VALUE IS "null" AND NEVER THE TYPE'S OWN DEFAULT NAME. Both enums have a
    /// member that means "not recorded" -- MeasuredTerms.None and AnnotationEvidenceSource.Unspecified
    /// -- and both of those stringify to a confident-looking word. Every project saved before keys
    /// 39-43 existed deserializes to exactly those defaults, because MessagePack assigns default(T)
    /// rather than the property initializer for an absent key. Writing "None" would publish
    /// "nothing was compared" about a run where the fact was simply never recorded, which is the
    /// confusion these members were added to end. The counts are int? for the same reason: null is
    /// "not recorded" and 0 is a measurement, and they must not print the same.
    ///
    /// WHY THE COUNTS DO NOT GO THROUGH ValueOrNull. BaseMetadataAccessor has four ValueOrNull
    /// overloads, two of which return "null" for any value within 1e-10 of zero, while
    /// BaseAnalysisMetadataAccessor has only two. The same expression therefore binds different
    /// overloads on the two sides, and a genuine count of 0 would print "null" in the alignment file
    /// and "0" in the analysis file -- the exact divergence AnnotationScoreFormat exists to end, and
    /// one that would destroy the only distinction the int? typing was chosen for. No integer,
    /// nullable or not, is passed to ValueOrNull.
    ///
    /// WHY THE FLAG SEPARATOR IS NOT A COMMA. Enum.ToString() on a multi-bit [Flags] value yields
    /// "Spectrum, AccurateMass". AlignmentCSVExporter quotes its fields, but
    /// AlignmentLongCSVExporter writes metadata values unquoted, and with ExportFormat.Csv the comma
    /// is a live separator -- so a long-format CSV export would gain phantom columns and shift
    /// everything to their right. The flags are joined with '|', which is safe in both formats and
    /// is not the tab separator either.
    ///
    /// ONE LIMITATION, stated rather than hidden: a result whose annotator genuinely compared
    /// nothing is indistinguishable here from one that never recorded the fact, because None is the
    /// type's not-recorded state by contract. No producer emits a result having compared nothing, so
    /// this is a statement about the contract and not a lost measurement.
    /// </remarks>
    public static class AnnotationEvidenceFormat
    {
        /// <summary>
        /// The text written wherever the run did not record the fact. Deliberately the same token
        /// the score columns use for "never computed", and the same one the neighbouring columns of
        /// both text formats already use.
        /// </summary>
        public const string NotRecorded = "null";

        /// <summary>
        /// The single tag both in-silico members are published under.
        /// </summary>
        /// <remarks>
        /// MS-DIAL keeps two members because the two directions of in-silico work fail differently:
        /// a spectrum predictor can be wrong about how a real compound fragments, a structure
        /// predictor can propose a compound that was never there. The evidence inventory this
        /// programme publishes against records in-silico assignment as one category, so the file
        /// says "InSilico" and the tool is identified beside it by AnnotatorID.
        /// </remarks>
        public const string InSilico = "InSilico";

        private const string TermSeparator = "|";

        /// <summary>
        /// The flags in declaration order, listed explicitly rather than taken from Enum.ToString()
        /// so that the rendered order stays stable if a member is ever added out of order, and so
        /// that no comma ever reaches a CSV field.
        /// </summary>
        private static readonly MeasuredTerms[] TermsInDeclarationOrder = new[]
        {
            MeasuredTerms.Spectrum,
            MeasuredTerms.AccurateMass,
            MeasuredTerms.RetentionTime,
            MeasuredTerms.RetentionIndex,
            MeasuredTerms.Ccs,
            MeasuredTerms.Isotope,
        };

        /// <summary>Whether the run recorded which terms it compared.</summary>
        public static bool IsTermsRecorded(MsScanMatchResult? result) {
            return result != null && result.MeasuredTerms != MeasuredTerms.None;
        }

        /// <summary>Whether the run recorded what kind of evidence backs the name.</summary>
        public static bool IsSourceRecorded(MsScanMatchResult? result) {
            return result != null && result.EvidenceSource != AnnotationEvidenceSource.Unspecified;
        }

        /// <summary>
        /// The compared terms as a '|'-joined list of flag names. Exposed separately from
        /// <see cref="Terms"/> so that an exporter using a different absent-value convention can
        /// reuse this decision without reimplementing the rendering.
        /// </summary>
        public static string TermList(MeasuredTerms terms) {
            return string.Join(TermSeparator, TermsInDeclarationOrder.Where(term => (terms & term) == term).Select(term => term.ToString()));
        }

        public static string Terms(MsScanMatchResult? result) {
            return IsTermsRecorded(result) ? TermList(result!.MeasuredTerms) : NotRecorded;
        }

        public static string Source(MsScanMatchResult? result) {
            if (!IsSourceRecorded(result)) {
                return NotRecorded;
            }
            switch (result!.EvidenceSource) {
                case AnnotationEvidenceSource.ByStructurePredictionTool:
                case AnnotationEvidenceSource.BySpectrumPredictionTool:
                    return InSilico;
                default:
                    return result.EvidenceSource.ToString();
            }
        }

        /// <summary>
        /// A candidate count. Zero is a measurement and prints as "0"; only a null -- the state of a
        /// run that never recorded the population -- prints as <see cref="NotRecorded"/>.
        /// </summary>
        public static string Count(int? value) {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : NotRecorded;
        }
    }
}
