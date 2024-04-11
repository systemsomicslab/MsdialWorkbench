using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
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
        public MethodModelBase(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFiles,
            FilePropertiesModel projectBaseParameter) {
            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            AnalysisFileModelCollection = analysisFileBeanModelCollection ?? throw new ArgumentNullException(nameof(analysisFileBeanModelCollection));
            AlignmentFiles = alignmentFiles ?? throw new ArgumentNullException(nameof(alignmentFiles));
        }

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
            var task = AnalysisModelBase?.SaveAsync(token) ?? Task.CompletedTask;

            try {
                AnalysisFileModel = analysisFile;
                AnalysisModelBase = LoadAnalysisFileCore(AnalysisFileModel);
            }
            catch {
                task.Wait();
                throw;
            }

            return task;
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
            catch {
                task.Wait();
                throw;
            }

            return task;
        }

        protected abstract IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel);

        public abstract Task RunAsync(ProcessOption option, CancellationToken token);

        public virtual Task LoadAsync(CancellationToken token) {
            var analysisFile = AnalysisFileModelCollection.IncludedAnalysisFiles.FirstOrDefault();
            if (AnalysisFileModel != analysisFile && !(analysisFile is null)) {
                AnalysisFileModel = analysisFile;
                AnalysisModelBase = LoadAnalysisFileCore(AnalysisFileModel);
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