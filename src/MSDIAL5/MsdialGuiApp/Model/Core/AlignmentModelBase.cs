using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    public abstract class AlignmentModelBase : BindableBase, IAlignmentModel, IDisposable
    {
        private readonly AlignmentFileBeanModel _alignmentFileModel;

        public AlignmentModelBase(AlignmentFileBeanModel alignmentFileModel) {
            _alignmentFileModel = alignmentFileModel ?? throw new ArgumentNullException(nameof(alignmentFileModel));
            Container = alignmentFileModel.LoadAlignmentResultAsync().Result;
            if (Container == null) {
                MessageBox.Show("No aligned spot information."); // TODO: Move to view.
                Container = new AlignmentResultContainer
                {
                    AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(),
                };
            }
        }

        public AlignmentResultContainer Container {
            get => container;
            private set => SetProperty(ref container, value);
        }
        private AlignmentResultContainer container;

        public virtual Task SaveAsync() {
            return _alignmentFileModel.SaveAlignmentResultAsync(Container);
        }

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel = string.Empty;

        public abstract void SearchFragment();
        public abstract void InvokeMsfinder();

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
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
