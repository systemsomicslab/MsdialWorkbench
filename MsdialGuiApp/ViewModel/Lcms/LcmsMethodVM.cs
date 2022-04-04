using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialLcMsApi.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Windows;
using CompMs.App.Msdial.ViewModel.Search;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsMethodVM : MethodViewModel {
        public LcmsMethodVM(
            LcmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService)
            : base(model) {
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

            Storage = model.Storage;

            AnalysisViewModel = model.ObserveProperty(m => m.AnalysisModel)
                .Where(m => m != null)
                .Select(m => new AnalysisLcmsVM(m, compoundSearchService, peakSpotTableService, proteomicsTableService))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AlignmentViewModel = model.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new LcmsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ShowExperimentSpectrumCommand = new ReactiveCommand().AddTo(Disposables);

            AnalysisViewModel //this.ObserveProperty(m => m.AnalysisVM)
                .Where(vm => vm != null)
                .Select(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm.ExperimentSpectrumViewModel, (a, b) => b))
                .Switch()
                .Subscribe(vm => MessageBroker.Default.Publish(vm))
                .AddTo(Disposables);
            PeakFilterViewModel = new PeakFilterViewModel(this.model.PeakFilterModel).AddTo(Disposables);
        }

        private readonly LcmsMethodModel model;
        private readonly IWindowService<ViewModel.CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisLcmsVM AnalysisVM => AnalysisViewModel.Value;
        // {
        //     get => analysisVM;
        //     set => SetProperty(ref analysisVM, value);
        // }
        // private AnalysisLcmsVM analysisVM;

        public ReadOnlyReactivePropertySlim<AnalysisLcmsVM> AnalysisViewModel { get; }

        public LcmsAlignmentViewModel AlignmentVM => AlignmentViewModel.Value;
        // {
        //     get => alignmentVM;
        //     set => SetProperty(ref alignmentVM, value);
        // }
        // private AlignmentLcmsVM alignmentVM;

        public ReadOnlyReactivePropertySlim<LcmsAlignmentViewModel> AlignmentViewModel { get; }

        public IMsdialDataStorage<MsdialLcmsParameter> Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private IMsdialDataStorage<MsdialLcmsParameter> storage;

        public PeakFilterViewModel PeakFilterViewModel { get; }

        public bool RefMatchedChecked {
            get => PeakFilterViewModel.RefMatched;
            set => PeakFilterViewModel.RefMatched = value;
        }
        public bool SuggestedChecked {
            get => PeakFilterViewModel.Suggested;
            set => PeakFilterViewModel.Suggested = value;
        }
        public bool UnknownChecked {
            get => PeakFilterViewModel.Unknown;
            set => PeakFilterViewModel.Unknown = value;
        }

        public bool Ms2AcquiredChecked {
            get => PeakFilterViewModel.Ms2Acquired;
            set => PeakFilterViewModel.Ms2Acquired = value;
        }
        public bool MolecularIonChecked {
            get => PeakFilterViewModel.MolecularIon;
            set => PeakFilterViewModel.MolecularIon = value;
        }
        public bool BlankFilterChecked {
            get => PeakFilterViewModel.Blank;
            set => PeakFilterViewModel.Blank = value;
        }
        public bool UniqueIonsChecked {
            get => PeakFilterViewModel.UniqueIons;
            set => PeakFilterViewModel.UniqueIons = value;
        }
        public bool ManuallyModifiedChecked {
            get => PeakFilterViewModel.ManuallyModified;
            set => PeakFilterViewModel.ManuallyModified = value;
        }

        public override int InitializeNewProject(Window window) {
            if (InitializeNewProjectCore(window) != 0) {
                return -1;
            }

            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();

            return 0;
        }

        private int InitializeNewProjectCore(Window window) {
            // Set analysis param
            if (!model.ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = Storage.Parameter.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!model.ProcessAnnotaion(window, Storage))
                    return -1;
            }

            // Run second process
            var param = Storage.Parameter;
            if (param.TargetOmics == TargetOmics.Proteomics) {
                if (!model.ProcessSeccondAnnotaion4ShotgunProteomics(window, Storage))
                    return -1;
            } 

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!model.ProcessAlignment(window, Storage))
                    return -1;
            }

            return 0;
        }

        public override void LoadProject() {
            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);

            // if (AnalysisVM != null) {
            //     AnalysisVM.Dispose();
            //     Disposables.Remove(AnalysisVM);
            // }
            // AnalysisVM = new AnalysisLcmsVM(
            //     model.AnalysisModel,
            //     compoundSearchService,
            //     peakSpotTableService, 
            //     proteomicsTableService) {
            //     DisplayFilters = displayFilters
            // }.AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);

            // if (AlignmentVM != null) {
            //     AlignmentVM?.Dispose();
            //     Disposables.Remove(AlignmentVM);
            // }
            // AlignmentVM = new AlignmentLcmsVM(
            //     model.AlignmentModel,
            //     compoundSearchService,
            //     peakSpotTableService,
            //     proteomicsTableService, parameter) {
            //     DisplayFilters = displayFilters
            // }.AddTo(Disposables);
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

        private void FragmentSearchSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowFragmentSearchSettingView(obj, true);
            }
            else {
                model.ShowShowFragmentSearchSettingView(obj, false);
            }
        }

        private DelegateCommand<Window> fragmentSearchSettingCommand;

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
    }
}
