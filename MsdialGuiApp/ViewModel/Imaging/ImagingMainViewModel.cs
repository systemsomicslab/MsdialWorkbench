using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class ImagingMainViewModel : MethodViewModel
    {
        public ImagingMainViewModel(ImagingImmsMethodModel model)
            : base(model,
                  new ReactiveProperty<IAnalysisResultViewModel>(), new ReactiveProperty<IAlignmentResultViewModel>(),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0]),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0])) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ImageViewModels = model.ImageModels.ToReadOnlyReactiveCollection(m => new ImagingImageViewModel(m)).AddTo(Disposables);
            RoiCompareViewModels = new ReadOnlyObservableCollection<ImagingRoiCompareViewModel>(new ObservableCollection<ImagingRoiCompareViewModel>());
        }

        public ReadOnlyObservableCollection<ImagingImageViewModel> ImageViewModels { get; }
        public ReadOnlyObservableCollection<ImagingRoiCompareViewModel> RoiCompareViewModels { get; }

        public ImagingImageViewModel SelectedImageViewModel {
            get => _selectedImageViewModel;
            set => SetProperty(ref _selectedImageViewModel, value);
        }
        private ImagingImageViewModel _selectedImageViewModel;

        public ImagingRoiCompareViewModel SelectedRoiCompareViewModel {
            get => _selectedRoiCompareViewModel;
            set => SetProperty(ref _selectedRoiCompareViewModel, value);
        }
        private ImagingRoiCompareViewModel _selectedRoiCompareViewModel;
        private readonly ImagingImmsMethodModel _model;

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            return Task.CompletedTask;
        }
    }
}
