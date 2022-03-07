using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class ProcessSettingViewModel : ViewModelBase
    {
        public ProcessSettingViewModel(ProjectSettingModel projectSettingModel) {
            var project = Observable.Return(new ProjectSettingViewModel(projectSettingModel));
            ProjectSettingViewModel = project
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            DatasetSettingViewModel = project
                .Select(psvm => psvm.DatasetSettingViewModel)
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            MethodSettingViewModel = DatasetSettingViewModel
                .Select(dsvm => dsvm?.MethodSettingViewModel ?? Observable.Never<MethodSettingViewModel>())
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedSettingViewModel = new ReactivePropertySlim<ISettingViewModel>().AddTo(Disposables);
            SelectedParentSettingViewModel = SelectedSettingViewModel
                .Do(v => Console.WriteLine($"Selected: {v}"))
                .Switch(ToParentSettingViewModel)
                .Do(v => Console.WriteLine($"SelectedParent2: {v}"))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                project.Switch(vm => vm.ObserveHasErrors),
                DatasetSettingViewModel.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
                MethodSettingViewModel.Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(false)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChangeAfterDecide = new[]
            {
                project.Switch(vm => vm.ObserveChangeAfterDecision),
                DatasetSettingViewModel.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
                MethodSettingViewModel.Switch(vm => vm?.ObserveChangeAfterDecision ?? Observable.Return(false)),
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ContinueCommand = SelectedParentSettingViewModel
                .Do(v => Console.WriteLine($"SelectedParent: {v}"))
                .Switch(vm => vm?.ObserveHasErrors ?? Observable.Return(true))
                .Inverse()
                .Do(v => Console.WriteLine($"ContinueCommand: {v}"))
                .ToReactiveCommand()
                .AddTo(Disposables);
            ContinueCommand.WithLatestFrom(SelectedParentSettingViewModel, (a, b) => b)
                .Where(vm => vm != null)
                .Subscribe(vm => vm.Next())
                .AddTo(Disposables);

            RunCommand = SelectedParentSettingViewModel
                .Switch(vm => vm is MethodSettingViewModel
                    ? vm.ObserveHasErrors
                    : Observable.Return(true))
                .Inverse()
                .ToReactiveCommand()
                .AddTo(Disposables);

            SelectedSettingViewModel.Value = ProjectSettingViewModel.Value.SettingViewModels.FirstOrDefault();
        }

        // public ProcessSettingViewModel(DatasetSettingModel datasetSettingModel) : this() {
        //     ProjectSettingViewModel = Observable.Return<ProjectSettingViewModel>(null)
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     var dataset = Observable.Return(new DatasetSettingViewModel(datasetSettingModel, Observable.Return(true)));
        //     DatasetSettingViewModel = dataset
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     MethodSettingViewModel = dataset
        //         .Select(dsvm => dsvm?.MethodSettingViewModel)
        //         .Switch()
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     dataset.Select(p => p.SettingViewModels.FirstOrDefault())
        //         .Where(vm => vm != null)
        //         .Take(1)
        //         .Subscribe(vm => SelectedSettingViewModel.Value = vm)
        //         .AddTo(Disposables);
        // }

        // public ProcessSettingViewModel(MethodSettingModel methodSettingModel) : this() {
        //     ProjectSettingViewModel = Observable.Return<ProjectSettingViewModel>(null)
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     DatasetSettingViewModel = Observable.Return<DatasetSettingViewModel>(null)
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     var method = Observable.Return(new MethodSettingViewModel(methodSettingModel, Observable.Return(true)));
        //     MethodSettingViewModel = method
        //         .ToReadOnlyReactivePropertySlim()
        //         .AddTo(Disposables);
        //     method.Select(p => p.SettingViewModels.FirstOrDefault())
        //         .Where(vm => vm != null)
        //         .Take(1)
        //         .Subscribe(vm => SelectedSettingViewModel.Value = vm)
        //         .AddTo(Disposables);
        // }

        public ReadOnlyReactivePropertySlim<ProjectSettingViewModel> ProjectSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<DatasetSettingViewModel> DatasetSettingViewModel { get; }

        public ReadOnlyReactivePropertySlim<MethodSettingViewModel> MethodSettingViewModel { get; }

        public ReactivePropertySlim<ISettingViewModel> SelectedSettingViewModel { get; } 

        public ReadOnlyReactivePropertySlim<ISettingViewModel> SelectedParentSettingViewModel { get; } 

        public ReactiveCommand ContinueCommand { get; } 

        public ReactiveCommand RunCommand { get; } 

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
    }
}
