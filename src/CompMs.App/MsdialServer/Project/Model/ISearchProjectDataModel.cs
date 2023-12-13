using CompMs.App.MsdialServer.Project.Entity;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Project.Model;

public interface ISearchProjectDataModel
{
    public IReadOnlyReactiveProperty<ProjectDataFile[]> Entries { get; }
    public Task SearchEntriesAsync();
}
