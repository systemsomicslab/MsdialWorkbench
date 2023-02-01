using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public class AlignedMS1DecStorageProcess
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Finalize: ";

        public async Task Finalize(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            Initialize(mainWindow);
            alignmentResult = await MainTaskAsync(mainWindow, alignmentResult);
            Finishing(mainWindow, alignmentResult);
        }

        private void Initialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0 %";
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();
        }

        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var analysisFiles = mainWindow.AnalysisFiles;
            var alignmentFiles = mainWindow.AlignmentFiles;
            var param = mainWindow.AnalysisParamForGC;
            var mspDB = mainWindow.MspDB;
            var alignmentFile = alignmentFiles[alignmentFiles.Count - 1];
            alignmentFile.SpectraFilePath = System.IO.Path.GetDirectoryName(alignmentFile.FilePath) + "\\" + alignmentFile.FileName + "." + SaveFileFormat.dcl;

            await Task.Run(() => {
                PeakAlignment.WriteAlignedSpotMs1DecResults(analysisFiles, alignmentFile.SpectraFilePath, alignmentResult, param, mspDB, progress => ReportProgress(progress));
            });
            return alignmentResult;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.Invoke((Action)(() =>
            {
                this.pbw.ProgressView.Value = progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + progress + " %";
            }));
        }

        private void Finishing(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            this.pbw.Close();

            var projectProperty = mainWindow.ProjectProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var alignmentFileBean = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];
            var param = mainWindow.AnalysisParamForGC;

            alignmentResult.IonizationType = Ionization.EI;
            alignmentResult.AnalysisParamForGC = param;

            mainWindow.IsEnabled = true;
            mainWindow.BarChartDisplayMode = BarChartDisplayMode.OriginalHeight;

            Mouse.OverrideCursor = Cursors.Wait;

            mainWindow.SaveProperty = DataStorageGcUtility.GetSavePropertyBean(mainWindow.ProjectProperty, mainWindow.RdamProperty, mainWindow.MspDB
                    , mainWindow.IupacReference, mainWindow.AnalysisParamForGC, mainWindow.AnalysisFiles, mainWindow.AlignmentFiles);
            MessagePackHandler.SaveToFile<SavePropertyBean>(mainWindow.SaveProperty, projectProperty.ProjectFilePath);
            MessagePackHandler.SaveToFile<AlignmentResultBean>(alignmentResult, alignmentFileBean.FilePath);
            //DataStorageGcUtility.SaveToXmlFile(mainWindow.SaveProperty, projectProperty.ProjectFilePath, typeof(SavePropertyBean));
            //DataStorageGcUtility.SaveToXmlFile(alignmentResult, alignmentFileBean.FilePath, typeof(AlignmentResultBean));

            mainWindow.FileNavigatorUserControlsRefresh(analysisFiles);

            mainWindow.PeakViewerForGcRefresh(0);
            mainWindow.ListBox_FileName.SelectedIndex = 0;

            mainWindow.AlignmentViewerForGcRefresh(mainWindow.AlignmentFiles.Count - 1);
            mainWindow.ListBox_AlignedFiles.SelectedIndex = mainWindow.AlignmentFiles.Count - 1;

            Mouse.OverrideCursor = null;
        }
    }
}
