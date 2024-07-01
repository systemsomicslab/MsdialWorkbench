using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;
#if !DEBUG_VENDOR_UNSUPPORTED && !RELEASE_VENDOR_UNSUPPORTED
using CompMs.RawDataHandler.Abf;
#endif

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// NewProjectFilePropertySettingWindow.xaml logic
    /// </summary>
    public partial class AnalysisFilePropertySetWin : Window
    {
        private MainWindow mainWindow;

        public AnalysisFilePropertySetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new AnalysisFilePropertySettingVM(this.mainWindow, this);
        }

        private void Click_AnalysisFilePathsSelect(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (this.mainWindow.ProjectProperty.SeparationType == SeparationType.IonMobility) {
                ofd.Filter = "IBF file(*.ibf)|*.ibf";
            }
            else {
                //ofd.Filter = "ABF file(*.abf)|*.abf|mzML file(*.mzml)|*.mzml|netCDF file(*.cdf)|*.cdf|IBF file(*.ibf)|*.ibf|WIFF file(*.wiff)|*.wiff|WIFF2 file(*.wiff2)|*.wiff2";
                ofd.Filter = "ABF file(*.abf)|*.abf|Hive file(*.hmd, *.mzB)|*.hmd;*.mzB|mzML file(*.mzml)|*.mzml|netCDF file(*.cdf)|*.cdf|IBF file(*.ibf)|*.ibf|WIFF file(*.wiff)|*.wiff|WIFF2 file(*.wiff2)|*.wiff2|Raw file(*.raw)|*.raw|LCD file(*.lcd)|*.lcd|QGD file(*.qgd)|*.qgd|LRP file(*.lrp)|*.lrp";
            }
            ofd.Title = "Import analysis files";
            ofd.InitialDirectory = mainWindow.ProjectProperty.ProjectFolderPath;
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true)
            {
                if (System.IO.Path.GetDirectoryName(ofd.FileNames[0]) != mainWindow.ProjectProperty.ProjectFolderPath)
                {
                    MessageBox.Show("The directory of analysis files should be where the project file is created.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                Mouse.OverrideCursor = Cursors.Wait;
                ((AnalysisFilePropertySettingVM)this.DataContext).ReadImportedFiles(ofd.FileNames);
                Mouse.OverrideCursor = null;
            }
        }

      
        private void datagrid_FileProperty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                
                var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                var clipTextList = new List<string[]>();
                
                foreach (var clip in clipText) { clipTextList.Add(clip.Split('\t')); }
                
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                var startRow = this.Datagrid_AnalysisFileProperties.Items.IndexOf(this.Datagrid_AnalysisFileProperties.SelectedCells[0].Item);
                int intValue;
                double doubleValue;

                if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 1)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;

                        ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileName = clipTextList[i][0];

                        if (clipTextList[i].Length > 1)
                        {
                            var fileType = clipTextList[i][1];
                            switch (fileType)
                            {
                                case "Sample":
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                    break;
                                case "Standard":
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Standard;
                                    break;
                                case "QC":
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.QC;
                                    break;
                                case "Blank":
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Blank;
                                    break;
                                default:
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                    break;
                            }
                        }

                        if (clipTextList[i].Length > 2)
                        {
                            ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileClass = clipTextList[i][2];
                        }

                        if (clipTextList[i].Length > 3) {
                            if (int.TryParse(clipTextList[i][3], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 4) {
                            if (int.TryParse(clipTextList[i][4], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 5) {
                            if (double.TryParse(clipTextList[i][5], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }
                    }
                }
                else if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 2)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;

                        var fileType = clipTextList[i][0];
                        switch (fileType)
                        {
                            case "Sample":
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                break;
                            case "Standard":
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Standard;
                                break;
                            case "QC":
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.QC;
                                break;
                            case "Blank":
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Blank;
                                break;
                            default:
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileType = AnalysisFileType.Sample;
                                break;
                        }

                        if (clipTextList[i].Length > 1)
                        {
                            ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileClass = clipTextList[i][1];
                        }

                        if (clipTextList[i].Length > 2) {
                            if (int.TryParse(clipTextList[i][2], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 3) {
                            if (int.TryParse(clipTextList[i][3], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }
                        }

                        if (clipTextList[i].Length > 4) {
                            if (double.TryParse(clipTextList[i][4], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }
                    }

                }
                else if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 3)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;
                        
                        ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileClass = clipTextList[i][0];

                        if (clipTextList[i].Length > 1) {
                            if (int.TryParse(clipTextList[i][1], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisBatch = intValue;
                            }
                            if (clipTextList[i].Length > 2) {
                                if (int.TryParse(clipTextList[i][2], out intValue)) {
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                                }
                            }
                            if (clipTextList[i].Length > 3) {
                                if (double.TryParse(clipTextList[i][3], out doubleValue)) {
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                                }
                            }
                        }
                    }
                }

                else if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 4)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;

                        if (int.TryParse(clipTextList[i][0], out intValue)) {
                            ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisBatch = intValue;
                        }
                        if (clipTextList[i].Length > 1)
                        {
                            if (int.TryParse(clipTextList[i][1], out intValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;
                            }

                            if (clipTextList[i].Length > 2) {
                                if (double.TryParse(clipTextList[i][2], out doubleValue)) {
                                    ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                                }
                            }
                        }
                    }
                }

                else if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 5)
                {
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;
                        if (int.TryParse(clipTextList[i][0], out intValue))
                            ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileAnalyticalOrder = intValue;

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                            }
                        }
                    }
                }

                else if (clipTextList.Count > 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 6) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;
                        if (double.TryParse(clipTextList[i][0], out doubleValue))
                            ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).InjectionVolume = doubleValue;
                    }
                }
                else if (clipTextList.Count == 1 && this.Datagrid_AnalysisFileProperties.SelectedCells[0].Column.DisplayIndex == 3)
                {
                    for (int i = 0; i < this.Datagrid_AnalysisFileProperties.SelectedCells.Count; i++)
                    {
                        if (this.Datagrid_AnalysisFileProperties.SelectedCells[i].Column.DisplayIndex != 3) continue;
                        if (startRow + i > this.Datagrid_AnalysisFileProperties.Items.Count - 1) break;
                        ((AnalysisFilePropertyVM)this.Datagrid_AnalysisFileProperties.Items[startRow + i]).AnalysisFileClass = clipTextList[0][0];
                    }
                }

                this.Datagrid_AnalysisFileProperties.UpdateLayout();

            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Datagrid_AnalysisFileProperties_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_AnalysisFileProperties.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_AnalysisFileProperties.BeginEdit();
        }

        private void dataGridFiles_DragOver(object sender, System.Windows.DragEventArgs e) {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private void dataGridFiles_Drop(object sender, System.Windows.DragEventArgs e) {
            string[] files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            string lastfile = "";
            var includedFiles = new List<string>(); 
            var excludedFiles = new List<string>();
            

            // Set BaseFileList Clone
            for (int i = 0; i < files.Length; i++) {
                if (IsAccepted(files[i])) {
                    includedFiles.Add(files[i]);
                }
                else {
                    excludedFiles.Add(System.IO.Path.GetFileName(files[i]));
                }
            }

            if (0 < excludedFiles.Count) {
                System.Windows.MessageBox.Show("The following file(s) cannot be converted because they are not acceptable raw files\n" +
                    String.Join("\n", excludedFiles.ToArray()),
                    "Unacceptable Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (includedFiles.Count > 0) {
                ((AnalysisFilePropertySettingVM)this.DataContext).ReadImportedFiles(includedFiles.ToArray());
            }
        }

        private bool IsAccepted(string file) {
            var extension = System.IO.Path.GetExtension(file).ToLower();
            if (extension != ".abf" && extension != ".mzml" &&
                extension != ".cdf" && extension != ".raw" &&
                extension != ".d" && extension != ".iabf" && extension != ".ibf" &&
                extension != ".wiff" && extension != ".wiff2" &&
                extension != ".lcd" && extension != ".qgd")
                return false;
            else
                return true;
        }
    }
}
