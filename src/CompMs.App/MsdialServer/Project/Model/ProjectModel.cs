using CompMs.App.MsdialServer.Project.Entity;
using CompMs.App.MsdialServer.Project.Service;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace CompMs.App.MsdialServer.Project.Model;

internal sealed class ProjectModel : IDisposable
{
    private CompositeDisposable _disposables = new();
    private readonly ILoadProjectDataService _loadProjectDataService;

    public ProjectModel(ILoadProjectDataService loadProjectDataService)
    {
        _loadProjectDataService = loadProjectDataService;
        ProjectDataStorage = new ReactivePropertySlim<ProjectDataStorage?>().AddTo(_disposables);
    }

    public ReactivePropertySlim<ProjectDataStorage?> ProjectDataStorage { get; }

    public async Task LoadAsync(ProjectDataFile projectDataFile) {
        ProjectDataStorage.Value = await _loadProjectDataService.LoadAsync(projectDataFile).ConfigureAwait(false);
    }

    public void Dispose() => _disposables.Dispose();
}
