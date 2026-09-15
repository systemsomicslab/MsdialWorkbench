using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Tests
{
    /// <summary>
    /// Which build of MS-DIAL this is, derived rather than typed.
    /// </summary>
    /// <remarks>
    /// MS-DIAL 5 stated its version from three hand-maintained .resx literals that had drifted apart
    /// and gone stale -- the GUI 5.5.250403-beta, the Console 5.5.241113 frozen since November 2024
    /// while two hundred commits landed, and MsdialCore still 4.24. Every build since then reported
    /// the same string, so a person who ran a locally built MS-DIAL for a paper could not afterwards
    /// say which one. The author asked on 2026-09-15 for main version plus date plus commit, with the
    /// commit kept internal so the displayed name stays short.
    /// </remarks>
    [TestClass()]
    public class MsdialBuildIdentityTests
    {
        /// <summary>
        /// THE STAMPING ACTUALLY HAPPENED IN THIS BUILD.
        /// </summary>
        /// <remarks>
        /// The only test here that can catch the build plumbing coming undone: a renamed MSBuild
        /// property, a project that stops importing Directory.Build.targets, an SDK that stops
        /// emitting AssemblyMetadata. Every other test exercises the composition in isolation and
        /// would keep passing while the real answer was "unknown".
        ///
        /// Asserted as shape rather than value, because the value is different in every build --
        /// which is the entire point of the change.
        /// </remarks>
        [TestMethod()]
        public void ThisBuildKnowsItsOwnIdentity() {
            Assert.AreNotEqual(MsdialBuildIdentity.Unknown, MsdialBuildIdentity.MainVersion,
                "Directory.Build.props no longer reaches the assembly");
            Assert.AreNotEqual(MsdialBuildIdentity.Unknown, MsdialBuildIdentity.BuildDate,
                "Directory.Build.targets no longer stamps the build date");
            Assert.IsTrue(MsdialBuildIdentity.IsCommitKnown,
                "SourceLink no longer writes the commit into AssemblyInformationalVersion");

            Assert.IsTrue(DateTime.TryParse(MsdialBuildIdentity.BuildDate, out _), MsdialBuildIdentity.BuildDate);
            Assert.AreEqual(MsdialBuildIdentity.ShortCommitLength, MsdialBuildIdentity.CommitId.Length);
            Assert.AreEqual(40, MsdialBuildIdentity.FullCommitId.Length, "a git commit id");
            Assert.IsTrue(MsdialBuildIdentity.FullCommitId.StartsWith(MsdialBuildIdentity.CommitId));
        }

        /// <summary>
        /// The user sees the version; the file records the commit.
        /// </summary>
        /// <remarks>
        /// The author's instruction, and the reason these are two properties rather than one string
        /// everything trims. A window title and an update dialog are not where a hex string earns
        /// its space; an exported file is exactly where it does, because the display version alone
        /// cannot distinguish two builds made on one day.
        /// </remarks>
        [TestMethod()]
        public void TheDisplayVersionOmitsTheCommitAndTheFullIdentityCarriesIt() {
            StringAssert.StartsWith(MsdialBuildIdentity.DisplayVersion, MsdialBuildIdentity.MainVersion + ".");
            Assert.IsFalse(MsdialBuildIdentity.DisplayVersion.Contains("+"),
                "the commit does not belong in a window title");

            Assert.AreEqual(
                MsdialBuildIdentity.DisplayVersion + "+" + MsdialBuildIdentity.CommitId,
                MsdialBuildIdentity.FullIdentity);
        }

        /// <summary>
        /// The displayed form is the one MS-DIAL has always used.
        /// </summary>
        /// <remarks>
        /// 5.5.241113 was the thirteenth of November 2024, so a version composed this way sorts and
        /// reads alongside every version already published. A build whose date went missing falls
        /// back to the bare main version rather than inventing one.
        /// </remarks>
        [TestMethod()]
        public void TheDisplayVersionIsMainVersionPlusSixDigitDate() {
            Assert.AreEqual("5.5.260915", MsdialBuildIdentity.ComposeDisplayVersion("5.5", "2026-09-15"));
            Assert.AreEqual("5.5.241113", MsdialBuildIdentity.ComposeDisplayVersion("5.5", "2024-11-13"));
            Assert.AreEqual("5.6.260101", MsdialBuildIdentity.ComposeDisplayVersion("5.6", "2026-01-01"));

            Assert.AreEqual("5.5", MsdialBuildIdentity.ComposeDisplayVersion("5.5", MsdialBuildIdentity.Unknown));
            Assert.AreEqual("5.5", MsdialBuildIdentity.ComposeDisplayVersion("5.5", "not a date"));
            Assert.AreEqual(MsdialBuildIdentity.Unknown,
                MsdialBuildIdentity.ComposeDisplayVersion(MsdialBuildIdentity.Unknown, "2026-09-15"));
        }

        /// <summary>
        /// THE DISPLAYED VERSION STAYS PARSEABLE BY WHAT ALREADY PARSES IT.
        /// </summary>
        /// <remarks>
        /// MS-DIAL Interactive learns which build it ran by executing the Console's --version and
        /// matching /(?:^|\s)(\d+\.\d+(?:\.\d+)+)(?:\s|$)/ against the output. That is how
        /// "5.5.241113" reached its publication report, and why that report said 5.5.241113 for two
        /// years: it has no knowledge of its own, only what MS-DIAL prints.
        ///
        /// So the displayed version has to remain a plain dotted number. FullIdentity does NOT match
        /// that pattern -- the '+' before the commit is neither whitespace nor end of input -- which
        /// is why --version prints the version on one line and the commit on the next, rather than
        /// the joined form. This test is what fails if the shape of DisplayVersion drifts.
        ///
        /// A laboratory build carrying a suffix does not match, and does not need to: that path
        /// falls back to the help banner, and a laboratory build is not the reanalysis path.
        /// </remarks>
        [TestMethod()]
        public void APublicBuildsDisplayedVersionIsAPlainDottedNumber() {
            if (MsdialBuildIdentity.VersionSuffix != string.Empty) {
                Assert.Inconclusive($"this build carries the laboratory suffix '{MsdialBuildIdentity.VersionSuffix}'");
            }

            var downstream = new System.Text.RegularExpressions.Regex(@"(?:^|\s)(\d+\.\d+(?:\.\d+)+)(?:\s|$)");
            var match = downstream.Match(MsdialBuildIdentity.DisplayVersion + "\n");

            Assert.IsTrue(match.Success, MsdialBuildIdentity.DisplayVersion);
            Assert.AreEqual(MsdialBuildIdentity.DisplayVersion, match.Groups[1].Value);
            Assert.IsFalse(downstream.IsMatch(MsdialBuildIdentity.FullIdentity + "\n"),
                "and the joined form does not, which is why --version prints two lines");
        }

        /// <summary>
        /// A LOCAL BUILD NEWER THAN THE RELEASE IS NOT OFFERED AN UPDATE TO IT.
        /// </summary>
        /// <remarks>
        /// The question the update notification needed and never asked: it compared version strings
        /// for INEQUALITY, so every locally built MS-DIAL was told on every start that a new version
        /// was available, and the newer thing was the one already running. That is the author's
        /// second complaint of 2026-09-15.
        ///
        /// The release feed already carries published_at and MS-DIAL already parses it into
        /// VersionDescriptionDocument.DatePublished, where it has never been read. So this needs no
        /// new request and no new field.
        /// </remarks>
        [TestMethod()]
        public void ABuildNewerThanAReleaseSaysSo() {
            // The shipped build date is this build's own, so these use the composition directly
            // through the public predicate with dates either side of it.
            var today = DateTime.Parse(MsdialBuildIdentity.BuildDate);

            Assert.IsTrue(MsdialBuildIdentity.IsNewerThan(today.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")),
                "a build made after the release is newer than it");
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan(today.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")),
                "a genuinely newer release must still be offered");
        }

        /// <summary>
        /// Same day is not newer, so a release published the day a build was made still notifies.
        /// </summary>
        /// <remarks>
        /// The resolution of a build stamp is a day. Comparing finer would make the answer depend on
        /// the hour someone happened to compile, which is not a property anyone can reason about;
        /// erring towards showing the notification is the safe direction, because the cost is one
        /// dismissed dialog and the cost of the other error is a missed release.
        /// </remarks>
        [TestMethod()]
        public void TheSameDayIsNotNewer() {
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan(MsdialBuildIdentity.BuildDate + "T23:59:59Z"));
        }

        /// <summary>
        /// An answer it cannot compute leaves the old behaviour alone.
        /// </summary>
        /// <remarks>
        /// Returning true on an unparseable or absent date would silently suppress every update
        /// notification the moment GitHub changed its date format -- a failure that would be
        /// invisible until someone noticed they had been on an old version for a year.
        /// </remarks>
        [TestMethod()]
        public void AnUncomputableComparisonDoesNotSuppressTheNotification() {
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan(null));
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan(string.Empty));
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan("   "));
            Assert.IsFalse(MsdialBuildIdentity.IsNewerThan("whenever"));
        }
    }
}
