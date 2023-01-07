using CompMs.App.Msdial.Dto;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Search;
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
        public MainWindowVM(
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<AnalysisFilePropertyResetViewModel> analysisFilePropertyResetService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<ProcessSettingViewModel> processSettingSerivce) {

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (analysisFilePropertyResetService is null) {
                throw new ArgumentNullException(nameof(analysisFilePropertyResetService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            _broker = MessageBroker.Default;

            _processSettingService = processSettingSerivce ?? throw new ArgumentNullException(nameof(processSettingSerivce));
            Model = new MainWindowModel(_broker);

            var projectViewModel = Model.ObserveProperty(m => m.CurrentProject)
                .Select(m => m is null ? null : new ProjectViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, analysisFilePropertyResetService, _broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var datasetViewModel = projectViewModel
                .SelectSwitch(project => project?.CurrentDatasetViewModel ?? Observable.Never<DatasetViewModel>())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var methodViewModel = datasetViewModel
                .SelectSwitch(dataset => dataset?.MethodViewModel ?? Observable.Never<MethodViewModel>())
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
            OpenProjectCommand = new AsyncReactiveCommand()
                .WithSubscribe(Model.LoadAsync)
                .AddTo(Disposables);
            OpenPreviousProjectCommand = new AsyncReactiveCommand<ProjectCrumb>()
                .WithSubscribe(Model.LoadProjectAsync)
                .AddTo(Disposables);

            _taskProgressCollection = new TaskProgressCollection();
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowSaving, "Saving...").AddTo(Disposables);
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowLoading, "Loading...").AddTo(Disposables);
            _broker.ToObservable<ITaskNotification>()
                .Subscribe(_taskProgressCollection.Update)
                .AddTo(Disposables);
        }

        private readonly IMessageBroker _broker;
        private readonly IWindowService<ProcessSettingViewModel> _processSettingService;

        public MainWindowModel Model { get; }

        public ReadOnlyReactivePropertySlim<ProjectViewModel> ProjectViewModel { get; }
        public ReadOnlyReactivePropertySlim<DatasetViewModel> DatasetViewModel { get; }
        public ReadOnlyReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        private readonly TaskProgressCollection _taskProgressCollection;
        public ReadOnlyObservableCollection<ProgressBarVM> TaskProgressCollection => _taskProgressCollection.ProgressBars;

        public IMsdialDataStorage<ParameterBase> Storage {
            get => _storage;
            set {
                if (SetProperty(ref _storage, value)) {
                    OnPropertyChanged(nameof(ProjectFile));
                }
            }
        }
        private IMsdialDataStorage<ParameterBase> _storage;

        public string ProjectFile => Storage?.Parameter is null ? string.Empty : Storage.Parameter.ProjectParam.ProjectFilePath;

        public AsyncReactiveCommand CreateNewProjectCommand { get; }

        private async Task CreateNewProject() {
            using (var vm = new ProcessSettingViewModel(Model.ProjectSetting)) {
                await RunProcess(vm);
            }
        }

        private async Task RunProcess(ProcessSettingViewModel viewmodel) {
            _processSettingService.ShowDialog(viewmodel);
            if (viewmodel.DialogResult.Value) {
                await viewmodel.Model.RunProcessAsync().ConfigureAwait(false);
                await Model.SaveAsync().ConfigureAwait(false);
            }
        }

        public AsyncReactiveCommand AddNewDatasetCommand { get; }

        private async Task AddNewDataset() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, _broker)) {
                await RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteAllMethodProcessCommand { get; }

        private async Task ExecuteAllMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.AllProcessMethodSettingModel, _broker)) {
                vm.MoveToDataCollectionSetting();
                await RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteIdentificationMethodProcessCommand { get; }

        private async Task ExecuteIdentificationMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.IdentificationProcessMethodSettingModel, _broker)) {
                vm.MoveToIdentificationSetting();
                await RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteAlignmentMethodProcessCommand { get; }

        private async Task ExecuteAlignmentMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.AlignmentProcessMethodSettingModel, _broker)) {
                vm.MoveToAlignmentSetting();
                await RunProcess(vm);
            }
        }

        public ReactiveCommand RunProcessAllCommand { get; }

        public AsyncReactiveCommand OpenProjectCommand { get; }
        public AsyncReactiveCommand<ProjectCrumb> OpenPreviousProjectCommand { get; }
        public ReadOnlyCollection<ProjectCrumb> PreviousProjects => Model.PreviousProjects;

        public AsyncReactiveCommand SaveProjectCommand { get; }
        public AsyncReactiveCommand SaveAsProjectCommand { get; }

        public DelegateCommand GoToTutorialCommand => _goToTutorialCommand ?? (_goToTutorialCommand = new DelegateCommand(GoToTutorial));
        private DelegateCommand _goToTutorialCommand;

        private void GoToTutorial() {
            System.Diagnostics.Process.Start("https://mtbinfo-team.github.io/mtbinfo.github.io/MS-DIAL/tutorial.html");
        }

        public DelegateCommand GoToLicenceCommand => _goToLicenceCommand ?? (_goToLicenceCommand = new DelegateCommand(GoToLicence));
        private DelegateCommand _goToLicenceCommand;

        private void GoToLicence() {
            System.Diagnostics.Process.Start("http://prime.psc.riken.jp/compms/licence/main.html");
        }

        public DelegateCommand ShowAboutCommand => _showAboutCommand ?? (_showAboutCommand = new DelegateCommand(ShowAbout));
        private DelegateCommand _showAboutCommand;

        private void ShowAbout() {
            var view = new View.Help.HelpAboutDialog();
            view.ShowDialog();
        }
    }
}
