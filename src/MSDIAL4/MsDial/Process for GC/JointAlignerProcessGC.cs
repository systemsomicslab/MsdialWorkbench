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
    public class JointAlignerProcessGC
    {
        private ProgressBarWin pbw;
        private string progressHeader = "Alignment: ";
        private string progressFileMax;

        public async Task Execute(MainWindow mainWindow)
        {
            Initialize(mainWindow);
            var alignmentResult = await MainTaskAsync(mainWindow);
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

        private async Task<AlignmentResultBean> MainTaskAsync(MainWindow mainWindow)
        {
            var analysisFiles = mainWindow.AnalysisFiles;
            var param = mainWindow.AnalysisParamForGC;
            var alignmentResultBean = new AlignmentResultBean();
            var project = mainWindow.ProjectProperty;

            await Task.Run(() =>
            {
                PeakAlignment.JointAligner(project, analysisFiles, param, alignmentResultBean, mainWindow.MspDB, progress => ReportProgress(progress));
            });
            return alignmentResultBean;
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
            await new QuantAndGapFillingProcessForGC().Execute(mainWindow, alignmentResult);
        }
    }
}
