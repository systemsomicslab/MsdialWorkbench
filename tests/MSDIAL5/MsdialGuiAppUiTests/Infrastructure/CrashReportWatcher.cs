using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Notices the crash reports MS-DIAL writes from its unhandled-exception handler.
    ///
    /// The directories mirror App.ReportDirectoryCandidates in MsdialGuiApp. Reports that already
    /// existed when the watcher started are ignored, so old crashes on this machine do not fail a
    /// run, but another MS-DIAL crashing while the tests run would be blamed on them.
    /// </summary>
    internal sealed class CrashReportWatcher
    {
        private readonly HashSet<string> _known;

        private CrashReportWatcher(HashSet<string> known) {
            _known = known;
        }

        public static CrashReportWatcher Start() {
            return new CrashReportWatcher(new HashSet<string>(Snapshot(), StringComparer.OrdinalIgnoreCase));
        }

        public IReadOnlyList<string> NewReports() {
            return Snapshot().Where(path => !_known.Contains(path)).OrderBy(path => path).ToList();
        }

        private static IEnumerable<string> Snapshot() {
            var directories = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSDIAL", "crash"),
                Path.Combine(Path.GetTempPath(), "MSDIAL", "crash"),
            };
            return directories.Where(Directory.Exists)
                .SelectMany(directory => Directory.GetFiles(directory, "MSDIAL-crash-*.log"));
        }
    }
}
