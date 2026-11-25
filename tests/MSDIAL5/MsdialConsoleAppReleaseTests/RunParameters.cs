using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    /// <summary>
    /// Reads what the person running the tests handed in through the run settings.
    ///
    /// A missing value means "this run was not set up for release checks" (the solution-wide
    /// dotnet test in CI, for one) and makes the test Inconclusive. A value that is given but points
    /// nowhere is a mistake in the setup and fails, so a release check cannot pass by skipping.
    /// </summary>
    internal static class RunParameters
    {
        public const string ConsoleExe = nameof(ConsoleExe);
        public const string DemoDataDir = nameof(DemoDataDir);
        public const string Cases = nameof(Cases);

        /// <summary>
        /// The JSON file that lists the cases: their raw data under <see cref="DemoDataDir"/> and
        /// method entries. It lives outside the repository, so the demo data's file names are not
        /// published.
        /// </summary>
        public const string CasesFile = nameof(CasesFile);

        /// <summary>
        /// The cases of <see cref="CasesFile"/>, as the rows of a data-driven test. Without the
        /// parameter there is one row, <c>null</c>, which the test reports Inconclusive.
        /// </summary>
        public static IEnumerable<object?[]> CaseData() {
            if (TestRun.Context?.Properties[CasesFile] is not string path || string.IsNullOrWhiteSpace(path)) {
                return new[] { new object?[] { null } };
            }
            if (!File.Exists(path)) {
                throw new FileNotFoundException($"{CasesFile} points to '{path}', which does not exist.", path);
            }
            return ReleaseTests.CasesFile.Load(path).Select(consoleCase => new object?[] { consoleCase });
        }

        public static ConsoleCase RequireCase(ConsoleCase? consoleCase) {
            if (consoleCase is null) {
                Assert.Inconclusive($"The run parameter '{CasesFile}' is not set. See tests/MSDIAL5/MsdialConsoleAppReleaseTests/README.md.");
            }
            return consoleCase!;
        }

        public static string RequireConsoleExe(TestContext context) {
            var path = Require(context, ConsoleExe);
            if (!File.Exists(path)) {
                Assert.Fail($"{ConsoleExe} points to '{path}', which does not exist.");
            }
            return path;
        }

        public static string RequireDemoDataDir(TestContext context) {
            var path = Require(context, DemoDataDir);
            if (!Directory.Exists(path)) {
                Assert.Fail($"{DemoDataDir} points to '{path}', which does not exist.");
            }
            return path;
        }

        /// <summary>
        /// Skips a case the run did not ask for. Without the parameter every case runs; with it
        /// ("sciex_wiff2,gcms_netcdf") only the named ones do.
        /// </summary>
        public static void SkipUnlessSelected(TestContext context, ConsoleCase consoleCase) {
            if (context.Properties[Cases] is string selected && !string.IsNullOrWhiteSpace(selected)) {
                var names = selected.Split(',').Select(name => name.Trim());
                if (!names.Contains(consoleCase.Name, StringComparer.OrdinalIgnoreCase)) {
                    Assert.Inconclusive($"'{consoleCase.Name}' is not in the run parameter '{Cases}'.");
                }
            }
        }

        private static string Require(TestContext context, string name) {
            if (context.Properties[name] is not string value || string.IsNullOrWhiteSpace(value)) {
                Assert.Inconclusive($"The run parameter '{name}' is not set. See tests/MSDIAL5/MsdialConsoleAppReleaseTests/README.md.");
                return null!;
            }
            return value;
        }
    }
}
