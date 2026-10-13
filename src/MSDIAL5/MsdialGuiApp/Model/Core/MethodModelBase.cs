using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{

    internal abstract class MethodModelBase : BindableBase, IMethodModel, IDisposable
    {
        private readonly IMessageBroker _broker;

        public MethodModelBase(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFiles,
            FilePropertiesModel projectBaseParameter,
            IMessageBroker broker) {
            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            AnalysisFileModelCollection = analysisFileBeanModelCollection ?? throw new ArgumentNullException(nameof(analysisFileBeanModelCollection));
            AlignmentFiles = alignmentFiles ?? throw new ArgumentNullException(nameof(alignmentFiles));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        /// <summary>
        /// How far this method type can show a project whose raw data files are missing.
        /// Unless a method type says otherwise, its peak spot viewer reads the raw data, and only alignment results can be browsed.
        /// </summary>
        public virtual BrowsingWithoutRawData BrowsingWithoutRawData => BrowsingWithoutRawData.AlignmentsOnly;

        public AnalysisFileBeanModel? AnalysisFileModel {
            get => analysisFileModel;
            set => SetProperty(ref analysisFileModel, value);
        }
        private AnalysisFileBeanModel? analysisFileModel;

        public AnalysisFileBeanModelCollection AnalysisFileModelCollection { get; }

        public IAnalysisModel? AnalysisModelBase {
            get => analysisModelBase;
            private set => SetProperty(ref analysisModelBase, value);
        }
        private IAnalysisModel? analysisModelBase;

        public Task LoadAnalysisFileAsync(AnalysisFileBeanModel analysisFile, CancellationToken token) {
            if (AnalysisFileModel == analysisFile || analysisFile is null) {
                return Task.CompletedTask;
            }
            // Refused before anything is saved or disposed, so the sample on screen stays as it is.
            if (CannotOpenReason(analysisFile, checkAgain: true) is { } refusal) {
                _broker.Publish(refusal);
                return Task.CompletedTask;
            }
            var task = AnalysisModelBase?.SaveAsync(token) ?? Task.CompletedTask;
            TryOpen(analysisFile);
            return task;
        }

        /// <summary>
        /// Why a sample cannot be opened in the peak spot viewer, or null when it can.
        /// Opening by hand checks the disk again; the automatic first open uses what the dataset found a moment before,
        /// so that a folder that is gone is not checked once for every sample in it.
        /// </summary>
        private ErrorMessageBoxRequest? CannotOpenReason(AnalysisFileBeanModel file, bool checkAgain) {
            if (BrowsingWithoutRawData == BrowsingWithoutRawData.NotSupported) {
                // Imaging: opening a file selects its image, and the project only opened because the raw data are there.
                return null;
            }
            if (BrowsingWithoutRawData == BrowsingWithoutRawData.AlignmentsOnly
                && !(checkAgain ? file.RefreshRawDataAvailability() : file.IsRawDataAvailable)) {
                return RawDataGuard.SampleNeedsRawData(file);
            }
            if (!file.PeakListFileExists()) {
                return RawDataGuard.PeakListMissing(file);
            }
            return null;
        }

        private void TryOpen(AnalysisFileBeanModel file) {
            try {
                AnalysisFileModel = file;
                AnalysisModelBase = LoadAnalysisFileCore(file);
            }
            catch (Exception ex) {
                // Never leave the selection on a sample whose model failed: the project stays open with no sample shown,
                // where this used to end in the crash dialog with the view bound to a disposed model.
                AnalysisModelBase = null;
                AnalysisFileModel = null;
                _broker.Publish(RawDataGuard.SampleOpenFailed(file, ex.GetBaseException()));
            }
        }


        public void AutoParametersSave(string starttimestamp, TimeSpan currentTimeSpan, ParameterBase param) {
            var elapsedTime = String.Format("{0}h{1}min{2}sec", currentTimeSpan.Hours, currentTimeSpan.Minutes, currentTimeSpan.Seconds);
            var folderpath = param.ProjectFolderPath;
            var endtimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            var output = Path.Combine(folderpath, Path.GetFileNameWithoutExtension(param.ProjectFileName) + "_param_" + endtimestamp + ".txt");
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Start time stamp: {0}", starttimestamp);
                sw.WriteLine("End time stamp: {0}", endtimestamp);
                sw.WriteLine("Analysis time: {0}", elapsedTime);
                sw.WriteLine(string.Join("\n", param.ParametersAsText()));
            };
        }

        protected abstract IAnalysisModel? LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile);

        public AlignmentFileBeanModel? AlignmentFile {
            get => _alignmentFile;
            set => SetProperty(ref _alignmentFile, value);
        }
        private AlignmentFileBeanModel? _alignmentFile;

        public AlignmentFileBeanModelCollection AlignmentFiles { get; }
        public IAlignmentModel? AlignmentModelBase {
            get => alignmentModelBase;
            private set => SetProperty(ref alignmentModelBase, value);
        }
        private IAlignmentModel? alignmentModelBase;

        public Task LoadAlignmentFileAsync(AlignmentFileBeanModel alignmentFileModel, CancellationToken token) {
            if (AlignmentFile == alignmentFileModel || alignmentFileModel is null) {
                return Task.CompletedTask;
            }
            var task = AlignmentModelBase?.SaveAsync() ?? Task.CompletedTask;

            try {
                AlignmentFile = alignmentFileModel;
                AlignmentModelBase = LoadAlignmentFileCore(AlignmentFile);
            }
            catch (Exception ex) {
                // As for a sample: the project stays open with no alignment result shown, instead of the crash dialog.
                AlignmentModelBase = null;
                AlignmentFile = null;
                _broker.Publish(RawDataGuard.AlignmentOpenFailed(alignmentFileModel, ex.GetBaseException()));
            }

            return task;
        }

        protected abstract IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel);

        public abstract Task RunAsync(ProcessOption option, CancellationToken token);

        /// <summary>
        /// Opens the first included sample that can be opened. When none can, no sample is opened and nothing more is said:
        /// the dataset has already said which raw data are missing, and alignment results open as usual.
        /// </summary>
        public virtual Task LoadAsync(CancellationToken token) {
            var analysisFile = AnalysisFileModelCollection.IncludedAnalysisFiles.FirstOrDefault(file => CannotOpenReason(file, checkAgain: false) is null);
            if (AnalysisFileModel != analysisFile && !(analysisFile is null)) {
                TryOpen(analysisFile);
            }
            return Task.CompletedTask;
        }

        public virtual Task SaveAsync() {
            return Task.WhenAll(new List<Task>
            {
                AnalysisModelBase?.SaveAsync(default) ?? Task.CompletedTask,
                AlignmentModelBase?.SaveAsync() ?? Task.CompletedTask,
            });
        }

        public void InvokeMsfinder(IResultModel model) {
            model.InvokeMsfinder();
        }

        // IDisposable interface
        private bool _disposedValue;
        protected CompositeDisposable Disposables = new CompositeDisposable();

        protected virtual void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}