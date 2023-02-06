using CompMs.App.Msdial.Model.Lcimms;
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

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsMethodViewModel : MethodViewModel
    {
        private readonly LcimmsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControlManager;

        private LcimmsMethodViewModel(
            LcimmsMethodModel model,
            IReadOnlyReactiveProperty<IAnalysisResultViewModel> analysisViewModelAsObservable,
            IReadOnlyReactiveProperty<IAlignmentResultViewModel> alignmentViewModelAsObservable,
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
        }

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

        public DelegateCommand ExportAlignmentResultCommand => _exportAlignmentResultCommand ?? (_exportAlignmentResultCommand = new DelegateCommand(ExportAlignment));
        private DelegateCommand _exportAlignmentResultCommand;

        private void ExportAlignment() {
            using (var vm = new AlignmentResultExportViewModel(_model.AlignmentResultExportModel, _broker)) {
                _broker.Publish(vm);
            }
        }

        public DelegateCommand ShowTicCommand => _showTicCommand ?? (_showTicCommand = new DelegateCommand(ShowTIC));
        private DelegateCommand _showTicCommand;

        private void ShowTIC() {
            var model = _model.PrepareTIC();
            if (model is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(model);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowBpcCommand => _showBpcCommand ?? (_showBpcCommand = new DelegateCommand(ShowBPC));
        private DelegateCommand _showBpcCommand;

        private void ShowBPC() {
            var model = _model.PrepareBPC();
            if (model is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(model);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ?? (_showTicBpcRepEIC = new DelegateCommand(ShowTicBpcRepEIC));
        private DelegateCommand _showTicBpcRepEIC;

        private void ShowTicBpcRepEIC() {
            var model = _model.PrepareTicBpcRepEIC();
            if (model is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(model);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowEicCommand => _showEicCommand ?? (_showEicCommand = new DelegateCommand(ShowEIC));
        private DelegateCommand _showEicCommand;

        public void ShowEIC() {
            var model = _model.PrepareEicSetting();
            using (var settingvm = new DisplayEicSettingViewModel(model)){
                _broker.Publish(settingvm);
                if (!settingvm.DialogResult) {
                    return;
                }
            }
            var chromatograms = model.PrepareChromatograms();
            if (chromatograms is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(chromatograms);
            _broker.Publish(vm);
        }

        private static IReadOnlyReactiveProperty<LcimmsAnalysisViewModel> ConvertToAnalysisViewModel(
            LcimmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<LcimmsAnalysisViewModel> result;
            using (var subject = new Subject<LcimmsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcimmsAnalysisViewModel(m, compoundSearchService, peakSpotTableService, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<LcimmsAlignmentViewModel> ConvertToAlignmentViewModel(
            LcimmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager,
            IMessageBroker broker) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<LcimmsAlignmentViewModel> result;
            using (var subject = new Subject<LcimmsAlignmentModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcimmsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, focusControlManager, broker))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AlignmentModel);
                subject.OnCompleted();
            }
            return result;
        }

        public static LcimmsMethodViewModel Create(LcimmsMethodModel model, IWindowService<CompoundSearchVM> compoundSearchService, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisViewModelAsObservable = ConvertToAnalysisViewModel(model, compoundSearchService, peakSpotTableService, focusControlManager);
            var alignmentViewModelAsObservable = ConvertToAlignmentViewModel(model, compoundSearchService, peakSpotTableService, focusControlManager, broker);

            return new LcimmsMethodViewModel(
                model,
                analysisViewModelAsObservable,
                alignmentViewModelAsObservable,
                PrepareChromatogramViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable),
                PrepareMassSpectrumViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable),
                focusControlManager,
                broker);
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<LcimmsAnalysisViewModel> analysisAsObservable, IObservable<LcimmsAlignmentViewModel> alignmentAsObservable) {
            var eic = analysisAsObservable;
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModels);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModels);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<LcimmsAnalysisViewModel> analysisAsObservable, IObservable<LcimmsAlignmentViewModel> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }
    }
}
