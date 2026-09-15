using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CompMs.Common
{
    /// <summary>
    /// Which build of MS-DIAL this is: the main version, the date it was built, and the commit it
    /// was built from.
    /// </summary>
    /// <remarks>
    /// WHAT THIS REPLACES. MS-DIAL 5 stated its version from three hand-maintained .resx literals
    /// that had drifted apart and gone stale: the GUI said 5.5.250403-beta, the Console said
    /// 5.5.241113 -- frozen since 2024-11-19 while two hundred commits landed -- and MsdialCore, from
    /// which ParameterBase takes its default, still said 4.24. So every build since November 2024
    /// reported the same version, and a person who ran a locally built MS-DIAL for a paper could not
    /// afterwards say which one. That is the author's complaint of 2026-09-15 and the reason this
    /// exists.
    ///
    /// THE SHAPE HE ASKED FOR: main version plus date for display, commit id kept internally so the
    /// user-facing string stays short. <see cref="DisplayVersion"/> is what a window title, an About
    /// box and the update check use; <see cref="FullIdentity"/> is what provenance carries.
    ///
    /// NONE OF IT IS TYPED BY HAND except the main version, which is one line in
    /// Directory.Build.props. The date is stamped at build time; the commit id is already compiled in
    /// by SourceLink, which the .NET SDK enables for every SDK-style project here -- the generated
    /// AssemblyInfo.cs carries AssemblyInformationalVersion = "1.0.0+&lt;40 hex&gt;" with no
    /// configuration at all. Reading it needs no git at build time and no network at run time.
    ///
    /// READ FROM THIS ASSEMBLY, DELIBERATELY. CompMs.Common is referenced by everything -- the GUI,
    /// the Console, every mode library -- and they are built from one working tree, so its commit is
    /// theirs. It also sidesteps the fact that MsdialGuiApp sets GenerateAssemblyInfo=false and
    /// therefore carries no informational version of its own: asking the entry assembly would return
    /// nothing exactly where the answer is most wanted.
    ///
    /// EVERY VALUE DEGRADES RATHER THAN THROWS. A build from a source zip with no git history, an
    /// assembly loaded through a path that strips attributes, a future SDK that stops emitting one --
    /// each leaves its field <see cref="Unknown"/> and the others intact. A version string is not
    /// worth failing a run over, and a partial identity is worth more than none.
    /// </remarks>
    public static class MsdialBuildIdentity
    {
        /// <summary>What a field says when the build did not record it.</summary>
        public const string Unknown = "unknown";

        /// <summary>
        /// The number of hex characters of the commit id kept for display.
        /// </summary>
        /// <remarks>
        /// Eight, which is what git itself shows and what a person can retype. The full forty are
        /// still available through <see cref="FullCommitId"/> for anything that has to look the
        /// commit up rather than read it.
        /// </remarks>
        public const int ShortCommitLength = 8;

        static MsdialBuildIdentity() {
            var assembly = typeof(MsdialBuildIdentity).GetTypeInfo().Assembly;
            MainVersion = Metadata(assembly, "MsdialMainVersion") ?? Unknown;
            BuildDate = Metadata(assembly, "MsdialBuildDate") ?? Unknown;
            FullCommitId = Commit(assembly) ?? Unknown;
            CommitId = FullCommitId.Length > ShortCommitLength
                ? FullCommitId.Substring(0, ShortCommitLength)
                : FullCommitId;
            DisplayVersion = ComposeDisplayVersion(MainVersion, BuildDate);
            FullIdentity = CommitId == Unknown
                ? DisplayVersion
                : DisplayVersion + "+" + CommitId;
        }

        /// <summary>The main version, as set in Directory.Build.props. For example "5.5".</summary>
        public static string MainVersion { get; }

        /// <summary>The date this binary was built, as yyyy-MM-dd.</summary>
        /// <remarks>
        /// The BUILD date, not the commit date. Two builds of one commit share a commit id and
        /// differ here, which is the honest reading: this says when the binary was made. It is also
        /// the value the update check orders against a release's published_at.
        /// </remarks>
        public static string BuildDate { get; }

        /// <summary>The first <see cref="ShortCommitLength"/> characters of the commit id.</summary>
        public static string CommitId { get; }

        /// <summary>The whole commit id, for looking the commit up rather than reading it.</summary>
        public static string FullCommitId { get; }

        /// <summary>
        /// WHAT A USER SEES: main version and build date, for example "5.5.260915".
        /// </summary>
        /// <remarks>
        /// Deliberately without the commit id. The author's instruction was that the displayed name
        /// stay short and the commit be kept internally -- a window title and an update dialog are
        /// not where a hex string earns its space.
        /// </remarks>
        public static string DisplayVersion { get; }

        /// <summary>
        /// WHAT PROVENANCE CARRIES: the display version and the commit, for example
        /// "5.5.260915+9ea428e9".
        /// </summary>
        /// <remarks>
        /// This is the string that belongs in an exported file, because it is the one that answers
        /// "which build produced this" exactly. The display version alone cannot: two builds a day
        /// apart share it whenever the date has not rolled over, and nothing about it identifies the
        /// source.
        /// </remarks>
        public static string FullIdentity { get; }

        /// <summary>
        /// Whether this build knows which commit it came from.
        /// </summary>
        /// <remarks>
        /// False for a build from a source archive with no git history. Callers that publish
        /// provenance should say so rather than imply an identity they do not have.
        /// </remarks>
        public static bool IsCommitKnown => CommitId != Unknown;

        /// <summary>
        /// Whether this build is newer than a release published at <paramref name="publishedAt"/>.
        /// </summary>
        /// <remarks>
        /// The question the update notification needs and never asked. It compared version STRINGS
        /// for inequality, so a locally built MS-DIAL -- which is what anyone developing it, or
        /// running a build made for a paper, is using -- was told on every start that a new version
        /// was available, and the newer thing was the one already running.
        ///
        /// Returns false when either side is unknown or unparseable, so an uncertain answer leaves
        /// the existing notification behaviour alone rather than silently suppressing a real update.
        /// Dates are compared by day: a release published hours after a build is a different
        /// artifact, but the resolution of a build stamp is a day and pretending otherwise would
        /// make the answer depend on the hour someone happened to compile.
        /// </remarks>
        public static bool IsNewerThan(string? publishedAt) {
            if (BuildDate == Unknown || string.IsNullOrWhiteSpace(publishedAt)) {
                return false;
            }
            if (!DateTime.TryParse(BuildDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var built)) {
                return false;
            }
            if (!DateTime.TryParse(publishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var published)) {
                return false;
            }
            return built.Date > published.Date;
        }

        /// <summary>
        /// "5.5" and "2026-09-15" become "5.5.260915".
        /// </summary>
        /// <remarks>
        /// The six-digit yyMMdd form is the one MS-DIAL has always used -- 5.5.241113 was 13 November
        /// 2024 -- so a version produced this way sorts and reads alongside every version already
        /// published. Internal and tested, because a date that silently stopped being appended would
        /// leave every build of a main version indistinguishable again.
        /// </remarks>
        internal static string ComposeDisplayVersion(string mainVersion, string buildDate) {
            if (mainVersion == Unknown) {
                return Unknown;
            }
            if (DateTime.TryParse(buildDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
                return mainVersion + "." + date.ToString("yyMMdd", CultureInfo.InvariantCulture);
            }
            return mainVersion;
        }

        private static string? Metadata(Assembly assembly, string key) {
            var value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == key)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// The commit id SourceLink appended to the informational version, if it is there.
        /// </summary>
        /// <remarks>
        /// The attribute reads "1.0.0+9ea428e9db1f589c0ff69395bd5a61285352df14". The part before '+'
        /// is the package version, which no project here sets, so it is not used: taking "1.0.0" for
        /// the MS-DIAL version would be worse than the stale literal this replaces.
        /// </remarks>
        private static string? Commit(Assembly assembly) {
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrWhiteSpace(informational)) {
                return null;
            }
            var separator = informational!.IndexOf('+');
            if (separator < 0 || separator == informational.Length - 1) {
                return null;
            }
            var commit = informational.Substring(separator + 1);
            return commit.All(IsHex) ? commit : null;
        }

        private static bool IsHex(char c) {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}
