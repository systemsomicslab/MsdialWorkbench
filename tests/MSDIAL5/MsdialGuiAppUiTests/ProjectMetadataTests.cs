using CompMs.App.Msdial.UiTests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests
{
    /// <summary>
    /// The project metadata (instrument, authors, license, collision energy, comment) that MassBank,
    /// MoNA and MSP exports carry: entered when the project is created, kept when it is saved and
    /// opened again, changed afterwards in "Project property setting", and written to the export.
    /// It uses the data of the MS-DIAL metabolomics hands-on (part 1, SCIEX WIFF, positive).
    /// </summary>
    [TestClass]
    [TestCategory("UI")]
    public class ProjectMetadataTests
    {
        private const string PropertyDialogTitle = "Project property setting";

        private static readonly RawDataset HandsOnPositive = new RawDataset("Hands-on data1 pos", @"data1\pos", Polarity.Positive, 1000,
            "0717_kinetex_wine_50_4min_pos_IDA_A1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_B1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_C1.wiff",
            "0717_kinetex_wine_50_4min_pos_IDA_D1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_E1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_F1.wiff");

        // The text box ids are the same on the measurement page of the project wizard and in the
        // project property dialog.
        private static readonly Metadata Entered = new Metadata {
            ["TextBox_InstrumentType"] = "LC-ESI-QTOF",
            ["TextBox_Instrument"] = "TripleTOF 5600, SCIEX",
            ["TextBox_Authors"] = "MS-DIAL UI test",
            ["TextBox_License"] = "CC BY",
            ["TextBox_CollisionEnergy"] = "35 +/- 15 V",
            ["TextBox_Comment"] = "Entered when the project was created",
        };

        private static readonly Metadata Edited = new Metadata {
            ["TextBox_InstrumentType"] = "LC-ESI-QTOF edited",
            ["TextBox_Instrument"] = "TripleTOF 6600, SCIEX",
            ["TextBox_Authors"] = "MS-DIAL UI test, edited",
            ["TextBox_License"] = "CC BY-SA",
            ["TextBox_CollisionEnergy"] = "40 V",
            ["TextBox_Comment"] = "Changed in Project property setting",
        };

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public void EntersSavesEditsAndExportsProjectMetadata() {
            var exe = RunParameters.RequireMsdialExe(TestContext);
            var handsOnDir = RunParameters.RequireHandsOnDataDir(TestContext);
            var work = WorkDirectory.Prepare(TestContext, handsOnDir, HandsOnPositive);

            // Entered in the project wizard, it shows in the property dialog and is saved.
            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.CreateProjectAndRun(session, HandsOnPositive, work, dragAndDrop: false, onMeasurementPage: wizard => EnterMetadata(session, wizard));
                Workflow.WaitForResults(session, HandsOnPositive, "processing");
                AssertPropertyDialogShows(session, Entered, "right after processing");
                Workflow.SaveProject(session);
                session.Close();
            }
            var project = WorkDirectory.ProjectFile(work);

            // It survives opening the project again, and can be changed there and saved.
            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.LoadProject(session, HandsOnPositive, project);
                AssertPropertyDialogShows(session, Entered, "after opening the saved project");
                EditInPropertyDialog(session, Edited, finish: true);
                Workflow.SaveProject(session);
                session.Close();
            }

            // The change survives, Cancel throws an edit away, and the export carries the values.
            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.LoadProject(session, HandsOnPositive, project);
                AssertPropertyDialogShows(session, Edited, "after editing, saving and opening again");
                EditInPropertyDialog(session, new Metadata { ["TextBox_Authors"] = "Should be thrown away" }, finish: false);
                AssertPropertyDialogShows(session, Edited, "after cancelling an edit");

                Workflow.BrowseResults(session, HandsOnPositive);
                var export = Path.Combine(work, "export");
                Workflow.ExportAlignment(session, export, dialog => ChooseMassBankRecords(session, dialog));
                AssertMassBankRecordsCarry(export, Edited);
                session.Close();
            }

            Directory.Delete(work, recursive: true);
        }

        private static void EnterMetadata(MsdialSession session, Window wizard) {
            var advanced = session.WaitFor("finding the metadata section of the measurement parameters",
                () => wizard.FindFirstDescendant(cf => cf.ByName("Advanced: add further meta data")),
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
            advanced.Patterns.ExpandCollapse.Pattern.Expand();
            session.Settle("opening the metadata section", Workflow.WizardTitle);
            foreach (var entry in Entered) {
                session.ById(wizard, entry.Key, Workflow.WizardTitle).AsTextBox().Enter(entry.Value);
            }
        }

        private static Window OpenPropertyDialog(MsdialSession session) {
            Workflow.SelectRibbonTab(session, "Option");
            session.Press(session.ButtonNamed(session.MainWindow, "Project property setting"), "Project property setting");
            return session.WaitForDialog(PropertyDialogTitle, Workflow.DialogTimeout);
        }

        private static void AssertPropertyDialogShows(MsdialSession session, Metadata expected, string when) {
            var dialog = OpenPropertyDialog(session);
            var shown = new Metadata();
            foreach (var key in expected.Keys) {
                shown[key] = session.ById(dialog, key, PropertyDialogTitle).AsTextBox().Text;
            }
            session.ButtonNamed(dialog, "Cancel", PropertyDialogTitle).Invoke();
            session.Settle("closing the project property dialog", PropertyDialogTitle);

            var differences = expected.Where(entry => shown[entry.Key] != entry.Value)
                .Select(entry => $"{entry.Key}: expected '{entry.Value}', shown '{shown[entry.Key]}'")
                .ToList();
            if (differences.Count > 0) {
                session.Fail($"checking the project properties {when}", "Project property setting shows other values:" + System.Environment.NewLine + string.Join(System.Environment.NewLine, differences));
            }
        }

        private static void EditInPropertyDialog(MsdialSession session, Metadata values, bool finish) {
            var dialog = OpenPropertyDialog(session);
            foreach (var entry in values) {
                session.ById(dialog, entry.Key, PropertyDialogTitle).AsTextBox().Enter(entry.Value);
            }
            session.ButtonNamed(dialog, finish ? "Finish" : "Cancel", PropertyDialogTitle).Invoke();
            session.Settle(finish ? "finishing the project property dialog" : "cancelling the project property dialog", PropertyDialogTitle);
        }

        private static void ChooseMassBankRecords(MsdialSession session, Window dialog) {
            var massBank = session.WaitFor("finding the MassBank export option",
                () => dialog.FindFirstDescendant(cf => cf.ByName("MassBank").And(cf.ByClassName("Expander"))),
                Controls.FindTimeout,
                title => title == Workflow.ExportDialogTitle);
            massBank.Patterns.ExpandCollapse.Pattern.Expand();
            session.Settle("opening the MassBank export option", Workflow.ExportDialogTitle);
            var record = session.WaitFor("finding the MassBank record check box",
                () => massBank.FindFirstDescendant(cf => cf.ByName("Record").And(cf.ByControlType(ControlType.CheckBox)))?.AsCheckBox(),
                Controls.FindTimeout,
                title => title == Workflow.ExportDialogTitle);
            record.IsChecked = true;
        }

        /// <summary>
        /// MassBank records carry four of the six fields: AUTHORS, LICENSE, AC$INSTRUMENT and
        /// AC$INSTRUMENT_TYPE. Collision energy and comment are not part of the record.
        /// </summary>
        private static void AssertMassBankRecordsCarry(string exportDirectory, Metadata expected) {
            var records = Directory.EnumerateFiles(exportDirectory, "*.txt", SearchOption.AllDirectories)
                .Where(file => File.ReadLines(file).Any(line => line.StartsWith("ACCESSION:")))
                .ToList();
            Assert.IsTrue(records.Count > 0, $"No MassBank record was exported to {exportDirectory}.");

            var wanted = new Dictionary<string, string> {
                ["AUTHORS"] = expected["TextBox_Authors"],
                ["LICENSE"] = expected["TextBox_License"],
                ["AC$INSTRUMENT"] = expected["TextBox_Instrument"],
                ["AC$INSTRUMENT_TYPE"] = expected["TextBox_InstrumentType"],
            };
            var lines = File.ReadAllLines(records[0]);
            var problems = wanted
                .Select(entry => (entry.Key, Expected: entry.Value, Found: lines.FirstOrDefault(line => line.StartsWith(entry.Key + ": "))?.Substring(entry.Key.Length + 2)))
                .Where(field => field.Found != field.Expected)
                .Select(field => $"{field.Key}: expected '{field.Expected}', found '{field.Found ?? "(missing)"}'")
                .ToList();
            Assert.IsTrue(problems.Count == 0, $"The MassBank record {records[0]} does not carry the project metadata:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, problems)}");
        }

        private sealed class Metadata : Dictionary<string, string> { }
    }
}
