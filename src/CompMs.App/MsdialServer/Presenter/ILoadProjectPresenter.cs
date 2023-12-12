using CompMs.App.MsdialServer.Entity;
using Reactive.Bindings;

namespace CompMs.App.MsdialServer.Presenter;

public interface ILoadProjectPresenter {
    IReadOnlyReactiveProperty<ProjectDataFile[]> Entries { get; }
    Task SearchEntriesAsync();
}
