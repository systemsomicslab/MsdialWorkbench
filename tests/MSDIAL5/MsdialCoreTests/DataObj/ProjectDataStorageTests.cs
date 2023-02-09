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
}