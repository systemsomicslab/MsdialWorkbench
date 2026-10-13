using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.Model.Core
{
    internal sealed class ProjectModel : BindableBase, IProjectModel
    {
        public ProjectModel(ProjectDataStorage storage, IMessageBroker broker) {
            Storage = storage;
            _broker = broker;
            Datasets = new ObservableCollection<IDatasetModel>();
            datasetSettingModel = new DatasetSettingModel(false, SetNewDataset, broker);
        }

        public ProjectParameter Parameter => Storage.ProjectParameter;

        public ProjectDataStorage Storage { get; }

        public ObservableCollection<IDatasetModel> Datasets { get; }

        public DatasetSettingModel DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel datasetSettingModel;

        public IDatasetModel? CurrentDataset {
            get => currentDataset;
            private set => SetProperty(ref currentDataset, value);
        }
        private IDatasetModel? currentDataset;

        IDatasetModel? IProjectModel.CurrentDataset {
            get => CurrentDataset;
            set => CurrentDataset = value;
        }

        private readonly object lockDatasetAdd = new object();
        private readonly IMessageBroker _broker;

        public void SetNewDataset(IDatasetModel dataset) {
            lock (lockDatasetAdd) {
                Datasets.Add(dataset);
                Storage.AddStorage(dataset.Storage);
            }
            CurrentDataset = dataset;
            DatasetSettingModel = new DatasetSettingModel(false, SetNewDataset, _broker);
        }

        public async Task SaveAsync() {
            if (CurrentDataset is not null) {
                await CurrentDataset.SaveAsync();
            }
            using var fs = new TemporaryFileStream(Storage.ProjectParameter.FilePath);
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                await Storage.Save(
                    streamManager,
                    serializer,
                    path => new DirectoryTreeStreamManager(path),
                    parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Save {parameter.ProjectFilePath} failed.")));
                streamManager.Complete();
            }
            fs.Move();
        }

        public async Task SaveAsAsync() {
            if (!(CurrentDataset is null)) {
                await CurrentDataset.SaveAsAsync();
            }
            using var fs = new TemporaryFileStream(Storage.ProjectParameter.FilePath);
            using (IStreamManager streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                await Storage.Save(
                    streamManager,
                    serializer,
                    path => new DirectoryTreeStreamManager(path),
                    parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Save {parameter.ProjectFilePath} failed.")));
                streamManager.Complete();

            }
            fs.Move();
        }

        public static async Task<ProjectModel> LoadAsync(string projectPath, IMessageBroker broker) {
            Mouse.OverrideCursor = Cursors.Wait;
            var message = new ShortMessageWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Loading project...",
            };
            message.Show();

            try {
                // Both routes read the datasets off the UI thread and come back to it, through these awaits, to build the models.
                if (projectPath.EndsWith(".mddata")) {
                    return await LoadMddatasetAsync(projectPath, broker);
                }
                return await LoadMdprojectAsync(projectPath, broker);
            }
            finally {
                // Also when opening is declined or fails: neither the window nor the wait cursor may outlive the attempt.
                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        private static async Task<ProjectModel> LoadMdprojectAsync(string projectPath, IMessageBroker broker) {
            var projectDir = Path.GetDirectoryName(projectPath);
            // Opening only reads the project file (ZipStreamManager.OpenGet reads the archive), so a project on read-only media
            // or open in another MS-DIAL can be opened too.
            using (var fs = File.Open(projectPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamManager = ZipStreamManager.OpenGet(fs)) {
                var deserializer = new MsdialIntegrateSerializer();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Reading the datasets is data work only, and a loaded library can be hundreds of MB: it runs off the UI thread,
                // which keeps pumping meanwhile (a minute without pumping is a ContextSwitchDeadlock under the debugger).
                // The picker and the messages go to the dispatcher themselves, and the models are built after the await,
                // back on the UI thread.
                var projectDataStorage = await Task.Run(() => ReadDatasetsAsync(
                    streamManager,
                    deserializer,
                    path => new DirectoryTreeStreamManager(path),
                    projectDir,
                    LocateDatasetFileAsync,
                    broker));
                System.Diagnostics.Debug.WriteLine($"Read the datasets of {projectPath} in {stopwatch.Elapsed.TotalSeconds:F1} s.");
                if (projectDataStorage == null) {
                    MessageBox.Show("Msdial cannot open the project: \n" + projectPath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception($"Msdial cannot open the project: \n{projectPath}");
                }
                projectDataStorage.FixProjectFolder(projectDir);
                var model = new ProjectModel(projectDataStorage, broker);
                model.Datasets.Clear();
                foreach (var dataset in projectDataStorage.Storages.Select(storage => new DatasetModel(storage, broker))){
                    model.Datasets.Add(dataset);
                }
                model.CurrentDataset = model.Datasets.LastOrDefault();
                if (!(model.CurrentDataset is null)) {
                    await model.CurrentDataset.LoadAsync();
                }

                return model;
            }
        }

        /// <summary>
        /// Reads the datasets of a project, and says for each dataset that cannot be loaded why it failed.
        /// A project none of whose datasets could be loaded is not opened (<see cref="OperationCanceledException"/>):
        /// a dataset that fails is dropped from the project, so the project would be saved back without any.
        /// </summary>
        /// <param name="locateDatasetFile">
        /// Asked for a dataset a file of which was not found, with the path of that file, which the message has just named:
        /// the dataset file itself, or a file beside it that a copy of the dataset picked elsewhere has to have.
        /// </param>
        internal static async Task<ProjectDataStorage> ReadDatasetsAsync(
            IStreamManager streamManager,
            IMsdialSerializer deserializer,
            Func<string, IStreamManager> datasetStreamManagerFactory,
            string projectDir,
            Func<ProjectBaseParameter, string?, Task<string?>> locateDatasetFile,
            IMessageBroker broker) {

            var unloadedDatasets = 0;
            var missingFiles = new ConcurrentDictionary<ProjectBaseParameter, string>();
            var projectDataStorage = await ProjectDataStorage.LoadReportingFailuresAsync(
                streamManager,
                deserializer,
                datasetStreamManagerFactory,
                projectDir,
                // The picker follows the message about the same dataset.
                parameter => locateDatasetFile(parameter, missingFiles.TryGetValue(parameter, out var missing) ? missing : null),
                (parameter, error) =>
                {
                    if (error is FileNotFoundException notFound && notFound.FileName is not null) {
                        missingFiles[parameter] = notFound.FileName;
                    }
                    Interlocked.Increment(ref unloadedDatasets);
                    broker.Publish(new ErrorMessageBoxRequest
                    {
                        Caption = "Dataset not loaded",
                        Content = MessageHelper.DatasetNotLoaded(parameter.ProjectFileName, error),
                        Icon = MessageBoxImage.Error,
                    });
                }).ConfigureAwait(false);
            if (projectDataStorage is not null && projectDataStorage.Storages.Count == 0 && unloadedDatasets > 0) {
                // What stopped each of them has been said.
                throw new OperationCanceledException("No dataset of the project could be loaded.");
            }
            return projectDataStorage!;
        }

        private static async Task<string?> LocateDatasetFileAsync(ProjectBaseParameter parameter, string? missingPath) {
            string? result = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var newofd = new OpenFileDialog
                {
                    Filter = "Dataset file(.mddata)|*.mddata|All(*)|*",
                    Title = MessageHelper.LocateDatasetFileTitle(parameter.ProjectFileName, missingPath),
                    RestoreDirectory = true
                };
                if (newofd.ShowDialog() == true) {
                    result = newofd.FileName;
                }
            });
            return result;
        }

        private static async Task<ProjectModel> LoadMddatasetAsync(string  mddata, IMessageBroker broker) {
            var folder = Path.GetDirectoryName(mddata);
            var title = Path.GetFileName(mddata);
            var storage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, folder, title + ".mdproject"));
            var deserializer = new MsdialIntegrateSerializer();

            using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                // As for a project file: the dataset and its loaded library are read off the UI thread, and the await comes back to it.
                var data = await Task.Run(() => deserializer.LoadAsync(manager, title, folder, string.Empty));
                manager.Complete();
                storage.AddStorage(data);
            }

            using (var fs = new TemporaryFileStream(storage.ProjectParameter.FilePath)) {
                using (IStreamManager streamManager = ZipStreamManager.OpenCreate(fs)) {
                    var serializer = new MsdialIgnoreSavingSerializer();
                    await storage.Save(
                        streamManager,
                        serializer,
                        path => null,
                        parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Save {parameter.ProjectFilePath} failed.")));
                    streamManager.Complete();
                }
                fs.Move();
            }

            var model = new ProjectModel(storage, broker);
            model.Datasets.Clear();
            foreach (var dataset in storage.Storages.Select(data => new DatasetModel(data, broker))){
                model.Datasets.Add(dataset);
            }
            model.CurrentDataset = model.Datasets.LastOrDefault();
            if (!(model.CurrentDataset is null)) {
                await model.CurrentDataset.LoadAsync();
            }
            return model;
        }
    }
}
