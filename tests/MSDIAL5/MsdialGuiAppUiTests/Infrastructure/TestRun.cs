using Microsoft.VisualStudio.TestTools.UnitTesting;

// The scenario datasets come from a file the run settings name, which test discovery cannot read;
// expand the data rows when the tests run instead.
[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>Set up once for the whole run, before any test.</summary>
    [TestClass]
    public static class TestRun
    {
        /// <summary>
        /// The run's context, for what has to read the run parameters without a test of its own:
        /// the data sources of data-driven tests.
        /// </summary>
        internal static TestContext? Context { get; private set; }

        [AssemblyInitialize]
        public static void Initialize(TestContext context) {
            Context = context;
            DpiAwareness.Apply();
        }
    }
}
