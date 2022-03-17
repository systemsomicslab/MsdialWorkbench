using CompMs.App.Msdial.View.Setting;
using CompMs.Common.Components;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.Model.Core {
    public class RtCorrectionProcessModelLegacy {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWindow pbw;
        private string progressHeader = "File progress: ";
        private string progressFileMax;
        private int currentProgress;
        private RetentionTimeCorrectionWinLegacy rtCorrectionWin;
        #endregion

        #region // data processing method summary
        public void Process(IReadOnlyList<AnalysisFileBean> files, 
            ParameterBase param, 
            RetentionTimeCorrectionWinLegacy rtCorrectionWin) {
            this.rtCorrectionWin = rtCorrectionWin;
            bgWorkerInitialize(files);

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);

            this.bgWorker.RunWorkerAsync(
                new Object[] { 
                    files, 
                    param,
                    rtCorrectionWin.VM.RtCorrectionCommon.StandardLibrary.Where(x => x.IsTargetMolecule).ToList(),
                    rtCorrectionWin.VM.RtCorrectionParam });
        }

        private void bgWorkerInitialize(IReadOnlyList<AnalysisFileBean> files) {
            this.rtCorrectionWin.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            this.progressFileMax = files.Count.ToString();
            this.currentProgress = 0;

            var vm = new ProgressBarVM {
                IsIndeterminate = true,
                Label = "File ",
            };
            this.pbw = new ProgressBarWindow {
                DataContext = vm,
                Owner = this.rtCorrectionWin,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
         
            this.pbw.Title = "RT correction";
            this.pbw.Show();

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }
        #endregion

        #region // background workers
        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var files = (IReadOnlyList<AnalysisFileBean>)arg[0];
            var param = (ParameterBase)arg[1];
            var iStandardLibrary = (List<MoleculeMsReference>)arg[2];
            var rtParam = (RetentionTimeCorrectionParam)arg[3];

            var tmp_originalSettings = param.MinimumAmplitude;
            param.MinimumAmplitude = iStandardLibrary.Min(y => y.MinimumPeakHeight);

            System.Threading.Tasks.ParallelOptions parallelOptions = new System.Threading.Tasks.ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = param.NumThreads;
            System.Threading.Tasks.Parallel.ForEach(files, parallelOptions, f => {
                StandardDataProviderFactory factory = new StandardDataProviderFactory();
                var provider = factory.Create(f);
                RetentionTimeCorrection.Execute(f, param, provider);
                this.bgWorker.ReportProgress(1);
            });

            param.MinimumAmplitude = tmp_originalSettings;
            e.Result = new object[] { param };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            ((ProgressBarVM)this.pbw.DataContext).CurrentValue += e.ProgressPercentage;
            ((ProgressBarVM)this.pbw.DataContext).Label = $"File {((ProgressBarVM)this.pbw.DataContext).CurrentValue}/{this.progressFileMax}";
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            ((ProgressBarVM)this.pbw.DataContext).Label = "Visualizing & Exporting PDF";
            object[] arg = (object[])e.Result;
            this.pbw.Close();

            this.rtCorrectionWin.VM.RtCorrectionResUpdate();

            Mouse.OverrideCursor = null;
            this.rtCorrectionWin.IsEnabled = true;
        }

        #endregion
    }
}
