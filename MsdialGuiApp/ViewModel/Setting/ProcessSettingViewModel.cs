using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class ProcessSettingViewModel : ViewModelBase
    {
        public ProcessSettingViewModel(ProjectSettingModel projectSettingModel) {
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = project
                .Select(psvm => psvm.DatasetSettingViewModel)
                .Switch();
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = dataset
                .Select(dsvm => dsvm?.MethodSettingViewModel ?? Observable.Never<MethodSettingViewModel>())
                .Switch();
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Switch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.Subscribe(Next).AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = ProjectSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = project
                .Select(psvm => psvm.DatasetSettingViewModel)
                .Switch();
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = dataset
                .Select(dsvm => dsvm?.MethodSettingViewModel ?? Observable.Never<MethodSettingViewModel>())
                .Switch();
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Switch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.Subscribe(Next).AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IDatasetModel datasetModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var datasetSettingModel = new DatasetSettingModel(datasetModel, broker);
            var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)).AddTo(Disposables));
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = dataset
                .Select(dsvm => dsvm?.MethodSettingViewModel ?? Observable.Never<MethodSettingViewModel>())
                .Switch();
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Switch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.Subscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IDatasetModel datasetModel, MethodSettingModel methodSettingModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var datasetSettingModel = new DatasetSettingModel(datasetModel, broker);
            var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)).AddTo(Disposables));
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = Observable.Return(new MethodSettingViewModel(methodSettingModel, Observable.Return(true)).AddTo(Disposables));
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Switch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.Subscribe(Next).AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }
        public ProcessSettingViewModel(ProjectSettingModel projectSettingModel, DatasetSettingModel datasetSettingModel, MethodSettingModel methodSettingModel) {
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)));
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = Observable.Return(new MethodSettingViewModel(methodSettingModel, Observable.Return(true)));
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Switch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.Subscribe(Next).AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = MethodSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }

        public ReadOnlyReactivePropertySlim<ProjectSettingViewModel> ProjectSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<DatasetSettingViewModel> DatasetSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<MethodSettingViewModel> MethodSettingViewModel { get; }

        public ReactivePropertySlim<ISettingViewModel> SelectedSettingViewModel { get; } 

        public ReadOnlyReactivePropertySlim<ISettingViewModel> SelectedParentSettingViewModel { get; } 

        public ReactiveCommand ContinueCommand { get; } 

        public ReactiveProperty<bool> CanRun { get; }
        public AsyncReactiveCommand RunCommand { get; } 

        public ReactivePropertySlim<bool> DialogResult { get; } 

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecide { get; }

        private IObservable<ISettingViewModel> ToParentSettingViewModel(ISettingViewModel selected) {
            if (ProjectSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return ProjectSettingViewModel.StartWith(ProjectSettingViewModel.Value);
            }
            if (DatasetSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return DatasetSettingViewModel.StartWith(DatasetSettingViewModel.Value);
            }
            if (MethodSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return MethodSettingViewModel.StartWith(MethodSettingViewModel.Value);
            }
            return Observable.Return<ISettingViewModel>(null);
        }

        private void Next() {
            var parent = SelectedParentSettingViewModel.Value;
            if (parent is null) {
                return;
            }
            var next = parent.Next(SelectedSettingViewModel.Value);
            if (!(next is null)) {
                SelectedSettingViewModel.Value = next;
                return;
            }
            switch (parent) {
                case ProjectSettingViewModel _:
                    SelectedSettingViewModel.Value = DatasetSettingViewModel.Value?.SettingViewModels.FirstOrDefault() ?? SelectedSettingViewModel.Value;
                    break;
                case DatasetSettingViewModel _:
                    SelectedSettingViewModel.Value = MethodSettingViewModel.Value?.SettingViewModels.FirstOrDefault() ?? SelectedSettingViewModel.Value;
                    break;
                case MethodSettingViewModel _:
                    break;
            }
        }

        private async Task RunProcessAsync() {
            var runSuccess = await MethodSettingViewModel.Value.TryRunAsync();
            if (!runSuccess) {
                return;
            }
            DatasetSettingViewModel.Value.Run();
            var projectRunTask = ProjectSettingViewModel.Value.RunAsync();
            await projectRunTask;
            if (projectRunTask.IsCompleted) {
                DialogResult.Value = true;
            }
        }
    }
}
