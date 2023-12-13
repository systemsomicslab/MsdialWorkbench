using CompMs.App.MsdialServer.Project.Entity;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Project.View;

public interface IListProjectDataViewModel
{
    IReadOnlyReactiveProperty<ProjectDataFile[]> Entries { get; }
    Task SearchEntriesAsync();
}
