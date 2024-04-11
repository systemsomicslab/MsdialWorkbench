using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal abstract class MethodViewModel : ViewModelBase
    {
        public MethodViewModel(IMethodModel model, IReadOnlyReactiveProperty<IAnalysisResultViewModel?> analysisFileViewModel, IReadOnlyReactiveProperty<IAlignmentResultViewModel?> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels) {
            Model = model;
            var analysisFilesView = model.AnalysisFileModelCollection.AnalysisFiles.ToReadOnlyReactiveCollection(file => new AnalysisFileBeanViewModel(file));
            AnalysisFilesView = CollectionViewSource.GetDefaultView(analysisFilesView);
            var alignmentFilesView = model.AlignmentFiles.Files.ToReadOnlyReactiveCollection(file => new AlignmentFileBeanViewModel(file));
            AlignmentFilesView = CollectionViewSource.GetDefaultView(alignmentFilesView);

            AnalysisFilesView.MoveCurrentToFirst();
            AlignmentFilesView.MoveCurrentToFirst();

            SelectedAnalysisFile = new ReactivePropertySlim<AnalysisFileBeanViewModel>(analysisFilesView.FirstOrDefault()).AddTo(Disposables);
            SelectedAlignmentFile = new ReactivePropertySlim<AlignmentFileBeanViewModel>().AddTo(Disposables);
            model.ObserveProperty(m => m.AnalysisFileModel)
                .Do(_ =>
                {
                    foreach (var file in analysisFilesView) {
                        file.IsSelected = false;
                    }
                })
                .Subscribe(file =>
                {
                    if (analysisFilesView.FirstOrDefault(v => v.File == file) is AnalysisFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);
            model.ObserveProperty(m => m.AlignmentFile)
                .Do(_ =>
                {
                    foreach (var file in alignmentFilesView) {
                        file.IsSelected = false;
                    }
                })
                .Subscribe(file =>
                {
                    if (alignmentFilesView.FirstOrDefault(v => v.File == file) is AlignmentFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);

            LoadAnalysisFileCommand = SelectedAnalysisFile
                .Select(file => file != null)
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => LoadAnalysisFileAsync(default))
                .AddTo(Disposables);
            LoadAlignmentFileCommand = SelectedAlignmentFile
                .Select(file => file != null)
                .ObserveOnUIDispatcher()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => LoadAlignmentFileAsync(default))
                .AddTo(Disposables);

            AnalysisViewModel = analysisFileViewModel.AddTo(Disposables);
            AlignmentViewModel = alignmentFileViewModel.AddTo(Disposables);
            resultViewModels = new List<IReadOnlyReactiveProperty<IResultViewModel?>> { AnalysisViewModel, AlignmentViewModel, };
            ResultViewModels = resultViewModels.AsReadOnly();

            viewModelChanged = new Subject<Unit>().AddTo(Disposables);
            selectedViewModel = AnalysisViewModel;
            viewModelChanged.OnNext(Unit.Default);

            ChromatogramViewModels = chromatogramViewModels.AddTo(Disposables);
            MassSpectrumViewModels = massSpectrumViewModels.AddTo(Disposables);

            viewModelChanged
                .Where(_ => SelectedViewModel == analysisFileViewModel)
                .Subscribe(_ =>
                {
                    chromatogramViewModels.SelectAnalysisFile();
                    massSpectrumViewModels.SelectAnalysisFile();
                }).AddTo(Disposables);
            viewModelChanged
                .Where(_ => SelectedViewModel == alignmentFileViewModel)
                .Subscribe(_ =>
                {
                    chromatogramViewModels.SelectAlignmentFile();
                    massSpectrumViewModels.SelectAlignmentFile();
                }).AddTo(Disposables);
            new IObservable<IReadOnlyReactiveProperty<IResultViewModel?>>[]
            {
                SelectedAnalysisFile.SkipNull().ToConstant(AnalysisViewModel),
                SelectedAlignmentFile.SkipNull().ToConstant(AlignmentViewModel),
            }.Merge()
            .Subscribe(vm => SelectedViewModel = vm)
            .AddTo(Disposables);
        }

        public IMethodModel Model { get; }

        public ReactivePropertySlim<AnalysisFileBeanViewModel> SelectedAnalysisFile { get; }
        public ReactivePropertySlim<AlignmentFileBeanViewModel> SelectedAlignmentFile { get; }

        public ICollectionView AnalysisFilesView { get; }
        public ICollectionView AlignmentFilesView { get; }

        public AsyncReactiveCommand LoadAnalysisFileCommand { get; }

        protected Task LoadAnalysisFileAsync(CancellationToken token) {
            if (!(SelectedAnalysisFile.Value is null)) {
                // foreach (AnalysisFileBeanViewModel analysisFile in AnalysisFilesView) {
                //     analysisFile.IsSelected = false;
                // }
                // SelectedAnalysisFile.Value.IsSelected = true;
                var task = LoadAnalysisFileCoreAsync(SelectedAnalysisFile.Value, token);
                ChromatogramViewModels.SelectAnalysisFile();
                MassSpectrumViewModels.SelectAnalysisFile();
                return task;
            }
            return Task.CompletedTask;
        }

        protected abstract Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token);


        public AsyncReactiveCommand LoadAlignmentFileCommand { get; }

        protected Task LoadAlignmentFileAsync(CancellationToken token) {
            if (!(SelectedAlignmentFile.Value is null)) {
                // foreach (AlignmentFileBeanViewModel alignmentFile in AlignmentFilesView) {
                //     alignmentFile.IsSelected = false;
                // }
                // SelectedAlignmentFile.Value.IsSelected = true;
                var task = LoadAlignmentFileCoreAsync(SelectedAlignmentFile.Value, token);
                ChromatogramViewModels.SelectAlignmentFile();
                MassSpectrumViewModels.SelectAlignmentFile();
                return task;
            }
            return Task.CompletedTask;
        }

        protected abstract Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token);

        public IReadOnlyReactiveProperty<IAnalysisResultViewModel?> AnalysisViewModel { get; }

        public IReadOnlyReactiveProperty<IAlignmentResultViewModel?> AlignmentViewModel { get; }

        public ReadOnlyCollection<IReadOnlyReactiveProperty<IResultViewModel?>> ResultViewModels { get; }
        private readonly List<IReadOnlyReactiveProperty<IResultViewModel?>> resultViewModels;

        private readonly Subject<Unit> viewModelChanged;
        public IReadOnlyReactiveProperty<IResultViewModel?> SelectedViewModel {
            get => selectedViewModel;
            set {
                if (SetProperty(ref selectedViewModel, value)) {
                    viewModelChanged.OnNext(Unit.Default);
                }
            }
        }
        private IReadOnlyReactiveProperty<IResultViewModel?> selectedViewModel;

        public ViewModelSwitcher ChromatogramViewModels { get; }
        public ViewModelSwitcher MassSpectrumViewModels { get; }


        public DelegateCommand GoToMsfinderCommand => _goToMsfinderCommand ??= new DelegateCommand(GoToMsfinderMethod);
        private DelegateCommand? _goToMsfinderCommand;

        private void GoToMsfinderMethod() {
            if (SelectedViewModel.Value is IResultViewModel vm) {
                Model.InvokeMsfinder(vm.Model);
            }
        }
    }
}
