using CompMs.App.MsdialServer.Project.Entity;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.MsdialServer.Project.Service
{
    internal interface ILoadProjectDataService
    {
        Task<ProjectDataStorage> LoadAsync(ProjectDataFile projectDataFile);
    }
}