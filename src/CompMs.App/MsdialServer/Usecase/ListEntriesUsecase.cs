using CompMs.App.MsdialServer.Entity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace CompMs.App.MsdialServer.Usecase;
public sealed class ListEntriesUsecase : IListEntriesUsecase, IDisposable
{
    private readonly ReactivePropertySlim<ProjectDataFile[]> _entries;
    private CompositeDisposable _disposables = new();

    public ListEntriesUsecase()
    {
        _entries = new ReactivePropertySlim<ProjectDataFile[]>([]).AddTo(_disposables);
    }

    public IReadOnlyReactiveProperty<ProjectDataFile[]> Entries => _entries;

    public Task SearchEntriesAsync() {
        _entries.Value = Directory.GetFileSystemEntries(@"D:\MS-DIAL demo files\2023_software\wine_metabolomics\pos")
            .Where(entry => entry.EndsWith(".mdproject"))
            .Select(entry => new ProjectDataFile(entry))
            .ToArray();
        return Task.CompletedTask;
    }

    public void Dispose() {
        _disposables.Dispose();
    }
}
