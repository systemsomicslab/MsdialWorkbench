using CompMs.Common.Parser;
using CompMs.MsdialCore.Export;
using Riken.Metabolomics.MsfinderCommon.Export;
using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Asks for what every MassBank record of the export states, then writes one record per query.
    /// </summary>
    public partial class MassBankExportWin : Window
    {
        private readonly MassBankExportVM vm;
        private readonly IReadOnlyList<MsfinderQueryFile> files;
        private readonly AnalysisParamOfMsfinder param;
        private readonly string msfinderVersion;

        public MassBankExportWin(IReadOnlyList<MsfinderQueryFile> files, AnalysisParamOfMsfinder param, MsfinderMassBankExportSettings suggested, string projectFolder, string msfinderVersion) {
            InitializeComponent();
            this.files = files;
            this.param = param;
            this.msfinderVersion = msfinderVersion;
            // Beside the project folder, not in it: MS-FINDER reads every MassBank record there as a query.
            var exportFolder = string.IsNullOrEmpty(projectFolder) ? string.Empty : projectFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "_MassBank";
            vm = new MassBankExportVM(suggested, projectFolder, msfinderVersion, exportFolder);
            DataContext = vm;
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e) {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog()) {
                fbd.Description = "Select a folder to export the MassBank records to";
                fbd.SelectedPath = Directory.Exists(vm.ExportFolder) ? vm.ExportFolder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    vm.ExportFolder = fbd.SelectedPath;
                }
            }
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e) {
            if (!vm.CanExport) {
                return;
            }
            var settings = vm.ToSettings();
            var folder = vm.ExportFolder;
            MsfinderMassBankExportSummary summary;
            Mouse.OverrideCursor = Cursors.Wait;
            try {
                summary = MsfinderMassBankExport.Export(files, param, settings, msfinderVersion, MassBankSplash.Compute, folder);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException) {
                Mouse.OverrideCursor = null;
                MessageBox.Show(this, "MassBank records were not exported: " + ex.Message, "MassBank export", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally {
                Mouse.OverrideCursor = null;
            }
            var image = summary.Written == 0 || summary.OtherRecords > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information;
            MessageBox.Show(this, summary.ToMessage(folder), "MassBank export", MessageBoxButton.OK, image);
            DialogResult = true;
        }
    }
}
