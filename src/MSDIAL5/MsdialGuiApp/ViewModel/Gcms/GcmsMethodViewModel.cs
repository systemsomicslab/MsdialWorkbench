using CompMs.App.Msdial.Model.Gcms;
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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsMethodViewModel : MethodViewModel
    {
        private readonly GcmsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControl;

        public GcmsMethodViewModel(GcmsMethodModel model, ReadOnlyReactivePropertySlim<GcmsAnalysisViewModel?> analysisFileViewModel, ReadOnlyReactivePropertySlim<GcmsAlignmentViewModel?> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels, IMessageBroker broker, FocusControlManager focusControl)
            : base(model, analysisFileViewModel, alignmentFileViewModel, chromatogramViewModels, massSpectrumViewModels) {
            _model = model;
            _broker = broker;
            _focusControl = focusControl;
            Disposables.Add(analysisFileViewModel);
            Disposables.Add(alignmentFileViewModel);

            var batchMsfinder = model.InternalMsfinderSettingModel;
            var msfinderBatchSettingVM = new InternalMsfinderBatchSettingViewModel(model.MsfinderSettingParameter, batchMsfinder, broker).AddTo(Disposables);
            ShowMsfinderSettingViewCommand = new ReactiveCommand().WithSubscribe(() => _broker.Publish(msfinderBatchSettingVM)).AddTo(Disposables);
        }
        public InternalMsfinderBatchSettingViewModel InternalMsfinderBatchSettingVM { get; }

        public ReactiveCommand ShowMsfinderSettingViewCommand { get; }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File is null || alignmentFile.File == _model.AlignmentFile) {
                return Task.CompletedTask;
            }

            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == _model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ??= new DelegateCommand(ExportAnalysis);
        private DelegateCommand? _exportAnalysisResultCommand;

        private void ExportAnalysis() {
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

        public DelegateCommand ShowEicCommand => _showEicCommand ??= new DelegateCommand(ShowChromatograms());
        private DelegateCommand? _showEicCommand;

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ??= new DelegateCommand(ShowChromatograms(tic: true, bpc: true, highestEic: true));
        private DelegateCommand? _showTicBpcRepEIC;

        private Action ShowChromatograms(bool tic = false, bool bpc = false, bool highestEic = false) {
            void InnerShowChromatorams() {
                var m = _model.ShowChromatograms(tic, bpc, highestEic);
                if (m is null) {
                    return;
                }
                var vm = new CheckChromatogramsViewModel(m, _broker);
                _broker.Publish(vm);
            }
            return InnerShowChromatorams;
        }

        public static GcmsMethodViewModel Create(GcmsMethodModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = Observable.Create<GcmsAnalysisModel?>(observer => {
                observer.OnNext(model.SelectedAnalysisModel);
                return model.ObserveProperty(m => m.SelectedAnalysisModel, isPushCurrentValueAtFirst: false).Subscribe(observer);
            }).Where(m => m is not null)
            .Select(m => new GcmsAnalysisViewModel(m!, peakSpotTableService, focusControlManager, broker))
            .ToReadOnlyReactivePropertySlim();
            var alignmentAsObservable = Observable.Create<GcmsAlignmentModel?>(observer => {
                observer.OnNext(model.SelectedAlignmentModel);
                return model.ObserveProperty(m => m.SelectedAlignmentModel, isPushCurrentValueAtFirst: false).Subscribe(observer);
            }).Where(m => m is not null)
            .Select(m => new GcmsAlignmentViewModel(m!, focusControlManager, broker))
            .ToReadOnlyReactivePropertySlim();

            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            var chromatogramSwitcher = new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase?>[] { eic, bar, alignmentEic});
            var rawdec = analysisAsObservable.Select(m => m?.RawDecSpectrumsViewModel);
            var productscan = analysisAsObservable.Select(m => (m?.EiChromatogramsViewModel));
            var rawpurified = analysisAsObservable.Select(m => (m?.RawPurifiedSpectrumsViewModel));
            var repref = alignmentAsObservable.Select(m => m?.Ms2SpectrumViewModel);
            var massSpectrumSwitcher = new ViewModelSwitcher(rawdec, repref, rawdec, productscan, rawpurified, repref);
            return new GcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, chromatogramSwitcher, massSpectrumSwitcher, broker, focusControlManager);
        }
    }
}
