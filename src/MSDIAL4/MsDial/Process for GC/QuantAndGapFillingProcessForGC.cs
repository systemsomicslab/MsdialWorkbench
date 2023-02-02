using Msdial.Gcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class QuantAndGapFillingProcessForGC
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Gap filling: ";
        private string progressFileMax;

        public async Task Execute(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            Initialize(mainWindow);
            alignmentResult = await MainTaskAsync(mainWindow, alignmentResult);
            await Finalize(mainWindow, alignmentResult);
        }

        private void Initialize(MainWindow mainWindow)
        {
            mainWindow.IsEnabled = false;
            this.progressFileMax = mainWindow.AnalysisFiles.Count.ToString();

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + "0/" + this.progressFileMax;
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = mainWindow.AnalysisFiles.Count;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();
        }

        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            var rdamProperty = mainWindow.RdamProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForGC;
            var projectProp = mainWindow.ProjectProperty;
            var mspDB = mainWindow.MspDB;
            var alignmentFile = mainWindow.AlignmentFiles[mainWindow.AlignmentFiles.Count - 1];

            try
            {
                await Task.Run(() =>
                {
                    PeakAlignment.QuantAndGapFilling(rdamProperty, analysisFiles, param,
                        alignmentResult, alignmentFile, projectProp, mspDB, progress => ReportProgress(progress));
                });
            }
            catch
            {
                Console.WriteLine("error");
            }

            return alignmentResult;
        }

        private void ReportProgress(int progress)
        {
            this.pbw.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.pbw.ProgressView.Value = progress;
                this.pbw.ProgressBar_Label.Content = this.progressHeader + progress + "/" + this.progressFileMax;
            }));
        }
        private async Task Finalize(MainWindow mainWindow, AlignmentResultBean alignmentResult)
        {
            this.pbw.Close();
            await new AlignedMS1DecStorageProcess().Finalize(mainWindow, alignmentResult);
        }
    }
}
