using CompMs.App.Msdial.UiTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.UiTests
{
    /// <summary>
    /// The path a person takes through an LC-MS project, end to end: create it, process it, look at
    /// the results, export them, close, and open the project again. It runs once per vendor format,
    /// because reading each format is its own code path that a release can break.
    /// </summary>
    [TestClass]
    [TestCategory("UI")]
    public class LcmsScenarioTests
    {
        // The datasets, one per vendor format, come from the file the run parameter DatasetsFile
        // names, so the demo data's file names stay out of the repository.
        public static IEnumerable<object?[]> Datasets => RunParameters.ScenarioDatasets();

        public static string DatasetName(MethodInfo method, object?[] data) => $"{method.Name} ({data[0] ?? "no " + RunParameters.DatasetsFile})";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [DynamicData(nameof(Datasets), DynamicDataDisplayName = nameof(DatasetName))]
        public void CreatesProcessesExportsAndReopensAProject(RawDataset? row) {
            var dataset = RunParameters.RequireDataset(row);
            RunParameters.SkipUnlessSelected(TestContext, dataset);
            var exe = RunParameters.RequireMsdialExe(TestContext);
            var demoDataDir = RunParameters.RequireDemoDataDir(TestContext);
            var work = WorkDirectory.Prepare(TestContext, demoDataDir, dataset);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.CreateProjectAndRun(session, dataset, work, dataset.NeedsDragAndDrop(demoDataDir));
                Workflow.WaitForResults(session, dataset, "processing");
                Workflow.BrowseResults(session, dataset);
                if (Workflow.HasAlignment(dataset)) {
                    Workflow.ExportAlignment(session, Path.Combine(work, "export"));
                }
                session.Close();
            }

            var project = WorkDirectory.ProjectFile(work);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.LoadProject(session, dataset, project);
                Workflow.BrowseResults(session, dataset);
                session.Close();
            }

            Directory.Delete(work, recursive: true);
        }
    }
}
