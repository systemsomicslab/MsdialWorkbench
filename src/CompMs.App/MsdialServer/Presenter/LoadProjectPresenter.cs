using CompMs.App.MsdialServer.Entity;
using CompMs.App.MsdialServer.Usecase;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Presenter;

public sealed class LoadProjectPresenter(IListEntriesUsecase listEntries) : ILoadProjectPresenter
{
    private readonly IListEntriesUsecase _listEntries = listEntries;

    public IReadOnlyReactiveProperty<ProjectDataFile[]> Entries => _listEntries.Entries;

    public Task SearchEntriesAsync() => _listEntries.SearchEntriesAsync();
}
