using CompMs.App.MsdialServer.Project.Entity;
using CompMs.App.MsdialServer.Project.Model;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Project.View;

public sealed class ListProjectDataViewModel(ISearchProjectDataModel listEntries) : IListProjectDataViewModel
{
    private readonly ISearchProjectDataModel _listEntries = listEntries;

    public IReadOnlyReactiveProperty<ProjectDataFile[]> Entries => _listEntries.Entries;

    public Task SearchEntriesAsync() => _listEntries.SearchEntriesAsync();
}
