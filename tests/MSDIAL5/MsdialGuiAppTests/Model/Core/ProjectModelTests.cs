using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core.Tests
{
    [TestClass()]
    public class ProjectModelTests
    {
        private MessageBroker _broker = new MessageBroker();
        private List<ErrorMessageBoxRequest> _messages = new List<ErrorMessageBoxRequest>();

        [TestInitialize()]
        public void Setup() {
            _broker = new MessageBroker();
            _messages = new List<ErrorMessageBoxRequest>();
            _broker.Subscribe<ErrorMessageBoxRequest>(request => { lock (_messages) { _messages.Add(request); } });
        }

        [TestMethod()]
        public async Task AProjectNoneOfWhoseDatasetsLoadsIsNotOpened() {
            var serializer = new FakeSerializer(title => new InvalidDataException($"{title} is corrupt"));
            var stream = await SaveProjectAsync(serializer, "Dataset1.mddata");

            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => ReadDatasetsAsync(streamManager, serializer));
            }

            Assert.AreEqual("Dataset not loaded", _messages.Single().Caption);
            StringAssert.Contains(_messages[0].Content, "\"Dataset1.mddata\"");
            StringAssert.Contains(_messages[0].Content, "InvalidDataException: Dataset1.mddata is corrupt");
        }

        [TestMethod()]
        public async Task AMissingLoadedLibraryIsNamedAndTheProjectIsNotOpened() {
            var missing = Path.Combine("Folder", "Dataset1_Loaded.msp2.dbs");
            var serializer = new FakeSerializer(title => new FileNotFoundException($"Could not find file '{missing}'.", missing));
            var stream = await SaveProjectAsync(serializer, "Dataset1.mddata");

            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => ReadDatasetsAsync(streamManager, serializer));
            }

            Assert.AreEqual(MessageHelper.DatasetFileNotFound("Dataset1.mddata", missing), _messages.Single().Content);
        }

        [TestMethod()]
        public async Task ThePickerIsToldWhichFileTheMessageNamed() {
            var missing = Path.Combine("Folder", "Dataset1_Loaded.msp2.dbs");
            var serializer = new FakeSerializer(title => new FileNotFoundException($"Could not find file '{missing}'.", missing));
            var stream = await SaveProjectAsync(serializer, "Dataset1.mddata");

            var asked = new List<(string Dataset, string? Missing, int MessagesBefore)>();
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => ProjectModel.ReadDatasetsAsync(streamManager, serializer, _ => null!, "Folder",
                    (parameter, missingPath) => {
                        asked.Add((parameter.ProjectFileName, missingPath, _messages.Count));
                        return Task.FromResult<string?>(null);
                    },
                    _broker));
            }

            Assert.AreEqual(("Dataset1.mddata", missing, 1), asked.Single());
        }

        [TestMethod()]
        public void ThePickerAsksForACompleteCopyWhenAFileBesideTheDatasetIsMissing() {
            Assert.AreEqual("Locate the dataset file Dataset1.mddata", MessageHelper.LocateDatasetFileTitle("Dataset1.mddata", Path.Combine("Folder", "Dataset1.mddata")));
            Assert.AreEqual("Locate the dataset file Dataset1.mddata", MessageHelper.LocateDatasetFileTitle("Dataset1.mddata", null));
            Assert.AreEqual(
                "Locate a copy of the dataset file Dataset1.mddata that has Dataset1_Loaded.msp2.dbs beside it",
                MessageHelper.LocateDatasetFileTitle("Dataset1.mddata", Path.Combine("Folder", "Dataset1_Loaded.msp2.dbs")));
        }

        [TestMethod()]
        public async Task AProjectOpensWithTheDatasetsThatLoad() {
            var serializer = new FakeSerializer(title => title == "Dataset1.mddata" ? new InvalidDataException($"{title} is corrupt") : null);
            var stream = await SaveProjectAsync(serializer, "Dataset1.mddata", "Dataset2.mddata");

            ProjectDataStorage actual;
            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                actual = await ReadDatasetsAsync(streamManager, serializer);
            }

            CollectionAssert.AreEqual(new[] { "Dataset2.mddata", }, actual.Storages.Select(storage => storage.Parameter.ProjectFileName).ToArray());
            StringAssert.Contains(_messages.Single().Content, "\"Dataset1.mddata\"");
        }

        // A .mddata opened directly gets a project file beside it, but only once its dataset has opened.
        [TestMethod()]
        public async Task ADatasetWhoseOpeningIsDeclinedWritesNoProjectFile() {
            var dataset = new FakeDatasetModel(() => throw new OperationCanceledException("Opening was declined because raw data files are missing."));
            var model = ProjectFor(dataset);
            var writes = 0;

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => ProjectModel.OpenThenWriteProjectFileAsync(model, _ => { writes++; return Task.CompletedTask; }));

            Assert.AreEqual(0, writes);
        }

        [TestMethod()]
        public async Task TheProjectFileIsWrittenOnceTheDatasetHasOpened() {
            var steps = new List<string>();
            var dataset = new FakeDatasetModel(() => { steps.Add("open"); return Task.CompletedTask; });
            var model = ProjectFor(dataset);

            var actual = await ProjectModel.OpenThenWriteProjectFileAsync(model, storage => { steps.Add("write " + storage.ProjectParameter.Title); return Task.CompletedTask; });

            CollectionAssert.AreEqual(new[] { "open", "write Dataset1.mddata.mdproject", }, steps);
            Assert.AreSame(dataset, actual.CurrentDataset);
            Assert.IsFalse(dataset.IsDisposed);
        }

        [TestMethod()]
        public async Task AProjectFileThatCannotBeWrittenClosesTheDatasetAgain() {
            var dataset = new FakeDatasetModel(() => Task.CompletedTask);
            var model = ProjectFor(dataset);

            await Assert.ThrowsExactlyAsync<IOException>(() => ProjectModel.OpenThenWriteProjectFileAsync(model, _ => throw new IOException("read-only")));

            Assert.IsTrue(dataset.IsDisposed);
        }

        private ProjectModel ProjectFor(FakeDatasetModel dataset) {
            var model = new ProjectModel(new ProjectDataStorage(new ProjectParameter(DateTime.Parse("2026/10/13 12:00:00"), "Folder", "Dataset1.mddata.mdproject")), _broker);
            model.Datasets.Add(dataset);
            return model;
        }

        private Task<ProjectDataStorage> ReadDatasetsAsync(IStreamManager streamManager, IMsdialSerializer serializer) {
            return ProjectModel.ReadDatasetsAsync(streamManager, serializer, _ => null!, "Folder", (_, _) => Task.FromResult<string?>(null), _broker);
        }

        private static async Task<MemoryStream> SaveProjectAsync(IMsdialSerializer serializer, params string[] titles) {
            var project = new ProjectDataStorage(new ProjectParameter(DateTime.Parse("2026/10/13 12:00:00"), "Folder", "Project.mdproject"));
            foreach (var title in titles) {
                project.AddStorage(new MsdialLcmsDataStorage { MsdialLcmsParameter = new MsdialLcmsParameter { ProjectFileName = title, ProjectFolderPath = "Folder", }, });
            }
            var stream = new MemoryStream();
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(stream)) {
                await project.Save(streamManager, serializer, null, null);
                streamManager.Complete();
            }
            return stream;
        }

        /// <summary>
        /// Keeps the datasets it saves, and fails to load one with the exception given for its title (none for null).
        /// </summary>
        sealed class FakeSerializer : IMsdialSerializer
        {
            private readonly Func<string, Exception?> _failure;
            private readonly Dictionary<string, IMsdialDataStorage<ParameterBase>> _storages = new Dictionary<string, IMsdialDataStorage<ParameterBase>>();

            public FakeSerializer(Func<string, Exception?> failure) {
                _failure = failure;
            }

            public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
                if (_failure(projectTitle) is Exception error) {
                    throw error;
                }
                return Task.FromResult(_storages[projectTitle]);
            }

            public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
                _storages[projectTitle] = dataStorage;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// A dataset whose opening does what it is given, as DatasetModel.LoadAsync asks about missing raw data and opens the first sample.
        /// </summary>
        sealed class FakeDatasetModel : BindableBase, IDatasetModel, IDisposable
        {
            private readonly Func<Task> _load;

            public FakeDatasetModel(Func<Task> load) {
                _load = load;
            }

            public bool IsDisposed { get; private set; }

            public Task LoadAsync() => _load();
            public void Dispose() => IsDisposed = true;

            public IMethodModel? Method => null;
            public IMsdialDataStorage<ParameterBase> Storage => throw new NotImplementedException();
            public AnalysisFileBeanModelCollection AnalysisFiles => throw new NotImplementedException();
            public MethodSettingModel AllProcessMethodSettingModel => throw new NotImplementedException();
            public MethodSettingModel IdentificationProcessMethodSettingModel => throw new NotImplementedException();
            public MethodSettingModel AlignmentProcessMethodSettingModel => throw new NotImplementedException();
            public AnalysisFilePropertyResetModel AnalysisFilePropertyResetModel => throw new NotImplementedException();
            public FileClassSetModel FileClassSetModel => throw new NotImplementedException();
            public Task SaveAsync() => throw new NotImplementedException();
            public Task SaveAsAsync() => throw new NotImplementedException();
            public Task SaveParameterAsAsync() => throw new NotImplementedException();
        }
    }
}
