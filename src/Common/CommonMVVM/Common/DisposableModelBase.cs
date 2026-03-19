using CompMs.CommonMVVM.Common;
using System;

namespace CompMs.CommonMVVM;

public class DisposableModelBase : BindableBase, IDisposable
{
    protected DisposableCollection Disposables { get; } = [];

    protected bool disposedValue;

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                Disposables.Dispose();
                Disposables.Clear();
            }
            disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
