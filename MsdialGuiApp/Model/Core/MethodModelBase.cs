using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{

    internal abstract class MethodModelBase : BindableBase, IMethodModel, IDisposable
    {
        public MethodModelBase(
            IEnumerable<AnalysisFileBean> analysisFiles,
            IEnumerable<AlignmentFileBean> alignmentFiles,
            ProjectBaseParameterModel projectBaseParameter) {
            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles ?? new AlignmentFileBean[] { });
            AnalysisFileModels = new ObservableCollection<AnalysisFileBeanModel>(analysisFiles?.Select(file => new AnalysisFileBeanModel(file)) ?? new AnalysisFileBeanModel[0]);
        }

        public AnalysisFileBeanModel AnalysisFileModel {
            get => analysisFileModel;
            set => SetProperty(ref analysisFileModel, value);
        }
        private AnalysisFileBeanModel analysisFileModel;
        public ObservableCollection<AnalysisFileBeanModel> AnalysisFileModels { get; }

        public IAnalysisModel AnalysisModelBase {
            get => analysisModelBase;
            private set => SetProperty(ref analysisModelBase, value);
        }
        private IAnalysisModel analysisModelBase;

        public Task LoadAnalysisFileAsync(AnalysisFileBeanModel analysisFile, CancellationToken token) {
            if (AnalysisFileModel == analysisFile || analysisFile is null) {
                return Task.CompletedTask;
            }
            var task = AnalysisModelBase?.SaveAsync(token) ?? Task.CompletedTask;
            AnalysisFileModel = analysisFile;
            AnalysisModelBase = LoadAnalysisFileCore(AnalysisFileModel);

            return task;
        }

        protected abstract IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile);

        public AlignmentFileBean AlignmentFile {
            get => alignmentFile;
            set => SetProperty(ref alignmentFile, value);
        }
        private AlignmentFileBean alignmentFile;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }
        public IAlignmentModel AlignmentModelBase {
            get => alignmentModelBase;
            private set => SetProperty(ref alignmentModelBase, value);
        }
        private IAlignmentModel alignmentModelBase;

        public Task LoadAlignmentFileAsync(AlignmentFileBean alignmentFile, CancellationToken token) {
            if (AlignmentFile == alignmentFile || alignmentFile is null) {
                return Task.CompletedTask;
            }
            var task = AlignmentModelBase?.SaveAsync() ?? Task.CompletedTask;

            AlignmentFile = alignmentFile;
            AlignmentModelBase = LoadAlignmentFileCore(AlignmentFile);

            return task;
        }

        protected abstract IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile);

        public abstract Task RunAsync(ProcessOption option, CancellationToken token);

        public virtual Task SaveAsync() {
            return Task.WhenAll(new List<Task>
            {
                AnalysisModelBase?.SaveAsync(default) ?? Task.CompletedTask,
                AlignmentModelBase?.SaveAsync() ?? Task.CompletedTask,
            });
        }

        private bool disposedValue;
        protected CompositeDisposable Disposables = new CompositeDisposable();

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}