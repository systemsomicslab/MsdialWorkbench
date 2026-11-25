using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    /// <summary>
    /// Processes demo data from every vendor format and processing mode the console supports, and
    /// checks that each run finishes with peaks in every file. Reading a vendor format is its own
    /// code path, and the vendor libraries ship as their own package, so either can break a release
    /// without any change in this repository.
    /// </summary>
    [TestClass]
    [TestCategory("Release")]
    public class ProcessingTests
    {
        // The cases come from the file the run parameter CasesFile names, so the demo data's file
        // names stay out of the repository.
        public static IEnumerable<object?[]> CaseData => RunParameters.CaseData();

        public static string CaseName(MethodInfo method, object?[] data) => $"{method.Name} ({data[0] ?? "no " + RunParameters.CasesFile})";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
        public void ProcessesDemoData(ConsoleCase? row) {
            var consoleCase = RunParameters.RequireCase(row);
            RunParameters.SkipUnlessSelected(TestContext, consoleCase);
            var exe = RunParameters.RequireConsoleExe(TestContext);
            var demoDataDir = RunParameters.RequireDemoDataDir(TestContext);
            if (consoleCase.KnownIssue is { } issue) {
                Assert.Inconclusive("Known issue: " + issue);
            }

            var directory = Path.Combine(Path.GetTempPath(), "MsdialConsoleReleaseTests", $"{consoleCase.Name}-{DateTime.Now:yyyyMMdd-HHmmss}");
            TestContext.WriteLine($"Working in {directory}. It is kept when the test fails.");
            var run = new ConsoleRun(TestContext, exe, demoDataDir, consoleCase, directory);
            run.Prepare();
            run.RunAndCheck();

            Directory.Delete(directory, recursive: true);
        }
    }
}
