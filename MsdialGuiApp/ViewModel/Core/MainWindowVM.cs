using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.Message;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    class MainWindowVM : ViewModelBase
    {
        public MainWindowVM(
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService,
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

            this.analysisFilePropertyResetService = analysisFilePropertyResetService;
            this.processSettingService = processSettingSerivce ?? throw new ArgumentNullException(nameof(processSettingSerivce));
            parameter = new ReactivePropertySlim<ParameterBase>().AddTo(Disposables);
            Model = new MainWindowModel(_broker);

            var projectViewModel = Model.ObserveProperty(m => m.CurrentProject)
                .Select(m => m is null ? null : new ProjectViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, analysisFilePropertyResetService, _broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var datasetViewModel = projectViewModel
                .Switch(project => project?.CurrentDatasetViewModel ?? Observable.Never<DatasetViewModel>())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
                //.Switch(project => project?.CurrentDatasetViewModel.StartWith(project.CurrentDatasetViewModel.Value));
            var methodViewModel = datasetViewModel
                .Switch(dataset => dataset?.MethodViewModel ?? Observable.Never<MethodViewModel>())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            //.Switch(dataset => dataset?.MethodViewModel.StartWith(dataset.MethodViewModel.Value));

            ProjectViewModel = projectViewModel;
            //.ToReadOnlyReactivePropertySlim()
            //.AddTo(Disposables);
            DatasetViewModel = datasetViewModel;
            //.ToReadOnlyReactivePropertySlim()
            //.AddTo(Disposables);
            MethodViewModel = methodViewModel;
                // .ToReadOnlyReactivePropertySlim()
                // .AddTo(Disposables);

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

            _taskProgressCollection = new TaskProgressCollection();
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowSaving, "Saving...").AddTo(Disposables);
            _taskProgressCollection.ShowWhileSwitchOn(Model.NowLoading, "Loading...").AddTo(Disposables);
            _broker.ToObservable<ITaskNotification>()
                .Subscribe(_taskProgressCollection.Update)
                .AddTo(Disposables);
        }

        private readonly IMessageBroker _broker;
        private readonly IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService;
        private readonly IWindowService<ProcessSettingViewModel> processSettingService;
        private readonly ReactivePropertySlim<ParameterBase> parameter;

        public MainWindowModel Model { get; }

        public ReadOnlyReactivePropertySlim<ProjectViewModel> ProjectViewModel { get; }
        public ReadOnlyReactivePropertySlim<DatasetViewModel> DatasetViewModel { get; }
        public ReadOnlyReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        private readonly TaskProgressCollection _taskProgressCollection;
        public ReadOnlyObservableCollection<ProgressBarVM> TaskProgressCollection => _taskProgressCollection.ProgressBars;

        public IMsdialDataStorage<ParameterBase> Storage {
            get => storage;
            set {
                if (SetProperty(ref storage, value)) {
                    OnPropertyChanged(nameof(ProjectFile));
                }
            }
        }
        private IMsdialDataStorage<ParameterBase> storage;

        public string ProjectFile => Storage?.Parameter is null ? string.Empty : Storage.Parameter.ProjectParam.ProjectFilePath;

        public AsyncReactiveCommand CreateNewProjectCommand { get; }

        private Task RunProcess(ProcessSettingViewModel viewmodel) {
            processSettingService.ShowDialog(viewmodel);
            if (viewmodel.DialogResult.Value) {
                return Model.SaveAsync();
            }
            return Task.CompletedTask;
        }

        private Task CreateNewProject() {
            using (var vm = new ProcessSettingViewModel(Model.ProjectSetting)) {
                return RunProcess(vm);
            }
        }

        public AsyncReactiveCommand AddNewDatasetCommand { get; }

        private Task AddNewDataset() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, _broker)) {
                return RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteAllMethodProcessCommand { get; }

        private Task ExecuteAllMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.AllProcessMethodSettingModel, _broker)) {
                return RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteIdentificationMethodProcessCommand { get; }

        private Task ExecuteIdentificationMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.IdentificationProcessMethodSettingModel, _broker)) {
                return RunProcess(vm);
            }
        }

        public AsyncReactiveCommand ExecuteAlignmentMethodProcessCommand { get; }

        private Task ExecuteAlignmentMethodProcess() {
            using (var vm = new ProcessSettingViewModel(Model.CurrentProject, Model.CurrentProject.CurrentDataset, Model.CurrentProject.CurrentDataset.AlignmentProcessMethodSettingModel, _broker)) {
                return RunProcess(vm);
            }
        }

        public ReactiveCommand RunProcessAllCommand { get; }

        public AsyncReactiveCommand OpenProjectCommand { get; }

        public AsyncReactiveCommand SaveProjectCommand { get; }

        public AsyncReactiveCommand SaveAsProjectCommand { get; }

        public DelegateCommand<Window> SaveParameterCommand => saveParameterCommand ?? (saveParameterCommand = new DelegateCommand<Window>(owner => SaveParameter(owner, Storage)));
        private DelegateCommand<Window> saveParameterCommand;

        private static void SaveParameter(Window owner, IMsdialDataStorage<ParameterBase> storage) {
            // TODO: implement process when parameter save failed.
            var sfd = new SaveFileDialog
            {
                Filter = "MED file(*.med)|*.med",
                Title = "Save file dialog",
                InitialDirectory = storage.Parameter.ProjectFolderPath
            };

            if (sfd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow()
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the parameter...",
                };
                message.Show();

                MessagePackHandler.SaveToFile(storage.Parameter, sfd.FileName);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand FilePropertyResetCommand => filePropertyResetCommand ?? (filePropertyResetCommand = new DelegateCommand(FilePropertyResettingWindow));
        private DelegateCommand filePropertyResetCommand;

        private void FilePropertyResettingWindow() {
            var storage = DatasetViewModel.Value.Model.Storage;
            var files = storage.AnalysisFiles;
            var analysisFilePropertySetModel = new AnalysisFilePropertySetModel(files, new ProjectBaseParameterModel(Storage.Parameter.ProjectParam));
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetViewModel(analysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertyResetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result == true) {
                    analysisFilePropertySetModel.Update();
                    parameter.ForceNotify();
                }
            }
        }

        public DelegateCommand GoToTutorialCommand => goToTutorialCommand ?? (goToTutorialCommand = new DelegateCommand(GoToTutorial));
        private DelegateCommand goToTutorialCommand;

        private void GoToTutorial() {
            System.Diagnostics.Process.Start("https://mtbinfo-team.github.io/mtbinfo.github.io/MS-DIAL/tutorial.html");
        }
    }
}
