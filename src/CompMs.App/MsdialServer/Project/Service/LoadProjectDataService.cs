using CompMs.App.MsdialServer.Project.Entity;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;

namespace CompMs.App.MsdialServer.Project.Service;

internal sealed class LoadProjectDataService : ILoadProjectDataService
{
    public Task<ProjectDataStorage> LoadAsync(ProjectDataFile projectDataFile) {
        using var fs = File.Open(projectDataFile.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamManager = ZipStreamManager.OpenGet(fs);
        var deserializer = new MsdialIntegrateSerializer();

        var projectDataStorage = ProjectDataStorage.LoadAsync(
            streamManager,
            deserializer,
            path => new DirectoryTreeStreamManager(path),
            projectDataFile.Folder,
            _ => throw new Exception("mddata does not found"),
            _ => throw new Exception("mddata does not found"));
        return projectDataStorage ?? throw new Exception("Loading mdproject failed.");
    }
}
