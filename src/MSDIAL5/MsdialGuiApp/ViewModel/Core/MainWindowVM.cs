using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Dto;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal sealed class MainWindowVM : ViewModelBase
    {
        public MainWindowVM(IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            _broker = MessageBroker.Default;

            Model = new MainWindowModel(_broker);

            var projectViewModel = Model.ObserveProperty(m => m.CurrentProject)
                .Select(m => m is null ? null : new ProjectViewModel(m, peakSpotTableService, _broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var datasetViewModel = projectViewModel
                .SelectSwitch(project => project?.CurrentDatasetViewModel ?? Observable.Never<DatasetViewModel?>())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var methodViewModel = datasetViewModel
                .SelectSwitch(dataset => dataset?.MethodViewModel ?? Observable.Never<MethodViewModel?>())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ProjectViewModel = projectViewModel;
            DatasetViewModel = datasetViewModel;
            MethodViewModel = methodViewModel;

            var projectSaveEnableState = new ReactivePropertySlim<bool>(true);
            CreateNewProjectCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(CreateNewProject)
                .AddTo(Disposables);
            AddNewDatasetCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(AddNewDataset)
                .AddTo(Disposables);
            ExecuteAllMethodProcessCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(ExecuteAllMethodProcess)
                .AddTo(Disposables);
            ExecuteIdentificationMethodProcessCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(ExecuteIdentificationMethodProcess)
                .AddTo(Disposables);
            ExecuteAlignmentMethodProcessCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(ExecuteAlignmentMethodProcess)
                .AddTo(Disposables);
            SaveProjectCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(Model.SaveAsync)
                .AddTo(Disposables);
            SaveAsProjectCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(Model.SaveAsAsync)
                .AddTo(Disposables);
            // Opening shares the state as well. A project is read off the UI thread, and the window keeps responding meanwhile:
            // nothing may start on the project that is about to be replaced, and nothing is opened over a project being processed or saved.
            OpenProjectCommand = projectSaveEnableState.ToAsyncReactiveCommand()
                .WithSubscribe(Model.LoadAsync)
                .AddTo(Disposables);
            OpenPreviousProjectCommand = projectSaveEnableState.ToAsyncReactiveCommand<ProjectCrumb>()
                .WithSubscribe(Model.LoadProjectAsync)
                .AddTo(Disposables);
            DeletePreviousProjectCommand = new AsyncReactiveCommand<ProjectCrumb>()
                .WithSubscribe(Model.DeleteProjectAsync)
                .AddTo(Disposables);

            // The views of the open project are disabled too while another is read, since they are disposed when it opens.
            IsNotLoading = Model.NowLoading.Inverse().ToReadOnlyReactivePropertySlim(initialValue: true).AddTo(Disposables);

            _taskProgressCollection = new TaskProgressCollection();
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowSaving, "Saving...").AddTo(Disposables);
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowLoading, "Loading...").AddTo(Disposables);
            _broker.ToObservable<ITaskNotification>()
                .Subscribe(_taskProgressCollection.Update)
                .AddTo(Disposables);
        }

        private readonly IMessageBroker _broker;

        public MainWindowModel Model { get; }

        public ReadOnlyReactivePropertySlim<ProjectViewModel?> ProjectViewModel { get; }
        public ReadOnlyReactivePropertySlim<DatasetViewModel?> DatasetViewModel { get; }
        public ReadOnlyReactivePropertySlim<MethodViewModel?> MethodViewModel { get; }

        public ReadOnlyReactivePropertySlim<bool> IsNotLoading { get; }

        private readonly TaskProgressCollection _taskProgressCollection;
        public ReadOnlyObservableCollection<ProgressBarVM> TaskProgressCollection => _taskProgressCollection.ProgressBars;

        public IMsdialDataStorage<ParameterBase>? Storage {
            get => _storage;
            set {
                if (SetProperty(ref _storage, value)) {
                    OnPropertyChanged(nameof(ProjectFile));
                }
            }
        }
        private IMsdialDataStorage<ParameterBase>? _storage;

        public string ProjectFile => Storage?.Parameter is null ? string.Empty : Storage.Parameter.ProjectParam.ProjectFilePath;

        public AsyncReactiveCommand CreateNewProjectCommand { get; }

        private async Task CreateNewProject() {
            using (var vm = new ProcessSettingViewModel(Model.ProjectSetting)) {
                await RunProcess(vm);
            }
        }

        /// <param name="processed">
        /// The project the process works on, which is the one saved afterwards. Null when the process creates the project,
        /// which is then the current one.
        /// </param>
        private async Task RunProcess(ProcessSettingViewModel viewmodel, IProjectModel? processed = null) {
            _broker.Publish(viewmodel);
            if (viewmodel.DialogResult.Value) {
                await viewmodel.Model.RunProcessAsync().ConfigureAwait(false);
                if (processed is null) {
                    await Model.SaveAsync().ConfigureAwait(false);
                }
                else {
                    await Model.SaveProjectAsync(processed).ConfigureAwait(false);
                }
            }
        }

        public AsyncReactiveCommand AddNewDatasetCommand { get; }

        private async Task AddNewDataset() {
            if (Model.CurrentProject is not IProjectModel project) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.ProjectIsNotCreated));
                return;
            }
            using var vm = new ProcessSettingViewModel(project, _broker);
            await RunProcess(vm, project);
        }

        public AsyncReactiveCommand ExecuteAllMethodProcessCommand { get; }

        private async Task ExecuteAllMethodProcess() {
            if (Model.CurrentProject is not IProjectModel project || project.CurrentDataset is not IDatasetModel dataset) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.DatasetIsNotSelected));
                return;
            }
            // Said before the settings dialog: processing reads the raw data of every sample.
            if (!RawDataGuard.EnsureAll(dataset.AnalysisFiles.AnalysisFiles, _broker, "All processing")) {
                return;
            }
            using var vm = new ProcessSettingViewModel(project, dataset, dataset.AllProcessMethodSettingModel, _broker);
            vm.MoveToDataCollectionSetting();
            await RunProcess(vm, project);
        }

        public AsyncReactiveCommand ExecuteIdentificationMethodProcessCommand { get; }

        private async Task ExecuteIdentificationMethodProcess() {
            if (Model.CurrentProject is not IProjectModel project || project.CurrentDataset is not IDatasetModel dataset) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.DatasetIsNotSelected));
                return;
            }
            if (!RawDataGuard.EnsureAll(dataset.AnalysisFiles.AnalysisFiles, _broker, "Identification")) {
                return;
            }
            using var vm = new ProcessSettingViewModel(project, dataset, dataset.IdentificationProcessMethodSettingModel, _broker);
            vm.MoveToIdentificationSetting();
            await RunProcess(vm, project);
        }

        public AsyncReactiveCommand ExecuteAlignmentMethodProcessCommand { get; }

        private async Task ExecuteAlignmentMethodProcess() {
            if (Model.CurrentProject is not IProjectModel project || project.CurrentDataset is not IDatasetModel dataset) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.DatasetIsNotSelected));
                return;
            }
            if (!RawDataGuard.EnsureAll(dataset.AnalysisFiles.AnalysisFiles, _broker, "Alignment")) {
                return;
            }
            using var vm = new ProcessSettingViewModel(project, dataset, dataset.AlignmentProcessMethodSettingModel, _broker);
            vm.MoveToAlignmentSetting();
            await RunProcess(vm, project);
        }

        public AsyncReactiveCommand OpenProjectCommand { get; }
        public AsyncReactiveCommand<ProjectCrumb> OpenPreviousProjectCommand { get; }
        public AsyncReactiveCommand<ProjectCrumb> DeletePreviousProjectCommand { get; }
        public ReadOnlyObservableCollection<ProjectCrumb> PreviousProjects => Model.PreviousProjects;

        public AsyncReactiveCommand SaveProjectCommand { get; }
        public AsyncReactiveCommand SaveAsProjectCommand { get; }

        public DelegateCommand GoToTutorialCommand => _goToTutorialCommand ??= new DelegateCommand(GoToTutorial);
        private DelegateCommand? _goToTutorialCommand;

        private void GoToTutorial() {
            System.Diagnostics.Process.Start(Resources.TUTORIAL_URI);
        }

        public DelegateCommand GoToLicenceCommand => _goToLicenceCommand ??= new DelegateCommand(GoToLicence);
        private DelegateCommand? _goToLicenceCommand;

        private void GoToLicence() {
            System.Diagnostics.Process.Start("https://systemsomicslab.github.io/compms/license/main.html");
        }

        public DelegateCommand ShowAboutCommand => _showAboutCommand ??= new DelegateCommand(ShowAbout);
        private DelegateCommand? _showAboutCommand;

        private void ShowAbout() {
            var view = new View.Help.HelpAboutDialog();
            view.ShowDialog();
        }
    }
}
