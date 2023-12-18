using CompMs.App.Msdial.Model.Imms;
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
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsMethodViewModel : MethodViewModel
    {
        private readonly ImmsMethodModel model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControlManager;

        private ImmsMethodViewModel(ImmsMethodModel model, IReadOnlyReactiveProperty<IAnalysisResultViewModel> analysisViewModelAsObservable, IReadOnlyReactiveProperty<IAlignmentResultViewModel> alignmentViewModelAsObservable, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels, FocusControlManager focusControlmanager, IMessageBroker broker)
            : base(model, analysisViewModelAsObservable, alignmentViewModelAsObservable, chromatogramViewModels, massSpectrumViewModels) {

            this.model = model;
            _broker = broker;
            _focusControlManager = focusControlmanager.AddTo(Disposables);
            ExportParameterCommand = new AsyncReactiveCommand().WithSubscribe(model.ParameterExportModel.ExportAsync).AddTo(Disposables);
        }

        public AsyncReactiveCommand ExportParameterCommand { get; }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return Task.CompletedTask;
            }
            return model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ?? (_exportAnalysisResultCommand = new DelegateCommand(ExportAnalysis));
        private DelegateCommand _exportAnalysisResultCommand;

        private void ExportAnalysis() {
            using (var vm = new AnalysisResultExportViewModel(model.CreateExportAnalysisResult())) {
                _broker.Publish(vm);
            }
        }
        

        public DelegateCommand ExportAlignmentResultCommand => _exportAlignmentResultCommand ?? (_exportAlignmentResultCommand = new DelegateCommand(ExportAlignment));
        private DelegateCommand _exportAlignmentResultCommand;

        private void ExportAlignment() {
            using (var vm = new AlignmentResultExportViewModel(model.AlignmentResultExportModel, _broker)) {
                _broker.Publish(vm);
            }
        }

        public DelegateCommand ShowTicCommand => _showTicCommand ?? (_showTicCommand = new DelegateCommand(ShowTIC));
        private DelegateCommand _showTicCommand;

        private void ShowTIC() {
            var m = model.PrepareTIC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m, _broker);
            _broker.Publish(vm);
        } 

        public DelegateCommand ShowBpcCommand => _showBpcCommand ?? (_showBpcCommand = new DelegateCommand(ShowBPC));
        private DelegateCommand _showBpcCommand;

        private void ShowBPC() {
            var m = model.PrepareBPC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m, _broker);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ?? (_showTicBpcRepEIC = new DelegateCommand(ShowTicBpcRepEIC));
        private DelegateCommand _showTicBpcRepEIC;

        private void ShowTicBpcRepEIC() {
            var m = model.PrepareTicBpcRepEIC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m, _broker);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowEicCommand => _showEicCommand ?? (_showEicCommand = new DelegateCommand(ShowEIC));
        private DelegateCommand _showEicCommand;

        private void ShowEIC() {
            var m = model.PrepareEicSetting();
            if (m is null) {
                return;
            }
            using (var settingvm = new DisplayEicSettingViewModel(m)) {
                _broker.Publish(settingvm);
                if (!settingvm.DialogResult) {
                    return;
                }
            }
            var chromatograms = m.PrepareChromatograms();
            if (chromatograms is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(chromatograms, _broker);
            _broker.Publish(vm);
        }

        private static IReadOnlyReactiveProperty<ImmsAnalysisViewModel> ConvertToAnalysisViewModel(
            ImmsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker messageBroker,
            FocusControlManager focusControlManager) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<ImmsAnalysisViewModel> result;
            using (var subject = new Subject<ImmsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new ImmsAnalysisViewModel(m, peakSpotTableService, messageBroker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<ImmsAlignmentViewModel> ConvertToAlignmentViewModel(
            ImmsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker messageBroker,
            FocusControlManager focusControlManager) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<ImmsAlignmentViewModel> result;
            using (var subject = new Subject<ImmsAlignmentModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new ImmsAlignmentViewModel(m, peakSpotTableService, messageBroker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AlignmentModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<ImmsAnalysisViewModel> analysisAsObservable, IObservable<ImmsAlignmentViewModel> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<ImmsAnalysisViewModel> analysisAsObservable, IObservable<ImmsAlignmentViewModel> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = Observable.Return<ViewModelBase>(null); // analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }

        public static ImmsMethodViewModel Create(ImmsMethodModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker messageBroker) {
            var focusControlManager = new FocusControlManager();

            var analysisViewModelAsObservable = ConvertToAnalysisViewModel(model, peakSpotTableService, messageBroker, focusControlManager);
            var alignmentViewModelAsObservable = ConvertToAlignmentViewModel(model, peakSpotTableService, messageBroker, focusControlManager);
            var chromatogramViewSwitcher = PrepareChromatogramViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable);
            var massSpectrumViewSwitcher =  PrepareMassSpectrumViewModels(analysisViewModelAsObservable, alignmentViewModelAsObservable);
            return new ImmsMethodViewModel(model, analysisViewModelAsObservable, alignmentViewModelAsObservable, chromatogramViewSwitcher, massSpectrumViewSwitcher, focusControlManager, messageBroker);
        }
    }
}
