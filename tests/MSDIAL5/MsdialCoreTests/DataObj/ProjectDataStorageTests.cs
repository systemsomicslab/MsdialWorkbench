using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ProjectDataStorageTests
    {
        [TestMethod()]
        public async Task LoadTest() {
            var projectParameter = new ProjectParameter(DateTime.Parse("2021/12/16 23:14:30"), "Folder", "TestProject");
            var proj = new ProjectDataStorage(projectParameter, new List<IMsdialDataStorage<ParameterBase>>());
            var storage1 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test1", } };
            var storage2 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test2", } };
            var storage3 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test3", } };
            proj.AddStorage(storage1);
            proj.AddStorage(storage2);
            proj.AddStorage(storage3);

            var serializer = new MockSerializer();
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer, _ => streamManager, null);
                streamManager.Complete();
            }

            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadAsync(streamManager, serializer, _ => null, "Folder", null, null);

                Assert.AreEqual(projectParameter.StartDate, actual.ProjectParameter.StartDate);
                Console.WriteLine(projectParameter.StartDate);
                Assert.AreEqual(projectParameter.FinalSavedDate, actual.ProjectParameter.FinalSavedDate);
                Console.WriteLine(projectParameter.FinalSavedDate);
                Assert.AreEqual(projectParameter.FolderPath, actual.ProjectParameter.FolderPath);
                Assert.AreEqual(projectParameter.Title, actual.ProjectParameter.Title);
                CollectionAssert.AreEqual(
                    proj.ProjectParameters.Select(parameter => parameter.ProjectFileName).ToArray(),
                    actual.ProjectParameters.Select(parameter => parameter.ProjectFileName).ToArray());
                Assert.AreEqual(proj.Storages.Count, actual.Storages.Count);
                foreach ((var exp, var act) in proj.Storages.Zip(actual.Storages, (e, a) => (e, a))) {
                    Assert.AreEqual(exp.Parameter.ProjectFileName, act.Parameter.ProjectFileName);
                }
            }
        }

        [TestMethod()]
        public async Task LoadFaultedTest() {
            var projectParameter = new ProjectParameter(DateTime.Parse("2022/03/11 16:23:50"), "Folder", "TestProject");
            var proj = new ProjectDataStorage(projectParameter, new List<IMsdialDataStorage<ParameterBase>>());
            var storage1 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test1", } };
            var storage2 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test2", } };
            var storage3 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test3", } };
            proj.AddStorage(storage1);
            proj.AddStorage(storage2);
            proj.AddStorage(storage3);

            var serializer = new FaultSerializer();
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer, null, null);
                streamManager.Complete();
            }

            var parameters = new List<ProjectBaseParameter>();
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadAsync(streamManager, serializer, _ => null, "Folder", null, parameters.Add);

                Assert.AreEqual(projectParameter.StartDate, actual.ProjectParameter.StartDate);
                Console.WriteLine(projectParameter.StartDate);
                Assert.AreEqual(projectParameter.FinalSavedDate, actual.ProjectParameter.FinalSavedDate);
                Console.WriteLine(projectParameter.FinalSavedDate);
                Assert.AreEqual(projectParameter.FolderPath, actual.ProjectParameter.FolderPath);
                Assert.AreEqual(projectParameter.Title, actual.ProjectParameter.Title);
                actual.ProjectParameters.ToList().ForEach(p => Console.WriteLine(p?.ProjectFileName));
                Assert.AreEqual(0, actual.ProjectParameters.Count);
                foreach (var parameter in parameters) {
                    CollectionAssert.DoesNotContain(
                        actual.ProjectParameters.Select(p => p.ProjectFileName).ToArray(),
                        parameter.ProjectFileName);
                }
                Assert.AreEqual(0, actual.Storages.Count);
                foreach (var parameter in parameters) {
                    CollectionAssert.DoesNotContain(
                        actual.Storages.Select(stroage => stroage.Parameter.ProjectFileName).ToArray(),
                        parameter.ProjectFileName);
                }
                CollectionAssert.AreEqual(
                    actual.ProjectParameters.Select(p => p.ProjectFileName).ToArray(),
                    actual.Storages.Select(storage => storage.Parameter.ProjectFileName).ToArray());
            }
        }

        [TestMethod()]
        public async Task LoadRetryOnNewFolderTest() {
            var projectParameter = new ProjectParameter(DateTime.Parse("2022/07/05 16:56:50"), "Folder", "TestProject");
            var proj = new ProjectDataStorage(projectParameter, new List<IMsdialDataStorage<ParameterBase>>());
            var storage1 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test1", } };
            var storage2 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test2", } };
            var storage3 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test3", } };
            proj.AddStorage(storage1);

            var serializer = new FaultIfDifferentDirectorySerializer("Folder");
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer, _ => streamManager, null);
                streamManager.Complete();
            }

            // project moved
            var new_stream = new MemoryStream(stream.ToArray());
            serializer.ChangeProjectDir("NewFolder");

            using (var streamManager = ZipStreamManager.OpenGet(new_stream)) {
                var actual = await ProjectDataStorage.LoadAsync(streamManager, serializer, _ => null, "NewFolder", null, null);

                Assert.AreEqual(projectParameter.StartDate, actual.ProjectParameter.StartDate);
                Console.WriteLine(projectParameter.StartDate);
                Assert.AreEqual(projectParameter.FinalSavedDate, actual.ProjectParameter.FinalSavedDate);
                Console.WriteLine(projectParameter.FinalSavedDate);
                Assert.AreEqual(projectParameter.FolderPath, actual.ProjectParameter.FolderPath);
                Assert.AreEqual(projectParameter.Title, actual.ProjectParameter.Title);
                CollectionAssert.AreEqual(
                    proj.ProjectParameters.Select(parameter => parameter.ProjectFileName).ToArray(),
                    actual.ProjectParameters.Select(parameter => parameter.ProjectFileName).ToArray());
                Assert.AreEqual(proj.Storages.Count, actual.Storages.Count);
                foreach ((var exp, var act) in proj.Storages.Zip(actual.Storages, (e, a) => (e, a))) {
                    Assert.AreEqual(exp.Parameter.ProjectFileName, act.Parameter.ProjectFileName);
                }
            }
        }

        [TestMethod()]
        public async Task MissingSiblingIsReportedOnceWithoutPicker() {
            var serializer = new MissingFileSerializer((folder, title) => folder == "Folder" ? title + "_Loaded.msp2.dbs" : null);
            var stream = await SaveProjectAsync(serializer, "Folder", "Test1");

            var failures = new List<(ProjectBaseParameter Parameter, Exception Error)>();
            var pickerCalls = 0;
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadReportingFailuresAsync(streamManager, serializer, _ => null, "Folder",
                    _ => { pickerCalls++; return Task.FromResult<string>(null); },
                    datasetLoadFailed: (parameter, error) => failures.Add((parameter, error)));
                Assert.AreEqual(0, actual.Storages.Count);
            }

            Assert.AreEqual(1, failures.Count);
            Assert.AreEqual("Test1", failures[0].Parameter.ProjectFileName);
            StringAssert.EndsWith(((FileNotFoundException)failures[0].Error).FileName, ".dbs");
            Assert.AreEqual(0, pickerCalls);
        }

        [TestMethod()]
        public async Task MissingDatasetFileOffersPickerAndReportsOnce() {
            var serializer = new MissingFileSerializer((folder, title) => folder == "Folder" ? title : null);
            var stream = await SaveProjectAsync(serializer, "Folder", "Test1");

            var failures = new List<Exception>();
            var pickerCalls = 0;
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadReportingFailuresAsync(streamManager, serializer, _ => null, "Folder",
                    _ => { pickerCalls++; return Task.FromResult<string>(null); },
                    datasetLoadFailed: (_, error) => failures.Add(error));
                Assert.AreEqual(0, actual.Storages.Count);
            }

            Assert.AreEqual(1, pickerCalls);
            Assert.AreEqual(1, failures.Count);
            Assert.AreEqual(Path.Combine("Folder", "Test1"), ((FileNotFoundException)failures[0]).FileName);
        }

        [TestMethod()]
        public async Task MovedProjectReportsTheFileMissingInTheNewFolder() {
            var serializer = new MissingFileSerializer((folder, title) =>
                folder == "NewFolder" ? title + "_Loaded.msp2.dbs"
                : folder == "Folder" ? title
                : null);
            var stream = await SaveProjectAsync(serializer, "Folder", "Test1");

            var failures = new List<Exception>();
            var pickerCalls = 0;
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadReportingFailuresAsync(streamManager, serializer, _ => null, "NewFolder",
                    _ => { pickerCalls++; return Task.FromResult<string>(null); },
                    datasetLoadFailed: (_, error) => failures.Add(error));
                Assert.AreEqual(0, actual.Storages.Count);
            }

            Assert.AreEqual(1, failures.Count);
            Assert.AreEqual(Path.Combine("NewFolder", "Test1_Loaded.msp2.dbs"), ((FileNotFoundException)failures[0]).FileName);
            Assert.AreEqual(0, pickerCalls);
        }

        [TestMethod()]
        public async Task OneArgumentOverloadReportsTheParameterOnce() {
            var serializer = new MissingFileSerializer((folder, title) => folder == "Folder" ? title : null);
            var stream = await SaveProjectAsync(serializer, "Folder", "Test1");

            // A picker that is cancelled used to report the same dataset a second time.
            var parameters = new List<ProjectBaseParameter>();
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                await ProjectDataStorage.LoadAsync(streamManager, serializer, _ => null, "Folder", _ => Task.FromResult<string>(null), parameters.Add);
            }

            Assert.AreEqual(1, parameters.Count);
            Assert.AreEqual("Test1", parameters[0].ProjectFileName);
        }

        [TestMethod()]
        public async Task OneArgumentOverloadReportsOnlyFilesNotFound() {
            var serializer = new FaultSerializer();
            var stream = await SaveProjectAsync(serializer, "Folder", "Test1");

            var parameters = new List<ProjectBaseParameter>();
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                await ProjectDataStorage.LoadAsync(streamManager, serializer, _ => null, "Folder", _ => Task.FromResult<string>(null), parameters.Add);
            }

            Assert.AreEqual(0, parameters.Count);
        }

        [TestMethod()]
        public async Task EveryOtherFailureIsReportedWithItsException() {
            var serializer = new FaultSerializer();
            var proj = new ProjectDataStorage(new ProjectParameter(DateTime.Parse("2026/10/13 12:00:00"), "Folder", "TestProject"), new List<IMsdialDataStorage<ParameterBase>>());
            proj.AddStorage(new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test1", ProjectFolderPath = "Folder", } });
            proj.AddStorage(new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test2", ProjectFolderPath = "Folder", } });
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer, null, null);
                streamManager.Complete();
            }

            var failures = new List<(ProjectBaseParameter Parameter, Exception Error)>();
            var pickerCalls = 0;
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.LoadReportingFailuresAsync(streamManager, serializer, _ => null, "Folder",
                    _ => { pickerCalls++; return Task.FromResult<string>(null); },
                    datasetLoadFailed: (parameter, error) => { lock (failures) { failures.Add((parameter, error)); } });
                Assert.AreEqual(0, actual.Storages.Count);
                Assert.AreEqual(0, actual.ProjectParameters.Count);
            }

            CollectionAssert.AreEquivalent(new[] { "Test1", "Test2", }, failures.Select(f => f.Parameter.ProjectFileName).ToArray());
            Assert.IsTrue(failures.All(f => f.Error.Message == "Serializer faulted!"));
            Assert.AreEqual(0, pickerCalls);
        }

        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2", @"Resources\Export")]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs", @"Resources\Export")]
        public async Task LoadFromFilesWithoutDbsNamesTheDbs() {
            const string title = "Dataset_2025_07_31_12_31_11.mddata";
            var folder = Path.Combine(Path.GetTempPath(), $"msdial-project-without-dbs-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try {
                foreach (var file in Directory.GetFiles("./Resources/Export", "Dataset_2025_07_31_12_31_11*")) {
                    File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
                }
                IMsdialDataStorage<ParameterBase> dataset;
                using (var streamManager = new DirectoryTreeStreamManager(folder)) {
                    dataset = await MsdialDataStorage.Serializer.LoadAsync(streamManager, title, folder, string.Empty);
                }
                dataset.FixDatasetFolder(folder);
                dataset.Parameter.ProjectFileName = title;
                var project = new ProjectDataStorage(new ProjectParameter(DateTime.Now, folder, "t.mdproject"));
                project.AddStorage(dataset);
                var stream = new MemoryStream();
                using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                    await project.Save(streamManager, new MsdialIgnoreSavingSerializer(), _ => null, null);
                    streamManager.Complete();
                }
                File.Delete(Path.Combine(folder, "Dataset_2025_07_31_12_31_11_Loaded.msp2.dbs"));

                var failures = new List<Exception>();
                var pickerCalls = 0;
                using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                    var actual = await ProjectDataStorage.LoadReportingFailuresAsync(streamManager, MsdialDataStorage.Serializer, path => new DirectoryTreeStreamManager(path), folder,
                        _ => { pickerCalls++; return Task.FromResult<string>(null); },
                        datasetLoadFailed: (_, error) => failures.Add(error));
                    Assert.AreEqual(0, actual.Storages.Count);
                }

                Assert.AreEqual(1, failures.Count);
                StringAssert.EndsWith(((FileNotFoundException)failures[0]).FileName, "_Loaded.msp2.dbs");
                Assert.AreEqual(0, pickerCalls);
            }
            finally {
                Directory.Delete(folder, recursive: true);
            }
        }

        private static async Task<MemoryStream> SaveProjectAsync(IMsdialSerializer serializer, string folder, string title) {
            var proj = new ProjectDataStorage(new ProjectParameter(DateTime.Parse("2026/10/11 12:00:00"), folder, "TestProject"), new List<IMsdialDataStorage<ParameterBase>>());
            proj.AddStorage(new MockStorage { Parameter = new ParameterBase { ProjectFileName = title, ProjectFolderPath = folder, } });
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer, null, null);
                streamManager.Complete();
            }
            return stream;
        }
    }

    class MockStorage : IMsdialDataStorage<ParameterBase> {
        public List<AnalysisFileBean> AnalysisFiles { get; set; }
        public List<AlignmentFileBean> AlignmentFiles { get; set; }
        public List<MoleculeMsReference> MspDB { get; set; }
        public List<MoleculeMsReference> TextDB { get; set; }
        public List<MoleculeMsReference> IsotopeTextDB { get; set; }
        public IupacDatabase IupacDatabase { get; set; }

        public ParameterBase Parameter { get; set; }

        public DataBaseMapper DataBaseMapper { get; set; }
        public DataBaseStorage DataBases { get; set; }

        public void FixDatasetFolder(string projectFolder) {
            Parameter.ProjectFolderPath = projectFolder;
        }

        public Task SaveAsync(IStreamManager streamManager, string projectTitle, string prefix) {
            throw new NotImplementedException();
        }

        public Task SaveParameterAsync(Stream stream) {
            throw new NotImplementedException();
        }

        public ParameterBase LoadParameter(Stream stream) {
            throw new NotImplementedException();
        }

        public AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage() {
            throw new NotImplementedException();
        }
    }

    class MockSerializer : IMsdialSerializer
    {
        private Dictionary<string, IMsdialDataStorage<ParameterBase>> storages = new Dictionary<string, IMsdialDataStorage<ParameterBase>>();
        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            var storage = storages[$"{prefix}/{projectTitle}"];
            storages.Remove($"{prefix}/{projectTitle}");
            return Task.FromResult(storage);
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            storages.Add($"{prefix}/{projectTitle}", dataStorage);
            return Task.CompletedTask;
        }
    }

    class FaultSerializer : IMsdialSerializer
    {
        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            // return Task.FromException<IMsdialDataStorage<ParameterBase>>(new Exception("Serializer fault!"));
            throw new Exception("Serializer faulted!");
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            return Task.CompletedTask;
        }
    }

    class FaultIfDifferentDirectorySerializer : IMsdialSerializer
    {
        public FaultIfDifferentDirectorySerializer(string projectDir) {
            _projectDir = projectDir;
        }

        private string _projectDir;

        public void ChangeProjectDir(string newProjectDir) {
            _projectDir = newProjectDir;
        }

        private Dictionary<string, IMsdialDataStorage<ParameterBase>> storages = new Dictionary<string, IMsdialDataStorage<ParameterBase>>();
        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            if (_projectDir != projectFolderPath) {
                throw new Exception("Serializer faulted!");
            }
            var storage = storages[$"{prefix}/{projectTitle}"];
            storages.Remove($"{prefix}/{projectTitle}");
            return Task.FromResult(storage);
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            storages.Add($"{prefix}/{projectTitle}", dataStorage);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Fails the way a dataset whose files are not all present fails: with a FileNotFoundException naming the file,
    /// given as (folder, dataset title) => the name of the missing file in that folder, or null when nothing is missing.
    /// </summary>
    class MissingFileSerializer : IMsdialSerializer
    {
        private readonly Func<string, string, string> _missingFile;
        private readonly Dictionary<string, IMsdialDataStorage<ParameterBase>> _storages = new Dictionary<string, IMsdialDataStorage<ParameterBase>>();

        public MissingFileSerializer(Func<string, string, string> missingFile) {
            _missingFile = missingFile;
        }

        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            if (_missingFile(projectFolderPath, projectTitle) is string missing) {
                var path = Path.Combine(projectFolderPath, missing);
                throw new FileNotFoundException($"Could not find file '{path}'.", path);
            }
            return Task.FromResult(_storages[$"{prefix}/{projectTitle}"]);
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            _storages[$"{prefix}/{projectTitle}"] = dataStorage;
            return Task.CompletedTask;
        }
    }
}