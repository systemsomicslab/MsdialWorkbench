using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Processing writes its results next to the raw files, so each test copies its raw files to a
    /// folder of its own rather than processing them where the shared data lives.
    /// </summary>
    internal static class WorkDirectory
    {
        public static string Prepare(TestContext context, string dataDir, RawDataset dataset) {
            var name = new string(dataset.Name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
            var work = Path.Combine(Path.GetTempPath(), "MsdialUiTests", $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(work);
            dataset.CopyTo(dataDir, work);
            context.WriteLine($"Working in {work}. It is kept when the test fails.");
            return work;
        }

        /// <summary>The project file processing saved, which the test fails without.</summary>
        public static string ProjectFile(string work) {
            var project = Path.Combine(work, Workflow.ProjectTitle + ".mdproject");
            Assert.IsTrue(File.Exists(project), $"Processing did not save the project file {project}.");
            return project;
        }
    }
}
