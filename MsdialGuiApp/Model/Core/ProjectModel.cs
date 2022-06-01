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
    public class ProjectModel : BindableBase, IProjectModel
    {
        public ProjectModel(ProjectDataStorage storage, IMessageBroker broker) {
            Storage = storage;
            _broker = broker;
            Datasets = new ObservableCollection<IDatasetModel>();
            DatasetSettingModel = new DatasetSettingModel(false, SetNewDataset, broker);
        }

        public ProjectParameter Parameter => Storage.ProjectParameter;

        public ProjectDataStorage Storage { get; }

        public ObservableCollection<IDatasetModel> Datasets { get; }

        public DatasetSettingModel DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel datasetSettingModel;

        public IDatasetModel CurrentDataset {
            get => currentDataset;
            private set => SetProperty(ref currentDataset, value);
        }
        private IDatasetModel currentDataset;

        IDatasetModel IProjectModel.CurrentDataset {
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
            if (!(CurrentDataset is null)) {
                await CurrentDataset.SaveAsync();
            }
            using (var fs = File.Open(Storage.ProjectParameter.FilePath, FileMode.Create))
            using (var streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                await Storage.Save(
                    streamManager,
                    serializer,
                    path => new DirectoryTreeStreamManager(path),
                    parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Save {parameter.ProjectFilePath} failed.")));
            }
        }

        public async Task SaveAsAsync() {
            if (!(CurrentDataset is null)) {
                await CurrentDataset.SaveAsAsync();
            }
            using (var fs = File.Open(Storage.ProjectParameter.FilePath, FileMode.Create))
            using (var streamManager = ZipStreamManager.OpenCreate(fs)) {
                var serializer = new MsdialIntegrateSerializer();
                await Storage.Save(
                    streamManager,
                    serializer,
                    path => new DirectoryTreeStreamManager(path),
                    parameter => Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Save {parameter.ProjectFilePath} failed.")));
            }
        }

        public static async Task<ProjectModel> LoadAsync(IMessageBroker broker) {
            var ofd = new OpenFileDialog
            {
                Filter = "MS project file(.mdproject)|*.mdproject", //|MTD3 file(.mtd3)|*.mtd3|All(*)|*",
                Title = "Import a project file",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true) {
                var projectDir = Path.GetDirectoryName(ofd.FileName);
                using (var fs = File.Open(ofd.FileName, FileMode.Open))
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
                        async parameter =>
                        {
                            string result = null;
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
                        MessageBox.Show("Msdial cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        throw new Exception($"Msdial cannot open the project: \n{ofd.FileName}");
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

            return null;
        }
    }
}
