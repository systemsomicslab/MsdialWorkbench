using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsMethodViewModel : MethodViewModel
    {
        private readonly LcimmsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControlManager;
        private readonly MolecularNetworkingExportSettingViewModel _molecularNetworkingExportSettingViewModel;
        private readonly MolecularNetworkingSendingToCytoscapeJsSettingViewModel _molecularNetworkingSendingToCytoscapeJsSettingViewModel;


        private LcimmsMethodViewModel(
            LcimmsMethodModel model,
            IReadOnlyReactiveProperty<IAnalysisResultViewModel?> analysisViewModelAsObservable,
            IReadOnlyReactiveProperty<IAlignmentResultViewModel?> alignmentViewModelAsObservable,
            ViewModelSwitcher chromatogramViewModels,
            ViewModelSwitcher massSpectrumViewModels,
            FocusControlManager focusControlManager,
            IMessageBroker broker)
            : base(model, analysisViewModelAsObservable, alignmentViewModelAsObservable, chromatogramViewModels, massSpectrumViewModels) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            _model = model;
            _broker = broker;
            _focusControlManager = focusControlManager.AddTo(Disposables);
            _molecularNetworkingExportSettingViewModel = new MolecularNetworkingExportSettingViewModel(_model.MolecularNetworkingSettingModel).AddTo(Disposables);
            _molecularNetworkingSendingToCytoscapeJsSettingViewModel = new MolecularNetworkingSendingToCytoscapeJsSettingViewModel(_model.MolecularNetworkingSettingModel).AddTo(Disposables);

            ExportParameterCommand = new AsyncReactiveCommand().WithSubscribe(model.ParameterExportModel.ExportAsync).AddTo(Disposables);

            var batchMsfinder = model.InternalMsfinderSettingModel;
            var msfinderBatchSettingVM = new InternalMsfinderBatchSettingViewModel(model.MsfinderSettingParameter, batchMsfinder, broker).AddTo(Disposables);
            ShowMsfinderSettingViewCommand = new ReactiveCommand().WithSubscribe(() => _broker.Publish(msfinderBatchSettingVM)).AddTo(Disposables);
        }

        public AsyncReactiveCommand ExportParameterCommand { get; }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File is null || analysisFile.File == _model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File is null || alignmentFile.File == _model.AlignmentFile) {
                return Task.CompletedTask;
            }
            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ??= new DelegateCommand(ExportAnalysis);
        private DelegateCommand? _exportAnalysisResultCommand;

        private void ExportAnalysis()
        {
            var m = _model.ExportAnalysis();
            using var vm = new AnalysisResultExportViewModel(m);
            _broker.Publish(vm);
        }


        public DelegateCommand ExportAlignmentResultCommand => _exportAlignmentResultCommand ??= new DelegateCommand(ExportAlignment);
        private DelegateCommand? _exportAlignmentResultCommand;

        private void ExportAlignment() {
            using var vm = new AlignmentResultExportViewModel(_model.AlignmentResultExportModel, _broker);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowTicCommand => _showTicCommand ??= new DelegateCommand(ShowChromatograms(tic: true));
        private DelegateCommand? _showTicCommand;

        public DelegateCommand ShowBpcCommand => _showBpcCommand ??= new DelegateCommand(ShowChromatograms(bpc: true));
        private DelegateCommand? _showBpcCommand;

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ??= new DelegateCommand(ShowChromatograms(tic: true, bpc: true, highestEic: true));
        private DelegateCommand? _showTicBpcRepEIC;

        public DelegateCommand ShowEicCommand => _showEicCommand ??= new DelegateCommand(ShowChromatograms());
        private DelegateCommand? _showEicCommand;

        private Action ShowChromatograms(bool tic = false, bool bpc = false, bool highestEic = false) {
            void InnerShowChromatograms() {
                var m = _model.PrepareChromatograms(tic, bpc, highestEic);
                if (m is null) {
                    return;
                }
                var vm = new CheckChromatogramsViewModel(m, _broker);
                _broker.Publish(vm);
            }
            return InnerShowChromatograms;
        }

        private static IReadOnlyReactiveProperty<LcimmsAnalysisViewModel?> ConvertToAnalysisViewModel(
            LcimmsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager,
            IMessageBroker broker) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            using var subject = new Subject<LcimmsAnalysisModel?>();
            var result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                .Select(m => m is null ? null : new LcimmsAnalysisViewModel(m, peakSpotTableService, focusControlManager, broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim();
            subject.OnNext(method.AnalysisModel);
            subject.OnCompleted();
            return result;
        }

        private static IReadOnlyReactiveProperty<LcimmsAlignmentViewModel?> ConvertToAlignmentViewModel(
            LcimmsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager,
            IMessageBroker broker) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            using var subject = new Subject<LcimmsAlignmentModel?>();
            var result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                .Select(m => m is null ? null : new LcimmsAlignmentViewModel(m, peakSpotTableService, focusControlManager, broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim();
            subject.OnNext(method.AlignmentModel);
            subject.OnCompleted();
            return result;
        }

        public static LcimmsMethodViewModel Create(LcimmsMethodModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisViewModelAsObservable = ConvertToAnalysisViewModel(model, peakSpotTableService, focusControlManager, broker);
            var alignmentViewModelAsObservable = ConvertToAlignmentViewModel(model, peakSpotTableService, focusControlManager, broker);

            return new LcimmsMethodViewModel(
                model,
                analysisViewModelAsObservable,
                alignmentViewModelAsObservable,
                PrepareChromatogramViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable),
                PrepareMassSpectrumViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable),
                focusControlManager,
                broker);
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<LcimmsAnalysisViewModel?> analysisAsObservable, IObservable<LcimmsAlignmentViewModel?> alignmentAsObservable) {
            var eic = analysisAsObservable;
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModels);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModels);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase?>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<LcimmsAnalysisViewModel?> analysisAsObservable, IObservable<LcimmsAlignmentViewModel?> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase?>[] { rawdec, ms2chrom, rawpur, repref});
        }

        public ReactiveCommand ShowMsfinderSettingViewCommand { get; }

        public DelegateCommand ShowMolecularNetworkingExportSettingCommand => _molecularNetworkingExportSettingCommand ??= new DelegateCommand(MolecularNetworkingExportSettingMethod);
        private DelegateCommand? _molecularNetworkingExportSettingCommand;

        private void MolecularNetworkingExportSettingMethod() {
            _broker.Publish(_molecularNetworkingExportSettingViewModel);
        }

        public DelegateCommand ShowMolecularNetworkingVisualizationSettingCommand => _molecularNetworkingVisualizationSettingCommand ??= new DelegateCommand(MolecularNetworkingVisualizationSettingMethod);
        private DelegateCommand? _molecularNetworkingVisualizationSettingCommand;

        private void MolecularNetworkingVisualizationSettingMethod() {
            _broker.Publish(_molecularNetworkingSendingToCytoscapeJsSettingViewModel);
        }

    }
}
