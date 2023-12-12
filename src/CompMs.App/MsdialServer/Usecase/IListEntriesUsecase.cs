using CompMs.App.MsdialServer.Entity;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Usecase;

public interface IListEntriesUsecase {
    public IReadOnlyReactiveProperty<ProjectDataFile[]> Entries { get; }
    public Task SearchEntriesAsync();
}
