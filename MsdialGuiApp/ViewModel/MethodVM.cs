using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class MethodViewModel : ViewModelBase
    {
        public MethodViewModel(MethodModelBase model) {
            Model = model;
            var analysisFilesView = model.AnalysisFiles.ToReadOnlyReactiveCollection(file => new AnalysisFileBeanViewModel(file));
            AnalysisFilesView = CollectionViewSource.GetDefaultView(analysisFilesView);
            var alignmentFilesView = model.AlignmentFiles.ToReadOnlyReactiveCollection(file => new AlignmentFileBeanViewModel(file));
            AlignmentFilesView = CollectionViewSource.GetDefaultView(alignmentFilesView);

            AnalysisFilesView.MoveCurrentToFirst();
            AlignmentFilesView.MoveCurrentToFirst();

            SelectedAnalysisFile = new ReactivePropertySlim<AnalysisFileBeanViewModel>(analysisFilesView.FirstOrDefault()).AddTo(Disposables);
            SelectedAlignmentFile = new ReactivePropertySlim<AlignmentFileBeanViewModel>(alignmentFilesView.FirstOrDefault()).AddTo(Disposables);

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
            SelectedViewModel = new ReactivePropertySlim<ResultVM>().AddTo(Disposables);
        }

        public MethodModelBase Model { get; }

        public ReactivePropertySlim<AnalysisFileBeanViewModel> SelectedAnalysisFile { get; }
        public ReactivePropertySlim<AlignmentFileBeanViewModel> SelectedAlignmentFile { get; }

        public ICollectionView AnalysisFilesView { get; }
        public ICollectionView AlignmentFilesView { get; }

        public ReactiveCommand LoadAnalysisFileCommand { get; }

        protected void LoadAnalysisFile() {
            if (!(SelectedAnalysisFile.Value is null)) {
                foreach (AnalysisFileBeanViewModel analysisFile in AnalysisFilesView) {
                    analysisFile.IsSelected = false;
                }
                SelectedAnalysisFile.Value.IsSelected = true;
                LoadAnalysisFileCore(SelectedAnalysisFile.Value);
            }
        }

        protected abstract void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile);

        public ReactiveCommand LoadAlignmentFileCommand { get; }

        protected void LoadAlignmentFile() {
            if (!(SelectedAlignmentFile.Value is null)) {
                foreach (AlignmentFileBeanViewModel alignmentFile in AlignmentFilesView) {
                    alignmentFile.IsSelected = false;
                }
                SelectedAlignmentFile.Value.IsSelected = true;
                LoadAlignmentFileCore(SelectedAlignmentFile.Value);
            }
        }

        protected abstract void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile);

        public ReactivePropertySlim<ResultVM> SelectedViewModel { get; }
    }
}
