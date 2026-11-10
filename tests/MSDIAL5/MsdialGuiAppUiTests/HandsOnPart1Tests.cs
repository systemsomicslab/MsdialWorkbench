using CompMs.App.Msdial.UiTests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests
{
    /// <summary>
    /// Part 1 of the MS-DIAL metabolomics hands-on ("natural product profiling and molecular
    /// spectrum networking"), step by step as the guide has people do it: annotate with the
    /// MassBank MSP, process, look at the results, export them, and export the molecular network.
    /// </summary>
    [TestClass]
    [TestCategory("UI")]
    public class HandsOnPart1Tests
    {
        private const string NetworkExportTitle = "Export setting of molecular spectrum networking data";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public void AnnotatesWithMassBankAndExportsTheMolecularNetwork() {
            var exe = RunParameters.RequireMsdialExe(TestContext);
            var handsOnDir = RunParameters.RequireHandsOnDataDir(TestContext);
            var dataset = HandsOnData.Part1Positive(handsOnDir);
            var work = WorkDirectory.Prepare(TestContext, handsOnDir, dataset);
            var library = HandsOnData.Part1Library(handsOnDir);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.CreateProjectAndRun(session, dataset, work, dragAndDrop: false, beforeRun: wizard => AddMspDataBase(session, wizard, library));
                Workflow.WaitForResults(session, dataset, "processing");
                Workflow.BrowseResults(session, dataset);

                var export = Path.Combine(work, "export");
                Workflow.ExportAlignment(session, export);
                AssertSomeSpotsAreAnnotated(export);

                var network = Path.Combine(work, "network");
                ExportMolecularNetwork(session, network);
                session.Close();
            }

            Directory.Delete(work, recursive: true);
        }

        /// <summary>
        /// "+" adds a database; a path ending in .msp makes it an MSP database, and the annotation
        /// method for it is added on its own once the database is valid.
        /// </summary>
        private static void AddMspDataBase(MsdialSession session, Window wizard, string library) {
            Workflow.ShowWizardPage(session, wizard, "Identification");
            session.Press(session.ById(wizard, "Button_AddDataBase", Workflow.WizardTitle).AsButton(), "Add database", Workflow.WizardTitle);
            session.ById(wizard, "TextBox_DataBasePath", Workflow.WizardTitle).AsTextBox().Enter(library);
            var name = Path.GetFileNameWithoutExtension(library);
            session.WaitFor("the annotation method for the MSP being added",
                () => wizard.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                    .Count(item => item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name?.StartsWith(name) ?? false) >= 2 ? wizard : null,
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
        }

        /// <summary>
        /// The alignment's height table names each spot in its "Metabolite name" column; with the
        /// MassBank library at least some of the wine's compounds must be found.
        /// </summary>
        private static void AssertSomeSpotsAreAnnotated(string exportDirectory) {
            var height = Directory.EnumerateFiles(exportDirectory, "Height_*.txt", SearchOption.AllDirectories).FirstOrDefault();
            Assert.IsNotNull(height, $"No height table (Height_*.txt) was exported to {exportDirectory}.");
            var lines = File.ReadAllLines(height);
            var headerIndex = Array.FindIndex(lines, line => line.StartsWith("Alignment ID"));
            Assert.IsTrue(headerIndex >= 0, $"The height table {height} has no 'Alignment ID' header row.");
            var nameColumn = Array.IndexOf(lines[headerIndex].Split('\t'), "Metabolite name");
            Assert.IsTrue(nameColumn >= 0, $"The height table {height} has no 'Metabolite name' column.");

            var rows = lines.Skip(headerIndex + 1).Select(line => line.Split('\t')).Where(cells => cells.Length > nameColumn).ToList();
            var annotated = rows.Count(cells => !string.IsNullOrWhiteSpace(cells[nameColumn])
                && !cells[nameColumn].Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                && !cells[nameColumn].StartsWith("w/o", StringComparison.OrdinalIgnoreCase)
                && !cells[nameColumn].Equals("null", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(annotated > 0, $"None of the {rows.Count} spots in {height} is annotated with the MassBank library.");
        }

        /// <summary>
        /// The guide: Export > "Molecular spectrum networking export" writes three files, the nodes
        /// and the edges as tab-separated text and both again for Cytoscape.js.
        /// </summary>
        private static void ExportMolecularNetwork(MsdialSession session, string directory) {
            Directory.CreateDirectory(directory);
            Workflow.SelectRibbonTab(session, "Export");
            session.Press(session.ButtonNamed(session.MainWindow, "Molecular spectrum networking export"), "Molecular spectrum networking export");
            var window = session.WaitForDialog(NetworkExportTitle, Workflow.DialogTimeout);
            session.ById(window, "TextBox_ExportFolderPath", NetworkExportTitle).AsTextBox().Enter(directory);
            session.Press(session.ById(window, "Button_Run", NetworkExportTitle).AsButton(), "Run", NetworkExportTitle);
            session.WaitFor("the molecular network being exported",
                () => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() >= 3 ? directory : null,
                Workflow.ProcessingTimeout,
                title => title == NetworkExportTitle || title.StartsWith("Progress"));
            session.Settle("exporting the molecular network", NetworkExportTitle);
            if (session.IsDialogOpen(NetworkExportTitle)) {
                session.ById(window, "Button_Cancel", NetworkExportTitle).AsButton().Invoke();
                session.Settle("closing the molecular network export", NetworkExportTitle);
            }

            // The guide calls the third file JSON; MS-DIAL writes it as cyelements-*.js, JSON inside.
            var nodes = SingleFile(directory, "node-*.txt");
            var edges = SingleFile(directory, "edge-*.txt");
            var elements = SingleFile(directory, "cyelements-*.js");
            Assert.IsTrue(File.ReadLines(nodes).Skip(1).Any(), $"The node table {nodes} has no node.");
            Assert.IsTrue(File.ReadLines(edges).Skip(1).Any(), $"The edge table {edges} has no edge.");
            Assert.IsTrue(File.ReadLines(elements).Take(5).Any(line => line.Contains("\"elements\"")), $"{elements} does not hold the network elements.");
        }

        private static string SingleFile(string directory, string pattern) {
            var files = Directory.GetFiles(directory, pattern);
            Assert.AreEqual(1, files.Length, $"Expected one {pattern} in {directory}, found: {string.Join(", ", Directory.GetFiles(directory).Select(Path.GetFileName))}");
            return files[0];
        }
    }
}
