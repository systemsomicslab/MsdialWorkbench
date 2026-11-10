using CompMs.App.Msdial.UiTests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests
{
    /// <summary>
    /// Part 2 of the MS-DIAL metabolomics hands-on ("untargeted lipidomics"), negative mode, as the
    /// guide has people do it: set the blank's type and the samples' classes, choose lipidomics,
    /// check all lipid classes, add the EquiSPLASH m/z-RT library with retention times and put it
    /// first, remove features found in the blank, process, normalize by SPLASH, and export the raw
    /// and the normalized heights.
    /// </summary>
    [TestClass]
    [TestCategory("UI")]
    public class HandsOnPart2Tests
    {
        private const string LipidDbTitle = "Lipid database setting";
        private const string NormalizationTitle = "Normalization setting";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public void ProfilesLipidsNormalizesBySplashAndExports() {
            var exe = RunParameters.RequireMsdialExe(TestContext);
            var handsOnDir = RunParameters.RequireHandsOnDataDir(TestContext);
            AssertOneLipidLibraryBeside(exe);
            var dataset = HandsOnData.Part2Negative(handsOnDir);
            var work = WorkDirectory.Prepare(TestContext, handsOnDir, dataset);
            var textLibrary = HandsOnData.Part2NegativeTextLibrary(handsOnDir);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.CreateProjectAndRun(session, dataset, work, dragAndDrop: false,
                    onFilesPage: wizard => SetTypesAndClasses(session, wizard),
                    onMeasurementPage: wizard => session.ById(wizard, "RadioButton_Lipidomics", Workflow.WizardTitle).AsRadioButton().IsChecked = true,
                    beforeRun: wizard => {
                        SetUpLipidAnnotation(session, wizard, textLibrary);
                        RemoveFeaturesFoundInBlank(session, wizard);
                    });
                Workflow.WaitForResults(session, dataset, "processing");
                Workflow.BrowseResults(session, dataset);

                NormalizeBySplash(session);
                var export = Path.Combine(work, "export");
                Workflow.ExportAlignment(session, export, dialog => ChooseHeights(session, dialog));
                CompareWithExpectedResult(export, Path.Combine(handsOnDir, @"expected_result\Height_Neg.txt"));
                session.Close();
            }

            Directory.Delete(work, recursive: true);
        }

        /// <summary>
        /// Lipidomics needs exactly one lipid library (.lbm or .lbm2) beside MSDIAL.exe; a release
        /// build downloads it (DownloadLibraries=true). Without it MS-DIAL refuses lipidomics, which
        /// a release should not ship with, so it is checked up front for a readable failure.
        /// </summary>
        private static void AssertOneLipidLibraryBeside(string exe) {
            var libraries = Directory.GetFiles(Path.GetDirectoryName(exe)!, "*.lbm*");
            Assert.AreEqual(1, libraries.Length, $"Lipidomics needs exactly one lipid library (*.lbm, *.lbm2) beside {exe}; found {libraries.Length}. Release builds download it with DownloadLibraries=true.");
        }

        /// <summary>The guide's metadata.xlsx: the blank is of type Blank, the samples of class SPF or GF.</summary>
        internal static void SetTypesAndClasses(MsdialSession session, Window wizard) {
            var rows = session.ById(wizard, "Datagrid_AnalysisFileProperties", Workflow.WizardTitle).FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            foreach (var row in rows) {
                var name = row.FindFirstDescendant(cf => cf.ByAutomationId("TextBox_FileName")).AsTextBox().Text;
                var isBlank = name.Contains("_Blank_");
                if (isBlank) {
                    SetFileType(session, row, "Blank");
                }
                var fileClass = isBlank ? "Blank" : name.Contains("_SPF_") ? "SPF" : "GF";
                row.FindFirstDescendant(cf => cf.ByAutomationId("TextBox_FileClass")).AsTextBox().Enter(fileClass);
            }
            session.Settle("setting the file types and classes", Workflow.WizardTitle);
        }

        /// <summary>
        /// The Type column is a DataGridComboBoxColumn: outside edit mode its combo box only shows
        /// the value, and choosing in it writes nothing back. As a user does, click the cell twice
        /// to edit it, choose, and press Enter to commit; then check the row shows the new type.
        /// </summary>
        private static void SetFileType(MsdialSession session, AutomationElement row, string type) {
            var cell = row.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)).Parent;
            session.WaitForDialog(Workflow.WizardTitle, Workflow.DialogTimeout).BringToFront();
            cell.Click();
            cell.Click();
            var editor = session.WaitFor("the file type cell entering edit mode",
                () => cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox))?.AsComboBox() is { IsEnabled: true } combo ? combo : null,
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
            editor.Select(type);
            FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
            session.Settle($"committing the file type {type}", Workflow.WizardTitle);
            var shown = row.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)).AsComboBox().SelectedItem?.Text
                ?? row.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)).Name;
            if (shown != type) {
                session.Fail("setting the file type", $"The row shows the type '{shown}' after choosing '{type}'.");
            }
        }

        internal static void SetUpLipidAnnotation(MsdialSession session, Window wizard, string textLibrary) {
            Workflow.ShowWizardPage(session, wizard, "Identification");

            // "Lipid database -> Set -> Check all".
            session.Press(session.ButtonNamed(wizard, "Configure lipid class", Workflow.WizardTitle), "Configure lipid class", Workflow.WizardTitle);
            var lipidDb = session.WaitForDialog(LipidDbTitle, Workflow.DialogTimeout, Workflow.WizardTitle);
            session.Press(session.ButtonNamed(lipidDb, "Check all", LipidDbTitle, Workflow.WizardTitle), "Check all", LipidDbTitle, Workflow.WizardTitle);
            session.Press(session.ButtonNamed(lipidDb, "Finish", LipidDbTitle, Workflow.WizardTitle), "Finish", LipidDbTitle, Workflow.WizardTitle);
            session.Settle("closing the lipid database setting", Workflow.WizardTitle);

            // The m/z-RT text library: "+", its path, retention time tolerance 0.5 min used for both
            // scoring and filtering, and moved to the top of the annotation methods.
            session.Press(session.ById(wizard, "Button_AddDataBase", Workflow.WizardTitle).AsButton(), "Add database", Workflow.WizardTitle);
            session.ById(wizard, "TextBox_DataBasePath", Workflow.WizardTitle).AsTextBox().Enter(textLibrary);
            var libraryName = Path.GetFileNameWithoutExtension(textLibrary);
            var annotator = session.WaitFor("the annotation method for the text library being added",
                () => wizard.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                    .FirstOrDefault(item => item.Name.EndsWith("TextDBAnnotatorSettingViewModel"))?.AsListBoxItem(),
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
            annotator.Select();
            session.Settle($"selecting the annotation method of {libraryName}", Workflow.WizardTitle);
            session.ByLabel(wizard, "Retention time tolerance:", ControlType.Edit, Workflow.WizardTitle).AsTextBox().Enter("0.5");
            var retentionTime = session.WaitFor("finding the retention time setting",
                () => wizard.FindFirstDescendant(cf => cf.ByName("Retention time setting").And(cf.ByClassName("Expander"))),
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
            retentionTime.Patterns.ExpandCollapse.Pattern.Expand();
            session.Settle("opening the retention time setting", Workflow.WizardTitle);
            session.ByLabel(wizard, "Use retention time for scoring", ControlType.CheckBox, Workflow.WizardTitle).AsCheckBox().IsChecked = true;
            session.ByLabel(wizard, "Use retention time for filtering", ControlType.CheckBox, Workflow.WizardTitle).AsCheckBox().IsChecked = true;
            session.Settle("setting the retention time use", Workflow.WizardTitle);

            // Read the settings back: a value typed into the wrong box processes without complaint.
            var tolerance = session.ByLabel(wizard, "Retention time tolerance:", ControlType.Edit, Workflow.WizardTitle).AsTextBox().Text;
            var scoring = session.ByLabel(wizard, "Use retention time for scoring", ControlType.CheckBox, Workflow.WizardTitle).AsCheckBox().IsChecked;
            var filtering = session.ByLabel(wizard, "Use retention time for filtering", ControlType.CheckBox, Workflow.WizardTitle).AsCheckBox().IsChecked;
            if (tolerance != "0.5" || scoring != true || filtering != true) {
                session.Fail("setting the text library's retention time", $"The text library shows tolerance '{tolerance}', scoring {scoring}, filtering {filtering}.");
            }

            session.Press(session.ById(wizard, "Button_MoveUpAnnotator", Workflow.WizardTitle).AsButton(), "Move the text library up", Workflow.WizardTitle);
            session.Settle("moving the text library to the top", Workflow.WizardTitle);

            var first = wizard.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(item => item.Name.EndsWith("AnnotatorSettingViewModel"));
            if (!first.Name.EndsWith("TextDBAnnotatorSettingViewModel")) {
                session.Fail("putting the text library first", $"The first annotation method is still {first.Name}.");
            }
        }

        private static void RemoveFeaturesFoundInBlank(MsdialSession session, Window wizard) {
            Workflow.ShowWizardPage(session, wizard, "Alignment parameters");
            var advanced = session.WaitFor("finding the advanced alignment parameters",
                () => wizard.FindFirstDescendant(cf => cf.ByName("Advanced").And(cf.ByClassName("Expander"))),
                Controls.FindTimeout,
                title => title == Workflow.WizardTitle);
            advanced.Patterns.ExpandCollapse.Pattern.Expand();
            session.Settle("opening the advanced alignment parameters", Workflow.WizardTitle);
            session.ById(wizard, "CheckBox_RemoveFeatureBasedOnBlank", Workflow.WizardTitle).AsCheckBox().IsChecked = true;
        }

        /// <summary>
        /// "Data visualization -> Normalization -> SPLASH lipidomix -> Find compounds -> Normalize".
        /// The guide's EquiSPLASH in pmol/uL plasma is the default.
        /// </summary>
        private static void NormalizeBySplash(MsdialSession session) {
            Workflow.SelectRibbonTab(session, "Data visualization");
            session.Press(session.ButtonNamed(session.MainWindow, "Normalization"), "Normalization");
            var dialog = session.WaitForDialog(NormalizationTitle, Workflow.DialogTimeout);
            session.WaitFor("finding SPLASH lipidomix",
                () => dialog.FindFirstDescendant(cf => cf.ByName("SPLASH lipidomix").And(cf.ByControlType(ControlType.RadioButton)))?.AsRadioButton(),
                Controls.FindTimeout,
                title => title == NormalizationTitle).IsChecked = true;
            session.Settle("choosing SPLASH lipidomix", NormalizationTitle);
            session.Press(session.ButtonNamed(dialog, "Find compounds", NormalizationTitle), "Find compounds", NormalizationTitle);
            session.Settle("finding the SPLASH compounds", NormalizationTitle);
            session.Press(session.ButtonNamed(dialog, "Normalize", NormalizationTitle), "Normalize", NormalizationTitle);
            session.WaitFor("the normalization finishing",
                () => session.IsDialogOpen(NormalizationTitle) ? null : dialog,
                Workflow.ProcessingTimeout,
                title => title == NormalizationTitle || title.StartsWith("Progress"));
            session.Settle("normalizing");
        }

        /// <summary>The guide: "Raw data (Height)" and "Normalized data (Height)".</summary>
        private static void ChooseHeights(MsdialSession session, Window dialog) {
            foreach (var label in new[] { "Raw data (Height)", "Normalized data (Height)" }) {
                var checkBox = session.WaitFor($"finding '{label}'",
                    () => dialog.FindFirstDescendant(cf => cf.ByName(label).And(cf.ByControlType(ControlType.CheckBox)))?.AsCheckBox(),
                    Controls.FindTimeout,
                    title => title == Workflow.ExportDialogTitle);
                checkBox.IsChecked = true;
            }
        }

        /// <summary>
        /// The guide's expected result was made by an earlier MS-DIAL and curated by hand, so the
        /// tables are not compared cell by cell. Every deuterated EquiSPLASH standard it annotates
        /// must be annotated here too, under the same name, and normalized; the rest is reported.
        /// </summary>
        private void CompareWithExpectedResult(string exportDirectory, string expectedHeight) {
            var height = Directory.GetFiles(exportDirectory, "Height_*.txt").SingleOrDefault();
            var normalized = Directory.GetFiles(exportDirectory, "NormalizedHeight_*.txt").SingleOrDefault();
            Assert.IsNotNull(height, $"No raw height table (Height_*.txt) in {exportDirectory}.");
            Assert.IsNotNull(normalized, $"No normalized height table (NormalizedHeight_*.txt) in {exportDirectory}.");

            var expected = AnnotatedNames(expectedHeight);
            var found = AnnotatedNames(height!);
            var normalizedFound = AnnotatedNames(normalized!);
            var standards = expected.Where(IsDeuterated).ToList();
            var missing = standards.Where(name => !found.Contains(name)).ToList();
            var notNormalized = standards.Where(name => !normalizedFound.Contains(name)).ToList();

            TestContext.WriteLine($"Annotated spots: {found.Count} here, {expected.Count} in the expected result; {expected.Count(found.Contains)} names in both.");
            TestContext.WriteLine($"Deuterated standards: {standards.Count - missing.Count} of {standards.Count} found.");
            Assert.IsTrue(missing.Count == 0, $"Standards the expected result annotates are not annotated here: {string.Join(", ", missing)}");
            Assert.IsTrue(notNormalized.Count == 0, $"Standards are missing from the normalized table: {string.Join(", ", notNormalized)}");
        }

        private static bool IsDeuterated(string name) => name.Contains("(d");

        private static HashSet<string> AnnotatedNames(string table) {
            var lines = File.ReadAllLines(table);
            var header = Array.FindIndex(lines, line => line.StartsWith("Alignment ID"));
            Assert.IsTrue(header >= 0, $"{table} has no 'Alignment ID' header row.");
            var column = Array.IndexOf(lines[header].Split('\t'), "Metabolite name");
            Assert.IsTrue(column >= 0, $"{table} has no 'Metabolite name' column.");
            return new HashSet<string>(lines.Skip(header + 1)
                .Select(line => line.Split('\t'))
                .Where(cells => cells.Length > column)
                .Select(cells => cells[column])
                .Where(name => !string.IsNullOrWhiteSpace(name) && name != "Unknown" && name != "null" && !name.StartsWith("w/o")));
        }
    }
}
