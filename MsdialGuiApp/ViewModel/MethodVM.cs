using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class MethodViewModel : ViewModelBase
    {
        public MethodViewModel(IMethodModel model, IReadOnlyReactiveProperty<AnalysisFileViewModel> analysisFileViewModel, IReadOnlyReactiveProperty<AlignmentFileViewModel> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels) {
            Model = model;
            var analysisFilesView = model.AnalysisFiles.ToReadOnlyReactiveCollection(file => new AnalysisFileBeanViewModel(file));
            AnalysisFilesView = CollectionViewSource.GetDefaultView(analysisFilesView);
            var alignmentFilesView = model.AlignmentFiles.ToReadOnlyReactiveCollection(file => new AlignmentFileBeanViewModel(file));
            AlignmentFilesView = CollectionViewSource.GetDefaultView(alignmentFilesView);

            AnalysisFilesView.MoveCurrentToFirst();
            AlignmentFilesView.MoveCurrentToFirst();

            SelectedAnalysisFile = new ReactivePropertySlim<AnalysisFileBeanViewModel>(analysisFilesView.FirstOrDefault()).AddTo(Disposables);
            SelectedAlignmentFile = new ReactivePropertySlim<AlignmentFileBeanViewModel>().AddTo(Disposables);
            model.ObserveProperty(m => m.AnalysisFile)
                .Do(_ => {
                    foreach (var file in analysisFilesView) {
                        file.IsSelected = false;
                    }
                })
                .Subscribe(file => {
                    if (analysisFilesView.FirstOrDefault(v => v.File == file) is AnalysisFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);
            model.ObserveProperty(m => m.AlignmentFile)
                .Do(_ => {
                    foreach (var file in alignmentFilesView) {
                        file.IsSelected = false;
                    }
                })
                .Subscribe(file => {
                    if (alignmentFilesView.FirstOrDefault(v => v.File == file) is AlignmentFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);

            LoadAnalysisFileCommand = SelectedAnalysisFile
                .Select(file => file != null)
                .ToReactiveCommand()
                .WithSubscribe(LoadAnalysisFile)
                .AddTo(Disposables);
            LoadAlignmentFileCommand = SelectedAlignmentFile
                .Select(file => file != null)
                .ToReactiveCommand()
                .WithSubscribe(LoadAlignmentFile)
                .AddTo(Disposables);

            AnalysisViewModel = analysisFileViewModel.AddTo(Disposables);
            AlignmentViewModel = alignmentFileViewModel.AddTo(Disposables);
            resultViewModels = new List<IReadOnlyReactiveProperty<ResultVM>> { AnalysisViewModel, AlignmentViewModel, };
            ResultViewModels = resultViewModels.AsReadOnly();

            viewModelChanged = new Subject<Unit>().AddTo(Disposables);
            SelectedViewModel = AnalysisViewModel;

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
            new IObservable<IReadOnlyReactiveProperty<ResultVM>>[]
            {
                SelectedAnalysisFile
                    .Where(file => !(file is null))
                    .Select(_ => AnalysisViewModel),
                SelectedAlignmentFile
                    .Where(file => !(file is null))
                    .Select(_ => AlignmentViewModel),
            }.Merge()
            .Subscribe(vm => SelectedViewModel = vm)
            .AddTo(Disposables);
        }

        public MethodViewModel(IMethodModel model, IReadOnlyReactiveProperty<AnalysisFileViewModel> analysisFileViewModel, IReadOnlyReactiveProperty<AlignmentFileViewModel> alignmentFileViewModel) {
            Model = model;
            var analysisFilesView = model.AnalysisFiles.ToReadOnlyReactiveCollection(file => new AnalysisFileBeanViewModel(file));
            AnalysisFilesView = CollectionViewSource.GetDefaultView(analysisFilesView);
            var alignmentFilesView = model.AlignmentFiles.ToReadOnlyReactiveCollection(file => new AlignmentFileBeanViewModel(file));
            AlignmentFilesView = CollectionViewSource.GetDefaultView(alignmentFilesView);

            AnalysisFilesView.MoveCurrentToFirst();
            AlignmentFilesView.MoveCurrentToFirst();

            SelectedAnalysisFile = new ReactivePropertySlim<AnalysisFileBeanViewModel>(analysisFilesView.FirstOrDefault()).AddTo(Disposables);
            SelectedAlignmentFile = new ReactivePropertySlim<AlignmentFileBeanViewModel>(alignmentFilesView.FirstOrDefault()).AddTo(Disposables);
            model.ObserveProperty(m => m.AnalysisFile)
                .Do(_ => {
                    foreach (var file in analysisFilesView) {
                        file.IsSelected = false;
                    }
                })
                .Subscribe(file => {
                    if (analysisFilesView.FirstOrDefault(v => v.File == file) is AnalysisFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);
            model.ObserveProperty(m => m.AlignmentFile)
                .Do(_ => {
                    foreach (var file in alignmentFilesView) {
                        file.IsSelected = false;
                    }
                    if (SelectedAlignmentFile.Value != null) {
                        SelectedAlignmentFile.Value.IsSelected = false;
                    }
                })
                .Subscribe(file => {
                    if (alignmentFilesView.FirstOrDefault(v => v.File == file) is AlignmentFileBeanViewModel vm) {
                        vm.IsSelected = true;
                    }
                }).AddTo(Disposables);

            LoadAnalysisFileCommand = SelectedAnalysisFile
                .Select(file => file != null)
                .ToReactiveCommand()
                .WithSubscribe(LoadAnalysisFile)
                .AddTo(Disposables);
            LoadAlignmentFileCommand = SelectedAlignmentFile
                .Select(file => file != null)
                .ToReactiveCommand()
                .WithSubscribe(LoadAlignmentFile)
                .AddTo(Disposables);

            AnalysisViewModel = analysisFileViewModel.AddTo(Disposables);
            AlignmentViewModel = alignmentFileViewModel.AddTo(Disposables);
            resultViewModels = new List<IReadOnlyReactiveProperty<ResultVM>> { AnalysisViewModel, AlignmentViewModel, };
            ResultViewModels = resultViewModels.AsReadOnly();
            viewModelChanged = new Subject<Unit>().AddTo(Disposables);
            SelectedViewModel = AnalysisViewModel;

            var chromatogramViewModels = new List<IReadOnlyReactiveProperty<ViewModelBase>> { };
            ChromatogramViewModels = new ViewModelSwitcher(null, null, chromatogramViewModels).AddTo(Disposables);

            var massSpectrumViewModels = new List<IReadOnlyReactiveProperty<ViewModelBase>> { };
            MassSpectrumViewModels = new ViewModelSwitcher(null, null, massSpectrumViewModels).AddTo(Disposables);
        }

        public IMethodModel Model { get; }

        public ReactivePropertySlim<AnalysisFileBeanViewModel> SelectedAnalysisFile { get; }
        public ReactivePropertySlim<AlignmentFileBeanViewModel> SelectedAlignmentFile { get; }

        public ICollectionView AnalysisFilesView { get; }
        public ICollectionView AlignmentFilesView { get; }

        public ReactiveCommand LoadAnalysisFileCommand { get; }

        protected void LoadAnalysisFile() {
            if (!(SelectedAnalysisFile.Value is null)) {
                // foreach (AnalysisFileBeanViewModel analysisFile in AnalysisFilesView) {
                //     analysisFile.IsSelected = false;
                // }
                // SelectedAnalysisFile.Value.IsSelected = true;
                LoadAnalysisFileCore(SelectedAnalysisFile.Value);
                ChromatogramViewModels.SelectAnalysisFile();
                MassSpectrumViewModels.SelectAnalysisFile();
            }
        }

        protected abstract void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile);

        public ReactiveCommand LoadAlignmentFileCommand { get; }

        protected void LoadAlignmentFile() {
            if (!(SelectedAlignmentFile.Value is null)) {
                // foreach (AlignmentFileBeanViewModel alignmentFile in AlignmentFilesView) {
                //     alignmentFile.IsSelected = false;
                // }
                // SelectedAlignmentFile.Value.IsSelected = true;
                LoadAlignmentFileCore(SelectedAlignmentFile.Value);
                ChromatogramViewModels.SelectAlignmentFile();
                MassSpectrumViewModels.SelectAlignmentFile();
            }
        }

        protected abstract void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile);

        public IReadOnlyReactiveProperty<AnalysisFileViewModel> AnalysisViewModel { get; }

        public IReadOnlyReactiveProperty<AlignmentFileViewModel> AlignmentViewModel { get; }

        public ReadOnlyCollection<IReadOnlyReactiveProperty<ResultVM>> ResultViewModels { get; }
        private readonly List<IReadOnlyReactiveProperty<ResultVM>> resultViewModels;

        private readonly Subject<Unit> viewModelChanged;
        public IReadOnlyReactiveProperty<ResultVM> SelectedViewModel {
            get => selectedViewModel;
            set {
                if (SetProperty(ref selectedViewModel, value)) {
                    viewModelChanged.OnNext(Unit.Default);
                }
            }
        }
        private IReadOnlyReactiveProperty<ResultVM> selectedViewModel;

        public ViewModelSwitcher ChromatogramViewModels { get; }
        public ViewModelSwitcher MassSpectrumViewModels { get; }
    }
}
