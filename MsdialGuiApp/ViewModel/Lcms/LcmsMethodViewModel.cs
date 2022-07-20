using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
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
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsMethodViewModel : MethodViewModel {
        private readonly LcmsMethodModel model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusManager;

        private LcmsMethodViewModel(
            LcmsMethodModel model,
            IReadOnlyReactiveProperty<LcmsAnalysisViewModel> analysisAsObservable,
            IReadOnlyReactiveProperty<LcmsAlignmentViewModel> alignmentAsObservable,
            IMessageBroker broker,
            FocusControlManager focusControlManager)
            : base(
                  model, analysisAsObservable, alignmentAsObservable,
                  PrepareChromatogramViewModels(analysisAsObservable, alignmentAsObservable),
                  PrepareMassSpectrumViewModels(analysisAsObservable, alignmentAsObservable)) {

            this.model = model;
            _broker = broker;
            _focusManager = focusControlManager.AddTo(Disposables);

            ShowExperimentSpectrumCommand = new ReactiveCommand().AddTo(Disposables);

            analysisAsObservable
                .Where(vm => vm != null)
                .Select(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm.ExperimentSpectrumViewModel, (a, b) => b))
                .Switch()
                .Subscribe(vm => broker.Publish(vm))
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilterModel).AddTo(Disposables);

            var _proteinGroupTableViewModel = (ProteinGroupTableViewModel)null; //new ProteinGroupTableViewModel();
            ShowProteinGroupTableCommand = new ReactiveCommand().AddTo(Disposables);
            ShowProteinGroupTableCommand.Subscribe(() => broker.Publish(_proteinGroupTableViewModel)).AddTo(Disposables);
        }

        public PeakFilterViewModel PeakFilterViewModel { get; }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
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

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(model.ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(model.ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

        public DelegateCommand<Window> ShowTicCommand => showTicCommand ?? (showTicCommand = new DelegateCommand<Window>(model.ShowTIC));
        private DelegateCommand<Window> showTicCommand;

        public DelegateCommand<Window> ShowBpcCommand => showBpcCommand ?? (showBpcCommand = new DelegateCommand<Window>(model.ShowBPC));
        private DelegateCommand<Window> showBpcCommand;

        public DelegateCommand<Window> ShowTicBpcRepEICCommand => showTicBpcRepEIC ?? (showTicBpcRepEIC = new DelegateCommand<Window>(model.ShowTicBpcRepEIC));
        private DelegateCommand<Window> showTicBpcRepEIC;

        public DelegateCommand<Window> ShowEicCommand => showEicCommand ?? (showEicCommand = new DelegateCommand<Window>(model.ShowEIC));
        private DelegateCommand<Window> showEicCommand;

        public ReactiveCommand ShowProteinGroupTableCommand { get; }

        public ReactiveCommand ShowExperimentSpectrumCommand { get; }

        public DelegateCommand<Window> ShowFragmentSearchSettingCommand => fragmentSearchSettingCommand ??
            (fragmentSearchSettingCommand = new DelegateCommand<Window>(FragmentSearchSettingMethod));
        private DelegateCommand<Window> fragmentSearchSettingCommand;

        public DelegateCommand<Window> ShowMassqlSearchSettingCommand => massqlSearchSettingCommand ??
            (massqlSearchSettingCommand= new DelegateCommand<Window>(MassqlSearchSettingMethod));
        private DelegateCommand<Window> massqlSearchSettingCommand;

        public DelegateCommand<Window> ShowMscleanrFilterSettingCommand => mscleanrFilterSettingCommand ??
            (mscleanrFilterSettingCommand = new DelegateCommand<Window>(MscleanrFilterSettingMethod));
        private DelegateCommand<Window> mscleanrFilterSettingCommand;

        private void FragmentSearchSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowFragmentSearchSettingView(obj, true);
            }
            else {
                model.ShowShowFragmentSearchSettingView(obj, false);
            }
        }

        private void MassqlSearchSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowMassqlSearchSettingView(obj, true);
            }
            else {
                model.ShowShowMassqlSearchSettingView(obj, false);
            }
        }

        private void MscleanrFilterSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowMscleanrFilterSettingView(obj, true);
            }
            else {
                MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
                //Console.WriteLine("Please select an item in Alignment navigator!!");
            }
        }

        public DelegateCommand GoToMsfinderCommand => goToMsfinderCommand ??  (goToMsfinderCommand = new DelegateCommand(GoToMsfinderMethod));
        private DelegateCommand goToMsfinderCommand;

        private void GoToMsfinderMethod() {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.GoToMsfinderMethod(true);
            }
            else {
                model.GoToMsfinderMethod(false);
            }
        }

        private static IReadOnlyReactiveProperty<LcmsAnalysisViewModel> ConvertToAnalysisViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            FocusControlManager focusManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            if (focusManager is null) {
                throw new ArgumentNullException(nameof(focusManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAnalysisViewModel> result;
            using (var subject = new Subject<LcmsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAnalysisViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, focusManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<LcmsAlignmentViewModel> ConvertToAlignmentViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAlignmentViewModel> result;
            using (var subject = new Subject<LcmsAlignmentModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, broker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AlignmentModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<LcmsAnalysisViewModel> analysisAsObservable, IObservable<LcmsAlignmentViewModel> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<LcmsAnalysisViewModel> analysisAsObservable, IObservable<LcmsAlignmentViewModel> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = Observable.Return<ViewModelBase>(null); // ms2 chrom
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }

        public static LcmsMethodViewModel Create(
                LcmsMethodModel model,
                IWindowService<CompoundSearchVM> compoundSearchService,
                IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
                IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
                IMessageBroker broker) {

            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = ConvertToAnalysisViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService, focusControlManager);
            var alignmentAsObservable = ConvertToAlignmentViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService, broker, focusControlManager);

            return new LcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, broker, focusControlManager);
        }
    }
}
