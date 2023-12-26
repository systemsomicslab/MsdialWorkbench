using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace CompMs.App.MsdialServer.Dataset.Model;

internal sealed class DatasetModel : IDisposable
{
    private CompositeDisposable _disposables = new();

    public DatasetModel()
    {
        DatasetStorage = new ReactivePropertySlim<IMsdialDataStorage<ParameterBase>?>().AddTo(_disposables);
    }

    public ReactivePropertySlim<IMsdialDataStorage<ParameterBase>?> DatasetStorage { get; }

    public void Dispose() => _disposables.Dispose();
}
