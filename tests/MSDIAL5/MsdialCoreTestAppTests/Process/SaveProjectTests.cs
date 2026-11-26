using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialIntegrate.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// Every Console mode saves its project through <see cref="CommonProcess.SaveProjectAsync"/>. A dataset that
/// could not be saved used to be reported on the output only, and the run still ended with 0.
/// </summary>
[TestClass]
public sealed class SaveProjectTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-save-project-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public async Task ADatasetThatCannotBeSavedFailsTheSaveAndIsNamed() {
        var project = Project(new LibraryWriteFailingStorage { MsdialGcmsParameter = Parameter("failing.mddata") });

        var (saved, error) = await SaveCapturingErrorAsync(project);

        Assert.IsFalse(saved);
        StringAssert.Contains(error, "failing.mddata");
    }

    [TestMethod]
    public async Task ASavedProjectOpensAgain() {
        var project = Project(new MsdialGcmsDataStorage { MsdialGcmsParameter = Parameter("saved.mddata") });

        var (saved, error) = await SaveCapturingErrorAsync(project);

        Assert.IsTrue(saved, error);
        Assert.AreEqual(string.Empty, error);
        var loaded = await LoadAsync(project.ProjectParameter.FilePath);
        Assert.AreEqual("saved.mddata", loaded.Storages.Single().Parameter.ProjectFileName);
    }

    private ProjectDataStorage Project(MsdialGcmsDataStorage storage) {
        storage.DataBases = DataBaseStorage.CreateEmpty();
        storage.DataBaseMapper = new DataBaseMapper();
        var project = new ProjectDataStorage(new ProjectParameter(DateTime.Now, _scratch, "project.mdproject"));
        project.AddStorage(storage);
        return project;
    }

    private MsdialGcmsParameter Parameter(string projectFileName) {
        return new MsdialGcmsParameter { ProjectFolderPath = _scratch, ProjectFileName = projectFileName, };
    }

    private static async Task<(bool Saved, string Error)> SaveCapturingErrorAsync(ProjectDataStorage project) {
        var originalError = Console.Error;
        var error = new StringWriter();
        try {
            Console.SetError(error);
            var saved = await CommonProcess.SaveProjectAsync(project);
            return (saved, error.ToString());
        }
        finally {
            Console.SetError(originalError);
        }
    }

    private static async Task<ProjectDataStorage> LoadAsync(string projectFile) {
        using var stream = File.Open(projectFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using IStreamManager streamManager = ZipStreamManager.OpenGet(stream);
        var project = await ProjectDataStorage.LoadAsync(
            streamManager,
            new MsdialIntegrateSerializer(),
            path => new DirectoryTreeStreamManager(path),
            Path.GetDirectoryName(projectFile)!,
            _ => Task.FromResult<string>(null),
            _ => Assert.Fail("A dataset of the saved project could not be loaded."));
        streamManager.Complete();
        return project;
    }

    private sealed class LibraryWriteFailingStorage : MsdialGcmsDataStorage
    {
        protected override void SaveMspDB(Stream stream) => throw new IOException("Simulated library write failure.");
    }
}
