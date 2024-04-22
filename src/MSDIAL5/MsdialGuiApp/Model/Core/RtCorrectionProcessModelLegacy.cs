using CompMs.App.Msdial.View.Setting;
using CompMs.Common.Components;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.Model.Core
{
    public class RtCorrectionProcessModelLegacy {
        private BackgroundWorker? _bgWorker;
        private ProgressBarWindow? _pbw;
        private string _progressFileMax = string.Empty;
        private RetentionTimeCorrectionWinLegacy? _rtCorrectionWin;

        public void Process(IReadOnlyList<AnalysisFileBean> files,
            RetentionTimeCorrectionBean[] retentionTimeCorrectionBeans,
            ParameterBase param,
            RetentionTimeCorrectionWinLegacy rtCorrectionWin) {
            _rtCorrectionWin = rtCorrectionWin;
            BgWorkerInitialize(files);

            _bgWorker!.DoWork += new DoWorkEventHandler(BgWorker_Process_DoWork);

            var tempFiles = files.Select(_ => Path.GetTempFileName()).ToArray();

            _bgWorker.RunWorkerAsync(
                new object[] { 
                    files, 
                    param,
                    rtCorrectionWin.VM.RtCorrectionCommon.StandardLibrary.Where(x => x.IsTargetMolecule).ToList(),
                    retentionTimeCorrectionBeans,
                    tempFiles });
        }

        private void BgWorkerInitialize(IReadOnlyList<AnalysisFileBean> files) {
            _rtCorrectionWin!.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            _progressFileMax = files.Count.ToString();

            var vm = new ProgressBarVM {
                IsIndeterminate = true,
                Label = "File ",
            };
            _pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = _rtCorrectionWin,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = "RT correction"
            };
            _pbw.Show();

            _bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _bgWorker.ProgressChanged += new ProgressChangedEventHandler(BgWorker_ProgressChanged);
            _bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgWorker_RunWorkerCompleted);
        }

        private void BgWorker_Process_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var files = (IReadOnlyList<AnalysisFileBean>)arg[0];
            var param = (ParameterBase)arg[1];
            var iStandardLibrary = (List<MoleculeMsReference>)arg[2];
            var rtCorrectionBeans = (RetentionTimeCorrectionBean[])arg[3];
            var rtCorrectionFilePaths = (string[])arg[4];

            var tmp_originalSettings = param.MinimumAmplitude;
            param.MinimumAmplitude = iStandardLibrary.Min(y => y.MinimumPeakHeight);

            System.Threading.Tasks.ParallelOptions parallelOptions = new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = param.NumThreads
            };
            System.Threading.Tasks.Parallel.For(0, files.Count, parallelOptions, i => {
                var f = files[i];
                var factory = new RawDataProviderFactory();
                var provider = factory.Create(f);
                RetentionTimeCorrection.Execute(f, rtCorrectionBeans[i], param, provider, rtCorrectionFilePaths[i]);
                _bgWorker!.ReportProgress(1);
            });

            param.MinimumAmplitude = tmp_originalSettings;
            e.Result = new object[] { param };
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            ((ProgressBarVM)_pbw!.DataContext).CurrentValue += e.ProgressPercentage;
            ((ProgressBarVM)_pbw.DataContext).Label = $"File {((ProgressBarVM)this._pbw.DataContext).CurrentValue}/{this._progressFileMax}";
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            ((ProgressBarVM)_pbw!.DataContext).Label = "Visualizing & Exporting PDF";
            _pbw.Close();

            _rtCorrectionWin!.VM.RtCorrectionResUpdate();
            Mouse.OverrideCursor = null;
            _rtCorrectionWin.IsEnabled = true;
        }
    }
}
