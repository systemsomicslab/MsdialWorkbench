using CompMs.App.Msdial.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class FileClassPropertiesModel : ReadOnlyObservableCollection<FileClassPropertyModel>, IDisposable
    {
        private readonly ReactiveProperty<List<string>> _orderedClasses;

        public FileClassPropertiesModel(ObservableCollection<FileClassPropertyModel> list) : base(list) {
            _orderedClasses = new[]
            {
                list.CollectionChangedAsObservable().ToUnit(),
                list.ObserveElementProperty(p => p.Order).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .SelectSwitch(_ => Observable.Defer(() => Observable.Return(list.OrderBy(prop => prop.Order).Select(prop => prop.Name).ToList())))
            .ToReactiveProperty();
        }

        public IObservable<IReadOnlyList<string>> OrderedClasses => _orderedClasses;

        // IDisposable interface
        private bool _disposedValue;

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _orderedClasses.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FileClassPropertiesModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
