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
        private readonly ProcessSettingModel _model;

        public ProcessSettingViewModel(ProjectSettingModel projectSettingModel) {
            _model = new ProcessSettingModel(projectSettingModel).AddTo(Disposables);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var dataset = project.SelectSwitch(psvm => psvm.DatasetSettingViewModel);
            DatasetSettingViewModel = dataset.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var method = dataset
                .Where(dsvm => dsvm is not null)
                .SelectSwitch(dsvm => dsvm!.MethodSettingViewModel);
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel?>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .SelectSwitch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedSettingViewModel
                .SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .SelectSwitch(vm => vm is MethodSettingViewModel
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

            SelectedSettingViewModel.Value = ProjectSettingViewModel.Value?.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            _model = new ProcessSettingModel(projectSettingModel).AddTo(Disposables);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = project
                .SelectSwitch(psvm => psvm.DatasetSettingViewModel);
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = dataset
                .Where(dsvm => dsvm is not null)
                .SelectSwitch(dsvm => dsvm!.MethodSettingViewModel);
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel?>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .SelectSwitch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedSettingViewModel
                .SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .SelectSwitch(vm => vm is MethodSettingViewModel
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

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value?.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IDatasetModel datasetModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            var datasetSettingModel = new DatasetSettingModel(datasetModel, broker);
            _model = new ProcessSettingModel(projectSettingModel, datasetSettingModel).AddTo(Disposables);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)).AddTo(Disposables));
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = dataset
                .Where(dsvm => dsvm != null)
                .SelectSwitch(dsvm => dsvm.MethodSettingViewModel);
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel?>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .SelectSwitch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedSettingViewModel
                .SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .SelectSwitch(vm => vm is MethodSettingViewModel
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

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value?.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingViewModel(IProjectModel projectModel, IDatasetModel datasetModel, MethodSettingModel methodSettingModel, IMessageBroker broker) {
            var projectSettingModel = new ProjectSettingModel(projectModel, broker);
            var datasetSettingModel = new DatasetSettingModel(datasetModel, broker);
            _model = new ProcessSettingModel(projectSettingModel, datasetSettingModel, methodSettingModel).AddTo(Disposables);
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel).AddTo(Disposables));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)).AddTo(Disposables));
            DatasetSettingViewModel = dataset
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var method = Observable.Return(new MethodSettingViewModel(methodSettingModel, Observable.Return(true)).AddTo(Disposables));
            MethodSettingViewModel = method
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel?>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .SelectSwitch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedSettingViewModel
                .SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .SelectSwitch(vm => vm is MethodSettingViewModel
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

            SelectedSettingViewModel.Value = DatasetSettingViewModel.Value?.SettingViewModels.FirstOrDefault();
        }
        public ProcessSettingViewModel(ProjectSettingModel projectSettingModel, DatasetSettingModel datasetSettingModel, MethodSettingModel methodSettingModel) {
            _model = new ProcessSettingModel(projectSettingModel, datasetSettingModel, methodSettingModel).AddTo(Disposables);
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

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel?>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .SelectSwitch(ToParentSettingViewModel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                dataset.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                method.SelectSwitch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedSettingViewModel
                .SelectSwitch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Next)
                .AddTo(Disposables);

            CanRun = SelectedParentSettingViewModel
                .SelectSwitch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors.Inverse()
                    : Observable.Return(false))
                .ToReactiveProperty()
                .AddTo(Disposables);
            RunCommand = CanRun
                .ToAsyncReactiveCommand()
                .WithSubscribe(RunProcessAsync)
                .AddTo(Disposables);

            DialogResult = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            SelectedSettingViewModel.Value = MethodSettingViewModel.Value?.SettingViewModels.FirstOrDefault();
        }

        public ProcessSettingModel Model => _model;
        public ReadOnlyReactivePropertySlim<ProjectSettingViewModel?> ProjectSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<DatasetSettingViewModel?> DatasetSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<MethodSettingViewModel?> MethodSettingViewModel { get; }

        public ReactivePropertySlim<ISettingViewModel?> SelectedSettingViewModel { get; } 

        public ReadOnlyReactivePropertySlim<ISettingViewModel?> SelectedParentSettingViewModel { get; } 

        public ReactiveCommand ContinueCommand { get; } 

        public ReactiveProperty<bool> CanRun { get; }
        public AsyncReactiveCommand RunCommand { get; } 

        public ReactivePropertySlim<bool> DialogResult { get; } 

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecide { get; }

        private IObservable<ISettingViewModel?> ToParentSettingViewModel(ISettingViewModel? selected) {
            if (selected is null) {
                return Observable.Return<ISettingViewModel?>(null);
            }
            if (ProjectSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return ProjectSettingViewModel.StartWith(ProjectSettingViewModel.Value);
            }
            if (DatasetSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return DatasetSettingViewModel.StartWith(DatasetSettingViewModel.Value);
            }
            if (MethodSettingViewModel.Value?.SettingViewModels.Contains(selected) ?? false) {
                return MethodSettingViewModel.StartWith(MethodSettingViewModel.Value);
            }
            return Observable.Return<ISettingViewModel?>(null);
        }

        private void Next() {
            if (SelectedParentSettingViewModel.Value is not ISettingViewModel parent || SelectedSettingViewModel.Value is not ISettingViewModel current) {
                return;
            }
            var next = parent.Next(current);
            if (next is not null) {
                SelectedSettingViewModel.Value = next;
                return;
            }
            switch (parent) {
                case ProjectSettingViewModel _:
                    SelectedSettingViewModel.Value = DatasetSettingViewModel.Value?.SettingViewModels.FirstOrDefault() ?? current;
                    break;
                case DatasetSettingViewModel _:
                    SelectedSettingViewModel.Value = MethodSettingViewModel.Value?.SettingViewModels.FirstOrDefault() ?? current;
                    break;
                case MethodSettingViewModel _:
                    break;
            }
        }

        public void MoveToDataCollectionSetting() {
            var vm = MethodSettingViewModel.Value?.GetDataCollectionSetting();
            if (vm != null) {
                SelectedSettingViewModel.Value = vm;
            }
        }

        public void MoveToIdentificationSetting() {
            var vm = MethodSettingViewModel.Value?.GetIdentificationSetting();
            if (vm != null) {
                SelectedSettingViewModel.Value = vm;
            }
        }

        public void MoveToAlignmentSetting() {
            var vm = MethodSettingViewModel.Value?.GetAlignmentSetting();
            if (vm != null) {
                SelectedSettingViewModel.Value = vm;
            }
        }

        private Task RunProcessAsync() {
            DialogResult.Value = true;
            return Task.CompletedTask;
        }
    }
}
