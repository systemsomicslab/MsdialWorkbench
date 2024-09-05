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
using Msdial.Common;
using Msdial.Common.Export;

namespace Rfx.Riken.OsakaUniv.ForAIF
{
    /// <summary>
    /// ParticularAlignmentSpotExporterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ParticularAlignmentSpotExporterWindow : Window
    {
        public ParticularAlignmentSpotExporterVM Vm { get; set; }
        public AlignmentPropertyBean Spot { get; set; }
        public MainWindow Window { get; set; }

        public ParticularAlignmentSpotExporterWindow() {
            InitializeComponent();
        }

        public ParticularAlignmentSpotExporterWindow(MainWindow mainWindow) {
            InitializeComponent();

            Window = mainWindow;
            Spot = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[mainWindow.FocusedAlignmentPeakID];
            Vm = new ParticularAlignmentSpotExporterVM() {
                ExportFilePath = mainWindow.ProjectProperty.ProjectFolderPath + "\\Spot"+Spot.AlignmentID,
                Smiles = MspDataRetrieve.GetSMILES(Spot.LibraryID, mainWindow.MspDB),
                InChIKey = MspDataRetrieve.GetInChIKey(Spot.LibraryID, mainWindow.MspDB),
                Formula = MspDataRetrieve.GetFormula(Spot.LibraryID, mainWindow.MspDB),
                IsExportAdditionalFile = true,
                FileID = -1
            };
            this.DataContext = Vm;
        }

        private void Click_ExportFilePathSelect(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Vm.ExportFilePath = fbd.SelectedPath;
            }
        }


        private void Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e) {
            Mouse.OverrideCursor = Cursors.Wait;
            Window.IsEnabled = false;
            var targetFileId = Window.FocusedFileID;
            var targetAlignmentId = Window.FocusedAlignmentFileID;
            Window.PeakViewDataAccessRefresh();
            var withCorrDec = true;
            var exportTargetFileId = Vm.IsExportAdditionalFile ? Vm.FileID : -1;
            var lcmsParam = Window.AnalysisParamForLC;
            if (!System.IO.Directory.Exists(Vm.ExportFilePath)) System.IO.Directory.CreateDirectory(Vm.ExportFilePath);
            if (lcmsParam.CompoundListInTargetMode != null && lcmsParam.CompoundListInTargetMode.Count > 0) {
                if (lcmsParam.RetentionTimeCorrectionCommon != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget == true) > 0) {
                    PrivateMethodTargetCompoundExport.ExportTargetResult(Window.ProjectProperty,Window.RdamProperty, new List<AnalysisFileBean>(Window.AnalysisFiles), Window.AlignmentFiles[Window.FocusedAlignmentFileID], Window.FocusedAlignmentResult, Window.MspDB, lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Where(x => x.IsTarget).ToList(), lcmsParam, Spot.AlignmentID, Vm.InChIKey, Vm.Smiles, Vm.Formula, Vm.ExportFilePath, exportTargetFileId, withCorrDec);
                }
                else {
                    PrivateMethodTargetCompoundExport.ExportTargetResult(Window.ProjectProperty, Window.RdamProperty, new List<AnalysisFileBean>(Window.AnalysisFiles), Window.AlignmentFiles[Window.FocusedAlignmentFileID], Window.FocusedAlignmentResult, Window.MspDB, null, lcmsParam, Spot.AlignmentID, Vm.InChIKey, Vm.Smiles, Vm.Formula, Vm.ExportFilePath, exportTargetFileId, withCorrDec);
                }
            }

            Window.AlignmentViewDataAccessRefresh();
            Window.PeakViewerForLcRefresh(targetFileId);
            Window.AlignmentViewerForLcRefresh(targetAlignmentId);

            Window.IsEnabled = true;
            Mouse.OverrideCursor = null;
            this.Close();
        }
    }

    public class ParticularAlignmentSpotExporterVM: ViewModelBase
    {
        private string path;
        private string smiles;
        private string inchikey;
        private string formula;
        private int fileId;
        private bool isExportAdditionalFile = true;

        public string ExportFilePath { get { return path; } set { if (path == value) return; path = value; OnPropertyChanged("ExportFilePath"); } }
        public int FileID { get { return fileId; } set { if (fileId == value) return; fileId = value; OnPropertyChanged("FileID"); } }
        public string Smiles { get { return smiles; } set { if (smiles == value) return; smiles = value; OnPropertyChanged("Smiles"); } }
        public string InChIKey { get { return inchikey; } set { if (inchikey == value) return; inchikey = value; OnPropertyChanged("InChIKey"); } }
        public string Formula { get { return formula; } set { if (formula == value) return; formula = value; OnPropertyChanged("Formula"); } }
        public bool IsExportAdditionalFile { get { return isExportAdditionalFile; } set { if (isExportAdditionalFile == value) return; isExportAdditionalFile = value; OnPropertyChanged("IsExportAdditionalFile"); } }
        public ParticularAlignmentSpotExporterVM() { }
    }
}
