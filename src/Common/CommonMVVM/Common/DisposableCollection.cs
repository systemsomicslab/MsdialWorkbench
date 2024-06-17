using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace CompMs.CommonMVVM.Common
{
    public class DisposableCollection : Collection<IDisposable>, IDisposable
    {
        protected override void InsertItem(int index, IDisposable item) {
            base.InsertItem(index, item);
            // After calling dispose method, adding items will be disposed.
            if (disposedValue) {
                item?.Dispose();
            }
        }

        protected override void SetItem(int index, IDisposable item) {
            base.SetItem(index, item);
            // After calling dispose method, adding items will be disposed.
            if (disposedValue) {
                item?.Dispose();
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (Application.Current?.Dispatcher is null) {
                        foreach (var disposable in this) {
                            disposable?.Dispose();
                        }
                    }
                    else {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var disposable in this) {
                                disposable?.Dispose();
                            }
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
