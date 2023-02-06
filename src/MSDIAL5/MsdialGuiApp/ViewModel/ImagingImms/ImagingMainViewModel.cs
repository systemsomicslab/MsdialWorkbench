using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.ImagingImms
{
    internal sealed class ImagingMainViewModel : MethodViewModel
    {
        private readonly ImagingImmsMethodModel _model;
        private readonly IMessageBroker _broker;

        public ImagingMainViewModel(ImagingImmsMethodModel model, IMessageBroker broker)
            : base(model,
                  new ReactiveProperty<IAnalysisResultViewModel>(), new ReactiveProperty<IAlignmentResultViewModel>(),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0]),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0])) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker;
            ImageViewModels = model.ImageModels.ToReadOnlyReactiveCollection(m => new ImagingImageViewModel(m, broker)).AddTo(Disposables);
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

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            return Task.CompletedTask;
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ?? (_exportAnalysisResultCommand = new DelegateCommand(ExportAnalysis));
        private DelegateCommand _exportAnalysisResultCommand;

        private void ExportAnalysis() {
            var m = _model.CreateExportAnalysisModel();
            using (var vm = new AnalysisResultExportViewModel(m)) {
                _broker.Publish(vm);
            }
        }

    }
}
