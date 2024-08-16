using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            if (projectPath.EndsWith(".mddata")) {
                return await LoadMddatasetAsync(projectPath, broker).ConfigureAwait(false);
            }

            var projectDir = Path.GetDirectoryName(projectPath);
            using (var fs = File.Open(projectPath, FileMode.Open))
            using (var streamManager = ZipStreamManager.OpenGet(fs)) {
                var deserializer = new MsdialIntegrateSerializer();

                Mouse.OverrideCursor = Cursors.Wait;
                var message = new ShortMessageWindow()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Loading project...",
                };
                message.Show();

                var projectDataStorage = await ProjectDataStorage.LoadAsync(
                    streamManager,
                    deserializer,
                    path => new DirectoryTreeStreamManager(path),
                    projectDir,
                    async parameter =>
                    {
                        string? result = null;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var newofd = new OpenFileDialog
                            {
                                Filter = "Dataset file(.mddata)|*.mddata|All(*)|*",
                                Title = "Import a project file",
                                RestoreDirectory = true
                            };
                            if (newofd.ShowDialog() == true) {
                                result = newofd.FileName;
                            }
                        });
                        return result;
                    },
                    parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"{parameter.ProjectFilePath} is not found.")));
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

                message.Close();
                Mouse.OverrideCursor = null;

                return model;
            }
        }

        private static async Task<ProjectModel> LoadMddatasetAsync(string  mddata, IMessageBroker broker) {
            var folder = Path.GetDirectoryName(mddata);
            var title = Path.GetFileNameWithoutExtension(mddata);
            var storage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, folder, title + ".mdproject"));
            var deserializer = new MsdialIntegrateSerializer();

            using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                var data = await deserializer.LoadAsync(manager, title, folder, string.Empty).ConfigureAwait(false);
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
