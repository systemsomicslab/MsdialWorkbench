using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Reads what the person running the tests handed in through the run settings.
    ///
    /// A missing value means "this run was not set up for UI tests" (the solution-wide dotnet test
    /// in CI, for one) and makes the test Inconclusive. A value that is given but points nowhere is
    /// a mistake in the setup and fails, so a release check cannot pass by silently skipping.
    /// </summary>
    internal static class RunParameters
    {
        public const string MsdialExe = nameof(MsdialExe);
        public const string DemoDataDir = nameof(DemoDataDir);

        public static string RequireMsdialExe(TestContext context) {
            var path = Require(context, MsdialExe);
            if (!File.Exists(path)) {
                Assert.Fail($"{MsdialExe} points to '{path}', which does not exist.");
            }
            return path;
        }

        public static string RequireDemoDataDir(TestContext context) {
            return RequireDirectory(context, DemoDataDir);
        }

        /// <summary>
        /// The unpacked metabolomics hands-on package (metabolomics_hands-on.zip, with data1.zip
        /// unpacked inside it), the data the MS-DIAL lectures walk through.
        /// </summary>
        public const string HandsOnDataDir = nameof(HandsOnDataDir);

        public static string RequireHandsOnDataDir(TestContext context) {
            return RequireDirectory(context, HandsOnDataDir);
        }

        private static string RequireDirectory(TestContext context, string name) {
            var path = Require(context, name);
            if (!Directory.Exists(path)) {
                Assert.Fail($"{name} points to '{path}', which does not exist.");
            }
            return path;
        }

        public const string Datasets = nameof(Datasets);

        /// <summary>
        /// Skips a dataset the run did not ask for. Without the parameter every dataset runs; with
        /// it ("Sciex WIFF,Agilent D") only the named ones do, which is how a single vendor is
        /// rechecked without the whole matrix.
        /// </summary>
        public static void SkipUnlessSelected(TestContext context, RawDataset dataset) {
            if (context.Properties[Datasets] is string selected && !string.IsNullOrWhiteSpace(selected)) {
                var names = selected.Split(',').Select(name => name.Trim());
                if (!names.Contains(dataset.Name, System.StringComparer.OrdinalIgnoreCase)) {
                    Assert.Inconclusive($"'{dataset.Name}' is not in the run parameter '{Datasets}'.");
                }
            }
        }

        private static string Require(TestContext context, string name) {
            if (context.Properties[name] is not string value || string.IsNullOrWhiteSpace(value)) {
                Assert.Inconclusive($"The run parameter '{name}' is not set. See tests/MSDIAL5/MsdialGuiAppUiTests/README.md.");
                return null!;
            }
            return value;
        }
    }
}
