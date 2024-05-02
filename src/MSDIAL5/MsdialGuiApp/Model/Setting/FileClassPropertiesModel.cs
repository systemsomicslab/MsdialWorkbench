using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class FileClassPropertiesModel : ReadOnlyObservableCollection<FileClassPropertyModel>, IDisposable
    {
        private readonly ReactiveProperty<List<string>> _orderedClasses;
        private readonly ObservableCollection<FileClassPropertyModel> _list;

        public FileClassPropertiesModel(ObservableCollection<FileClassPropertyModel> list) : base(list) {
            _orderedClasses = new[]
            {
                list.CollectionChangedAsObservable().ToUnit(),
                list.ObserveElementProperty(p => p.Order).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .Select(_ => list.OrderBy(prop => prop.Order).Select(prop => prop.Name).ToList())
            .ToReactiveProperty();
            _list = list;
        }

        public IObservable<IReadOnlyList<string>> OrderedClasses => _orderedClasses;

        public IReadOnlyDictionary<string, Color> ClassToColor => _list.ToDictionary(prop => prop.Name, prop => prop.Color);

        public IObservable<IReadOnlyList<string>> GetOrderedUsedClasses(AnalysisFileBeanModelCollection files) {
            var includedNames = files.AnalysisFiles.Select(f => f.ObserveProperty(f_ => f_.AnalysisFileIncluded).Select(include => include ? f.ObserveProperty(f_ => f_.AnalysisFileClass) : Observable.Return<string?>(null)).Switch())
                .CombineLatest()
                .Select(fs => fs.Where(f => f is not null).ToHashSet());
            return new[]
            {
                _list.CollectionChangedAsObservable().ToUnit(),
                _list.ObserveElementProperty(p => p.Order).ToUnit(),
            }.Merge().StartWith(Unit.Default)
            .CombineLatest(includedNames, (_, names) => _list.Where(prop => names.Contains(prop.Name)).OrderBy(prop => prop.Order).Select(prop => prop.Name).ToList());
        }

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
