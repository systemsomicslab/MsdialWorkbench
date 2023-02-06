using Msdial.Lcms.DataProcess;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class will describe how the MS/MS spectrum will be assigned to each 'alignment' spot.
    /// Now the MS/MS spectrum of a sample file having the highest identification score will be assigned to the alignment spot.
    /// In the case that no identification result is assigned to every samples, the MS/MS spectrum of a sample file having the most abundant spectrum will be assigned to the alignment spot.
    /// This process will be performed as ansynchronous process.
    /// </summary>
    public class AlignmentFinalizeProcessLC
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Finalize: ";

        public async Task<AlignmentResultBean> Finalize(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            Initialize(mainWindow);
            alignmentResult = await MainTaskAsync(mainWindow, alignmentResult);
            this.pbw.Close();
            return alignmentResult;
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
            var param = mainWindow.AnalysisParamForLC;
            var targetFormulaDB = mainWindow.TargetFormulaLibrary;
            var projectProperty = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;
            var alignmentFile = alignmentFiles[alignmentFiles.Count - 1];
            alignmentFile.SpectraFilePath = System.IO.Path.GetDirectoryName(alignmentFile.FilePath) + "\\" + alignmentFile.FileName + "." + SaveFileFormat.dcl;

            try
            {
                await Task.Run(() =>
                {
                    if (projectProperty.CheckAIF)
                    {
                        alignmentFile.SpectraFilePath = System.IO.Path.GetDirectoryName(alignmentFile.FilePath) + "\\" + alignmentFile.FileName + ".0." + SaveFileFormat.dcl;
                        for (var i = 0; i < projectProperty.Ms2LevelIdList.Count; i++)
                        {
                            var specFilePath = System.IO.Path.GetDirectoryName(alignmentFile.FilePath) + "\\" + alignmentFile.FileName + "." + i + "." + SaveFileFormat.dcl;
                            ProcessAlignmentFinalization.Execute(analysisFiles, specFilePath,
                                alignmentResult, param, projectProperty, mspDB, targetFormulaDB, progress => ReportProgress(progress), i + 1);
                        }
                    }
                    else
                        ProcessAlignmentFinalization.Execute(analysisFiles, alignmentFile.SpectraFilePath,
                            alignmentResult, param, projectProperty, mspDB, targetFormulaDB, progress => ReportProgress(progress));

                });
            }
            catch
            {

            }
            return alignmentResult;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.pbw.ProgressView.Value = progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + progress + " %";
            }));
        }
    }
}
