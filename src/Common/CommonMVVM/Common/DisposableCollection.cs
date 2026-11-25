using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                        // The items are taken now: DisposableModelBase clears the collection as soon as this returns,
                        // before the dispatcher runs the disposal, and a clear while it runs would break the enumeration.
                        var disposables = this.ToArray();
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var disposable in disposables) {
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
