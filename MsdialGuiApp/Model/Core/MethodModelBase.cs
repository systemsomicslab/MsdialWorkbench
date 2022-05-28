using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{

    public abstract class MethodModelBase : BindableBase, IMethodModel, IDisposable
    {
        public MethodModelBase(
            IEnumerable<AnalysisFileBean> analysisFiles,
            IEnumerable<AlignmentFileBean> alignmentFiles) {
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles ?? new AnalysisFileBean[] { });
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles ?? new AlignmentFileBean[] { });
        }

        public AnalysisFileBean AnalysisFile {
            get => analysisFile;
            set => SetProperty(ref analysisFile, value);
        }
        private AnalysisFileBean analysisFile;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        public IAnalysisModel AnalysisModelBase {
            get => analysisModelBase;
            private set => SetProperty(ref analysisModelBase, value);
        }
        private IAnalysisModel analysisModelBase;

        public void LoadAnalysisFile(AnalysisFileBean analysisFile) {
            if (AnalysisFile == analysisFile || analysisFile is null) {
                return;
            }
            AnalysisFile = analysisFile;
            AnalysisModelBase = LoadAnalysisFileCore(AnalysisFile);
        }

        protected abstract IAnalysisModel LoadAnalysisFileCore(AnalysisFileBean analysisFile);

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

        public void LoadAlignmentFile(AlignmentFileBean alignmentFile) {
            if (AlignmentFile == alignmentFile || alignmentFile is null) {
                return;
            }
            AlignmentFile = alignmentFile;
            AlignmentModelBase = LoadAlignmentFileCore(AlignmentFile);
        }

        protected abstract IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile);

        public abstract void Run(ProcessOption option);

        public virtual async Task SaveAsync() {
            if (AlignmentModelBase is null) {
                return;
            }
            await AlignmentModelBase.SaveAsync();
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