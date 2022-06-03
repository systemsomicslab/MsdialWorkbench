using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using Microsoft.Win32;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    internal sealed class DatasetModel : DisposableModelBase, IDatasetModel
    {
        private readonly IMessageBroker _broker;
        private readonly ProjectBaseParameterModel _projectBaseParameter;

        public DatasetModel(IMsdialDataStorage<ParameterBase> storage, IMessageBroker broker) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _broker = broker;
            _projectBaseParameter = new ProjectBaseParameterModel(Storage.Parameter.ProjectParam);
            observeParameterChanged = new BehaviorSubject<Unit>(Unit.Default).AddTo(Disposables);
            AnalysisFilePropertySetModel = new AnalysisFilePropertySetModel(Storage.AnalysisFiles, _projectBaseParameter, observeParameterChanged);

            AllProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, broker);
            IdentificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, broker);
            AlignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, broker);
        }

        public IMethodModel Method {
            get => method;
            private set {
                var prev = method;
                if (SetProperty(ref method, value)) {
                    prev?.Dispose();
                }
            }
        }
        private IMethodModel method;

        public IMsdialDataStorage<ParameterBase> Storage { get; }

        public IObservable<Unit> ObserveParameterChanged => observeParameterChanged;
        private readonly BehaviorSubject<Unit> observeParameterChanged;
        public MethodSettingModel AllProcessMethodSettingModel {
            get => allProcessMethodSettingModel;
            private set => SetProperty(ref allProcessMethodSettingModel, value);
        }
        private MethodSettingModel allProcessMethodSettingModel;

        public MethodSettingModel IdentificationProcessMethodSettingModel {
            get => identificationProcessMethodSettingModel;
            private set => SetProperty(ref identificationProcessMethodSettingModel, value);
        }
        private MethodSettingModel identificationProcessMethodSettingModel;

        public MethodSettingModel AlignmentProcessMethodSettingModel {
            get => alignmentProcessMethodSettingModel;
            private set => SetProperty(ref alignmentProcessMethodSettingModel, value);
        }
        private MethodSettingModel alignmentProcessMethodSettingModel;

        private Task HandlerAsync(MethodSettingModel setting, IMethodModel model, CancellationToken token) {
            Method = model;
            AllProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, _broker);
            IdentificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, _broker);
            AlignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, Storage, HandlerAsync, _projectBaseParameter, ObserveParameterChanged, _broker);
            return Method.RunAsync(setting.Option, token);
        }

        public AnalysisFilePropertySetModel AnalysisFilePropertySetModel { get; }

        public void AnalysisFilePropertyUpdate() {
            AnalysisFilePropertySetModel.Update();
        }

        public Task SaveAsync() {
            // TODO: implement process when project save failed.
            var streamManager = new DirectoryTreeStreamManager(Storage.Parameter.ProjectFolderPath);
            return Task.WhenAll(new[]
            {
                Storage?.SaveAsync(streamManager, Storage.Parameter.ProjectFileName, string.Empty) ?? Task.CompletedTask,
                Method?.SaveAsync() ?? Task.CompletedTask,
            });
        }

        public async Task SaveAsAsync() {
            // TODO: Move these dialogs to the view.
            var sfd = new SaveFileDialog
            {
                Filter = "Dataset file(*.mddata)|*.mddata",
                Title = "Save project dialog",
                InitialDirectory = Storage.Parameter.ProjectFolderPath,
            };

            if (sfd.ShowDialog() == true) {
                if (Path.GetDirectoryName(sfd.FileName) != Storage.Parameter.ProjectFolderPath) {
                    MessageBox.Show("Save folder should be the same folder as analysis files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var message = new ShortMessageWindow()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the project as...",
                };

                message.Show();
                Storage.Parameter.ProjectFileName = Path.GetFileName(sfd.FileName);
                Storage.FixDatasetFolder(Path.GetDirectoryName(sfd.FileName));
                await SaveAsync(); // Shouldn't use ConfigureAwait(true) 
                message.Close();
            }
        }

        public async Task LoadAsync() {
            var factory = new MethodSettingModelFactory(Storage, _projectBaseParameter, ObserveParameterChanged, ProcessOption.All, _broker);
            Method = await Task.Run(() =>
            {
                var method = factory.BuildMethod();
                method.LoadAnalysisFileAsync(Storage.AnalysisFiles.FirstOrDefault(), default);
                return method;
            });
        }

        public static async Task<DatasetModel> LoadAsync(string datasetFile, IMessageBroker broker) {
            // TODO: Move these dialogs to the view.
            var message = new ShortMessageWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Loading project...",
            };
            message.Show();

            var storage = await LoadProjectFromPathAsync(datasetFile);
            if (storage == null) {
                MessageBox.Show("Msdial cannot open the project: \n" + datasetFile, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            var result = new DatasetModel(storage, broker);
            var factory = new MethodSettingModelFactory(storage, result._projectBaseParameter, result.ObserveParameterChanged, ProcessOption.All, broker);
            result.Method = factory.BuildMethod();
            message.Close();

            return result;
        }

        private static async Task<IMsdialDataStorage<ParameterBase>> LoadProjectFromPathAsync(string projectfile) {
            var projectFolder = Path.GetDirectoryName(projectfile);
            var projectFileName = Path.GetFileName(projectfile);
            var serializer = new MsdialIntegrateSerializer();
            var streamManager = new DirectoryTreeStreamManager(projectFolder);
            var storage = await serializer.LoadAsync(streamManager, projectFileName, projectFolder, string.Empty);
            storage.FixDatasetFolder(projectFolder);
            return storage;
        }
    }
}
