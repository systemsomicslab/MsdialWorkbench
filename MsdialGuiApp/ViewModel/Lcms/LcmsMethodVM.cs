using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsMethodVM : MethodViewModel {
        public LcmsMethodVM(
            LcmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService)
            : base(model,
                  ConvertToAnalysisViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService),
                  ConvertToAlignmentViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService)) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
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

            this.model = model;

            ShowExperimentSpectrumCommand = new ReactiveCommand().AddTo(Disposables);

            AnalysisViewModel //this.ObserveProperty(m => m.AnalysisVM)
                .OfType<AnalysisLcmsVM>()
                .Where(vm => vm != null)
                .Select(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm.ExperimentSpectrumViewModel, (a, b) => b))
                .Switch()
                .Subscribe(vm => MessageBroker.Default.Publish(vm))
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(this.model.PeakFilterModel).AddTo(Disposables);
        }

        private readonly LcmsMethodModel model;

        public PeakFilterViewModel PeakFilterViewModel { get; }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);
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

        public ReactiveCommand ShowExperimentSpectrumCommand { get; }

        public DelegateCommand<Window> ShowFragmentSearchSettingCommand => fragmentSearchSettingCommand ??
            (fragmentSearchSettingCommand = new DelegateCommand<Window>(FragmentSearchSettingMethod));
        private DelegateCommand<Window> fragmentSearchSettingCommand;

        private void FragmentSearchSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowFragmentSearchSettingView(obj, true);
            }
            else {
                model.ShowShowFragmentSearchSettingView(obj, false);
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

        private static IObservable<AnalysisLcmsVM> ConvertToAnalysisViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService) {
            return method.ObserveProperty(m => m.AnalysisModel)
                .Where(m => m != null)
                .Select(m => new AnalysisLcmsVM(m, compoundSearchService, peakSpotTableService, proteomicsTableService))
                .DisposePreviousValue();
        }

        private static IObservable<LcmsAlignmentViewModel> ConvertToAlignmentViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService) {
            return method.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new LcmsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService))
                .DisposePreviousValue();
        }
    }
}
