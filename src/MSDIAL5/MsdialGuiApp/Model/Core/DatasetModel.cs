using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    internal sealed class DatasetModel : DisposableModelBase, IDatasetModel
    {
        private readonly AlignmentFileBeanModelCollection _alignmentFileBeanModelCollection;
        private readonly AnalysisFileBeanModelCollection _analysisFileBeanModelCollection;
        private readonly IMessageBroker _broker;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly SerialDisposable _methodDisposable;

        public DatasetModel(IMsdialDataStorage<ParameterBase> storage, IMessageBroker broker) : this(storage, new AnalysisFileBeanModelCollection(storage.AnalysisFiles), broker) {

        }

        public DatasetModel(IMsdialDataStorage<ParameterBase> storage, AnalysisFileBeanModelCollection files, IMessageBroker broker) {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _broker = broker;
            _projectBaseParameter = new FilePropertiesModel(storage.Parameter.ProjectParam).AddTo(Disposables);
            StudyContext = new StudyContextModel(storage.Parameter.ProjectParam);
            _analysisFileBeanModelCollection = files.AddTo(Disposables);
            _alignmentFileBeanModelCollection = new AlignmentFileBeanModelCollection(storage.AlignmentFiles, storage.AnalysisFiles).AddTo(Disposables);
            AnalysisFilePropertyResetModel = new AnalysisFilePropertyResetModel(files, _projectBaseParameter);
            FileClassSetModel = new FileClassSetModel(_projectBaseParameter);

            allProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, files, _alignmentFileBeanModelCollection, storage, HandlerAsync, _projectBaseParameter, StudyContext, broker);
            identificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, files, _alignmentFileBeanModelCollection, storage, HandlerAsync, _projectBaseParameter, StudyContext, broker);
            alignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, files, _alignmentFileBeanModelCollection, storage, HandlerAsync, _projectBaseParameter, StudyContext, broker);
            _methodDisposable = new SerialDisposable().AddTo(Disposables);
        }

        public IMethodModel? Method {
            get => method;
            private set {
                if (SetProperty(ref method, value)) {
                    _methodDisposable.Disposable = method;
                }
            }
        }
        private IMethodModel? method;

        public IMsdialDataStorage<ParameterBase> Storage { get; }

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
            AllProcessMethodSettingModel = new MethodSettingModel(ProcessOption.All, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, Storage, HandlerAsync, _projectBaseParameter, StudyContext, _broker);
            IdentificationProcessMethodSettingModel = new MethodSettingModel(ProcessOption.IdentificationPlusAlignment, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, Storage, HandlerAsync, _projectBaseParameter, StudyContext, _broker);
            AlignmentProcessMethodSettingModel = new MethodSettingModel(ProcessOption.Alignment, _analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, Storage, HandlerAsync, _projectBaseParameter, StudyContext, _broker);
            _analysisFileBeanModelCollection.ReleaseMSDecLoaders();
            return Method.RunAsync(setting.Option, token);
        }

        public AnalysisFilePropertyResetModel AnalysisFilePropertyResetModel { get; }
        public FileClassSetModel FileClassSetModel { get; }
        public StudyContextModel StudyContext { get; }

        public async Task SaveAsync() {
            // TODO: implement process when project save failed.
            Storage.Parameter.ProjectParam.MsdialVersionNumber = Properties.Resources.VERSION;
            Storage.Parameter.ProjectParam.FinalSavedDate = DateTime.Now;
            using (var streamManager = new DirectoryTreeStreamManager(Storage.Parameter.ProjectFolderPath)) {
                await Task.WhenAll(new[]
                {
                    Storage?.SaveAsync(streamManager, Storage.Parameter.ProjectFileName, string.Empty) ?? Task.CompletedTask,
                    Method?.SaveAsync() ?? Task.CompletedTask,
                }).ConfigureAwait(false);
                ((IStreamManager)streamManager).Complete();
            }
        }

        public async Task SaveAsAsync() {
            string fileName = string.Empty;
            var request = new SaveFileNameRequest(file => fileName = file)
            {
                Filter = "Dataset file(*.mddata)|*.mddata",
                Title = "Save project dialog",
            };
            _broker.Publish(request);
            // TODO: Move these dialogs to the view.

            if (request.Result != true) {
                return;
            }
            if (Path.GetDirectoryName(fileName) != Storage.Parameter.ProjectFolderPath) {
                MessageBox.Show("Save folder should be the same folder as analysis files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = new ShortMessageWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Saving the project as...",
            };
            message.Show();
            Storage.Parameter.ProjectFileName = Path.GetFileName(fileName);
            Storage.FixDatasetFolder(Path.GetDirectoryName(fileName));
            await SaveAsync(); // Shouldn't use ConfigureAwait(true) 
            message.Close();
        }

        public async Task LoadAsync() {
            var factory = new MethodSettingModelFactory(_analysisFileBeanModelCollection, _alignmentFileBeanModelCollection, Storage, _projectBaseParameter, StudyContext, ProcessOption.All, _broker);
            var method = factory.BuildMethod();
            await method.LoadAsync(default).ConfigureAwait(false);
            Method = method;
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
            if (storage is null) {
                MessageBox.Show("Msdial cannot open the project: \n" + datasetFile, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception($"Msdial cannot open the project: {datasetFile}");
            }
            var result = new DatasetModel(storage, broker);
            var factory = new MethodSettingModelFactory(result._analysisFileBeanModelCollection, result._alignmentFileBeanModelCollection, storage, result._projectBaseParameter, result.StudyContext, ProcessOption.All, broker);
            result.Method = factory.BuildMethod();
            message.Close();

            return result;
        }

        public async Task SaveParameterAsAsync() {
            await Task.Yield();
            var saveFileRequest = new SaveFileNameRequest(file =>
            {
                var shortMessageRequest = new ProcessMessageRequest("Saving the parameter as...",
                    async () =>
                    {
                        using (var stream = File.Open(file, FileMode.Create)) {
                            await Storage.SaveParameterAsync(stream).ConfigureAwait(false);
                        }
                    });

                _broker.Publish(shortMessageRequest);
            })
            {
                Filter = "Msdial parameter file(*.mdparameter)|*.mdparameter",
                Title = "Save parameter dialog",
            };
            _broker.Publish(saveFileRequest);
        }

        private static async Task<IMsdialDataStorage<ParameterBase>> LoadProjectFromPathAsync(string projectfile) {
            var projectFolder = Path.GetDirectoryName(projectfile);
            var projectFileName = Path.GetFileName(projectfile);
            var serializer = new MsdialIntegrateSerializer();
            using (IStreamManager streamManager = new DirectoryTreeStreamManager(projectFolder)) {
                var storage = await serializer.LoadAsync(streamManager, projectFileName, projectFolder, string.Empty);
                streamManager.Complete();
                storage.FixDatasetFolder(projectFolder);
               
                return storage;
            }
        }

        

        AnalysisFileBeanModelCollection IDatasetModel.AnalysisFiles => _analysisFileBeanModelCollection;
    }
}
