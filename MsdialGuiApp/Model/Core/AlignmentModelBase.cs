using CompMs.CommonMVVM;
using System;
using System.Reactive.Disposables;

namespace CompMs.App.Msdial.Model.Core
{
    internal class AlignmentModelBase : BindableBase, IDisposable
    {
        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel = string.Empty;

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
