using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
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
    internal sealed class ImagingImmsMainViewModel : MethodViewModel
    {
        private readonly ImagingImmsMethodModel _model;
        private readonly IMessageBroker _broker;

        public ImagingImmsMainViewModel(ImagingImmsMethodModel model, IMessageBroker broker, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model,
                  new ReactiveProperty<IAnalysisResultViewModel>(), new ReactiveProperty<IAlignmentResultViewModel>(),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0]),
                  new ViewModelSwitcher(Observable.Never<ViewModelBase>(), Observable.Never<ViewModelBase>(), new IObservable<ViewModelBase>[0])) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker;
            var focusManager = new FocusControlManager().AddTo(Disposables);
            ImageViewModels = model.ImageModels.ToReadOnlyReactiveCollection(m => new ImagingImmsImageViewModel(m, focusManager, broker, peakSpotTableService)).AddTo(Disposables);
            RoiCompareViewModels = new ReadOnlyObservableCollection<ImagingRoiCompareViewModel>(new ObservableCollection<ImagingRoiCompareViewModel>());
            ExportParameterCommand = new AsyncReactiveCommand().WithSubscribe(model.ParameterExporModel.ExportAsync).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<ImagingImmsImageViewModel> ImageViewModels { get; }
        public ReadOnlyObservableCollection<ImagingRoiCompareViewModel> RoiCompareViewModels { get; }
        public ImagingImmsImageViewModel? SelectedImageViewModel {
            get => _selectedImageViewModel;
            set => SetProperty(ref _selectedImageViewModel, value);
        }
        private ImagingImmsImageViewModel? _selectedImageViewModel;

        public ImagingRoiCompareViewModel? SelectedRoiCompareViewModel {
            get => _selectedRoiCompareViewModel;
            set => SetProperty(ref _selectedRoiCompareViewModel, value);
        }
        private ImagingRoiCompareViewModel? _selectedRoiCompareViewModel;

        public AsyncReactiveCommand ExportParameterCommand { get; }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            return Task.CompletedTask;
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ??= new DelegateCommand(ExportAnalysis);
        private DelegateCommand? _exportAnalysisResultCommand;

        private void ExportAnalysis() {
            var m = _model.CreateExportAnalysisModel();
            using (var vm = new AnalysisResultExportViewModel(m)) {
                _broker.Publish(vm);
            }
        }
    }
}
