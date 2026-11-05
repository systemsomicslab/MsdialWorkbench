using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// The steps of the standard workflow that several tests share: create and process a project,
    /// wait for its results, load it again, look at it, save it, and export the alignment.
    /// </summary>
    internal static class Workflow
    {
        public const string ProjectTitle = "uitest";
        public const string WizardTitle = "Setting project parameters";
        private const string FileDialogTitle = "Import analysis files";
        private const string ProjectDialogTitle = "Import a project file";
        public const string ExportDialogTitle = "Alignment result export";

        public static readonly TimeSpan DialogTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Goes through the project wizard and presses Run.
        /// </summary>
        /// <param name="onMeasurementPage">
        /// Anything more to set on the "Measurement parameters" page, after the data type and
        /// polarity, such as the project metadata.
        /// </param>
        public static void CreateProjectAndRun(MsdialSession session, RawDataset dataset, string work, bool dragAndDrop, Action<Window>? onMeasurementPage = null) {
            session.Press(session.ById(session.MainWindow, "Button_NewProject").AsButton(), "New project");
            var wizard = session.WaitForDialog(WizardTitle, DialogTimeout);

            session.ById(wizard, "TextBox_ProjectTitle", WizardTitle).AsTextBox().Enter(ProjectTitle);
            session.ById(wizard, "TextBox_ProjectFolderPath", WizardTitle).AsTextBox().Enter(work);
            Next(session, wizard);

            var fileList = session.ById(wizard, "Datagrid_AnalysisFileProperties", WizardTitle);
            if (dragAndDrop) {
                session.DragFromExplorer(work, dataset.Files, wizard, fileList, WizardTitle);
            }
            else {
                session.Press(session.ById(wizard, "Button_AnalysisFilePathsSelect", WizardTitle).AsButton(), "Browse analysis files", WizardTitle);
                session.ChooseFiles(session.WaitForDialog(FileDialogTitle, DialogTimeout, WizardTitle), work, dataset.Files.ToArray());
            }
            session.WaitFor("the analysis files being listed",
                () => fileList.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).Length == dataset.Files.Count ? wizard : null,
                DialogTimeout,
                title => title == WizardTitle);
            Next(session, wizard);

            var polarity = dataset.Polarity == Polarity.Positive ? "RadioButton_PositiveMode" : "RadioButton_NegativeMode";
            foreach (var id in new[] { "RadioButton_CentroidMode", "RadioButton_CentroidModeMS2", polarity }) {
                session.ById(wizard, id, WizardTitle).AsRadioButton().IsChecked = true;
            }
            onMeasurementPage?.Invoke(wizard);
            Next(session, wizard); // Data collection
            Next(session, wizard); // Peak detection

            session.ById(wizard, "TextBox_MinimumPeakHeight", WizardTitle).AsTextBox().Enter(dataset.MinimumPeakHeight.ToString());
            session.Press(session.ButtonNamed(wizard, "Run", WizardTitle), "Run", WizardTitle);
        }

        private static void Next(MsdialSession session, Window wizard) {
            session.Press(session.ButtonNamed(wizard, "Next", WizardTitle), "Next", WizardTitle);
            session.Settle("moving to the next page of the project settings", WizardTitle);
        }

        /// <summary>
        /// Waits for the results view: the main window names the dataset and lists every file
        /// and, when there is one, the alignment. Only the progress dialogs may be open meanwhile;
        /// anything else, such as "No peak information", fails the test.
        /// </summary>
        public static void WaitForResults(MsdialSession session, RawDataset dataset, string step) {
            session.WaitFor(step,
                () => session.MainWindow.Title.StartsWith("Dataset:")
                    && ItemCount(session, "ListBox_FileName") == dataset.Files.Count
                    && (!HasAlignment(dataset) || ItemCount(session, "ListBox_AlignedFiles") > 0)
                    ? session.MainWindow
                    : null,
                ProcessingTimeout,
                title => title.StartsWith("Progress"));
            session.Settle($"showing the results after {step}");
        }

        // Alignment needs more than one file to align.
        public static bool HasAlignment(RawDataset dataset) => dataset.Files.Count > 1;

        private static int ItemCount(MsdialSession session, string listId) {
            return session.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(listId))?.AsListBox().Items.Length ?? 0;
        }

        public static void LoadProject(MsdialSession session, RawDataset dataset, string projectFile) {
            session.Press(session.ById(session.MainWindow, "Button_LoadProject").AsButton(), "Load project");
            session.ChooseFiles(session.WaitForDialog(ProjectDialogTitle, DialogTimeout), Path.GetDirectoryName(projectFile)!, Path.GetFileName(projectFile));
            WaitForResults(session, dataset, "loading the project");
        }

        public static void BrowseResults(MsdialSession session, RawDataset dataset) {
            var files = session.ById(session.MainWindow, "ListBox_FileName").AsListBox();
            foreach (var item in files.Items) {
                item.Select();
                session.Settle($"opening the analysis file {DisplayedName(item)}");
            }
            if (HasAlignment(dataset)) {
                var alignment = session.ById(session.MainWindow, "ListBox_AlignedFiles").AsListBox().Items.First();
                alignment.Select();
                session.Settle($"opening the alignment {DisplayedName(alignment)}");
            }
        }

        // The items' own names are their view model types; what the user reads is the text inside.
        private static string DisplayedName(AutomationElement item) {
            return item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name ?? item.Name;
        }

        /// <summary>Saves the open project with the quick access toolbar's save button.</summary>
        public static void SaveProject(MsdialSession session) {
            session.Press(session.ById(session.MainWindow, "Button_QuickSaveProject").AsButton(), "Save project");
            session.Settle("saving the project", title => title.StartsWith("Progress") || title.StartsWith("Saving"));
        }

        public static void SelectRibbonTab(MsdialSession session, string name) {
            session.WaitFor($"finding the {name} tab",
                () => session.MainWindow.FindFirstDescendant(cf => cf.ByName(name).And(cf.ByControlType(ControlType.TabItem)))?.AsTabItem(),
                Controls.FindTimeout).Select();
        }

        /// <summary>
        /// Exports the open project's alignment into <paramref name="directory"/>.
        /// </summary>
        /// <param name="configure">
        /// What to choose in the export dialog before pressing Export; the default formats when null.
        /// </param>
        public static void ExportAlignment(MsdialSession session, string directory, Action<Window>? configure = null) {
            Directory.CreateDirectory(directory);
            SelectRibbonTab(session, "Export");
            session.Press(session.ButtonNamed(session.MainWindow, "Alignment result"), "Alignment result export");
            var dialog = session.WaitForDialog(ExportDialogTitle, DialogTimeout);
            session.ById(dialog, "TextBox_ExportDirectory", ExportDialogTitle).AsTextBox().Enter(directory);
            configure?.Invoke(dialog);
            session.Press(session.ButtonNamed(dialog, "Export", ExportDialogTitle), "Export", ExportDialogTitle);
            session.WaitFor("the alignment result being exported",
                () => !session.IsDialogOpen(ExportDialogTitle) || Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any() ? directory : null,
                ProcessingTimeout,
                title => title == ExportDialogTitle || title.StartsWith("Progress") || title.StartsWith("Export"));
            session.Settle("exporting the alignment result", ExportDialogTitle);
            // The dialog closes itself after exporting; close it only if it stayed.
            if (session.IsDialogOpen(ExportDialogTitle)) {
                session.ButtonNamed(dialog, "Cancel", ExportDialogTitle).Invoke();
                session.Settle("closing the export dialog", ExportDialogTitle);
            }
        }
    }
}
